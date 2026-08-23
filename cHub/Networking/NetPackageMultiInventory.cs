using cHub.Modules.Inventory;

namespace cHub.Networking
{
    public class NetPackageMultiInventory : NetPackage
    {
        private byte action;
        private int index;
        private string text;
        private string playerId;
        private string response;

        public override NetPackageDirection PackageDirection => NetPackageDirection.Both;

        public NetPackageMultiInventory SetupRequest(byte requestedAction, int requestedIndex,
            string requestedText, string requestedPlayerId)
        {
            action = requestedAction; index = requestedIndex; text = requestedText ?? string.Empty;
            playerId = requestedPlayerId ?? string.Empty; response = string.Empty; return this;
        }

        private NetPackageMultiInventory SetupResponse(string json)
        { action = 255; response = json ?? string.Empty; return this; }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer); System.IO.BinaryWriter output = writer;
            output.Write(action); output.Write(index); output.Write(text ?? string.Empty);
            output.Write(playerId ?? string.Empty); output.Write(response ?? string.Empty);
        }

        public override void read(PooledBinaryReader reader)
        {
            System.IO.BinaryReader input = reader;
            action = input.ReadByte(); index = input.ReadInt32(); text = input.ReadString();
            playerId = input.ReadString(); response = input.ReadString();
        }

        public override void ProcessPackage(World world, GameManager callbacks)
        {
            bool server = ConnectionManager.Instance != null && ConnectionManager.Instance.IsServer;
            if (action == 255 && !server) { MultiInventoryClient.Receive(response); return; }
            if (!server || Sender == null) return;
            string json = MultiInventoryService.ProcessNetwork(Sender, world, playerId, action, index, text);
            Sender.SendPackage(NetPackageManager.GetPackage<NetPackageMultiInventory>().SetupResponse(json));
        }

        public override int GetLength() => 32768;
    }
}
