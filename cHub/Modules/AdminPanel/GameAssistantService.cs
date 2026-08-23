using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using cHub.Shared.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace cHub.Modules.AdminPanel
{
    public static class GameAssistantService
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(35) };
        private static readonly object RateSync = new object();
        private static readonly object ReviewSync = new object();
        private static readonly Dictionary<string, DateTime> LastRequests =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentQueue<AssistantReply> ClientReplies =
            new ConcurrentQueue<AssistantReply>();

        public const int MaxQuestionLength = 500;
        public static bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
        // Prefer the c/Hub-specific override, while also supporting OpenAI's
        // standard environment variable. Never read a key from mod JSON/XML.
        private static string ApiKey =>
            Environment.GetEnvironmentVariable("CHUB_OPENAI_API_KEY") ??
            Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        private static string Model => Environment.GetEnvironmentVariable("CHUB_OPENAI_MODEL") ?? "gpt-5.6-luna";

        public static bool TryBeginRequest(string playerId, out string error)
        {
            error = null;
            if (!IsConfigured)
            {
                error = "AI indisponibil: configureaza OPENAI_API_KEY pe server si reporneste jocul.";
                return false;
            }
            lock (RateSync)
            {
                DateTime now = DateTime.UtcNow;
                if (LastRequests.TryGetValue(playerId ?? string.Empty, out DateTime last) &&
                    (now - last).TotalSeconds < 8)
                {
                    error = "Asteapta cateva secunde inainte de urmatoarea intrebare.";
                    return false;
                }
                LastRequests[playerId ?? string.Empty] = now;
            }
            return true;
        }

        public static async Task<string> AskAsync(string playerId, string playerName, string question)
        {
            string trimmed = (question ?? string.Empty).Trim();
            if (trimmed.Length > MaxQuestionLength) trimmed = trimmed.Substring(0, MaxQuestionLength);
            StoreQuestionForReview(playerId, playerName, trimmed);
            try
            {
                var payload = new
                {
                    model = Model,
                    store = false,
                    safety_identifier = CreateSafetyIdentifier(playerId),
                    instructions = "You are c/Hub Game Assistant for 7 Days to Die. Answer in the same language as the player. " +
                        "Only answer questions about 7 Days to Die and installed c/Hub gameplay: items, inventory, crafting, quests, skills, enemies, survival, servers and what-if gameplay scenarios. " +
                        "Be concise, practical and honest about version-dependent or unknown facts. Never claim to have changed the game, inventory or server. " +
                        "If the request is unrelated to the game, politely redirect to game topics.",
                    input = "Player: " + (playerName ?? "Survivor") + "\nQuestion: " + trimmed,
                    max_output_tokens = 450,
                    text = new { verbosity = "low" }
                };
                using (var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses"))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                    request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    using (HttpResponseMessage response = await Http.SendAsync(request).ConfigureAwait(false))
                    {
                        string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                        {
                            Logger.Warning("[GameAssistant] OpenAI returned HTTP " + (int)response.StatusCode + ".");
                            return "Serviciul AI nu a putut raspunde momentan. Incearca din nou.";
                        }
                        return ExtractText(json);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("[GameAssistant] Request failed: " + ex.Message);
                return "Conexiunea cu asistentul a esuat. Verifica reteaua serverului si incearca din nou.";
            }
        }

        public static void EnqueueClientReply(string requestId, string answer)
        {
            ClientReplies.Enqueue(new AssistantReply { RequestId = requestId, Answer = answer ?? string.Empty });
        }

        public static bool TryDequeueReply(out AssistantReply reply) => ClientReplies.TryDequeue(out reply);

        private static string CreateSafetyIdentifier(string playerId)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes("chub:" + (playerId ?? "unknown")));
                return "chub_" + BitConverter.ToString(hash, 0, 12).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static void StoreQuestionForReview(string playerId, string playerName, string question)
        {
            if (string.IsNullOrWhiteSpace(question)) return;
            try
            {
                string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Mods", "cHub", "Data", "GameAssistantReview");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory,
                    "questions-" + DateTime.UtcNow.ToString("yyyy-MM") + ".jsonl");
                var record = new
                {
                    schema = 1,
                    timestampUtc = DateTime.UtcNow.ToString("o"),
                    player = string.IsNullOrWhiteSpace(playerName) ? "Survivor" : playerName.Trim(),
                    playerRef = CreateSafetyIdentifier(playerId),
                    question,
                    model = Model,
                    source = "cHub.GameAssistant"
                };
                string line = JsonConvert.SerializeObject(record, Formatting.None) + Environment.NewLine;
                lock (ReviewSync)
                {
                    File.AppendAllText(path, line, new UTF8Encoding(false));
                    UpdateReviewSummary(directory, CreateSafetyIdentifier(playerId));
                }
            }
            catch (Exception ex)
            {
                // Review logging must never prevent the player from receiving an answer.
                Logger.Warning("[GameAssistant] Could not store review question: " + ex.Message);
            }
        }

        private static void UpdateReviewSummary(string directory, string playerRef)
        {
            string path = Path.Combine(directory, "summary.json");
            AssistantReviewSummary summary = new AssistantReviewSummary();
            if (File.Exists(path))
            {
                try { summary = JsonConvert.DeserializeObject<AssistantReviewSummary>(File.ReadAllText(path)) ?? summary; }
                catch { summary = new AssistantReviewSummary(); }
            }
            string month = DateTime.UtcNow.ToString("yyyy-MM");
            if (!string.Equals(summary.CurrentMonth, month, StringComparison.Ordinal))
            { summary.CurrentMonth = month; summary.QuestionsThisMonth = 0; }
            summary.TotalQuestions++;
            summary.QuestionsThisMonth++;
            summary.LastQuestionUtc = DateTime.UtcNow.ToString("o");
            if (!summary.UniquePlayerRefs.Contains(playerRef)) summary.UniquePlayerRefs.Add(playerRef);
            File.WriteAllText(path, JsonConvert.SerializeObject(summary, Formatting.Indented), new UTF8Encoding(false));
        }

        private static string ExtractText(string json)
        {
            JObject root = JObject.Parse(json);
            string direct = (string)root["output_text"];
            if (!string.IsNullOrWhiteSpace(direct)) return direct.Trim();
            IEnumerable<string> texts = root["output"]?.Children()
                .SelectMany(item => item["content"]?.Children() ?? Enumerable.Empty<JToken>())
                .Select(content => (string)content["text"])
                .Where(text => !string.IsNullOrWhiteSpace(text)) ?? Enumerable.Empty<string>();
            string result = string.Join("\n", texts).Trim();
            return string.IsNullOrWhiteSpace(result) ? "Asistentul nu a furnizat un raspuns. Incearca sa reformulezi." : result;
        }
    }

    public sealed class AssistantReply
    {
        public string RequestId { get; set; }
        public string Answer { get; set; }
    }

    public sealed class AssistantReviewSummary
    {
        public long TotalQuestions { get; set; }
        public string CurrentMonth { get; set; } = string.Empty;
        public long QuestionsThisMonth { get; set; }
        public string LastQuestionUtc { get; set; } = string.Empty;
        public List<string> UniquePlayerRefs { get; set; } = new List<string>();
        public int UniquePlayers => UniquePlayerRefs.Count;
    }
}
