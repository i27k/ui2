using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using cHub.Shared.Base;
using cHub.Shared.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Logger = cHub.Shared.Utils.Logger;

namespace cHub.Services.Chat
{
    public sealed class DiscordBridgeService : Service
    {
        private const string ApiBase = "https://discord.com/api/v10";
        private readonly ConcurrentQueue<string> _outbound = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<DiscordInbound> _inbound = new ConcurrentQueue<DiscordInbound>();
        private volatile bool _shutdown;
        private volatile bool _workerStarted;
        private string _token;
        private string _channelId;
        private string _botId;
        private string _lastMessageId;
        private DiscordBridgeRuntime _runtime;
        public bool IsConfigured { get; private set; }
        public bool IsConnected { get; private set; }

        public override void Initialize()
        {
            _token = Environment.GetEnvironmentVariable("CHUB_DISCORD_BOT_TOKEN");
            _channelId = Environment.GetEnvironmentVariable("CHUB_DISCORD_CHANNEL_ID");
            IsConfigured = string.Equals(Environment.GetEnvironmentVariable("CHUB_DISCORD_ENABLED"), "true", StringComparison.OrdinalIgnoreCase) &&
                           !string.IsNullOrWhiteSpace(_token) && !string.IsNullOrWhiteSpace(_channelId);
            if (!IsConfigured)
            {
                Logger.Info("[DiscordBridge] Disabled or not configured.");
                IsInitialized = true;
                return;
            }
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            GameObject host = new GameObject("cHub.DiscordBridge");
            UnityEngine.Object.DontDestroyOnLoad(host);
            _runtime = host.AddComponent<DiscordBridgeRuntime>();
            _runtime.Service = this;
            IsInitialized = true;
            Logger.Info("[DiscordBridge] Configured. Waiting for authoritative server state.");
        }

        public override void Shutdown()
        {
            _shutdown = true;
            IsConnected = false;
            if (_runtime != null) UnityEngine.Object.Destroy(_runtime.gameObject);
            _token = null;
        }

        public void ForwardGameChat(string playerName, string message)
        {
            if (!IsConfigured || !_workerStarted || string.IsNullOrWhiteSpace(message) || message.StartsWith("/")) return;
            _outbound.Enqueue("**[GAME] " + Sanitize(playerName, 80) + ":** " + Sanitize(message, 1600));
        }

        internal void TickMainThread()
        {
            if (!IsConfigured || _shutdown || ConnectionManager.Instance == null || !ConnectionManager.Instance.IsServer) return;
            if (!_workerStarted)
            {
                _workerStarted = true;
                Task.Run((Action)WorkerLoop);
            }
            int handled = 0;
            while (handled++ < 12 && _inbound.TryDequeue(out DiscordInbound item))
            {
                if (string.Equals(item.Content.Trim(), "/online", StringComparison.OrdinalIgnoreCase))
                    _outbound.Enqueue(BuildOnlineResponse());
                else
                    BroadcastToGame("[7289DA][Discord][-] " + item.Author + ": " + item.Content);
            }
        }

