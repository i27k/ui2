using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using cHub.Shared.Utils;

namespace cHub.Modules.AdminPanel
{
    public static class RotService
    {
        private static readonly object Sync = new object();
        private static readonly Random Random = new Random();
        private static Dictionary<string, RotRecord> _records;
        private static string DataPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Mods", "cHub", "Data", "rot-scores.json");

        public static void AwardZombieKill(EntityPlayer killer, EntityAlive victim)
        {
            if (killer == null || victim == null || !(victim is EntityZombie)) return;
            if (ConnectionManager.Instance != null && !ConnectionManager.Instance.IsServer) return;
            string id = killer.PersistentPlayerData?.PrimaryId?.CombinedString;
            if (string.IsNullOrWhiteSpace(id)) return;

            bool bloodMoon = false;
            try { bloodMoon = GameStats.GetBool(EnumGameStats.BloodMoonDay); }
            catch { }

            bool drop;
            RotRecord record;
            lock (Sync)
            {
                EnsureLoaded();
                if (!_records.TryGetValue(id, out record))
                    _records[id] = record = new RotRecord { PlayerId = id };
                record.Name = string.IsNullOrWhiteSpace(killer.EntityName) ? "Unknown survivor" : killer.EntityName;
                record.Rot++;
                record.ZombieKills++;
                record.LastSeenUtc = DateTime.UtcNow;
                // Blood Moon: every zombie. Worldwide: one 15% roll per ten kills.
                drop = bloodMoon || (record.ZombieKills % 10 == 0 && Random.NextDouble() < 0.15d);
                if (drop) record.FleshDrops++;
                SaveUnsafe();
            }

            if (!drop) return;
            ItemValue flesh = ItemClass.GetItem("foodRottingFlesh", false);
            if (flesh.IsEmpty()) return;
            GameManager.Instance?.ItemDropServer(new ItemStack(flesh, 1),
                victim.position + new UnityEngine.Vector3(0f, 0.35f, 0f),
                UnityEngine.Vector3.zero, killer.entityId, 60f, false);
        }

        public static RotSnapshot GetSnapshot(string playerId)
        {
            lock (Sync)
            {
                EnsureLoaded();
                RotRecord own = null;
                if (!string.IsNullOrWhiteSpace(playerId) && _records.TryGetValue(playerId, out RotRecord found))
                    own = found.Clone();
                return new RotSnapshot
                {
                    Own = own ?? new RotRecord { PlayerId = playerId ?? string.Empty, Name = "No Rot yet" },
                    Top = _records.Values.OrderByDescending(r => r.Rot)
                        .ThenBy(r => r.Name).Take(20).Select(r => r.Clone()).ToList()
                };
            }
        }

        public static string SerializeSnapshot(string playerId) =>
            JsonConvert.SerializeObject(GetSnapshot(playerId));

        private static void EnsureLoaded()
        {
            if (_records != null) return;
            try
            {
                _records = File.Exists(DataPath)
                    ? JsonConvert.DeserializeObject<Dictionary<string, RotRecord>>(File.ReadAllText(DataPath))
                    : null;
            }
            catch (Exception ex) { Logger.Error($"[Rot] Load failed: {ex}"); }
            _records = _records == null
                ? new Dictionary<string, RotRecord>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, RotRecord>(_records, StringComparer.OrdinalIgnoreCase);
        }

        private static void SaveUnsafe()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DataPath));
                string temporary = DataPath + ".tmp";
                File.WriteAllText(temporary, JsonConvert.SerializeObject(_records, Formatting.Indented));
                if (File.Exists(DataPath)) File.Delete(DataPath);
                File.Move(temporary, DataPath);
            }
            catch (Exception ex) { Logger.Error($"[Rot] Save failed: {ex}"); }
        }
    }

    public sealed class RotRecord
    {
        public string PlayerId { get; set; }
        public string Name { get; set; }
        public long Rot { get; set; }
        public long ZombieKills { get; set; }
        public long FleshDrops { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public RotRecord Clone() => (RotRecord)MemberwiseClone();
    }

    public sealed class RotSnapshot
    {
        public RotRecord Own { get; set; } = new RotRecord();
        public List<RotRecord> Top { get; set; } = new List<RotRecord>();
    }
}
