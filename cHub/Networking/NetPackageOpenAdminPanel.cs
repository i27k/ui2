using cHub.Modules.AdminPanel;
using cHub.Services.Players;
using cHub.Core;
using cHub.Services.Roles;
using PermissionIds = cHub.Shared.Constants.Permissions;
using cHub.Shared.Utils;

namespace cHub.Networking
{
    public class NetPackageOpenAdminPanel : NetPackage
    {
        public override NetPackageDirection PackageDirection =>
            NetPackageDirection.ToClient;

        public override void read(PooledBinaryReader reader)
        {
        }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
        }

        public override void ProcessPackage(World world, GameManager callbacks)
        {
            if (ConnectionManager.Instance != null &&
                ConnectionManager.Instance.IsServer)
            {
                return;
            }

            if (!AdminPanelService.TryOpenLocal(out string error))
            {
                Logger.Warning(
                    $"Admin Panel open request failed on the client: {error}");
            }
        }

        public override int GetLength()
        {
            return 0;
        }
    }

    public class NetPackageMagicTeleport : NetPackage
    {
        private static readonly System.Collections.Generic.Dictionary<string, PendingRequest> Pending =
            new System.Collections.Generic.Dictionary<string, PendingRequest>();
        private byte _action;
        private string _requestId;
        private string _requesterId;
        private string _requesterName;
        private string _targetId;
        private bool _accepted;

        public override NetPackageDirection PackageDirection => NetPackageDirection.Both;

        public NetPackageMagicTeleport SetupRequest(string requesterId, string requesterName, string targetId)
        {
            _action = 0; _requestId = System.Guid.NewGuid().ToString("N");
            _requesterId = requesterId; _requesterName = requesterName; _targetId = targetId;
            return this;
        }

        public NetPackageMagicTeleport SetupResponse(string requestId, bool accepted)
        {
            _action = 2; _requestId = requestId; _accepted = accepted;
            return this;
        }

        public NetPackageMagicTeleport SetupAdminTeleport(string administratorId,
            string sourcePlayerId, string targetPlayerId)
        {
            _action = 3;
            _requesterId = administratorId;
            _requesterName = sourcePlayerId;
            _targetId = targetPlayerId;
            return this;
        }

        public NetPackageMagicTeleport SetupZombieSpawn(string administratorId,
            string entityClassName, int count)
        {
            _action = 4;
            _requesterId = administratorId;
            _requesterName = entityClassName;
            _targetId = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return this;
        }

        private NetPackageMagicTeleport SetupNotification(string requestId, string requesterName)
        {
            _action = 1; _requestId = requestId; _requesterName = requesterName;
            return this;
        }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            System.IO.BinaryWriter output = writer;
            output.Write(_action);
            output.Write(string.IsNullOrEmpty(_requestId) ? string.Empty : _requestId);
            output.Write(string.IsNullOrEmpty(_requesterId) ? string.Empty : _requesterId);
            output.Write(string.IsNullOrEmpty(_requesterName) ? string.Empty : _requesterName);
            output.Write(string.IsNullOrEmpty(_targetId) ? string.Empty : _targetId);
            output.Write(_accepted);
        }

        public override void read(PooledBinaryReader reader)
        {
            System.IO.BinaryReader input = reader;
            _action = input.ReadByte(); _requestId = input.ReadString();
            _requesterId = input.ReadString(); _requesterName = input.ReadString();
            _targetId = input.ReadString(); _accepted = input.ReadBoolean();
        }

        public override void ProcessPackage(World world, GameManager callbacks)
        {
            bool server = ConnectionManager.Instance != null && ConnectionManager.Instance.IsServer;
            if (_action == 1 && !server)
            {
                AdminPanelService.ShowMagicTeleportRequest(_requestId, _requesterName);
                return;
            }
            if (!server || Sender == null) return;
            if (_action == 0)
            {
                if (!PlayerIdentityResolver.TryResolve(Sender, out string senderId) ||
                    !string.Equals(senderId, _requesterId, System.StringComparison.OrdinalIgnoreCase)) return;
                ClientInfo target = FindClient(_targetId);
                if (target == null || string.Equals(senderId, _targetId, System.StringComparison.OrdinalIgnoreCase)) return;
                Pending[_requestId] = new PendingRequest { Requester = Sender, Target = target, Created = System.DateTime.UtcNow };
                target.SendPackage(NetPackageManager.GetPackage<NetPackageMagicTeleport>()
                    .SetupNotification(_requestId, _requesterName));
            }
            else if (_action == 3)
            {
                if (!PlayerIdentityResolver.TryResolve(Sender, out string senderId) ||
                    !string.Equals(senderId, _requesterId, System.StringComparison.OrdinalIgnoreCase)) return;
                RoleService roles = ServiceRegistry.Get<RoleService>();
                if (roles == null || !roles.HasPermission(senderId, PermissionIds.TeleportPlayers)) return;
                ClientInfo sourceClient = FindClient(_requesterName);
                ClientInfo targetClient = FindClient(_targetId);
                if (sourceClient == null || targetClient == null || sourceClient == targetClient) return;
                if (world?.Players?.dict != null &&
                    world.Players.dict.TryGetValue(sourceClient.entityId, out EntityPlayer source) &&
                    world.Players.dict.TryGetValue(targetClient.entityId, out EntityPlayer destination))
                    source.Teleport(destination.position + new UnityEngine.Vector3(1.5f, 0f, 1.5f), 0f);
            }
            else if (_action == 4)
            {
                if (!PlayerIdentityResolver.TryResolve(Sender, out string senderId) ||
                    !string.Equals(senderId, _requesterId, System.StringComparison.OrdinalIgnoreCase)) return;
                RoleService roles = ServiceRegistry.Get<RoleService>();
                if (roles == null || !roles.HasPermission(senderId, PermissionIds.AdminPanelManage)) return;
                if (!int.TryParse(_targetId, out int count)) return;
                count = UnityEngine.Mathf.Clamp(count, 1, 25);
                int classId = EntityClass.GetId(_requesterName);
                if (classId < 0 || world?.Players?.dict == null ||
                    !world.Players.dict.TryGetValue(Sender.entityId, out EntityPlayer administrator)) return;
                for (int index = 0; index < count; index++)
                {
                    float angle = (360f / count) * index * UnityEngine.Mathf.Deg2Rad;
                    UnityEngine.Vector3 position = administrator.position +
                        new UnityEngine.Vector3(UnityEngine.Mathf.Cos(angle) * 12f, 1f,
                            UnityEngine.Mathf.Sin(angle) * 12f);
                    Entity zombie = EntityFactory.CreateEntity(classId, position);
                    if (zombie != null) world.SpawnEntityInWorld(zombie);
                }
            }
            else if (_action == 2 && Pending.TryGetValue(_requestId, out PendingRequest request))
            {
                Pending.Remove(_requestId);
                if (request.Target != Sender || (System.DateTime.UtcNow - request.Created).TotalSeconds > 30 || !_accepted) return;
                if (world?.Players?.dict != null &&
                    world.Players.dict.TryGetValue(request.Requester.entityId, out EntityPlayer requester) &&
                    world.Players.dict.TryGetValue(request.Target.entityId, out EntityPlayer target))
                    requester.Teleport(target.position + new UnityEngine.Vector3(1.5f, 0f, 1.5f), 0f);
            }
        }

        private static ClientInfo FindClient(string playerId)
        {
            if (ConnectionManager.Instance?.Clients?.List == null) return null;
            foreach (ClientInfo client in ConnectionManager.Instance.Clients.List)
                if (PlayerIdentityResolver.TryResolve(client, out string id) &&
                    string.Equals(id, playerId, System.StringComparison.OrdinalIgnoreCase)) return client;
            return null;
        }

        public override int GetLength() => 160;

        private class PendingRequest
        {
            public ClientInfo Requester; public ClientInfo Target; public System.DateTime Created;
        }
    }

    public class NetPackageRotData : NetPackage
    {
        private byte _action;
        private string _playerId;
        private string _payload;
        public override NetPackageDirection PackageDirection => NetPackageDirection.Both;

        public NetPackageRotData SetupRequest(string playerId)
        { _action = 0; _playerId = playerId ?? string.Empty; _payload = string.Empty; return this; }
        private NetPackageRotData SetupResponse(string payload)
        { _action = 1; _payload = payload ?? string.Empty; _playerId = string.Empty; return this; }
        public override void write(PooledBinaryWriter writer)
        { base.write(writer); ((System.IO.BinaryWriter)writer).Write(_action); ((System.IO.BinaryWriter)writer).Write(_playerId ?? string.Empty); ((System.IO.BinaryWriter)writer).Write(_payload ?? string.Empty); }
        public override void read(PooledBinaryReader reader)
        { _action = ((System.IO.BinaryReader)reader).ReadByte(); _playerId = ((System.IO.BinaryReader)reader).ReadString(); _payload = ((System.IO.BinaryReader)reader).ReadString(); }
        public override void ProcessPackage(World world, GameManager callbacks)
        {
            bool server = ConnectionManager.Instance != null && ConnectionManager.Instance.IsServer;
            if (_action == 0 && server && Sender != null)
            {
                if (!PlayerIdentityResolver.TryResolve(Sender, out string senderId) ||
                    !string.Equals(senderId, _playerId, System.StringComparison.OrdinalIgnoreCase)) return;
                Sender.SendPackage(NetPackageManager.GetPackage<NetPackageRotData>()
                    .SetupResponse(RotService.SerializeSnapshot(senderId)));
            }
            else if (_action == 1 && !server) AdminPanelService.ReceiveRotSnapshot(_payload);
        }
        public override int GetLength() => 8192;
    }

    public class NetPackageGameAssistant : NetPackage
    {
        private byte _action;
        private string _requestId;
        private string _playerId;
        private string _playerName;
        private string _text;

        public override NetPackageDirection PackageDirection => NetPackageDirection.Both;

        public NetPackageGameAssistant SetupRequest(string requestId, string playerId,
            string playerName, string question)
        {
            _action = 0; _requestId = requestId ?? string.Empty; _playerId = playerId ?? string.Empty;
            _playerName = playerName ?? string.Empty; _text = question ?? string.Empty; return this;
        }

        private NetPackageGameAssistant SetupResponse(string requestId, string answer)
        {
            _action = 1; _requestId = requestId ?? string.Empty; _text = answer ?? string.Empty;
            _playerId = string.Empty; _playerName = string.Empty; return this;
        }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer); System.IO.BinaryWriter output = writer;
            output.Write(_action); output.Write(_requestId ?? string.Empty);
            output.Write(_playerId ?? string.Empty); output.Write(_playerName ?? string.Empty);
            output.Write(_text ?? string.Empty);
        }

        public override void read(PooledBinaryReader reader)
        {
            System.IO.BinaryReader input = reader;
            _action = input.ReadByte(); _requestId = input.ReadString();
            _playerId = input.ReadString(); _playerName = input.ReadString(); _text = input.ReadString();
        }

        public override void ProcessPackage(World world, GameManager callbacks)
        {
            bool server = ConnectionManager.Instance != null && ConnectionManager.Instance.IsServer;
            if (_action == 1 && !server)
            {
                GameAssistantService.EnqueueClientReply(_requestId, _text);
                return;
            }
            if (_action != 0 || !server || Sender == null) return;
            if (!PlayerIdentityResolver.TryResolve(Sender, out string senderId) ||
                !string.Equals(senderId, _playerId, System.StringComparison.OrdinalIgnoreCase)) return;
            string question = (_text ?? string.Empty).Trim();
            if (question.Length == 0 || question.Length > GameAssistantService.MaxQuestionLength) return;
            ClientInfo recipient = Sender;
            if (!GameAssistantService.TryBeginRequest(senderId, out string error))
            {
                recipient.SendPackage(NetPackageManager.GetPackage<NetPackageGameAssistant>()
                    .SetupResponse(_requestId, error));
                return;
            }
            ProcessAsync(recipient, senderId, _playerName, question, _requestId);
        }

        private static async void ProcessAsync(ClientInfo recipient, string playerId,
            string playerName, string question, string requestId)
        {
            string answer = await GameAssistantService.AskAsync(playerId, playerName, question);
            try
            {
                recipient?.SendPackage(NetPackageManager.GetPackage<NetPackageGameAssistant>()
                    .SetupResponse(requestId, answer));
            }
            catch (System.Exception ex)
            {
                Logger.Warning("[GameAssistant] Could not deliver reply: " + ex.Message);
            }
        }

        public override int GetLength() => 16384;
    }
}