        private void WorkerLoop()
        {
            int failures = 0;
            while (!_shutdown)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_botId)) Authenticate();
                    FlushOutbound();
                    PollInbound();
                    IsConnected = true;
                    failures = 0;
                    Thread.Sleep(2200);
                }
                catch (DiscordRateLimitException rate)
                {
                    IsConnected = false;
                    int wait = Math.Max(1000, Math.Min(120000, rate.RetryMilliseconds));
                    Logger.Warning("[DiscordBridge] Rate limited; retrying after " + wait + "ms.");
                    Thread.Sleep(wait);
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    failures = Math.Min(failures + 1, 6);
                    int wait = Math.Min(60000, 1000 * (1 << failures));
                    Logger.Warning("[DiscordBridge] Connection error: " + ex.Message + ". Retry in " + wait + "ms.");
                    Thread.Sleep(wait);
                }
            }
        }

        private void Authenticate()
        {
            JObject user = JObject.Parse(Request("GET", "/users/@me", null));
            _botId = (string)user["id"];
            if (string.IsNullOrWhiteSpace(_botId)) throw new InvalidOperationException("Bot identity missing.");
            Logger.Info("[DiscordBridge] Bot authenticated; channel bridge online.");
        }

        private void FlushOutbound()
        {
            int count = 0;
            while (count++ < 10 && _outbound.TryDequeue(out string content))
            {
                JObject body = new JObject { ["content"] = Sanitize(content, 1900), ["allowed_mentions"] = new JObject { ["parse"] = new JArray() } };
                try { Request("POST", "/channels/" + _channelId + "/messages", body.ToString(Formatting.None)); }
                catch { _outbound.Enqueue(content); throw; }
            }
        }

        private void PollInbound()
        {
            string query = string.IsNullOrWhiteSpace(_lastMessageId) ? "?limit=1" : "?limit=50&after=" + Uri.EscapeDataString(_lastMessageId);
            JArray messages = JArray.Parse(Request("GET", "/channels/" + _channelId + "/messages" + query, null));
            List<JObject> ordered = messages.OfType<JObject>().OrderBy(m => ParseSnowflake((string)m["id"])).ToList();
            if (string.IsNullOrWhiteSpace(_lastMessageId))
            {
                if (ordered.Count > 0) _lastMessageId = (string)ordered[ordered.Count - 1]["id"];
                return;
            }
            foreach (JObject message in ordered)
            {
                string id = (string)message["id"];
                if (!string.IsNullOrWhiteSpace(id)) _lastMessageId = id;
                JObject author = message["author"] as JObject;
                if (author == null || (bool?)author["bot"] == true || string.Equals((string)author["id"], _botId, StringComparison.Ordinal)) continue;
                string content = Sanitize((string)message["content"], 1600);
                if (string.IsNullOrWhiteSpace(content)) continue;
                _inbound.Enqueue(new DiscordInbound { Author = Sanitize((string)author["global_name"] ?? (string)author["username"], 80), Content = content });
            }
        }

        private string Request(string method, string route, string json)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiBase + route);
            request.Method = method;
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;
            request.Headers[HttpRequestHeader.Authorization] = "Bot " + _token;
            request.UserAgent = "DiscordBot (cHub, 0.0.3.0)";
            request.Accept = "application/json";
            if (json != null)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                request.ContentType = "application/json";
                request.ContentLength = bytes.Length;
                using (Stream stream = request.GetRequestStream()) stream.Write(bytes, 0, bytes.Length);
            }
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream())) return reader.ReadToEnd();
            }
            catch (WebException ex)
            {
                HttpWebResponse response = ex.Response as HttpWebResponse;
                string body = string.Empty;
                if (response != null)
                    using (response)
                    using (StreamReader reader = new StreamReader(response.GetResponseStream())) body = reader.ReadToEnd();
                if (response != null && (int)response.StatusCode == 429)
                {
                    int retry = 5000;
                    try { retry = (int)Math.Ceiling((double)JObject.Parse(body)["retry_after"] * 1000d); } catch { }
                    throw new DiscordRateLimitException(retry);
                }
                throw new InvalidOperationException("Discord HTTP " + (response == null ? "network failure" : ((int)response.StatusCode).ToString()));
            }
        }

        private static string BuildOnlineResponse()
        {
            List<string> names = new List<string>();
            if (ConnectionManager.Instance?.Clients?.List != null)
                names.AddRange(ConnectionManager.Instance.Clients.List.Where(c => c != null && !string.IsNullOrWhiteSpace(c.playerName)).Select(c => c.playerName));
            if (!GameManager.IsDedicatedServer && GameManager.Instance?.World != null)
                names.AddRange(GameManager.Instance.World.GetLocalPlayers().Where(p => p != null).Select(p => p.EntityName));
            names = names.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
            return names.Count == 0 ? "**c/Hub /online:** no players online." : "**c/Hub /online — " + names.Count + " player(s):**\n" + string.Join("\n", names.Select(n => "• " + Sanitize(n, 80)));
        }

        private static void BroadcastToGame(string message)
        {
            if (GameManager.Instance == null) return;
            if (!GameManager.IsDedicatedServer && GameManager.Instance.World != null)
                foreach (EntityPlayerLocal local in GameManager.Instance.World.GetLocalPlayers())
                    GameManager.Instance.ChatMessageClient(EChatType.Global, -1, message, new List<int> { local.entityId }, EMessageSender.Server, GeneratedTextManager.BbCodeSupportMode.Supported);
            if (ConnectionManager.Instance?.Clients?.List == null) return;
            foreach (ClientInfo client in ConnectionManager.Instance.Clients.List)
                client?.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Global, -1, message, null, EMessageSender.Server, GeneratedTextManager.BbCodeSupportMode.Supported));
        }

        private static string Sanitize(string value, int max)
        {
            string text = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("@everyone", "@ everyone").Replace("@here", "@ here").Trim();
            return text.Length <= max ? text : text.Substring(0, max);
        }

        private static ulong ParseSnowflake(string value) { return ulong.TryParse(value, out ulong id) ? id : 0UL; }
        private sealed class DiscordInbound { public string Author; public string Content; }
        private sealed class DiscordRateLimitException : Exception
        {
            public int RetryMilliseconds { get; }
            public DiscordRateLimitException(int retryMilliseconds) { RetryMilliseconds = retryMilliseconds; }
        }
    }

    internal sealed class DiscordBridgeRuntime : MonoBehaviour
    {
        public DiscordBridgeService Service;
        private void Update() { Service?.TickMainThread(); }
    }
}
