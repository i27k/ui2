using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using cHub.Services.Players;
using cHub.Shared.Utils;

namespace cHub.Modules.Party
{
    public enum cHubPartyRank { Member, Officer, Leader }

    public sealed class cHubPartyMember
    {
        public string PlayerId { get; set; }
        public string Name { get; set; }
        public cHubPartyRank Rank { get; set; }
        public DateTime LastSeenUtc { get; set; }
    }

    public sealed class cHubPartyRecord
    {
        public string Id { get; set; }
        public string LeaderId { get; set; }
        public List<cHubPartyMember> Members { get; set; } = new List<cHubPartyMember>();
    }

    public static class PartyService
    {
        public const int MaximumMembers = 10;
        private static readonly object Sync = new object();
        private static List<cHubPartyRecord> _parties;
        private static string DataPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Mods", "cHub", "Data", "parties.json");

        public static cHubPartyRecord GetFor(string playerId)
        {
            lock (Sync)
            {
                EnsureLoaded();
                return Clone(_parties.FirstOrDefault(p => p.Members.Any(m => Same(m.PlayerId, playerId))));
            }
        }

        public static string Create(string playerId, string name)
        {
            lock (Sync)
            {
                EnsureLoaded();
                if (_parties.Any(p => p.Members.Any(m => Same(m.PlayerId, playerId))))
                    return "You already belong to a party.";
                _parties.Add(new cHubPartyRecord
                {
                    Id = Guid.NewGuid().ToString("N"), LeaderId = playerId,
                    Members = new List<cHubPartyMember> { NewMember(playerId, name, cHubPartyRank.Leader) }
                });
                SaveUnsafe();
            }
            return "Party created. You are the leader.";
        }

        public static string Delete(string actorId)
        {
            lock (Sync)
            {
                EnsureLoaded();
                cHubPartyRecord party = FindUnsafe(actorId);
                if (party == null) return "No active party.";
                if (!Same(party.LeaderId, actorId)) return "Only the leader can delete the party.";
                _parties.Remove(party);
                SaveUnsafe();
            }
            TryDisbandVanilla();
            return "Party deleted.";
        }

        public static string Invite(string actorId, string targetId, string targetName, int targetEntityId)
        {
            lock (Sync)
            {
                EnsureLoaded();
                cHubPartyRecord party = FindUnsafe(actorId);
                if (party == null) return "Create a party first.";
                cHubPartyMember actor = party.Members.First(m => Same(m.PlayerId, actorId));
                if (actor.Rank == cHubPartyRank.Member) return "Only Leader or Officer can invite.";
                if (party.Members.Count >= MaximumMembers) return "Party is full (10/10).";
                if (party.Members.Any(m => Same(m.PlayerId, targetId))) return "Player is already in the party.";
                if (_parties.Any(p => p.Members.Any(m => Same(m.PlayerId, targetId)))) return "Player belongs to another party.";
                if (targetEntityId < 0) return "Offline players cannot receive a live invite.";
            }
            EntityPlayerLocal local = GameManager.Instance?.World?.GetLocalPlayers()?.FirstOrDefault();
            if (local == null) return "Local player unavailable.";
            ConnectionManager.Instance?.SendToServer(NetPackageManager.GetPackage<NetPackagePartyActions>()
                .Setup(NetPackagePartyActions.PartyActions.SendInvite, local.entityId, targetEntityId, null, string.Empty), false);
            return "Vanilla party invite sent to " + targetName + ".";
        }

        public static string AddAcceptedMember(string leaderId, string playerId, string name)
        {
            lock (Sync)
            {
                EnsureLoaded();
                cHubPartyRecord party = FindUnsafe(leaderId);
                if (party == null || party.Members.Count >= MaximumMembers) return "Party unavailable or full.";
                if (!party.Members.Any(m => Same(m.PlayerId, playerId)))
                    party.Members.Add(NewMember(playerId, name, cHubPartyRank.Member));
                SaveUnsafe();
                return "Member synchronized.";
            }
        }

        public static string Remove(string actorId, string targetId)
        {
            lock (Sync)
            {
                EnsureLoaded();
                cHubPartyRecord party = FindUnsafe(actorId);
                if (party == null) return "No active party.";
                if (!Same(party.LeaderId, actorId)) return "Only the leader can remove members.";
                if (Same(targetId, party.LeaderId)) return "The leader cannot be removed.";
                cHubPartyMember target = party.Members.FirstOrDefault(m => Same(m.PlayerId, targetId));
                if (target == null) return "Player is not a party member.";
                party.Members.Remove(target);
                SaveUnsafe();
            }
            return "Member removed from the persistent party.";
        }

