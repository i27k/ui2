using System;
using System.IO;
using System.Linq;
using System.Text;
using cHub.Core;
using cHub.Services.Players;
using Newtonsoft.Json;

namespace cHub.Modules.Inventory
{
    public sealed class MultiInventoryState
    {
        public int ActiveIndex { get; set; }
        public string[] Names { get; set; }
        public bool[] Locked { get; set; }
        public string[] LootFilters { get; set; }
        public string Error { get; set; }
        public string SearchQuery { get; set; }
        public string SearchItems { get; set; }
        public int[] SearchInventories { get; set; }
        public int[] SearchSlots { get; set; }
        public int SearchPage { get; set; }
        public int SearchPages { get; set; }
        public int HighlightSlot { get; set; } = -1;
        public bool CursorUpdated { get; set; }
        public string CursorStack { get; set; }
    }

    internal sealed class MultiInventoryRecord
    {
        public int ActiveIndex;
        public string[] Names = new string[MultiInventoryService.InventoryCount];
        public bool[] Locked = new bool[MultiInventoryService.InventoryCount];
        public string[] LootFilters = new string[MultiInventoryService.InventoryCount];
        public ItemStack[][] Slots = new ItemStack[MultiInventoryService.InventoryCount][];
    }

    public static class MultiInventoryClient
    {
        public static MultiInventoryState State { get; private set; } = DefaultState();
        public static event Action Changed;
        public static event Action<int> LocateContextRequested;
        public static ItemStack[] SearchResults { get; private set; } = ItemStack.CreateArray(MultiInventoryService.SlotCount);
        private static XUiC_DragAndDropWindow pendingDragWindow;

        public static void Receive(string json)
        {
            try
            {
                State = JsonConvert.DeserializeObject<MultiInventoryState>(json) ?? DefaultState();
                SearchResults = DecodeStacks(State.SearchItems);
                if (State.CursorUpdated && pendingDragWindow != null)
                {
                    ItemStack[] cursor = DecodeStacks(State.CursorStack, 1);
                    pendingDragWindow.CurrentStack = cursor.Length > 0 ? cursor[0] : ItemStack.Empty;
                    pendingDragWindow = null;
                }
                XUiC_cHubGlobalSearchGrid.NotifyChanged();
                Changed?.Invoke();
            }
            catch (Exception ex)
            {
                StartupTerminal.Audit("MULTI_INVENTORY", "CLIENT_STATE", "FAILED", ex.Message);
            }
        }

        public static void Request(byte action, int index, string text = "")
        {
            EntityPlayerLocal local = GameManager.Instance?.World?.GetLocalPlayers()?.FirstOrDefault();
            if (local == null) return;

            bool server = ConnectionManager.Instance != null && ConnectionManager.Instance.IsServer;
            if (server)
            {
                Receive(MultiInventoryService.ProcessLocal(local, action, index, text));
                return;
            }

            string playerId = local.PersistentPlayerData?.PrimaryId?.CombinedString ?? string.Empty;
            ConnectionManager.Instance?.SendToServer(
                NetPackageManager.GetPackage<cHub.Networking.NetPackageMultiInventory>()
                    .SetupRequest(action, index, text, playerId), false);
        }

        public static void RequestTransfer(int targetIndex, XUiC_DragAndDropWindow dragWindow)
        {
            ItemStack cursor = dragWindow?.CurrentStack;
            if (cursor == null || cursor.IsEmpty()) return;
            EntityPlayerLocal local = GameManager.Instance?.World?.GetLocalPlayers()?.FirstOrDefault();
            if (local == null) return;

            pendingDragWindow = dragWindow;
            bool server = ConnectionManager.Instance != null && ConnectionManager.Instance.IsServer;
            if (server)
            {
                Receive(MultiInventoryService.ProcessLocalTransfer(local, targetIndex, cursor.Clone()));
                return;
            }

            string playerId = local.PersistentPlayerData?.PrimaryId?.CombinedString ?? string.Empty;
            ConnectionManager.Instance?.SendToServer(
                NetPackageManager.GetPackage<cHub.Networking.NetPackageMultiInventory>()
                    .SetupRequest(MultiInventoryService.TransferAction, targetIndex, string.Empty, playerId), false);
        }

        public static void RequestLocateContext(int resultIndex)
        {
            LocateContextRequested?.Invoke(resultIndex);
        }

        private static MultiInventoryState DefaultState()
        {
            return new MultiInventoryState
            {
                ActiveIndex = 0,
                Names = Enumerable.Range(1, MultiInventoryService.InventoryCount)
                    .Select(i => "Inventory " + i).ToArray(),
                Locked = new bool[MultiInventoryService.InventoryCount]
                , LootFilters = new string[MultiInventoryService.InventoryCount]
            };
        }

