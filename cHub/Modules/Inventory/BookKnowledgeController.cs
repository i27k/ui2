using System;
using System.Collections.Generic;
using System.Linq;
using cHub.Core;
using UnityEngine;

public class XUiC_cHubBookInfoWindow : XUiC_ItemInfoWindow
{
    private const string EnabledPref = "cHub.BookKnowledge.Enabled";
    private string _cachedKnowledge = string.Empty;
    private BookKnowledge _knowledge = BookKnowledge.Empty;
    private float _refreshTimer;

    public static bool TrackerEnabled => PlayerPrefs.GetInt(EnabledPref, 1) != 0;

    public static bool ToggleTracker()
    {
        bool enabled = !TrackerEnabled;
        SetTrackerEnabled(enabled);
        return enabled;
    }

    public static void SetTrackerEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(EnabledPref, enabled ? 1 : 0);
        PlayerPrefs.Save();
        StartupTerminal.Audit("BOOK_TRACKER", "TOGGLE", "OK", "enabled=" + enabled + " clientOnly=true");
    }

    public override void Init()
    {
        base.Init();
        StartupTerminal.ReportFeature("ui.book-knowledge", "Book and magazine learning status, collection progress and missing-volume list", true);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        _refreshTimer += deltaTime;
        if (_refreshTimer < 0.15f) return;
        _refreshTimer = 0f;

        BookKnowledge updated = TrackerEnabled ? Analyze(itemStack) : BookKnowledge.Empty;
        string updatedKnowledge = updated.CacheKey;
        if (updatedKnowledge == _cachedKnowledge) return;
        _cachedKnowledge = updatedKnowledge;
        _knowledge = updated;
        SetAllChildrenDirty();

        if (_knowledge.Visible)
            StartupTerminal.Trace("BOOK_TRACKER", "ITEM_SELECTED",
                "item=" + _knowledge.ItemName + " learned=" + _knowledge.Learned +
                " progress=" + _knowledge.Current + "/" + _knowledge.Maximum +
                " missing=" + _knowledge.Remaining);
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "chub_book_visible": value = _knowledge.Visible.ToString().ToLowerInvariant(); return true;
            case "chub_book_state": value = _knowledge.State; return true;
            case "chub_book_state_color": value = _knowledge.Learned ? "82,245,125,255" : "255,78,88,255"; return true;
            case "chub_book_series": value = _knowledge.Series; return true;
            case "chub_book_progress": value = _knowledge.Current + " / " + _knowledge.Maximum; return true;
            case "chub_book_remaining": value = _knowledge.Remaining <= 0 ? "COMPLET" : _knowledge.Remaining + " RAMASE"; return true;
            case "chub_book_missing": value = _knowledge.Missing; return true;
        }
        return base.GetBindingValueInternal(ref value, bindingName);
    }

    private BookKnowledge Analyze(ItemStack stack)
    {
        try
        {
            EntityPlayerLocal player = xui?.playerUI?.entityPlayer;
            ItemClass item = stack?.itemValue?.ItemClass;
            if (player?.Progression == null || item?.Actions == null) return BookKnowledge.Empty;

            string skillName = ResolveProgressionName(item, player.Progression);
            if (string.IsNullOrWhiteSpace(skillName)) return BookKnowledge.Empty;

            ProgressionValue currentValue = player.Progression.GetProgressionValue(skillName);
            ProgressionClass currentClass = currentValue?.ProgressionClass;
            if (currentClass == null) return BookKnowledge.Empty;

            BookKnowledge result = new BookKnowledge();
            result.Visible = true;
            result.ItemName = item.GetLocalizedItemName();

            if (currentClass.IsBook)
            {
                ProgressionClass group = currentClass.Parent;
                List<ProgressionClass> volumes = group?.Children?.Where(c => c != null && c.IsBook).ToList()
                    ?? new List<ProgressionClass> { currentClass };
                List<string> missing = new List<string>();
                int learnedCount = 0;
                foreach (ProgressionClass volume in volumes)
                {
                    ProgressionValue value = player.Progression.GetProgressionValue(volume.Name);
                    bool learned = value != null && value.Level > 0;
                    if (learned) learnedCount++;
                    else missing.Add(Localize(volume.NameKey, volume.Name));
                }

                result.Learned = currentValue.Level > 0;
                result.State = result.Learned ? "DEJA INVATATA" : "NEINVATATA";
                result.Series = Localize(group?.NameKey, group?.Name ?? "COLECTIE");
                result.Current = learnedCount;
                result.Maximum = Math.Max(1, volumes.Count);
                result.Remaining = Math.Max(0, result.Maximum - result.Current);
                result.Missing = missing.Count == 0 ? "Toate volumele seriei sunt invatate."
                    : "Lipsesc: " + string.Join(" • ", missing.ToArray());
            }
            else
            {
                int maximum = Math.Max(1, currentClass.MaxLevel);
                int current = Mathf.Clamp(currentValue.Level, 0, maximum);
                result.Learned = current >= maximum;
                result.State = result.Learned ? "SKILL COMPLET" : "REVISTA NECESARA";
                result.Series = Localize(currentClass.NameKey, currentClass.Name);
                result.Current = current;
                result.Maximum = maximum;
                result.Remaining = Math.Max(0, maximum - current);
                result.Missing = result.Remaining == 0 ? "Nivelul maxim este deja atins."
                    : "Mai ai nevoie de " + result.Remaining + " reviste pentru nivelul maxim.";
            }
            return result;
        }
        catch (Exception ex)
        {
            StartupTerminal.Audit("BOOK_TRACKER", "ANALYZE", "FAILED", ex.GetType().Name + ": " + ex.Message);
            return BookKnowledge.Empty;
        }
    }

    private static string ResolveProgressionName(ItemClass item, Progression progression)
    {
        // Current vanilla versions store books and crafting magazines as Action=Eat.
        // The progression they teach is exposed through the inherited "Unlocks" property.
        string unlocks = item?.Properties?.GetValue("Unlocks");
        if (!string.IsNullOrWhiteSpace(unlocks))
        {
            foreach (string candidate in unlocks.Split(','))
            {
                string name = candidate.Trim();
                if (name.Length == 0) continue;
                ProgressionValue value = progression.GetProgressionValue(name);
                ProgressionClass progressionClass = value?.ProgressionClass;
                if (progressionClass != null && (progressionClass.IsBook || progressionClass.IsCrafting))
                    return name;
            }
        }

        // Compatibility fallback for older game builds and third-party items.
        ItemActionLearnRecipe learn = item?.Actions?.OfType<ItemActionLearnRecipe>().FirstOrDefault();
        if (learn?.SkillsToGain != null)
        {
            foreach (string candidate in learn.SkillsToGain)
            {
                if (string.IsNullOrWhiteSpace(candidate)) continue;
                if (progression.GetProgressionValue(candidate)?.ProgressionClass != null)
                    return candidate;
            }
        }
        return string.Empty;
    }

    private static string Localize(string key, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            string localized = Localization.Get(key, false, null);
            if (!string.IsNullOrWhiteSpace(localized) && !string.Equals(localized, key, StringComparison.OrdinalIgnoreCase))
                return localized;
        }
        return string.IsNullOrWhiteSpace(fallback) ? "Necunoscut" : fallback;
    }

    private sealed class BookKnowledge
    {
        internal static readonly BookKnowledge Empty = new BookKnowledge
        { Visible=false, ItemName="", State="", Series="", Missing="", Maximum=0 };
        internal bool Visible, Learned;
        internal string ItemName, State, Series, Missing;
        internal int Current, Maximum, Remaining;

        internal string CacheKey => Visible + "|" + Learned + "|" + ItemName + "|" + State + "|" +
                                    Series + "|" + Missing + "|" + Current + "|" + Maximum + "|" + Remaining;
    }
}