        public static string SetRank(string actorId, string targetId, cHubPartyRank rank)
        {
            lock (Sync)
            {
                EnsureLoaded();
                cHubPartyRecord party = FindUnsafe(actorId);
                if (party == null || !Same(party.LeaderId, actorId)) return "Only the leader can change ranks.";
                if (Same(targetId, party.LeaderId)) return "Leader rank cannot be changed.";
                cHubPartyMember target = party.Members.FirstOrDefault(m => Same(m.PlayerId, targetId));
                if (target == null) return "Select a party member.";
                target.Rank = rank == cHubPartyRank.Officer ? cHubPartyRank.Officer : cHubPartyRank.Member;
                SaveUnsafe();
                return target.Name + " is now " + target.Rank + ".";
            }
        }

        public static void SynchronizeAcceptedVanillaMembers(string localId)
        {
            try
            {
                EntityPlayerLocal local = GameManager.Instance?.World?.GetLocalPlayers()?.FirstOrDefault();
                object vanilla = FindVanillaParty(local?.entityId ?? -1);
                if (vanilla == null) return;
                MethodInfo getIds = vanilla.GetType().GetMethod("GetMemberIdList");
                IEnumerable ids = getIds?.Invoke(vanilla, null) as IEnumerable;
                if (ids == null) return;
                lock (Sync)
                {
                    EnsureLoaded();
                    cHubPartyRecord party = FindUnsafe(localId);
                    if (party == null) return;
                    foreach (object raw in ids)
                    {
                        int entityId = Convert.ToInt32(raw);
                        EntityPlayer player = GameManager.Instance?.World?.GetEntity(entityId) as EntityPlayer;
                        string id = player?.PersistentPlayerData?.PrimaryId?.CombinedString;
                        if (string.IsNullOrWhiteSpace(id) || party.Members.Any(m => Same(m.PlayerId, id))) continue;
                        party.Members.Add(NewMember(id, player.EntityName, cHubPartyRank.Member));
                    }
                    SaveUnsafe();
                }
            }
            catch (Exception ex) { Logger.Warning("[Party] Vanilla synchronization failed: " + ex.Message); }
        }

        private static object FindVanillaParty(int entityId)
        {
            if (entityId < 0 || PartyManager.Current == null) return null;
            FieldInfo field = typeof(PartyManager).GetField("partyList", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            IEnumerable parties = field?.GetValue(PartyManager.Current) as IEnumerable;
            if (parties == null) return null;
            foreach (object party in parties)
            {
                MethodInfo contains = party.GetType().GetMethod("ContainsMember");
                if (contains != null && Convert.ToBoolean(contains.Invoke(party, new object[] { entityId }))) return party;
            }
            return null;
        }

        private static void TryDisbandVanilla()
        {
            try
            {
                EntityPlayerLocal local = GameManager.Instance?.World?.GetLocalPlayers()?.FirstOrDefault();
                object party = FindVanillaParty(local?.entityId ?? -1);
                party?.GetType().GetMethod("Disband")?.Invoke(party, null);
            }
            catch { }
        }

        private static cHubPartyMember NewMember(string id, string name, cHubPartyRank rank) =>
            new cHubPartyMember { PlayerId = id, Name = string.IsNullOrWhiteSpace(name) ? "Unknown" : name,
                Rank = rank, LastSeenUtc = DateTime.UtcNow };
        private static cHubPartyRecord FindUnsafe(string id) => _parties.FirstOrDefault(p => p.Members.Any(m => Same(m.PlayerId, id)));
        private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        private static cHubPartyRecord Clone(cHubPartyRecord source) => source == null ? null : JsonConvert.DeserializeObject<cHubPartyRecord>(JsonConvert.SerializeObject(source));
        private static void EnsureLoaded()
        {
            if (_parties != null) return;
            try { _parties = File.Exists(DataPath) ? JsonConvert.DeserializeObject<List<cHubPartyRecord>>(File.ReadAllText(DataPath)) : null; }
            catch (Exception ex) { Logger.Warning("[Party] Load failed: " + ex.Message); }
            _parties = _parties ?? new List<cHubPartyRecord>();
        }
        private static void SaveUnsafe()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DataPath));
            string temp = DataPath + ".tmp";
            File.WriteAllText(temp, JsonConvert.SerializeObject(_parties, Formatting.Indented));
            if (File.Exists(DataPath)) File.Delete(DataPath);
            File.Move(temp, DataPath);
        }
    }
}