        private static ItemStack[] DecodeStacks(string encoded, int count = MultiInventoryService.SlotCount)
        {
            if (string.IsNullOrWhiteSpace(encoded)) return ItemStack.CreateArray(count);
            try
            {
                using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(encoded)))
                using (BinaryReader reader = new BinaryReader(stream))
                    return GameUtils.ReadItemStack(reader);
            }
            catch { return ItemStack.CreateArray(count); }
        }
    }

    public static class MultiInventoryService
    {
        public const int InventoryCount = 10;
        public const int SlotCount = 45;
        public const byte StateAction = 0;
        public const byte SwitchAction = 1;
        public const byte RenameAction = 2;
        public const byte LockAction = 3;
        public const byte SortAction = 4;
        public const byte SearchAction = 5;
        public const byte LocateAction = 6;
        public const byte TransferAction = 7;
        public const byte SetLootFilterAction = 8;
        public const byte RouteLootAction = 9;

        private static readonly object Sync = new object();

        private sealed class InventorySecurityException : Exception
        {
            public string Reason { get; private set; }

            public InventorySecurityException(string reason)
                : base(reason)
            {
                Reason = reason ?? "UNKNOWN_SECURITY_ERROR";
            }
        }
        public static string ProcessLocal(EntityPlayer player, byte action, int index, string text)
        {
            string id = player?.PersistentPlayerData?.PrimaryId?.CombinedString;
            return Process(player, null, id, action, index, text);
        }

        public static string ProcessLocalTransfer(EntityPlayer player, int index, ItemStack cursor)
        {
            string id = player?.PersistentPlayerData?.PrimaryId?.CombinedString;
            return Process(player, null, id, TransferAction, index, string.Empty, cursor);
        }

        public static string ProcessNetwork(ClientInfo sender, World world, string claimedPlayerId,
            byte action, int index, string text)
        {
            if (sender == null || world?.Players?.dict == null ||
                !PlayerIdentityResolver.TryResolve(sender, out string actualId) ||
                !string.Equals(actualId, claimedPlayerId, StringComparison.OrdinalIgnoreCase) ||
                !world.Players.dict.TryGetValue(sender.entityId, out EntityPlayer player))
                return SerializeError("Identitatea jucatorului nu a putut fi validata.");
            ItemStack cursor = action == TransferAction ? sender.latestPlayerData?.dragAndDropItem : null;
            string result = Process(player, sender, actualId, action, index, text, cursor);
            return result;
        }

        private static string Process(EntityPlayer player, ClientInfo sender, string playerId,
            byte action, int index, string text, ItemStack cursor = null)
        {
            if (player?.bag == null || string.IsNullOrWhiteSpace(playerId))
                return SerializeError("Inventarul jucatorului nu este disponibil.");

            lock (Sync)
            {
                try
                {
                    StartupTerminal.Audit(
                        "MULTI_INVENTORY", "PROCESS_STEP", "OK",
                        "1-BEFORE_LOAD player=" + playerId);

                    MultiInventoryRecord record = Load(playerId);

                    StartupTerminal.Audit(
                        "MULTI_INVENTORY", "PROCESS_STEP", "OK",
                        "2-AFTER_LOAD record=" + (record != null ? "OK" : "NULL"));

                    Normalize(record);

                    StartupTerminal.Audit(
                        "MULTI_INVENTORY", "PROCESS_STEP", "OK",
                        "3-AFTER_NORMALIZE");

                    index = Math.Max(0, Math.Min(InventoryCount - 1, index));

                    StartupTerminal.Audit(
                        "MULTI_INVENTORY", "PROCESS_STEP", "OK",
                        "4-BEFORE_ACTION action=" + action +
                        " index=" + index +
                        " active=" + record.ActiveIndex);

                    if (action == SwitchAction && index != record.ActiveIndex)
                    {
                        StartupTerminal.Audit(
                            "MULTI_INVENTORY", "SWITCH_STEP", "OK",
                            "1-BEGIN active=" + record.ActiveIndex + " target=" + index);

                        SaveActiveBag(record, player);

                        StartupTerminal.Audit(
                            "MULTI_INVENTORY", "SWITCH_STEP", "OK",
                            "2-SAVED_ACTIVE");

                        ItemStack[] target = CloneSlots(record.Slots[index]);

                        StartupTerminal.Audit(
                            "MULTI_INVENTORY", "SWITCH_STEP", "OK",
                            "3-CLONED_TARGET slots=" +
                            (target != null ? target.Length.ToString() : "NULL"));

                        player.bag.SetSlots(target);

                        StartupTerminal.Audit(
                            "MULTI_INVENTORY", "SWITCH_STEP", "OK",
                            "4-SET_SLOTS");

                        player.bag.onBackpackChanged();

                        StartupTerminal.Audit(
                            "MULTI_INVENTORY", "SWITCH_STEP", "OK",
                            "5-BACKPACK_CHANGED");

                        record.ActiveIndex = index;
                        Save(playerId, record);

                        StartupTerminal.Audit(
                            "MULTI_INVENTORY", "SWITCH_STEP", "OK",
                            "6-SAVED_RECORD");

                        SyncBag(player, sender);

                        StartupTerminal.Audit(
                            "MULTI_INVENTORY", "SWITCH", "OK",
                            "player=" + playerId + " target=" + (index + 1));
                    }
                    else if (action == RenameAction)
                    {
                        string name = SanitizeName(text);
                        if (name.Length > 0 && !record.Locked[index]) record.Names[index] = name;
                        Save(playerId, record);
                    }
                    else if (action == LockAction)
                    {
                        record.Locked[index] = !record.Locked[index];
                        Save(playerId, record);
                    }
                    else if (action == SortAction && !record.Locked[index])
                    {
                        if (index == record.ActiveIndex) SaveActiveBag(record, player);
                        record.Slots[index] = SortSlots(record.Slots[index]);
                        if (index == record.ActiveIndex)
                        {
                            player.bag.SetSlots(CloneSlots(record.Slots[index]));
                            player.bag.onBackpackChanged();
                            SyncBag(player, sender);
                        }
                        Save(playerId, record);
                    }
                    else if (action == TransferAction)
                    {
                        if (cursor == null || cursor.IsEmpty())
                            return SerializeError("Itemul din cursor nu este sincronizat inca. Incearca din nou.");
                        if (record.Locked[index])
                            return SerializeError("Inventarul tinta este blocat.");

                        if (index == record.ActiveIndex) SaveActiveBag(record, player);
                        ItemStack remaining = cursor.Clone();
                        int originalCount = remaining.count;
                        TransferIntoSlots(record.Slots[index], remaining);
                        if (index == record.ActiveIndex)
                        {
                            player.bag.SetSlots(CloneSlots(record.Slots[index]));
                            player.bag.onBackpackChanged();
                            SyncBag(player, sender);
                        }
                        Save(playerId, record);
                        if (sender?.latestPlayerData != null)
                            sender.latestPlayerData.dragAndDropItem = remaining.IsEmpty() ? ItemStack.Empty : remaining.Clone();

                        MultiInventoryState transferred = ToState(record);
                        transferred.CursorUpdated = true;
                        transferred.CursorStack = EncodeStacks(new[] { remaining.IsEmpty() ? ItemStack.Empty : remaining });
                        StartupTerminal.Audit("MULTI_INVENTORY", "DROP_TRANSFER", "OK",
                            "player=" + playerId + " target=" + (index + 1) +
                            " moved=" + (originalCount - Math.Max(0, remaining.count)) +
                            " remaining=" + Math.Max(0, remaining.count));
                        return JsonConvert.SerializeObject(transferred);
                    }

                    else if (action == SetLootFilterAction)
                    {
                        record.LootFilters[index] = SanitizeFilter(text);
                        Save(playerId, record);
                        StartupTerminal.Audit("MULTI_INVENTORY", "LOOT_FILTER", "OK",
                            "player=" + playerId + " target=" + (index + 1) +
                            " filter=" + (record.LootFilters[index].Length == 0 ? "OFF" : record.LootFilters[index]));
                    }
                    else if (action == RouteLootAction)
                    {
                        int moved = RouteFilteredLoot(record, player);
                        if (moved > 0)
                        {
                            Save(playerId, record);
                            player.bag.onBackpackChanged();
                            SyncBag(player, sender);
                            StartupTerminal.Audit("MULTI_INVENTORY", "LOOT_ROUTE", "OK",
                                "player=" + playerId + " stacks=" + moved);
                        }
                    }

                    else if (action == SearchAction)
                    {
                        return JsonConvert.SerializeObject(Search(record, player, text, index));
                    }
                    else if (action == LocateAction)
                    {
                        int slot;
                        if (!int.TryParse(text, out slot)) slot = -1;
                        if (index != record.ActiveIndex)
                        {
                            SaveActiveBag(record, player);
                            player.bag.SetSlots(CloneSlots(record.Slots[index]));
                            player.bag.onBackpackChanged();
                            record.ActiveIndex = index;
                            Save(playerId, record);
                            SyncBag(player, sender);
                        }
                        MultiInventoryState located = ToState(record);
                        located.HighlightSlot = slot;
                        return JsonConvert.SerializeObject(located);
                    }

                    return JsonConvert.SerializeObject(ToState(record));
                }
                catch (Exception ex)
                {
                    StartupTerminal.Audit("MULTI_INVENTORY", "PROCESS", "FAILED",
                        ex.GetType().Name + ": " + ex.Message);
                    return SerializeError(ex.Message);
                }
            }
        }

        private static void SaveActiveBag(MultiInventoryRecord record, EntityPlayer player)
        {
            record.Slots[record.ActiveIndex] = CloneSlots(player.bag.GetSlots());
        }

        private static void SyncBag(EntityPlayer player, ClientInfo sender)
        {
            if (sender != null)
                sender.SendPackage(NetPackageManager.GetPackage<NetPackageBag>()
                    .Setup(player.entityId, player.bag));
        }

        private static ItemStack[] SortSlots(ItemStack[] source)
        {
            ItemStack[] result = ItemStack.CreateArray(SlotCount);
            ItemStack[] used = (source ?? new ItemStack[0])
                .Where(s => s != null && !s.IsEmpty())
                .OrderBy(s => s.itemValue?.ItemClass?.GetLocalizedItemName() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(s => s.count).Select(s => s.Clone()).ToArray();
            for (int i = 0; i < used.Length && i < result.Length; i++) result[i] = used[i];
            return result;
        }

        private static void TransferIntoSlots(ItemStack[] slots, ItemStack remaining)
        {
            if (slots == null || remaining == null || remaining.IsEmpty()) return;
            for (int i = 0; i < slots.Length && remaining.count > 0; i++)
            {
                ItemStack target = slots[i];
                if (target == null || target.IsEmpty()) continue;
                int moved;
                if (!target.CanStackPartlyWith(remaining, out moved) || moved <= 0) continue;
                target.count += moved;
                remaining.count -= moved;
            }
            for (int i = 0; i < slots.Length && remaining.count > 0; i++)
            {
                if (slots[i] != null && !slots[i].IsEmpty()) continue;
                int maximum = remaining.itemValue?.ItemClass?.MaxCount ?? remaining.count;
                int moved = Math.Min(maximum, remaining.count);
                ItemStack placed = remaining.Clone();
                placed.count = moved;
                slots[i] = placed;
                remaining.count -= moved;
            }
            if (remaining.count <= 0) remaining.Clear();
        }

        private static int RouteFilteredLoot(MultiInventoryRecord record, EntityPlayer player)
        {
            SaveActiveBag(record, player);
            ItemStack[] active = record.Slots[record.ActiveIndex];
            int movedStacks = 0;
            for (int slot = 0; slot < active.Length; slot++)
            {
                ItemStack source = active[slot];
                if (source == null || source.IsEmpty()) continue;
                int target = FindFilteredTarget(record, source);
                if (target < 0 || target == record.ActiveIndex || record.Locked[target]) continue;
                ItemStack remaining = source.Clone();
                int before = remaining.count;
                TransferIntoSlots(record.Slots[target], remaining);
                int moved = before - Math.Max(0, remaining.count);
                if (moved <= 0) continue;
                active[slot] = remaining.IsEmpty() ? ItemStack.Empty : remaining;
                movedStacks++;
            }
            if (movedStacks > 0) player.bag.SetSlots(CloneSlots(active));
            return movedStacks;
        }

        private static int FindFilteredTarget(MultiInventoryRecord record, ItemStack stack)
        {
            string localized = stack.itemValue?.ItemClass?.GetLocalizedItemName() ?? string.Empty;
            string internalName = stack.itemValue?.ItemClass?.GetItemName() ?? string.Empty;
            for (int i = 0; i < InventoryCount; i++)
            {
                string filter = record.LootFilters?[i] ?? string.Empty;
                if (filter.Length == 0) continue;
                string[] entries = filter.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (entries.Any(entry =>
                {
                    string token = entry.Trim();
                    return token.Length > 0 &&
                        (localized.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         internalName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
                })) return i;
            }
            return -1;
        }

        private static MultiInventoryState Search(MultiInventoryRecord record, EntityPlayer player,
            string query, int requestedPage)
        {
            string[] terms = (query ?? string.Empty).Trim().ToLowerInvariant().Split(
                new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var matches = new System.Collections.Generic.List<Tuple<ItemStack, int, int>>();
            for (int inventory = 0; inventory < InventoryCount; inventory++)
            {
                ItemStack[] slots = inventory == record.ActiveIndex ? player.bag.GetSlots() : record.Slots[inventory];
                for (int slot = 0; slots != null && slot < slots.Length; slot++)
                {
                    ItemStack stack = slots[slot];
                    if (stack == null || stack.IsEmpty()) continue;
                    string name = stack.itemValue?.ItemClass?.GetLocalizedItemName()?.ToLowerInvariant() ?? string.Empty;
                    if (terms.All(term => name.Contains(term)))
                        matches.Add(Tuple.Create(stack.Clone(), inventory, slot));
                }
            }

            int pages = Math.Max(1, (matches.Count + SlotCount - 1) / SlotCount);
            int page = Math.Max(0, Math.Min(pages - 1, requestedPage));
            var pageItems = matches.Skip(page * SlotCount).Take(SlotCount).ToList();
            ItemStack[] result = ItemStack.CreateArray(SlotCount);
            int[] inventories = Enumerable.Repeat(-1, SlotCount).ToArray();
            int[] slotsResult = Enumerable.Repeat(-1, SlotCount).ToArray();
            for (int i = 0; i < pageItems.Count; i++)
            {
                result[i] = pageItems[i].Item1;
                inventories[i] = pageItems[i].Item2;
                slotsResult[i] = pageItems[i].Item3;
            }
            MultiInventoryState state = ToState(record);
            state.SearchQuery = query ?? string.Empty;
            state.SearchItems = EncodeStacks(result);
            state.SearchInventories = inventories;
            state.SearchSlots = slotsResult;
            state.SearchPage = page;
            state.SearchPages = pages;
            return state;
        }

        private static string EncodeStacks(ItemStack[] stacks)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
                    GameUtils.WriteItemStack(writer, stacks);
                return Convert.ToBase64String(stream.ToArray());
            }
        }
        private static void QuarantineInventoryFile(
    string path,
    string playerId,
    string reason)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                string quarantineRoot = Path.Combine(
                    RootPath,
                    "_quarantine");

                Directory.CreateDirectory(quarantineRoot);

                string worldIdentity = SafeFileName(GetWorldIdentity());
                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");

                string originalName = Path.GetFileNameWithoutExtension(path);

                string quarantineName =
                    timestamp + "_" +
                    worldIdentity + "_" +
                    originalName + ".bin";

                string quarantinePath = Path.Combine(
                    quarantineRoot,
                    quarantineName);

                // Extrem de improbabil, dar nu suprascriem niciodată
                // un fișier deja aflat în quarantine.
                if (File.Exists(quarantinePath))
                {
                    quarantinePath = Path.Combine(
                        quarantineRoot,
                        timestamp + "_" +
                        Guid.NewGuid().ToString("N") + "_" +
                        originalName + ".bin");
                }

                File.Move(path, quarantinePath);

                StartupTerminal.Audit(
                    "INVENTORY_SECURITY",
                    "QUARANTINE",
                    "OK",
                    "player=" + playerId +
                    " world=" + GetWorldIdentity() +
                    " reason=" + reason +
                    " source=" + path +
                    " quarantine=" + quarantinePath);
            }
            catch (Exception ex)
            {
                StartupTerminal.Audit(
                    "INVENTORY_SECURITY",
                    "QUARANTINE",
                    "FAILED",
                    "player=" + playerId +
                    " path=" + path +
                    " reason=" + reason +
                    " error=" + ex.GetType().Name +
                    ": " + ex.Message);

                throw;
            }
        }
        private static MultiInventoryRecord RecoverInvalidInventory(
    string path,
    string playerId,
    string reason)
        {
            StartupTerminal.Audit(
                "INVENTORY_SECURITY",
                "RECOVERY",
                "OK",
                "player=" + playerId +
                " world=" + GetWorldIdentity() +
                " reason=" + reason);

            QuarantineInventoryFile(
                path,
                playerId,
                reason);

            MultiInventoryRecord cleanRecord = CreateRecord();

            // IMPORTANT:
            // Save() îl generează direct ca CHUBINV4,
            // semnat cu HMAC și legat de world + player.
            Save(playerId, cleanRecord);

            StartupTerminal.Audit(
                "INVENTORY_SECURITY",
                "RECOVERY",
                "OK",
                "Clean CHUBINV4 generated." +
                " player=" + playerId +
                " world=" + GetWorldIdentity());

            return cleanRecord;
        }
        private static MultiInventoryRecord Load(string playerId)
        {
            string path = GetPath(playerId);

            StartupTerminal.Audit(
                "MULTI_INVENTORY",
                "LOAD_STEP",
                "OK",
                "1-PATH path=" + path +
                " exists=" + File.Exists(path));

            // =========================================================
            // NU EXISTĂ FIȘIER
            // =========================================================
            if (!File.Exists(path))
            {
                StartupTerminal.Audit(
                    "MULTI_INVENTORY",
                    "LOAD_STEP",
                    "OK",
                    "2-CREATE_NEW");

                return CreateRecord();
            }

            try
            {
                // =====================================================
                // IMPORTANT:
                // Orice InventorySecurityException aruncată din acest
                // bloc va ieși mai întâi din using.
                //
                // FileStream + BinaryReader vor fi Dispose() înainte
                // să ajungem în catch-ul de jos.
                // =====================================================

                using (FileStream stream = File.OpenRead(path))
                using (BinaryReader reader =
                       new BinaryReader(stream, Encoding.UTF8))
                {
                    StartupTerminal.Audit(
                        "MULTI_INVENTORY",
                        "LOAD_STEP",
                        "OK",
                        "3-OPEN length=" + stream.Length);

                    string version;

                    try
                    {
                        version = reader.ReadString();
                    }
                    catch (Exception ex)
                    {
                        throw new InventorySecurityException(
                            "HEADER_READ_FAILED type=" +
                            ex.GetType().Name);
                    }

                    StartupTerminal.Audit(
                        "MULTI_INVENTORY",
                        "LOAD_STEP",
                        "OK",
                        "4-VERSION version=" + version +
                        " pos=" + stream.Position);

                    // =================================================
                    // CHUBINV4
                    // =================================================
                    if (version == "CHUBINV4")
                    {
                        int payloadLength;

                        try
                        {
                            payloadLength = reader.ReadInt32();
                        }
                        catch (Exception ex)
                        {
                            throw new InventorySecurityException(
                                "PAYLOAD_LENGTH_READ_FAILED type=" +
                                ex.GetType().Name);
                        }

                        // ---------------------------------------------
                        // PAYLOAD LENGTH VALIDATION
                        // ---------------------------------------------
                        if (payloadLength <= 0)
                        {
                            throw new InventorySecurityException(
                                "INVALID_PAYLOAD_LENGTH value=" +
                                payloadLength);
                        }

                        long remainingFileBytes =
                            stream.Length - stream.Position;

                        // Trebuie să rămână minimum:
                        //
                        // payload
                        // +
                        // 4 bytes signatureLength
                        // +
                        // 32 bytes SHA256 HMAC
                        //
                        if (remainingFileBytes < 36 ||
                            payloadLength > remainingFileBytes - 36)
                        {
                            throw new InventorySecurityException(
                                "PAYLOAD_LENGTH_EXCEEDS_FILE payload=" +
                                payloadLength +
                                " remaining=" +
                                remainingFileBytes);
                        }

                        // ---------------------------------------------
                        // PAYLOAD
                        // ---------------------------------------------
                        byte[] payload =
                            reader.ReadBytes(payloadLength);

                        if (payload.Length != payloadLength)
                        {
                            throw new InventorySecurityException(
                                "PAYLOAD_INCOMPLETE expected=" +
                                payloadLength +
                                " actual=" +
                                payload.Length);
                        }

                        // ---------------------------------------------
                        // SIGNATURE LENGTH
                        // ---------------------------------------------
                        if (stream.Length - stream.Position < 4)
                        {
                            throw new InventorySecurityException(
                                "SIGNATURE_LENGTH_MISSING");
                        }

                        int signatureLength =
                            reader.ReadInt32();

                        if (signatureLength != 32)
                        {
                            throw new InventorySecurityException(
                                "INVALID_SIGNATURE_LENGTH value=" +
                                signatureLength);
                        }

                        // ---------------------------------------------
                        // SIGNATURE
                        // ---------------------------------------------
                        byte[] signature =
                            reader.ReadBytes(signatureLength);

                        if (signature.Length != signatureLength)
                        {
                            throw new InventorySecurityException(
                                "SIGNATURE_INCOMPLETE expected=" +
                                signatureLength +
                                " actual=" +
                                signature.Length);
                        }

                        // ---------------------------------------------
                        // NU ACCEPTĂM BYTES EXTRA
                        // ---------------------------------------------
                        if (stream.Position != stream.Length)
                        {
                            throw new InventorySecurityException(
                                "UNEXPECTED_TRAILING_DATA bytes=" +
                                (stream.Length - stream.Position));
                        }

                        // =============================================
                        // HMAC VERIFICATION
                        // =============================================
                        if (!InventorySecurity.Verify(
                                payload,
                                signature))
                        {
                            StartupTerminal.Audit(
                                "INVENTORY_SECURITY",
                                "HMAC_VERIFY",
                                "FAILED",
                                "player=" + playerId +
                                " world=" +
                                GetWorldIdentity());

                            throw new InventorySecurityException(
                                "HMAC_INVALID");
                        }

                        StartupTerminal.Audit(
                            "INVENTORY_SECURITY",
                            "HMAC_VERIFY",
                            "OK",
                            "player=" + playerId);

                        // =============================================
                        // HMAC VALID.
                        // ACUM putem interpreta payload-ul.
                        // =============================================
                        using (MemoryStream payloadStream =
                               new MemoryStream(payload))
                        using (BinaryReader payloadReader =
                               new BinaryReader(
                                   payloadStream,
                                   Encoding.UTF8))
                        {
                            string storedWorld;
                            string storedPlayer;

                            try
                            {
                                storedWorld =
                                    payloadReader.ReadString();

                                storedPlayer =
                                    payloadReader.ReadString();
                            }
                            catch (Exception ex)
                            {
                                throw new InventorySecurityException(
                                    "IDENTITY_READ_FAILED type=" +
                                    ex.GetType().Name);
                            }

                            string currentWorld =
                                GetWorldIdentity();

                            // =========================================
                            // WORLD BINDING
                            // =========================================
                            if (!string.Equals(
                                    storedWorld,
                                    currentWorld,
                                    StringComparison.Ordinal))
                            {
                                StartupTerminal.Audit(
                                    "INVENTORY_SECURITY",
                                    "WORLD_BINDING",
                                    "FAILED",
                                    "stored=" + storedWorld +
                                    " current=" + currentWorld +
                                    " player=" + playerId);

                                throw new InventorySecurityException(
                                    "WORLD_MISMATCH stored=" +
                                    storedWorld +
                                    " current=" +
                                    currentWorld);
                            }

                            StartupTerminal.Audit(
                                "INVENTORY_SECURITY",
                                "WORLD_BINDING",
                                "OK",
                                "world=" + currentWorld +
                                " player=" + playerId);

                            // =========================================
                            // PLAYER BINDING
                            // =========================================
                            if (!string.Equals(
                                    storedPlayer,
                                    playerId,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                StartupTerminal.Audit(
                                    "INVENTORY_SECURITY",
                                    "PLAYER_BINDING",
                                    "FAILED",
                                    "stored=" + storedPlayer +
                                    " requested=" + playerId);

                                throw new InventorySecurityException(
                                    "PLAYER_MISMATCH stored=" +
                                    storedPlayer +
                                    " requested=" +
                                    playerId);
                            }

                            StartupTerminal.Audit(
                                "INVENTORY_SECURITY",
                                "PLAYER_BINDING",
                                "OK",
                                "player=" + playerId);

                            // =========================================
                            // ACTIVE INVENTORY
                            // =========================================
                            int activeIndex;

                            try
                            {
                                activeIndex =
                                    payloadReader.ReadInt32();
                            }
                            catch (Exception ex)
                            {
                                throw new InventorySecurityException(
                                    "ACTIVE_INDEX_READ_FAILED type=" +
                                    ex.GetType().Name);
                            }

                            if (activeIndex < 0 ||
                                activeIndex >= InventoryCount)
                            {
                                throw new InventorySecurityException(
                                    "ACTIVE_INDEX_INVALID value=" +
                                    activeIndex);
                            }

                            StartupTerminal.Audit(
                                "MULTI_INVENTORY",
                                "LOAD_STEP",
                                "OK",
                                "5-ACTIVE active=" +
                                activeIndex +
                                " version=CHUBINV4");

                            MultiInventoryRecord record =
                                new MultiInventoryRecord
                                {
                                    ActiveIndex = activeIndex
                                };

                            // =========================================
                            // INVENTORIES
                            // =========================================
                            for (int i = 0;
                                 i < InventoryCount;
                                 i++)
                            {
                                try
                                {
                                    record.Names[i] =
                                        payloadReader.ReadString();

                                    record.Locked[i] =
                                        payloadReader.ReadBoolean();

                                    record.LootFilters[i] =
                                        payloadReader.ReadString();
                                }
                                catch (Exception ex)
                                {
                                    throw new InventorySecurityException(
                                        "INVENTORY_METADATA_READ_FAILED inventory=" +
                                        (i + 1) +
                                        " type=" +
                                        ex.GetType().Name);
                                }

                                StartupTerminal.Audit(
                                    "MULTI_INVENTORY",
                                    "LOAD_SLOT",
                                    "OK",
                                    "V4 BEFORE_ITEMS inventory=" +
                                    (i + 1) +
                                    " pos=" +
                                    payloadStream.Position +
                                    "/" +
                                    payloadStream.Length);

                                try
                                {
                                    record.Slots[i] =
                                        GameUtils.ReadItemStack(
                                            payloadReader);
                                }
                                catch (Exception ex)
                                {
                                    StartupTerminal.Audit(
                                        "MULTI_INVENTORY",
                                        "READ_ITEMS_TEST",
                                        "FAILED",
                                        "version=V4 inventory=" +
                                        (i + 1) +
                                        " pos=" +
                                        payloadStream.Position +
                                        " type=" +
                                        ex.GetType().FullName +
                                        " message=" +
                                        ex.Message);

                                    throw new InventorySecurityException(
                                        "ITEM_DATA_READ_FAILED inventory=" +
                                        (i + 1) +
                                        " type=" +
                                        ex.GetType().Name);
                                }

                                StartupTerminal.Audit(
                                    "MULTI_INVENTORY",
                                    "READ_ITEMS_TEST",
                                    "OK",
                                    "version=V4 inventory=" +
                                    (i + 1) +
                                    " slots=" +
                                    (record.Slots[i] != null
                                        ? record.Slots[i].Length.ToString()
                                        : "NULL") +
                                    " pos=" +
                                    payloadStream.Position);
                            }

                            // =========================================
                            // PAYLOAD TREBUIE CONSUMAT COMPLET
                            // =========================================
                            if (payloadStream.Position !=
                                payloadStream.Length)
                            {
                                throw new InventorySecurityException(
                                    "PAYLOAD_TRAILING_DATA bytes=" +
                                    (payloadStream.Length -
                                     payloadStream.Position));
                            }

                            StartupTerminal.Audit(
                                "MULTI_INVENTORY",
                                "LOAD_STEP",
                                "OK",
                                "6-COMPLETE version=CHUBINV4" +
                                " player=" + playerId +
                                " world=" + currentWorld);

                            return record;
                        }
                    }

                    // =================================================
                    // LEGACY CHUBINV2 / CHUBINV3
                    // =================================================
                    if (version != "CHUBINV2" &&
                        version != "CHUBINV3")
                    {
                        throw new InventorySecurityException(
                            "UNKNOWN_VERSION version=" +
                            version);
                    }

                    StartupTerminal.Audit(
                        "INVENTORY_SECURITY",
                        "LEGACY_LOAD",
                        "OK",
                        "version=" + version +
                        " player=" + playerId +
                        " unsigned=true");

                    int legacyActiveIndex;

                    try
                    {
                        legacyActiveIndex =
                            reader.ReadInt32();
                    }
                    catch (Exception ex)
                    {
                        throw new InventorySecurityException(
                            "LEGACY_ACTIVE_INDEX_READ_FAILED type=" +
                            ex.GetType().Name);
                    }

                    MultiInventoryRecord legacyRecord =
                        new MultiInventoryRecord
                        {
                            ActiveIndex =
                                legacyActiveIndex
                        };

                    StartupTerminal.Audit(
                        "MULTI_INVENTORY",
                        "LOAD_STEP",
                        "OK",
                        "5-ACTIVE active=" +
                        legacyActiveIndex +
                        " version=" +
                        version);

                    for (int i = 0;
                         i < InventoryCount;
                         i++)
                    {
                        StartupTerminal.Audit(
                            "MULTI_INVENTORY",
                            "LOAD_SLOT",
                            "OK",
                            "BEGIN inventory=" +
                            (i + 1) +
                            " pos=" +
                            stream.Position +
                            "/" +
                            stream.Length);

                        try
                        {
                            legacyRecord.Names[i] =
                                reader.ReadString();

                            legacyRecord.Locked[i] =
                                reader.ReadBoolean();

                            if (version == "CHUBINV3")
                            {
                                legacyRecord.LootFilters[i] =
                                    reader.ReadString();
                            }
                            else
                            {
                                legacyRecord.LootFilters[i] =
                                    string.Empty;
                            }

                            legacyRecord.Slots[i] =
                                GameUtils.ReadItemStack(reader);
                        }
                        catch (Exception ex)
                        {
                            StartupTerminal.Audit(
                                "MULTI_INVENTORY",
                                "READ_ITEMS_TEST",
                                "FAILED",
                                "version=" +
                                version +
                                " inventory=" +
                                (i + 1) +
                                " pos=" +
                                stream.Position +
                                " type=" +
                                ex.GetType().FullName +
                                " message=" +
                                ex.Message);

                            throw new InventorySecurityException(
                                "LEGACY_DATA_READ_FAILED version=" +
                                version +
                                " inventory=" +
                                (i + 1) +
                                " type=" +
                                ex.GetType().Name);
                        }

                        StartupTerminal.Audit(
                            "MULTI_INVENTORY",
                            "READ_ITEMS_TEST",
                            "OK",
                            "version=" +
                            version +
                            " inventory=" +
                            (i + 1) +
                            " slots=" +
                            (legacyRecord.Slots[i] != null
                                ? legacyRecord.Slots[i].Length.ToString()
                                : "NULL") +
                            " pos=" +
                            stream.Position);
                    }

                    StartupTerminal.Audit(
                        "MULTI_INVENTORY",
                        "LOAD_STEP",
                        "OK",
                        "6-COMPLETE version=" +
                        version +
                        " pos=" +
                        stream.Position +
                        "/" +
                        stream.Length);

                    return legacyRecord;
                }
            }

            // =========================================================
            // SECURITY FAILURE
            //
            // Când ajungem aici, using-urile de mai sus AU FĂCUT
            // Dispose deja.
            //
            // Deci .bin-ul nu mai este ținut deschis de Load().
            // =========================================================
            catch (InventorySecurityException ex)
            {
                StartupTerminal.Audit(
                    "INVENTORY_SECURITY",
                    "RECOVERY_BEGIN",
                    "OK",
                    "player=" + playerId +
                    " world=" +
                    GetWorldIdentity() +
                    " reason=" +
                    ex.Reason);

                return RecoverInvalidInventory(
                    path,
                    playerId,
                    ex.Reason);
            }

            // =========================================================
            // ORICE ALTĂ EROARE DE CITIRE
            //
            // O tratăm tot ca fișier corupt și îl păstrăm în quarantine.
            // =========================================================
            catch (Exception ex)
            {
                string reason =
                    "UNEXPECTED_READ_ERROR type=" +
                    ex.GetType().Name +
                    " message=" +
                    ex.Message;

                StartupTerminal.Audit(
                    "INVENTORY_SECURITY",
                    "RECOVERY_BEGIN",
                    "FAILED",
                    "player=" + playerId +
                    " world=" +
                    GetWorldIdentity() +
                    " reason=" +
                    reason);

                return RecoverInvalidInventory(
                    path,
                    playerId,
                    reason);
            }
        }
        

        private static void Save(string playerId, MultiInventoryRecord record)
        {
            string path = GetPath(playerId);

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string temporary = path + ".tmp";
            string worldIdentity = GetWorldIdentity();

            byte[] payload;

            // Construim payload-ul care va fi protejat de HMAC.
            using (MemoryStream payloadStream = new MemoryStream())
            {
                using (BinaryWriter payloadWriter =
                       new BinaryWriter(payloadStream, Encoding.UTF8, true))
                {
                    // Leagă fișierul de lumea și jucătorul curent.
                    payloadWriter.Write(worldIdentity);
                    payloadWriter.Write(playerId ?? string.Empty);

                    payloadWriter.Write(record.ActiveIndex);

                    for (int i = 0; i < InventoryCount; i++)
                    {
                        payloadWriter.Write(
                            record.Names[i] ?? ("Inventory " + (i + 1)));

                        payloadWriter.Write(record.Locked[i]);

                        payloadWriter.Write(
                            record.LootFilters[i] ?? string.Empty);

                        GameUtils.WriteItemStack(
                            payloadWriter,
                            record.Slots[i]);
                    }
                }

                payload = payloadStream.ToArray();
            }

            // Semnătura este calculată server-side.
            byte[] signature = InventorySecurity.Sign(payload);

            using (FileStream stream = File.Create(temporary))
            using (BinaryWriter writer =
                   new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write("CHUBINV4");

                writer.Write(payload.Length);
                writer.Write(payload);

                writer.Write(signature.Length);
                writer.Write(signature);
            }

            if (File.Exists(path))
                File.Delete(path);

            File.Move(temporary, path);

            StartupTerminal.Audit(
                "MULTI_INVENTORY",
                "SAVE_V4",
                "OK",
                "world=" + worldIdentity +
                " player=" + playerId +
                " payloadBytes=" + payload.Length +
                " hmacBytes=" + signature.Length);
        }

        private static MultiInventoryRecord CreateRecord()
        {
            MultiInventoryRecord record = new MultiInventoryRecord();
            for (int i = 0; i < InventoryCount; i++)
            {
                record.Names[i] = "Inventory " + (i + 1);
                record.Slots[i] = ItemStack.CreateArray(SlotCount);
            }
            return record;
        }

        private static void Normalize(MultiInventoryRecord record)
        {
            if (record.Names == null || record.Names.Length != InventoryCount)
                record.Names = Enumerable.Range(1, InventoryCount).Select(i => "Inventory " + i).ToArray();
            if (record.Locked == null || record.Locked.Length != InventoryCount)
                record.Locked = new bool[InventoryCount];
            if (record.LootFilters == null || record.LootFilters.Length != InventoryCount)
                record.LootFilters = new string[InventoryCount];
            if (record.Slots == null || record.Slots.Length != InventoryCount)
                record.Slots = new ItemStack[InventoryCount][];
            for (int i = 0; i < InventoryCount; i++)
            {
                if (string.IsNullOrWhiteSpace(record.Names[i])) record.Names[i] = "Inventory " + (i + 1);
                record.LootFilters[i] = SanitizeFilter(record.LootFilters[i]);
                if (record.Slots[i] == null || record.Slots[i].Length != SlotCount)
                    record.Slots[i] = ResizeSlots(record.Slots[i]);
            }
            record.ActiveIndex = Math.Max(0, Math.Min(InventoryCount - 1, record.ActiveIndex));
        }

        private static ItemStack[] ResizeSlots(ItemStack[] source)
        {
            ItemStack[] result = ItemStack.CreateArray(SlotCount);
            if (source != null)
                for (int i = 0; i < source.Length && i < result.Length; i++)
                    result[i] = source[i]?.Clone() ?? ItemStack.Empty;
            return result;
        }

        private static ItemStack[] CloneSlots(ItemStack[] source) => ResizeSlots(source);
        private static string SanitizeName(string value)
        {
            string name = (value ?? string.Empty).Trim();
            return name.Length > 24 ? name.Substring(0, 24) : name;
        }
        private static string SanitizeFilter(string value)
        {
            string filter = (value ?? string.Empty).Trim();
            return filter.Length > 240 ? filter.Substring(0, 240) : filter;
        }

        private static string RootPath => Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "Mods",
    "cHub",
    "Data",
    "inventories");
        private static string GetPath(string playerId)
        {
            string gameName = GamePrefs.GetString(EnumGamePrefs.GameName);
            string gameWorld = GamePrefs.GetString(EnumGamePrefs.GameWorld);

            if (string.IsNullOrWhiteSpace(gameName))
                gameName = "UnknownGame";

            if (string.IsNullOrWhiteSpace(gameWorld))
                gameWorld = "UnknownWorld";

            string safeGameName = SafeFileName(gameName);
            string safeGameWorld = SafeFileName(gameWorld);

            string safePlayerId = Convert.ToBase64String(Encoding.UTF8.GetBytes(playerId))
                .Replace('/', '_')
                .Replace('+', '-')
                .TrimEnd('=');

            return Path.Combine(
                RootPath,
                safeGameWorld,
                safeGameName,
                safePlayerId + ".bin");
        }
        private static string GetWorldIdentity()
        {
            string gameName = GamePrefs.GetString(EnumGamePrefs.GameName);
            string gameWorld = GamePrefs.GetString(EnumGamePrefs.GameWorld);

            if (string.IsNullOrWhiteSpace(gameName))
                gameName = "UnknownGame";

            if (string.IsNullOrWhiteSpace(gameWorld))
                gameWorld = "UnknownWorld";

            return gameWorld.Trim() + "|" + gameName.Trim();
        }

        private static string SafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');

            return value.Trim();
        }
        private static MultiInventoryState ToState(MultiInventoryRecord record) => new MultiInventoryState
        { ActiveIndex = record.ActiveIndex, Names = (string[])record.Names.Clone(),
            Locked = (bool[])record.Locked.Clone(), LootFilters = (string[])record.LootFilters.Clone() };
        private static string SerializeError(string error) => JsonConvert.SerializeObject(new MultiInventoryState
        { ActiveIndex = 0, Names = Enumerable.Range(1, InventoryCount).Select(i => "Inventory " + i).ToArray(),
            Locked = new bool[InventoryCount], LootFilters = new string[InventoryCount], Error = error });
    }
}
