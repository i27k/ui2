using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cHub.Core;
using cHub.Modules.Inventory;
using UnityEngine;
using Newtonsoft.Json;

internal static class cHubItemSearch
{
    private sealed class FilterState
    {
        public string Query = string.Empty;
        public string[] Terms = new string[0];
        public int Revision;
    }

    private static readonly Dictionary<string, FilterState> filters =
        new Dictionary<string, FilterState>(StringComparer.OrdinalIgnoreCase);

    public static int GetRevision(string scope) => GetState(scope).Revision;

    public static void SetQuery(string value, string source)
    {
        FilterState state = GetState(source);
        string next = (value ?? string.Empty).Trim();
        if (string.Equals(state.Query, next, StringComparison.OrdinalIgnoreCase)) return;

        state.Query = next;
        state.Terms = next.ToLowerInvariant().Split(
            new[] { ' ', '\t' },
            StringSplitOptions.RemoveEmptyEntries);
        state.Revision++;
        StartupTerminal.Audit(
            "ITEM_SEARCH",
            "FILTER",
            "OK",
            "source=" + source + " query='" + state.Query + "' clientOnly=true nonDestructive=true");
    }

    public static void Apply(XUiController root, string scope)
    {
        if (root == null) return;
        ApplyRecursive(root, GetState(scope));
    }

    private static void ApplyRecursive(XUiController controller, FilterState state)
    {
        XUiC_ItemStack slot = controller as XUiC_ItemStack;
        if (slot != null && slot.ViewComponent != null)
            slot.ViewComponent.IsVisible = Matches(slot.ItemStack, state.Terms);

        foreach (XUiController child in controller.Children)
            if (child != null) ApplyRecursive(child, state);
    }

    private static bool Matches(ItemStack stack, string[] terms)
    {
        if (terms.Length == 0) return true;
        if (stack == null || stack.IsEmpty() || stack.itemValue == null) return false;

        ItemClass item = stack.itemValue.ItemClass;
        if (item == null) return false;

        string searchable = (item.GetLocalizedItemName() ?? string.Empty).ToLowerInvariant();
        foreach (string term in terms)
            if (!searchable.Contains(term)) return false;
        return true;
    }

    private static FilterState GetState(string scope)
    {
        FilterState state;
        if (!filters.TryGetValue(scope, out state))
        {
            state = new FilterState();
            filters[scope] = state;
        }
        return state;
    }
}

internal static class cHubItemSearchUi
{
    public static XUiC_TextInput FindInput(XUiController root, string id)
    {
        if (root == null) return null;
        if (root.ViewComponent != null &&
            string.Equals(root.ViewComponent.ID, id, StringComparison.OrdinalIgnoreCase))
            return root as XUiC_TextInput;

        foreach (XUiController child in root.Children)
        {
            XUiC_TextInput found = FindInput(child, id);
            if (found != null) return found;
        }
        return null;
    }
}

public class XUiC_cHubLootWindow : XUiC_LootWindow
{
    private const string SearchScope = "loot_vehicle";
    private XUiC_TextInput searchInput;
    private int appliedRevision = -1;
    private float refreshTimer;

    public override void Init()
    {
        base.Init();
        searchInput = cHubItemSearchUi.FindInput(this, "chubLootSearchInput");
        if (searchInput != null)
            searchInput.OnChangeHandler += (sender, text, finished) =>
                cHubItemSearch.SetQuery(text, SearchScope);
        StartupTerminal.ReportFeature("ui.item-search.loot", "Live client-only filtering for chests, loot and vehicle storage", searchInput != null);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        refreshTimer += deltaTime;
        if (appliedRevision == cHubItemSearch.GetRevision(SearchScope) && refreshTimer < 0.12f) return;
        refreshTimer = 0f;
        appliedRevision = cHubItemSearch.GetRevision(SearchScope);
        cHubItemSearch.Apply(this, SearchScope);
    }

    public override void OnClose()
    {
        cHubItemSearch.SetQuery(string.Empty, SearchScope);
        cHubItemSearch.Apply(this, SearchScope);
        if (searchInput != null) searchInput.Text = string.Empty;
        base.OnClose();
    }
}

public class XUiC_cHubBagStorageWindow : XUiC_BagContainer
{
    private const string SearchScope = "bag_storage_vehicle";
    private XUiC_TextInput searchInput;
    private int appliedRevision = -1;
    private float refreshTimer;

    public override void Init()
    {
        base.Init();
        searchInput = cHubItemSearchUi.FindInput(this, "chubBagStorageSearchInput");
        if (searchInput != null)
            searchInput.OnChangeHandler += (sender, text, finished) =>
                cHubItemSearch.SetQuery(text, SearchScope);
        StartupTerminal.ReportFeature("ui.item-search.vehicle-storage",
            "Independent search for vehicle and bag storage", searchInput != null);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        refreshTimer += deltaTime;
        if (appliedRevision == cHubItemSearch.GetRevision(SearchScope) && refreshTimer < 0.12f) return;
        refreshTimer = 0f;
        appliedRevision = cHubItemSearch.GetRevision(SearchScope);
        cHubItemSearch.Apply(this, SearchScope);
    }

    public override void OnClose()
    {
        cHubItemSearch.SetQuery(string.Empty, SearchScope);
        cHubItemSearch.Apply(this, SearchScope);
        if (searchInput != null) searchInput.Text = string.Empty;
        base.OnClose();
    }
}

public class XUiC_cHubBackpackWindow : XUiC_BackpackWindow
{
    private const string SearchScope = "backpack";
    private XUiC_TextInput searchInput;
    private int appliedRevision = -1;
    private float refreshTimer;
    private XUiController inventoryContext;
    private XUiC_TextInput inventoryRenameInput;
    private XUiController inventoryFilterPanel;
    private XUiC_TextInput inventoryFilterInput;
    private int contextInventoryIndex;
    private XUiController globalSearchPanel;
    private XUiController backpackContent;
    private int searchPage;
    private float highlightTimer;
    private XUiC_ItemStack highlightedStack;
    private XUiController locateContext;
    private int locateResultIndex = -1;
    private readonly bool[] inventoryTabHovered = new bool[MultiInventoryService.InventoryCount];
    private float inventoryRightClickCooldown;
    private float lootRouteTimer;
    private float externalPositionRestoreTimer;
    private int lastLootFingerprint = int.MinValue;
    private XUiController inventoryScaleSlider;
    private XUiController inventoryScaleKnob;
    private float inventoryUiScale = 1f;
    private float inventoryUiScaleBase = 1f;
    private XUiController inventoryToolScaleSlider;
    private XUiController inventoryToolScaleKnob;
    private float inventoryToolScale = 1f;
    private XUiController toolbarScaleSlider;
    private XUiController toolbarScaleKnob;
    private float toolbarScale = 1f;
    private XUiController inventoryMoveHandle;
    private XUiC_SimpleButton uiEditToggleButton;
    private XUiController basicsMoveHandle;
    private XUiController inspectMoveHandle;
    private XUiController emptyInspectMoveHandle;
    private XUiController craftingQueueMoveHandle;
    private XUiController craftingInfoMoveHandle;
    private XUiController slidersMoveHandle;
    private XUiController toolbarMoveHandle;
    private XUiController creativeMoveHandle;
    private XUiController inventorySlotsMoveHandle;
    private XUiController tabsScaleSlider;
    private XUiController tabsScaleKnob;
    private float tabsScale = 1f;
    private XUiController queueScaleSlider;
    private XUiController queueScaleKnob;
    private float queueScale = 1f;
    private bool uiEditVisible = true;
    private Vector3 inventoryScaleSliderBasePosition;
    private Vector3 inventoryToolScaleSliderBasePosition;
    private Vector3 toolbarScaleSliderBasePosition;
    private Vector3 inventoryMoveHandleBasePosition;
    private Vector3 uiEditToggleBasePosition;
    private Vector3 slidersMoveHandleBasePosition;
    private Vector3 tabsScaleSliderBasePosition;
    private XUiController inventoryHeaderSection;
    private XUiController inventorySearchSection;
    private XUiController inventoryTabsSection;
    private Vector3 inventoryHeaderBasePosition;
    private Vector3 inventorySearchBasePosition;
    private Vector3 inventoryTabsBasePosition;
    private Vector3 inventoryTopOffset;
    private XUiC_SimpleButton resetPositionsButton;
    private XUiC_SimpleButton undoPositionsButton;
    private XUiC_SimpleButton layoutPresetsButton;
    private XUiController layoutPresetPanel;
    private XUiController layoutPresetMoveHandle;
    private XUiC_TextInput layoutPresetNameInput;
    private Vector3 layoutPresetPanelDefaultPosition;
    private Vector3 resetPositionsBasePosition;
    private Vector3 undoPositionsBasePosition;
    private Vector3 layoutPresetsBasePosition;
    private Dictionary<string, float> undoLayout;
    private string selectedLayoutPreset = string.Empty;
    private readonly Dictionary<string, Vector3> externalWindowDefaults =
        new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
    private Vector3 inventorySlotsDefaultPosition;

    public override void Init()
    {
        base.Init();
        searchInput = cHubItemSearchUi.FindInput(this, "chubBackpackSearchInput");
        if (searchInput != null)
            searchInput.OnChangeHandler += (sender, text, finished) =>
            {
                string query = (text ?? string.Empty).Trim();
                cHubItemSearch.SetQuery(string.Empty, SearchScope);
                searchPage = 0;
                if (query.Length == 0) SetGlobalSearchVisible(false);
                else MultiInventoryClient.Request(MultiInventoryService.SearchAction, searchPage, query);
            };
        BindInventoryWorkspace();
        BindInventoryScaleSlider();
        MultiInventoryClient.Changed += MultiInventoryChanged;
        MultiInventoryClient.LocateContextRequested += ShowLocateContext;
        StartupTerminal.ReportFeature("ui.item-search.backpack", "Live client-only filtering for player inventory", searchInput != null);
        StartupTerminal.ReportFeature("ui.multi-inventory", "Ten independent server-authoritative inventories with context controls", true);
    }

    public override void OnOpen()
    {
        base.OnOpen();
        ApplyInventoryToolScale(false);
        ApplyToolbarScale(false);
        ApplyTabsScale(false);
        ApplyQueueScale(false);
        RestoreInventoryPosition();
        if (backpackContent?.ViewComponent?.UiTransform != null)
            RestoreTransformPosition(backpackContent.ViewComponent.UiTransform, "InventorySlots");
        RestoreExternalWindowPositions();
        ApplyUiEditVisibility(false);
        HideInventoryContext();
        MultiInventoryClient.Request(MultiInventoryService.StateAction, 0);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (inventoryRightClickCooldown > 0f) inventoryRightClickCooldown -= deltaTime;
        if (inventoryRightClickCooldown <= 0f && Input.GetMouseButtonDown(1))
        {
            for (int i = 0; i < inventoryTabHovered.Length; i++)
            {
                if (!inventoryTabHovered[i]) continue;
                inventoryRightClickCooldown = 0.2f;
                ShowInventoryContext(i);
                StartupTerminal.Audit("MULTI_INVENTORY", "TAB_CLICK", "OK",
                    "button=right-native target=" + (i + 1));
                break;
            }
        }
        lootRouteTimer += deltaTime;
        if (lootRouteTimer >= 0.5f)
        {
            lootRouteTimer = 0f;
            int fingerprint = GetBackpackFingerprint();
            if (fingerprint != lastLootFingerprint)
            {
                lastLootFingerprint = fingerprint;
                MultiInventoryClient.Request(MultiInventoryService.RouteLootAction, 0);
            }
        }
        externalPositionRestoreTimer += deltaTime;
        if (externalPositionRestoreTimer >= 0.1f)
        {
            externalPositionRestoreTimer = 0f;
            RestoreExternalWindowPositions();
        }
        if (highlightTimer > 0f)
        {
            highlightTimer -= deltaTime;
            if (highlightTimer <= 0f && highlightedStack != null)
            {
                highlightedStack.IsSelected = false;
                highlightedStack = null;
            }
        }
        refreshTimer += deltaTime;
        if (appliedRevision == cHubItemSearch.GetRevision(SearchScope) && refreshTimer < 0.12f) return;
        refreshTimer = 0f;
        appliedRevision = cHubItemSearch.GetRevision(SearchScope);
        cHubItemSearch.Apply(this, SearchScope);
    }

    public override void OnClose()
    {
        cHubItemSearch.SetQuery(string.Empty, SearchScope);
        cHubItemSearch.Apply(this, SearchScope);
        if (searchInput != null) searchInput.Text = string.Empty;
        HideInventoryContext();
        base.OnClose();
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        if (bindingName.StartsWith("chub_inventory_tab_", StringComparison.OrdinalIgnoreCase))
        {
            int number;
            if (int.TryParse(bindingName.Substring("chub_inventory_tab_".Length), out number))
            {
                int index = number - 1;
                MultiInventoryState state = MultiInventoryClient.State;
                bool locked = state?.Locked != null && index >= 0 && index < state.Locked.Length && state.Locked[index];
                value = (state != null && state.ActiveIndex == index ? "● " : string.Empty) +
                    (locked ? "L " : string.Empty) + number;
                return true;
            }
        }
        if (bindingName == "chub_inventory_active_name")
        {
            MultiInventoryState state = MultiInventoryClient.State;
            value = state?.Names != null && state.ActiveIndex >= 0 && state.ActiveIndex < state.Names.Length
                ? state.Names[state.ActiveIndex] : "Inventory 1";
            return true;
        }
        if (bindingName == "chub_inventory_context_title")
        {
            string[] names = MultiInventoryClient.State?.Names;
            value = names != null && contextInventoryIndex < names.Length
                ? names[contextInventoryIndex] : "Inventory " + (contextInventoryIndex + 1);
            return true;
        }
        if (bindingName == "chub_inventory_lock_caption")
        {
            bool[] locks = MultiInventoryClient.State?.Locked;
            value = locks != null && contextInventoryIndex < locks.Length && locks[contextInventoryIndex]
                ? "UNLOCK" : "LOCK";
            return true;
        }
        if (bindingName == "chub_inventory_search_page")
        {
            MultiInventoryState state = MultiInventoryClient.State;
            value = "PAGE " + ((state?.SearchPage ?? 0) + 1) + " / " + Math.Max(1, state?.SearchPages ?? 1);
            return true;
        }
        if (bindingName == "chub_inventory_ui_scale")
        {
            value = Mathf.RoundToInt(inventoryUiScale * 100f) + "%";
            return true;
        }
        if (bindingName == "chub_inventory_tool_scale")
        {
            value = Mathf.RoundToInt(inventoryToolScale * 100f) + "%";
            return true;
        }
        if (bindingName == "chub_toolbar_scale")
        {
            value = Mathf.RoundToInt(toolbarScale * 100f) + "%";
            return true;
        }
        if (bindingName == "chub_tabs_scale")
        {
            value = Mathf.RoundToInt(tabsScale * 100f) + "%";
            return true;
        }
        if (bindingName == "chub_queue_scale")
        {
            value = Mathf.RoundToInt(queueScale * 100f) + "%";
            return true;
        }
        if (bindingName == "chub_ui_edit_caption")
        {
            value = uiEditVisible ? "UI EDIT: ON" : "UI EDIT: OFF";
            return true;
        }
        if (bindingName == "chub_layout_selected")
        {
            value = selectedLayoutPreset.Length == 0 ? "NO PRESET SELECTED" : selectedLayoutPreset;
            return true;
        }
        if (bindingName.StartsWith("chub_layout_preset_", StringComparison.OrdinalIgnoreCase))
        {
            int number;
            if (int.TryParse(bindingName.Substring("chub_layout_preset_".Length), out number))
            {
                string[] names = UiLayoutPresetStore.GetNames();
                value = number > 0 && number <= names.Length ? names[number - 1] : "— EMPTY —";
                return true;
            }
        }
        return base.GetBindingValueInternal(ref value, bindingName);
    }

    private void BindInventoryScaleSlider()
    {
        inventoryScaleSlider = FindController<XUiController>(this, "chubInventoryScaleSlider");
        inventoryScaleKnob = FindController<XUiController>(this, "chubInventoryScaleKnob");
        inventoryToolScaleSlider = FindController<XUiController>(this, "chubInventoryToolScaleSlider");
        inventoryToolScaleKnob = FindController<XUiController>(this, "chubInventoryToolScaleKnob");
        toolbarScaleSlider = FindController<XUiController>(this, "chubToolbarScaleSlider");
        toolbarScaleKnob = FindController<XUiController>(this, "chubToolbarScaleKnob");
        tabsScaleSlider = FindController<XUiController>(this, "chubTabsScaleSlider");
        tabsScaleKnob = FindController<XUiController>(this, "chubTabsScaleKnob");
        inventorySlotsMoveHandle = FindController<XUiController>(this, "chubInventorySlotsMoveHandle");
        inventoryMoveHandle = FindController<XUiController>(this, "chubInventoryMoveHandle");
        inventoryHeaderSection = FindController<XUiController>(this, "header");
        inventorySearchSection = FindController<XUiController>(this, "chubBackpackSearch");
        inventoryTabsSection = FindController<XUiController>(this, "chubInventoryTabs");
        slidersMoveHandle = FindController<XUiController>(this, "chubSlidersMoveHandle");
        uiEditToggleButton = FindController<XUiC_SimpleButton>(this, "btnChubToggleUiEdit");
        resetPositionsButton = FindController<XUiC_SimpleButton>(this, "btnChubResetPositions");
        undoPositionsButton = FindController<XUiC_SimpleButton>(this, "btnChubUndoPositions");
        layoutPresetsButton = FindController<XUiC_SimpleButton>(this, "btnChubLayoutPresets");
        layoutPresetPanel = FindController<XUiController>(this, "chubLayoutPresetPanel");
        layoutPresetMoveHandle = FindController<XUiController>(this, "chubLayoutPresetMoveHandle");
        layoutPresetNameInput = FindController<XUiC_TextInput>(this, "chubLayoutPresetNameInput");
        layoutPresetPanelDefaultPosition = GetLocalPosition(layoutPresetPanel);
        inventoryScaleSliderBasePosition = GetLocalPosition(inventoryScaleSlider);
        inventoryToolScaleSliderBasePosition = GetLocalPosition(inventoryToolScaleSlider);
        toolbarScaleSliderBasePosition = GetLocalPosition(toolbarScaleSlider);
        tabsScaleSliderBasePosition = GetLocalPosition(tabsScaleSlider);
        inventoryHeaderBasePosition = GetLocalPosition(inventoryHeaderSection);
        inventorySearchBasePosition = GetLocalPosition(inventorySearchSection);
        inventoryTabsBasePosition = GetLocalPosition(inventoryTabsSection);
        inventoryTopOffset = new Vector3(
            PlayerPrefs.GetFloat("cHub.InventoryTopOffsetX", 0f),
            PlayerPrefs.GetFloat("cHub.InventoryTopOffsetY", 0f), 0f);
        inventoryMoveHandleBasePosition = GetLocalPosition(inventoryMoveHandle);
        uiEditToggleBasePosition = GetLocalPosition(uiEditToggleButton);
        slidersMoveHandleBasePosition = GetLocalPosition(slidersMoveHandle);
        resetPositionsBasePosition = GetLocalPosition(resetPositionsButton);
        undoPositionsBasePosition = GetLocalPosition(undoPositionsButton);
        layoutPresetsBasePosition = GetLocalPosition(layoutPresetsButton);
        RestoreSliderGroupPosition();
        inventoryUiScale = Mathf.Clamp(PlayerPrefs.GetFloat("cHub.InterfaceScale", 1f), 0.65f, 1.25f);
        float current = Mathf.Max(0.01f, GamePrefs.GetFloat(EnumGamePrefs.OptionsHudSize));
        inventoryUiScaleBase = Mathf.Max(0.01f, current / Mathf.Max(0.01f, inventoryUiScale));
        if (inventoryScaleSlider != null)
        {
            inventoryScaleSlider.OnDrag += (sender, dragType, delta) =>
            {
                if (dragType != EDragType.Dragging || Mathf.Abs(delta.y) < 0.01f) return;
                float step = Mathf.Max(0.01f, Mathf.Abs(delta.y) / 650f);
                float next = Mathf.Clamp(inventoryUiScale + (delta.y > 0f ? step : -step), 0.65f, 1.25f);
                next = Mathf.Round(next * 100f) / 100f;
                if (Mathf.Approximately(next, inventoryUiScale)) return;
                inventoryUiScale = next;
                PlayerPrefs.SetFloat("cHub.InterfaceScale", inventoryUiScale);
                PlayerPrefs.Save();
                ApplyInventoryUiScale(true);
            };
        }
        inventoryToolScale = Mathf.Clamp(
            PlayerPrefs.GetFloat("cHub.InventoryToolScale", 1f), 0.65f, 1.35f);
        if (inventoryToolScaleSlider != null)
        {
            inventoryToolScaleSlider.OnDrag += (sender, dragType, delta) =>
            {
                if (dragType != EDragType.Dragging || Mathf.Abs(delta.y) < 0.01f) return;
                float step = Mathf.Max(0.01f, Mathf.Abs(delta.y) / 650f);
                float next = Mathf.Clamp(inventoryToolScale + (delta.y > 0f ? step : -step), 0.65f, 1.35f);
                next = Mathf.Round(next * 100f) / 100f;
                if (Mathf.Approximately(next, inventoryToolScale)) return;
                inventoryToolScale = next;
                PlayerPrefs.SetFloat("cHub.InventoryToolScale", inventoryToolScale);
                PlayerPrefs.Save();
                ApplyInventoryToolScale(true);
            };
        }
        toolbarScale = Mathf.Clamp(PlayerPrefs.GetFloat("cHub.ToolbarScale", 1f), 0.65f, 1.35f);
        if (toolbarScaleSlider != null)
        {
            toolbarScaleSlider.OnDrag += (sender, dragType, delta) =>
            {
                if (dragType != EDragType.Dragging || Mathf.Abs(delta.y) < 0.01f) return;
                float step = Mathf.Max(0.01f, Mathf.Abs(delta.y) / 650f);
                float next = Mathf.Clamp(toolbarScale + (delta.y > 0f ? step : -step), 0.65f, 1.35f);
                next = Mathf.Round(next * 100f) / 100f;
                if (Mathf.Approximately(next, toolbarScale)) return;
                toolbarScale = next;
                PlayerPrefs.SetFloat("cHub.ToolbarScale", toolbarScale);
                PlayerPrefs.Save();
                ApplyToolbarScale(true);
            };
        }
        tabsScale = Mathf.Clamp(PlayerPrefs.GetFloat("cHub.InventoryTabsScale", 1f), 0.65f, 1.35f);
        BindVerticalScale(tabsScaleSlider, () => tabsScale, value =>
        {
            tabsScale = value;
            PlayerPrefs.SetFloat("cHub.InventoryTabsScale", value);
            ApplyTabsScale(true);
        });
        if (inventoryMoveHandle != null)
        {
            inventoryMoveHandle.OnDrag += (sender, dragType, delta) =>
            {
                if (dragType != EDragType.Dragging) return;
                inventoryTopOffset += new Vector3(delta.x, delta.y, 0f);
                PlayerPrefs.SetFloat("cHub.InventoryTopOffsetX", inventoryTopOffset.x);
                PlayerPrefs.SetFloat("cHub.InventoryTopOffsetY", inventoryTopOffset.y);
                ApplyInventoryTopLayout();
            };
            inventoryMoveHandle.OnMouseUpDown += (sender, down) =>
            {
                if (!down) PlayerPrefs.Save();
            };
        }
        if (inventorySlotsMoveHandle != null && backpackContent?.ViewComponent?.UiTransform != null)
        {
            Transform slotsRoot = backpackContent.ViewComponent.UiTransform;
            inventorySlotsDefaultPosition = slotsRoot.localPosition;
            RestoreTransformPosition(slotsRoot, "InventorySlots");
            inventorySlotsMoveHandle.OnDrag += (sender, dragType, delta) =>
            {
                if (dragType != EDragType.Dragging) return;
                Vector3 position = slotsRoot.localPosition;
                position.x += delta.x / Mathf.Max(0.01f, inventoryToolScale);
                position.y += delta.y / Mathf.Max(0.01f, inventoryToolScale);
                slotsRoot.localPosition = position;
                SaveTransformPosition(position, "InventorySlots");
            };
            inventorySlotsMoveHandle.OnMouseUpDown += (sender, down) => { if (!down) PlayerPrefs.Save(); };
        }
        CaptureAllExternalWindowDefaults();
        basicsMoveHandle = BindBenchMoveHandle(
            "chubBasicsMoveHandle",
            "windowCraftingList",
            "Basics");

        inspectMoveHandle = BindBenchMoveHandle(
            "chubInspectMoveHandle",
            "itemInfoPanel",
            "Inspect");

        emptyInspectMoveHandle = BindBenchMoveHandle(
            "chubEmptyInspectMoveHandle",
            "emptyInfoPanel",
            "EmptyInspect");

        craftingQueueMoveHandle = BindBenchMoveHandle(
            "chubCraftingQueueMoveHandle",
            "windowCraftingQueue",
            "CraftingQueue");

        craftingInfoMoveHandle = BindBenchMoveHandle(
            "chubCraftingInfoMoveHandle",
            "craftingInfoPanel",
            "CraftingInfo");
        toolbarMoveHandle = BindExternalMoveHandle(
            "chubToolbarMoveHandle", "windowToolbelt", "Toolbar");
        creativeMoveHandle = BindExternalMoveHandle(
            "chubCreativeMoveHandle", "windowCreative2", "CreativeInventory");
        XUiController queueController = xui?.GetWindow("windowCraftingQueue")?.Controller;
        queueScaleSlider = FindController<XUiController>(queueController, "chubQueueScaleSlider");
        queueScaleKnob = FindController<XUiController>(queueController, "chubQueueScaleKnob");
        queueScale = Mathf.Clamp(PlayerPrefs.GetFloat("cHub.CraftingQueueScale", 1f), 0.65f, 1.35f);
        BindVerticalScale(queueScaleSlider, () => queueScale, value =>
        {
            queueScale = value;
            PlayerPrefs.SetFloat("cHub.CraftingQueueScale", value);
            ApplyQueueScale(true);
        });
        BindSliderGroupMove();
        BindLayoutManager();
        uiEditVisible = PlayerPrefs.GetInt("cHub.UiEditVisible", 1) != 0;
        if (uiEditToggleButton != null)
        {
            uiEditToggleButton.OnPressed += (sender, mouseButton) =>
            {
                if (mouseButton != 0 && mouseButton != -1) return;
                uiEditVisible = !uiEditVisible;
                PlayerPrefs.SetInt("cHub.UiEditVisible", uiEditVisible ? 1 : 0);
                PlayerPrefs.Save();
                ApplyUiEditVisibility(true);
            };
        }
        ApplyInventoryUiScale(false);
        ApplyInventoryToolScale(false);
        ApplyToolbarScale(false);
        ApplyTabsScale(false);
        ApplyQueueScale(false);
        RestoreInventoryPosition();
        ApplyUiEditVisibility(false);
        StartupTerminal.ReportFeature("ui.inventory-scale", "Vertical live UI scale beside inventory",
            inventoryScaleSlider != null && inventoryScaleKnob != null);
        StartupTerminal.ReportFeature("ui.inventory-tool-scale",
            "Independent live inventory scaling",
            inventoryToolScaleSlider != null && inventoryToolScaleKnob != null);
        StartupTerminal.ReportFeature("ui.toolbar-scale", "Independent live toolbar scaling",
            toolbarScaleSlider != null && toolbarScaleKnob != null);
        StartupTerminal.ReportFeature("ui.inventory-drag", "Movable persistent inventory window",
            inventoryMoveHandle != null);
        StartupTerminal.ReportFeature("ui.edit-toggle", "TAB toggle for sliders and move handles",
            uiEditToggleButton != null);
    }

    private void ApplyInventoryUiScale(bool notify)
    {
        if (xui == null) return;
        float requested = inventoryUiScaleBase * inventoryUiScale;
        GamePrefs.Set(EnumGamePrefs.OptionsHudSize, requested);
        GameOptionsManager.OnGamePrefChanged(EnumGamePrefs.OptionsHudSize);
        float active = Mathf.Max(0.01f, GameOptionsManager.GetActiveUiScale());
        xui.SetScale(active);

        // Keep the compact survival HUD at its original apparent size.
        float baseActive = Mathf.Max(0.01f, active / Mathf.Max(0.01f, inventoryUiScale));
        float inverse = baseActive / active;
        foreach (string windowName in new[]
        {
            "HUDLeftStatBars", "HUDRightStatBars", "windowCompass", "windowQuestTracker",
            "windowRecipeTracker", "windowGroupBars", "windowLocation"
        })
        {
            XUiV_Window window = xui.GetWindow(windowName);
            if (window?.Controller?.ViewComponent?.UiTransform != null)
                window.Controller.ViewComponent.UiTransform.localScale = new Vector3(inverse, inverse, 1f);
        }

        UpdateInventoryScaleKnob();
        SetAllChildrenDirty();
        if (!notify) return;
        StartupTerminal.Audit("UI_SCALE", "INVENTORY_VERTICAL", "OK",
            "relative=" + inventoryUiScale.ToString("0.00") +
            " requested=" + requested.ToString("0.000") + " active=" + active.ToString("0.000"));
    }

    private void UpdateInventoryScaleKnob()
    {
        if (inventoryScaleKnob?.ViewComponent?.UiTransform == null) return;
        float t = Mathf.InverseLerp(0.65f, 1.25f, inventoryUiScale);
        Transform transform = inventoryScaleKnob.ViewComponent.UiTransform;
        Vector3 position = transform.localPosition;
        position.y = Mathf.Lerp(-246f, -18f, t);
        transform.localPosition = position;
        RefreshBindings();
    }

    private void ApplyInventoryToolScale(bool notify)
    {
        if (xui == null) return;
        if (ViewComponent?.UiTransform != null)
            ViewComponent.UiTransform.localScale = Vector3.one;
        foreach (string id in new[]
        {
            "content", "chubInventoryContext",
            "chubInventoryFilterPanel", "chubGlobalSearchPanel", "chubLocateContext"
        })
        {
            XUiController section = FindController<XUiController>(this, id);
            if (section?.ViewComponent?.UiTransform != null)
                section.ViewComponent.UiTransform.localScale =
                    new Vector3(inventoryToolScale, inventoryToolScale, 1f);
        }

        // The held stack is rendered in a separate window. Keep it on the
        // same scale as the inventory so dragging remains under the cursor.
        XUiV_Window dragAndDrop = xui.GetWindow("dragAndDropItemStack");
        if (dragAndDrop?.Controller?.ViewComponent?.UiTransform != null)
            dragAndDrop.Controller.ViewComponent.UiTransform.localScale =
                new Vector3(inventoryToolScale, inventoryToolScale, 1f);

        // The controls are outside the scaled inventory sections.
        float inverse = 1f;
        foreach (XUiController control in new[]
        {
            inventoryScaleSlider, inventoryToolScaleSlider, toolbarScaleSlider,
            tabsScaleSlider, inventoryMoveHandle, uiEditToggleButton, slidersMoveHandle
        })
        {
            if (control?.ViewComponent?.UiTransform != null)
                control.ViewComponent.UiTransform.localScale = new Vector3(inverse, inverse, 1f);
        }
        SetCompensatedPosition(inventoryScaleSlider, inventoryScaleSliderBasePosition, inverse);
        SetCompensatedPosition(inventoryToolScaleSlider, inventoryToolScaleSliderBasePosition, inverse);
        SetCompensatedPosition(toolbarScaleSlider, toolbarScaleSliderBasePosition, inverse);
        SetCompensatedPosition(tabsScaleSlider, tabsScaleSliderBasePosition, inverse);
        // MOVE INV and UI EDIT belong to the inventory bar. Counter-scale
        // their size only; retaining local position keeps them attached.
        SetCompensatedPosition(slidersMoveHandle, slidersMoveHandleBasePosition, inverse);
        SetCompensatedPosition(resetPositionsButton, resetPositionsBasePosition, inverse);
        SetCompensatedPosition(undoPositionsButton, undoPositionsBasePosition, inverse);
        SetCompensatedPosition(layoutPresetsButton, layoutPresetsBasePosition, inverse);
        UpdateInventoryToolScaleKnob();
        SetAllChildrenDirty();
        if (!notify) return;
        StartupTerminal.Audit("UI_SCALE", "INVENTORY_TOOLBAR", "OK",
            "relative=" + inventoryToolScale.ToString("0.00") +
            " targets=windowBackpack,dragAndDropItemStack controls=fixed");
    }

    private void UpdateInventoryToolScaleKnob()
    {
        if (inventoryToolScaleKnob?.ViewComponent?.UiTransform == null) return;
        float t = Mathf.InverseLerp(0.65f, 1.35f, inventoryToolScale);
        Transform transform = inventoryToolScaleKnob.ViewComponent.UiTransform;
        Vector3 position = transform.localPosition;
        position.y = Mathf.Lerp(-246f, -18f, t);
        transform.localPosition = position;
        RefreshBindings();
    }

    private void ApplyToolbarScale(bool notify)
    {
        if (xui == null) return;
        // SetStackPanelScale affects and repositions the entire open inventory
        // stack in this build, so only the vanilla toolbelt window is touched.
        XUiV_Window toolbelt = xui.GetWindow("windowToolbelt");
        if (toolbelt?.Controller?.ViewComponent?.UiTransform != null)
            toolbelt.Controller.ViewComponent.UiTransform.localScale =
                new Vector3(toolbarScale, toolbarScale, 1f);
        UpdateToolbarScaleKnob();
        if (!notify) return;
        StartupTerminal.Audit("UI_SCALE", "TOOLBAR", "OK",
            "relative=" + toolbarScale.ToString("0.00") + " target=windowToolbelt");
    }

    private void UpdateToolbarScaleKnob()
    {
        if (toolbarScaleKnob?.ViewComponent?.UiTransform == null) return;
        float t = Mathf.InverseLerp(0.65f, 1.35f, toolbarScale);
        Transform transform = toolbarScaleKnob.ViewComponent.UiTransform;
        Vector3 position = transform.localPosition;
        position.y = Mathf.Lerp(-246f, -18f, t);
        transform.localPosition = position;
        RefreshBindings();
    }

    private void ApplyTabsScale(bool notify)
    {
        ApplyInventoryTopLayout();
        UpdateVerticalKnob(tabsScaleKnob, tabsScale);
        RefreshBindings();
        if (notify) StartupTerminal.Audit("UI_SCALE", "INVENTORY_TABS", "OK",
            "relative=" + tabsScale.ToString("0.00") + " targets=header,search,tabs");
    }

    private void ApplyQueueScale(bool notify)
    {
        // ============================================================
        // cHub - CRAFTING QUEUE SCALE
        // Scalează STRICT grid-ul celor 10 sloturi din Crafting Queue.
        // Nu modifică Inventory / Toolbar / alte ferestre.
        // ============================================================

        XUiController queueWindow =
            xui?.GetWindow("windowCraftingQueue")?.Controller;

        if (queueWindow == null)
        {
            if (notify)
            {
                StartupTerminal.Audit(
                    "UI_SCALE",
                    "CRAFTING_QUEUE",
                    "FAILED",
                    "windowCraftingQueue controller=NULL");
            }

            return;
        }

        // Grid-ul vanilla care conține sloturile Crafting Queue.
        XUiController queueGrid =
            FindController<XUiController>(
                queueWindow,
                "queue");

        // ------------------------------------------------------------
        // Diagnostic
        // ------------------------------------------------------------

        if (queueGrid == null)
        {
            if (notify)
            {
                StartupTerminal.Audit(
                    "UI_SCALE",
                    "CRAFTING_QUEUE",
                    "FAILED",
                    "window=OK" +
                    " grid(queue)=NULL" +
                    " slider=" +
                    (queueScaleSlider != null ? "OK" : "NULL") +
                    " knob=" +
                    (queueScaleKnob != null ? "OK" : "NULL"));
            }

            // Knob-ul trebuie totuși să reflecte valoarea salvată.
            UpdateVerticalKnob(
                queueScaleKnob,
                queueScale);

            RefreshBindings();

            return;
        }

        Transform queueTransform =
            queueGrid.ViewComponent?.UiTransform;

        if (queueTransform == null)
        {
            if (notify)
            {
                StartupTerminal.Audit(
                    "UI_SCALE",
                    "CRAFTING_QUEUE",
                    "FAILED",
                    "window=OK grid=OK UiTransform=NULL");
            }

            return;
        }

        // ------------------------------------------------------------
        // Clamp de siguranță
        // ------------------------------------------------------------

        queueScale = Mathf.Clamp(
            queueScale,
            0.65f,
            1.35f);

        // ------------------------------------------------------------
        // Aplicăm scale STRICT pe grid-ul queue.
        // Astfel MOVE QUEUE rămâne independent.
        // ------------------------------------------------------------

        queueTransform.localScale =
            new Vector3(
                queueScale,
                queueScale,
                1f);

        // ------------------------------------------------------------
        // Actualizăm poziția knob-ului sliderului.
        // ------------------------------------------------------------

        UpdateVerticalKnob(
            queueScaleKnob,
            queueScale);

        // Actualizează textul:
        // {chub_queue_scale}
        RefreshBindings();

        // ------------------------------------------------------------
        // Audit
        // ------------------------------------------------------------

        if (notify)
        {
            StartupTerminal.Audit(
                "UI_SCALE",
                "CRAFTING_QUEUE",
                "OK",
                "window=OK" +
                " grid=OK" +
                " slider=" +
                (queueScaleSlider != null ? "OK" : "NULL") +
                " knob=" +
                (queueScaleKnob != null ? "OK" : "NULL") +
                " relative=" +
                queueScale.ToString("0.00") +
                " localScale=" +
                queueTransform.localScale.x.ToString("0.00") +
                "," +
                queueTransform.localScale.y.ToString("0.00") +
                " slots=10");
        }
    }

    private void BindVerticalScale(XUiController slider, Func<float> getValue, Action<float> setValue)
    {
        if (slider == null) return;
        slider.OnDrag += (sender, dragType, delta) =>
        {
            if (dragType != EDragType.Dragging || Mathf.Abs(delta.y) < 0.01f) return;
            float step = Mathf.Max(0.01f, Mathf.Abs(delta.y) / 650f);
            float next = Mathf.Clamp(getValue() + (delta.y > 0f ? step : -step), 0.65f, 1.35f);
            next = Mathf.Round(next * 100f) / 100f;
            if (Mathf.Approximately(next, getValue())) return;
            setValue(next);
            PlayerPrefs.Save();
        };
    }

    private static void UpdateVerticalKnob(XUiController knob, float value)
    {
        if (knob?.ViewComponent?.UiTransform == null) return;
        float t = Mathf.InverseLerp(0.65f, 1.35f, value);
        Transform transform = knob.ViewComponent.UiTransform;
        Vector3 position = transform.localPosition;
        position.y = Mathf.Lerp(-116f, -18f, t);
        transform.localPosition = position;
    }

    private void RestoreInventoryPosition()
    {
        ApplyInventoryTopLayout();
    }

    private void ApplyInventoryTopLayout()
    {
        ApplyTopSection(inventoryHeaderSection, inventoryHeaderBasePosition, tabsScale);
        ApplyTopSection(inventorySearchSection, inventorySearchBasePosition, tabsScale);
        ApplyTopSection(inventoryTabsSection, inventoryTabsBasePosition, tabsScale);
    }

    private void ApplyTopSection(XUiController section, Vector3 basePosition, float scale)
    {
        if (section?.ViewComponent?.UiTransform == null) return;
        Transform transform = section.ViewComponent.UiTransform;
        transform.localScale = new Vector3(scale, scale, 1f);
        transform.localPosition = new Vector3(
            basePosition.x * scale + inventoryTopOffset.x,
            basePosition.y * scale + inventoryTopOffset.y,
            basePosition.z);
    }

    private static void RestoreTransformPosition(Transform root, string key)
    {
        if (root == null || !PlayerPrefs.HasKey("cHub." + key + "PositionX")) return;
        Vector3 position = root.localPosition;
        position.x = PlayerPrefs.GetFloat("cHub." + key + "PositionX", position.x);
        position.y = PlayerPrefs.GetFloat("cHub." + key + "PositionY", position.y);
        root.localPosition = position;
    }

    private static void SaveTransformPosition(Vector3 position, string key)
    {
        PlayerPrefs.SetFloat("cHub." + key + "PositionX", position.x);
        PlayerPrefs.SetFloat("cHub." + key + "PositionY", position.y);
    }

    private void RestoreExternalWindowPositions()
    {
        if (toolbarMoveHandle == null)
            toolbarMoveHandle = BindExternalMoveHandle(
                "chubToolbarMoveHandle", "windowToolbelt", "Toolbar");
        if (creativeMoveHandle == null)
            creativeMoveHandle = BindExternalMoveHandle(
                "chubCreativeMoveHandle", "windowCreative2", "CreativeInventory");
        RestoreExternalWindowPosition("windowCraftingList", "Basics");
        RestoreExternalWindowPosition("itemInfoPanel", "Inspect");
        RestoreExternalWindowPosition("emptyInfoPanel", "EmptyInspect");
        RestoreExternalWindowPosition("windowCraftingQueue", "CraftingQueue");
        RestoreExternalWindowPosition("craftingInfoPanel", "CraftingInfo");
        RestoreExternalWindowPosition("windowToolbelt", "Toolbar");
        RestoreExternalWindowPosition("windowCreative2", "CreativeInventory");
        ApplyUiEditVisibility(false);
    }

    private void CaptureAllExternalWindowDefaults()
    {
        foreach (var pair in ExternalUiWindows())
            CaptureExternalWindowDefault(pair.Item1, pair.Item2);
        PlayerPrefs.Save();
    }

    private void CaptureExternalWindowDefault(string preferenceKey, string windowName)
    {
        Transform root = xui?.GetWindow(windowName)?.Controller?.ViewComponent?.UiTransform;
        if (root == null) return;
        if (!externalWindowDefaults.ContainsKey(preferenceKey))
            externalWindowDefaults[preferenceKey] = root.localPosition;
        string xKey = "cHub." + preferenceKey + "DefaultPositionX";
        if (PlayerPrefs.HasKey(xKey)) return;
        PlayerPrefs.SetFloat(xKey, root.localPosition.x);
        PlayerPrefs.SetFloat("cHub." + preferenceKey + "DefaultPositionY", root.localPosition.y);
    }

    private static Tuple<string, string>[] ExternalUiWindows()
    {
        return new[]
        {
            Tuple.Create("Basics", "windowCraftingList"), Tuple.Create("Inspect", "itemInfoPanel"),
            Tuple.Create("EmptyInspect", "emptyInfoPanel"), Tuple.Create("CraftingQueue", "windowCraftingQueue"),
            Tuple.Create("CraftingInfo", "craftingInfoPanel"), Tuple.Create("Toolbar", "windowToolbelt"),
            Tuple.Create("CreativeInventory", "windowCreative2")
        };
    }

    private void RestoreExternalWindowPosition(string windowName, string preferenceKey)
    {
        string xKey = "cHub." + preferenceKey + "PositionX";
        if (!PlayerPrefs.HasKey(xKey)) return;
        XUiV_Window window = xui?.GetWindow(windowName);
        Transform root = window?.Controller?.ViewComponent?.UiTransform;
        if (root == null) return;
        Vector3 position = root.localPosition;
        float savedX = PlayerPrefs.GetFloat(xKey, position.x);
        float savedY = PlayerPrefs.GetFloat("cHub." + preferenceKey + "PositionY", position.y);
        if (Mathf.Abs(position.x - savedX) < 0.01f && Mathf.Abs(position.y - savedY) < 0.01f) return;
        position.x = savedX;
        position.y = savedY;
        root.localPosition = position;
    }

    private XUiController BindExternalMoveHandle(string handleId, string windowName, string preferenceKey)
    {
        XUiV_Window window = xui?.GetWindow(windowName);
        XUiController target = window?.Controller;
        XUiController handle = FindController<XUiController>(target, handleId);
        if (target?.ViewComponent?.UiTransform == null || handle == null)
        {
            StartupTerminal.Audit("UI_MOVE", preferenceKey.ToUpperInvariant(), "FAILED",
                "window=" + windowName + " handle=" + handleId);
            return null;
        }
        Transform root = target.ViewComponent.UiTransform;
        if (!externalWindowDefaults.ContainsKey(preferenceKey))
            externalWindowDefaults[preferenceKey] = root.localPosition;
        CaptureExternalWindowDefault(preferenceKey, windowName);
        if (PlayerPrefs.HasKey("cHub." + preferenceKey + "PositionX"))
        {
            Vector3 saved = root.localPosition;
            saved.x = PlayerPrefs.GetFloat("cHub." + preferenceKey + "PositionX", saved.x);
            saved.y = PlayerPrefs.GetFloat("cHub." + preferenceKey + "PositionY", saved.y);
            root.localPosition = saved;
        }
        handle.OnDrag += (sender, dragType, delta) =>
        {
            if (dragType != EDragType.Dragging) return;
            Vector3 position = root.localPosition;
            position.x += delta.x;
            position.y += delta.y;
            root.localPosition = position;
            PlayerPrefs.SetFloat("cHub." + preferenceKey + "PositionX", position.x);
            PlayerPrefs.SetFloat("cHub." + preferenceKey + "PositionY", position.y);
        };
        handle.OnMouseUpDown += (sender, down) => { if (!down) PlayerPrefs.Save(); };
        StartupTerminal.Audit("UI_MOVE", preferenceKey.ToUpperInvariant(), "OK",
            "window=" + windowName + " persistent=true");
        return handle;
    }

    private XUiController BindBenchMoveHandle(
    string handleId,
    string windowName,
    string preferenceKey)
    {
        XUiV_Window window = xui?.GetWindow(windowName);
        XUiController target = window?.Controller;

        if (target?.ViewComponent?.UiTransform == null)
        {
            StartupTerminal.Audit(
                "UI_MOVE",
                "BENCH_" + preferenceKey.ToUpperInvariant(),
                "FAILED",
                "window=" + windowName + " target=NULL");

            return null;
        }

        XUiController handle =
            FindController<XUiController>(target, handleId);

        if (handle == null)
        {
            StartupTerminal.Audit(
                "UI_MOVE",
                "BENCH_" + preferenceKey.ToUpperInvariant(),
                "FAILED",
                "window=" + windowName +
                " handle=" + handleId +
                " handle=NULL");

            return null;
        }

        Transform root = target.ViewComponent.UiTransform;

        if (!externalWindowDefaults.ContainsKey(preferenceKey))
            externalWindowDefaults[preferenceKey] = root.localPosition;

        CaptureExternalWindowDefault(preferenceKey, windowName);

        // Restore poziția salvată.
        string xKey = "cHub." + preferenceKey + "PositionX";
        string yKey = "cHub." + preferenceKey + "PositionY";

        if (PlayerPrefs.HasKey(xKey))
        {
            Vector3 position = root.localPosition;

            position.x = PlayerPrefs.GetFloat(
                xKey,
                position.x);

            position.y = PlayerPrefs.GetFloat(
                yKey,
                position.y);

            root.localPosition = position;
        }

        handle.OnDrag += (sender, dragType, delta) =>
        {
            if (dragType != EDragType.Dragging)
                return;

            Vector3 position = root.localPosition;

            position.x += delta.x;
            position.y += delta.y;

            root.localPosition = position;

            PlayerPrefs.SetFloat(
                xKey,
                position.x);

            PlayerPrefs.SetFloat(
                yKey,
                position.y);
        };

        handle.OnMouseUpDown += (sender, down) =>
        {
            if (!down)
            {
                PlayerPrefs.Save();

                StartupTerminal.Audit(
                    "UI_MOVE",
                    "BENCH_" + preferenceKey.ToUpperInvariant(),
                    "OK",
                    "saved=true x=" +
                    root.localPosition.x.ToString("0.0") +
                    " y=" +
                    root.localPosition.y.ToString("0.0"));
            }
        };

        StartupTerminal.Audit(
            "UI_MOVE",
            "BENCH_" + preferenceKey.ToUpperInvariant(),
            "OK",
            "window=" + windowName +
            " handle=" + handleId +
            " persistent=true");

        return handle;
    }
    private void ApplyUiEditVisibility(bool notify)
    {
        foreach (XUiController control in new[]
        {
            inventoryScaleSlider, inventoryToolScaleSlider, toolbarScaleSlider,
            tabsScaleSlider, queueScaleSlider, inventoryMoveHandle, inventorySlotsMoveHandle,
            basicsMoveHandle, inspectMoveHandle,
            emptyInspectMoveHandle, craftingQueueMoveHandle, craftingInfoMoveHandle,
            slidersMoveHandle, toolbarMoveHandle, creativeMoveHandle,
            resetPositionsButton, undoPositionsButton, layoutPresetsButton
        })
        {
            if (control?.ViewComponent != null) control.ViewComponent.IsVisible = uiEditVisible;
        }
        RefreshBindings();
        if (notify)
            StartupTerminal.Audit("UI_EDIT", "TOGGLE", "OK",
                "visible=" + uiEditVisible + " controls=sliders,inventory,basics,inspect");
    }

    private static Vector3 GetLocalPosition(XUiController controller)
    {
        return controller?.ViewComponent?.UiTransform != null
            ? controller.ViewComponent.UiTransform.localPosition : Vector3.zero;
    }

    private static void SetCompensatedPosition(XUiController controller, Vector3 basePosition, float inverse)
    {
        if (controller?.ViewComponent?.UiTransform == null) return;
        controller.ViewComponent.UiTransform.localPosition =
            new Vector3(basePosition.x * inverse, basePosition.y * inverse, basePosition.z);
    }

    private void BindLayoutManager()
    {
        BindContextButton("btnChubResetPositions", ResetUiPositions);
        BindContextButton("btnChubUndoPositions", UndoUiPositions);
        BindContextButton("btnChubLayoutPresets", () => SetLayoutPresetPanelVisible(true));
        BindContextButton("btnChubLayoutPresetClose", () => SetLayoutPresetPanelVisible(false));
        BindContextButton("btnChubLayoutPresetSave", SaveNamedLayoutPreset);
        BindContextButton("btnChubLayoutPresetLoad", LoadSelectedLayoutPreset);
        BindContextButton("btnChubLayoutPresetDelete", DeleteSelectedLayoutPreset);
        for (int i = 0; i < 6; i++)
        {
            int captured = i;
            BindContextButton("btnChubLayoutPreset" + (i + 1), () => SelectLayoutPreset(captured));
        }
        BindLayoutPresetPanelMove();
        SetLayoutPresetPanelVisible(false);
    }

    private void BindLayoutPresetPanelMove()
    {
        Transform panel = layoutPresetPanel?.ViewComponent?.UiTransform;
        if (panel == null || layoutPresetMoveHandle == null) return;
        Vector3 position = panel.localPosition;
        position.x = PlayerPrefs.GetFloat("cHub.LayoutPresetPanelPositionX", position.x);
        position.y = PlayerPrefs.GetFloat("cHub.LayoutPresetPanelPositionY", position.y);
        panel.localPosition = position;
        layoutPresetMoveHandle.OnDrag += (sender, dragType, delta) =>
        {
            if (dragType != EDragType.Dragging) return;
            Vector3 moved = panel.localPosition;
            moved.x += delta.x;
            moved.y += delta.y;
            panel.localPosition = moved;
            PlayerPrefs.SetFloat("cHub.LayoutPresetPanelPositionX", moved.x);
            PlayerPrefs.SetFloat("cHub.LayoutPresetPanelPositionY", moved.y);
        };
        layoutPresetMoveHandle.OnMouseUpDown += (sender, down) =>
        {
            if (!down) PlayerPrefs.Save();
        };
        StartupTerminal.Audit("UI_MOVE", "LAYOUT_PRESET_MANAGER", "OK", "persistent=true preset=true");
    }

    private void ResetUiPositions()
    {
        undoLayout = CaptureLayout();
        Dictionary<string, float> vanilla = CaptureLayout();
        vanilla["InventoryTopOffsetX"] = 0f;
        vanilla["InventoryTopOffsetY"] = 0f;
        vanilla["SliderGroupOffsetX"] = 0f;
        vanilla["SliderGroupOffsetY"] = 0f;
        vanilla["InventorySlotsPositionX"] = inventorySlotsDefaultPosition.x;
        vanilla["InventorySlotsPositionY"] = inventorySlotsDefaultPosition.y;
        vanilla["LayoutPresetPanelPositionX"] = layoutPresetPanelDefaultPosition.x;
        vanilla["LayoutPresetPanelPositionY"] = layoutPresetPanelDefaultPosition.y;
        foreach (var window in ExternalUiWindows())
        {
            bool hasRuntimeDefault = externalWindowDefaults.TryGetValue(window.Item1, out Vector3 known);
            bool hasSavedDefault = PlayerPrefs.HasKey("cHub." + window.Item1 + "DefaultPositionX");
            if (!hasRuntimeDefault && !hasSavedDefault) continue;
            Vector3 fallback = hasRuntimeDefault ? known : Vector3.zero;
            vanilla[window.Item1 + "PositionX"] = PlayerPrefs.GetFloat(
                "cHub." + window.Item1 + "DefaultPositionX", fallback.x);
            vanilla[window.Item1 + "PositionY"] = PlayerPrefs.GetFloat(
                "cHub." + window.Item1 + "DefaultPositionY", fallback.y);
        }
        ApplyLayout(vanilla);
        StartupTerminal.Audit("UI_LAYOUT", "RESET_POSITIONS", "OK", "undo=available vanilla=true");
    }

    private void UndoUiPositions()
    {
        if (undoLayout == null) return;
        Dictionary<string, float> current = CaptureLayout();
        ApplyLayout(undoLayout);
        undoLayout = current;
        StartupTerminal.Audit("UI_LAYOUT", "UNDO", "OK", "swap=true");
    }

    private Dictionary<string, float> CaptureLayout()
    {
        var values = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            ["InventoryTopOffsetX"] = inventoryTopOffset.x,
            ["InventoryTopOffsetY"] = inventoryTopOffset.y,
            ["SliderGroupOffsetX"] = PlayerPrefs.GetFloat("cHub.SliderGroupOffsetX", 0f),
            ["SliderGroupOffsetY"] = PlayerPrefs.GetFloat("cHub.SliderGroupOffsetY", 0f),
            ["InterfaceScale"] = inventoryUiScale,
            ["InventoryScale"] = inventoryToolScale,
            ["ToolbarScale"] = toolbarScale,
            ["TabsScale"] = tabsScale,
            ["QueueScale"] = queueScale
        };
        if (layoutPresetPanel?.ViewComponent?.UiTransform != null)
        {
            Vector3 panel = layoutPresetPanel.ViewComponent.UiTransform.localPosition;
            values["LayoutPresetPanelPositionX"] = panel.x;
            values["LayoutPresetPanelPositionY"] = panel.y;
        }
        if (backpackContent?.ViewComponent?.UiTransform != null)
        {
            Vector3 p = backpackContent.ViewComponent.UiTransform.localPosition;
            values["InventorySlotsPositionX"] = p.x;
            values["InventorySlotsPositionY"] = p.y;
        }
        foreach (var pair in ExternalUiWindows())
        {
            Transform root = xui?.GetWindow(pair.Item2)?.Controller?.ViewComponent?.UiTransform;
            if (root == null) continue;
            values[pair.Item1 + "PositionX"] = root.localPosition.x;
            values[pair.Item1 + "PositionY"] = root.localPosition.y;
        }
        return values;
    }

    private void ApplyLayout(Dictionary<string, float> values)
    {
        if (values == null) return;
        float oldSliderX = PlayerPrefs.GetFloat("cHub.SliderGroupOffsetX", 0f);
        float oldSliderY = PlayerPrefs.GetFloat("cHub.SliderGroupOffsetY", 0f);
        float newSliderX = Value(values, "SliderGroupOffsetX", oldSliderX);
        float newSliderY = Value(values, "SliderGroupOffsetY", oldSliderY);
        Vector3 sliderDelta = new Vector3(newSliderX - oldSliderX, newSliderY - oldSliderY, 0f);
        inventoryScaleSliderBasePosition += sliderDelta;
        inventoryToolScaleSliderBasePosition += sliderDelta;
        toolbarScaleSliderBasePosition += sliderDelta;
        tabsScaleSliderBasePosition += sliderDelta;
        slidersMoveHandleBasePosition += sliderDelta;
        resetPositionsBasePosition += sliderDelta;
        undoPositionsBasePosition += sliderDelta;
        layoutPresetsBasePosition += sliderDelta;

        inventoryTopOffset = new Vector3(Value(values, "InventoryTopOffsetX", 0f),
            Value(values, "InventoryTopOffsetY", 0f), 0f);
        inventoryUiScale = Value(values, "InterfaceScale", inventoryUiScale);
        inventoryToolScale = Value(values, "InventoryScale", inventoryToolScale);
        toolbarScale = Value(values, "ToolbarScale", toolbarScale);
        tabsScale = Value(values, "TabsScale", tabsScale);
        queueScale = Value(values, "QueueScale", queueScale);

        foreach (var pair in values) PlayerPrefs.SetFloat("cHub." + pair.Key, pair.Value);
        PlayerPrefs.SetFloat("cHub.InterfaceScale", inventoryUiScale);
        PlayerPrefs.SetFloat("cHub.InventoryToolScale", inventoryToolScale);
        PlayerPrefs.SetFloat("cHub.ToolbarScale", toolbarScale);
        PlayerPrefs.SetFloat("cHub.InventoryTabsScale", tabsScale);
        PlayerPrefs.SetFloat("cHub.CraftingQueueScale", queueScale);
        if (backpackContent?.ViewComponent?.UiTransform != null)
        {
            Vector3 p = backpackContent.ViewComponent.UiTransform.localPosition;
            p.x = Value(values, "InventorySlotsPositionX", p.x);
            p.y = Value(values, "InventorySlotsPositionY", p.y);
            backpackContent.ViewComponent.UiTransform.localPosition = p;
        }
        if (layoutPresetPanel?.ViewComponent?.UiTransform != null)
        {
            Vector3 panel = layoutPresetPanel.ViewComponent.UiTransform.localPosition;
            panel.x = Value(values, "LayoutPresetPanelPositionX", panel.x);
            panel.y = Value(values, "LayoutPresetPanelPositionY", panel.y);
            layoutPresetPanel.ViewComponent.UiTransform.localPosition = panel;
        }
        foreach (var pair in ExternalUiWindows())
        {
            Transform root = xui?.GetWindow(pair.Item2)?.Controller?.ViewComponent?.UiTransform;
            if (root == null) continue;
            Vector3 p = root.localPosition;
            p.x = Value(values, pair.Item1 + "PositionX", p.x);
            p.y = Value(values, pair.Item1 + "PositionY", p.y);
            root.localPosition = p;
        }
        PlayerPrefs.Save();
        ApplyInventoryUiScale(false);
        ApplyInventoryToolScale(false);
        ApplyToolbarScale(false);
        ApplyTabsScale(false);
        ApplyQueueScale(false);
        RestoreExternalWindowPositions();
        RefreshBindings();
    }

    private static float Value(Dictionary<string, float> values, string key, float fallback)
        => values.TryGetValue(key, out float value) ? value : fallback;

    private void SaveNamedLayoutPreset()
    {
        string name = (layoutPresetNameInput?.Text ?? string.Empty).Trim();
        if (name.Length == 0) return;
        UiLayoutPresetStore.Save(name, CaptureLayout());
        selectedLayoutPreset = name;
        RefreshBindings();
    }

    private void SelectLayoutPreset(int index)
    {
        string[] names = UiLayoutPresetStore.GetNames();
        if (index < 0 || index >= names.Length) return;
        selectedLayoutPreset = names[index];
        if (layoutPresetNameInput != null) layoutPresetNameInput.Text = selectedLayoutPreset;
        RefreshBindings();
    }

    private void LoadSelectedLayoutPreset()
    {
        if (selectedLayoutPreset.Length == 0) return;
        undoLayout = CaptureLayout();
        ApplyLayout(UiLayoutPresetStore.Load(selectedLayoutPreset));
    }

    private void DeleteSelectedLayoutPreset()
    {
        if (selectedLayoutPreset.Length == 0) return;
        UiLayoutPresetStore.Delete(selectedLayoutPreset);
        selectedLayoutPreset = string.Empty;
        RefreshBindings();
    }

    private void SetLayoutPresetPanelVisible(bool visible)
    {
        if (layoutPresetPanel?.ViewComponent != null) layoutPresetPanel.ViewComponent.IsVisible = visible;
    }

    private void BindSliderGroupMove()
    {
        if (slidersMoveHandle == null) return;
        slidersMoveHandle.OnDrag += (sender, dragType, delta) =>
        {
            if (dragType != EDragType.Dragging) return;
            Vector3 move = new Vector3(delta.x, delta.y, 0f);
            inventoryScaleSliderBasePosition += move;
            inventoryToolScaleSliderBasePosition += move;
            toolbarScaleSliderBasePosition += move;
            tabsScaleSliderBasePosition += move;
            slidersMoveHandleBasePosition += move;
            resetPositionsBasePosition += move;
            undoPositionsBasePosition += move;
            layoutPresetsBasePosition += move;
            SaveSliderGroupPosition();
            float inverse = 1f;
            SetCompensatedPosition(inventoryScaleSlider, inventoryScaleSliderBasePosition, inverse);
            SetCompensatedPosition(inventoryToolScaleSlider, inventoryToolScaleSliderBasePosition, inverse);
            SetCompensatedPosition(toolbarScaleSlider, toolbarScaleSliderBasePosition, inverse);
            SetCompensatedPosition(tabsScaleSlider, tabsScaleSliderBasePosition, inverse);
            SetCompensatedPosition(slidersMoveHandle, slidersMoveHandleBasePosition, inverse);
            SetCompensatedPosition(resetPositionsButton, resetPositionsBasePosition, inverse);
            SetCompensatedPosition(undoPositionsButton, undoPositionsBasePosition, inverse);
            SetCompensatedPosition(layoutPresetsButton, layoutPresetsBasePosition, inverse);
        };
        slidersMoveHandle.OnMouseUpDown += (sender, down) => { if (!down) PlayerPrefs.Save(); };
    }

    private void RestoreSliderGroupPosition()
    {
        if (!PlayerPrefs.HasKey("cHub.SliderGroupOffsetX")) return;
        Vector3 offset = new Vector3(
            PlayerPrefs.GetFloat("cHub.SliderGroupOffsetX", 0f),
            PlayerPrefs.GetFloat("cHub.SliderGroupOffsetY", 0f), 0f);
        inventoryScaleSliderBasePosition += offset;
        inventoryToolScaleSliderBasePosition += offset;
        toolbarScaleSliderBasePosition += offset;
        tabsScaleSliderBasePosition += offset;
        slidersMoveHandleBasePosition += offset;
        resetPositionsBasePosition += offset;
        undoPositionsBasePosition += offset;
        layoutPresetsBasePosition += offset;
    }

    private void SaveSliderGroupPosition()
    {
        // The first bar is the stable reference for the whole three-slider cluster.
        PlayerPrefs.SetFloat("cHub.SliderGroupOffsetX", inventoryScaleSliderBasePosition.x - 612f);
        PlayerPrefs.SetFloat("cHub.SliderGroupOffsetY", inventoryScaleSliderBasePosition.y + 43f);
    }

    private void BindInventoryWorkspace()
    {
        for (int i = 0; i < MultiInventoryService.InventoryCount; i++)
        {
            int captured = i;
            XUiC_SimpleButton tabButton = FindController<XUiC_SimpleButton>(
                this, "btnChubInventory" + (i + 1));
            if (tabButton == null)
            {
                StartupTerminal.Audit("MULTI_INVENTORY", "TAB_BIND", "FAILED",
                    "button=btnChubInventory" + (i + 1));
                continue;
            }
            tabButton.OnPressed += (sender, mouseButton) =>
            {
                StartupTerminal.Audit(
                     "MULTI_INVENTORY",
                     "RAW_TAB_CLICK",
                     "OK",
                      "target=" + (captured + 1) +
                    " mouseButton=" + mouseButton +
                        " LMB=" + Input.GetMouseButton(0) +
                       " RMB=" + Input.GetMouseButton(1));
                bool rightClick = mouseButton == 1 || Input.GetMouseButton(1);
                if (rightClick)
                {
                    inventoryRightClickCooldown = 0.2f;
                    ShowInventoryContext(captured);
                    StartupTerminal.Audit("MULTI_INVENTORY", "TAB_CLICK", "OK",
                        "button=right target=" + (captured + 1));
                }
                else if (mouseButton == 0 || mouseButton == -1)
                {
                    HideInventoryContext();
                    XUiC_DragAndDropWindow dragWindow = xui?.DragAndDropWindow;
                    if (dragWindow?.CurrentStack != null && !dragWindow.CurrentStack.IsEmpty())
                        MultiInventoryClient.RequestTransfer(captured, dragWindow);
                    else
                        MultiInventoryClient.Request(MultiInventoryService.SwitchAction, captured);
                    StartupTerminal.Audit("MULTI_INVENTORY", "TAB_CLICK", "OK",
                        "button=left target=" + (captured + 1));
                }
            };
            tabButton.OnHovered += (sender, isOver) => inventoryTabHovered[captured] = isOver;
            StartupTerminal.Audit("MULTI_INVENTORY", "TAB_BIND", "OK",
                "button=btnChubInventory" + (i + 1) + " left=open/drop right=options");
        }

        inventoryContext = FindController<XUiController>(this, "chubInventoryContext");
        globalSearchPanel = FindController<XUiController>(this, "chubGlobalSearchPanel");
        backpackContent = FindController<XUiController>(this, "content");
        locateContext = FindController<XUiController>(this, "chubLocateContext");
        inventoryRenameInput = FindController<XUiC_TextInput>(this, "chubInventoryRenameInput");
        inventoryFilterPanel = FindController<XUiController>(this, "chubInventoryFilterPanel");
        inventoryFilterInput = FindController<XUiC_TextInput>(this, "chubInventoryFilterInput");
        BindContextButton("btnChubInventoryRename", () =>
        {
            MultiInventoryClient.Request(MultiInventoryService.RenameAction, contextInventoryIndex,
                inventoryRenameInput?.Text ?? string.Empty);
        });
        BindContextButton("btnChubInventoryLock", () =>
            MultiInventoryClient.Request(MultiInventoryService.LockAction, contextInventoryIndex));
        BindContextButton("btnChubInventorySort", () =>
            MultiInventoryClient.Request(MultiInventoryService.SortAction, contextInventoryIndex));
        BindContextButton("btnChubInventoryClearSearch", () =>
        {
            if (searchInput != null) searchInput.Text = string.Empty;
            cHubItemSearch.SetQuery(string.Empty, SearchScope);
        });
        BindContextButton("btnChubInventoryFilterOpen", ShowInventoryFilter);
        BindContextButton("btnChubInventoryFilterApply", () =>
        {
            MultiInventoryClient.Request(MultiInventoryService.SetLootFilterAction,
                contextInventoryIndex, inventoryFilterInput?.Text ?? string.Empty);
            HideInventoryFilter();
        });
        BindContextButton("btnChubInventoryFilterClear", () =>
        {
            if (inventoryFilterInput != null) inventoryFilterInput.Text = string.Empty;
            MultiInventoryClient.Request(MultiInventoryService.SetLootFilterAction,
                contextInventoryIndex, string.Empty);
            HideInventoryFilter();
        });
        BindContextButton("btnChubInventoryFilterClose", HideInventoryFilter);
        BindContextButton("btnChubInventoryContextClose", HideInventoryContext);
        BindContextButton("btnChubSearchPrevious", () => ChangeSearchPage(-1));
        BindContextButton("btnChubSearchNext", () => ChangeSearchPage(1));
        BindContextButton("btnChubLocateItem", LocateSelectedResult);
        BindContextButton("btnChubLocateCancel", HideLocateContext);
        HideInventoryContext();
        HideInventoryFilter();
        SetGlobalSearchVisible(false);
        HideLocateContext();
    }

    private void BindContextButton(string id, Action action)
    {
        XUiC_SimpleButton button = FindController<XUiC_SimpleButton>(this, id);
        if (button != null) button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton == 0 || mouseButton == -1) action();
        };
    }

    private void ShowInventoryContext(int index)
    {
        contextInventoryIndex = index;
        if (inventoryRenameInput != null)
        {
            string[] names = MultiInventoryClient.State?.Names;
            inventoryRenameInput.Text = names != null && index < names.Length
                ? names[index] : "Inventory " + (index + 1);
        }
        if (inventoryContext?.ViewComponent != null) inventoryContext.ViewComponent.IsVisible = true;
        RefreshBindings();
    }

    private void ShowInventoryFilter()
    {
        string[] filters = MultiInventoryClient.State?.LootFilters;
        if (inventoryFilterInput != null)
            inventoryFilterInput.Text = filters != null && contextInventoryIndex < filters.Length
                ? filters[contextInventoryIndex] ?? string.Empty : string.Empty;
        if (inventoryFilterPanel?.ViewComponent != null)
            inventoryFilterPanel.ViewComponent.IsVisible = true;
        HideInventoryContext();
    }

    private void HideInventoryFilter()
    {
        if (inventoryFilterPanel?.ViewComponent != null)
            inventoryFilterPanel.ViewComponent.IsVisible = false;
    }

    private void HideInventoryContext()
    {
        if (inventoryContext?.ViewComponent != null) inventoryContext.ViewComponent.IsVisible = false;
    }

    private static int GetBackpackFingerprint()
    {
        EntityPlayerLocal player = GameManager.Instance?.World?.GetLocalPlayers()?.FirstOrDefault();
        ItemStack[] slots = player?.bag?.GetSlots();
        if (slots == null) return 0;
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < slots.Length; i++)
            {
                ItemStack stack = slots[i];
                if (stack == null || stack.IsEmpty()) continue;
                hash = hash * 31 + i;
                hash = hash * 31 + stack.count;
                hash = hash * 31 + (stack.itemValue?.ItemClass?.GetLocalizedItemName()?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    private void MultiInventoryChanged()
    {
        RefreshBindings();
        MultiInventoryState state = MultiInventoryClient.State;
        bool hasSearch = !string.IsNullOrWhiteSpace(state?.SearchQuery);
        SetGlobalSearchVisible(hasSearch);
        if (hasSearch) searchPage = state.SearchPage;
        if (state != null && state.HighlightSlot >= 0) HighlightBackpackSlot(state.HighlightSlot);
        if (inventoryContext?.ViewComponent != null && inventoryContext.ViewComponent.IsVisible)
            ShowInventoryContext(contextInventoryIndex);
    }

    private void ChangeSearchPage(int direction)
    {
        MultiInventoryState state = MultiInventoryClient.State;
        if (state == null || string.IsNullOrWhiteSpace(state.SearchQuery)) return;
        int page = Math.Max(0, Math.Min(Math.Max(0, state.SearchPages - 1), state.SearchPage + direction));
        MultiInventoryClient.Request(MultiInventoryService.SearchAction, page, state.SearchQuery);
    }

    private void SetGlobalSearchVisible(bool visible)
    {
        if (globalSearchPanel?.ViewComponent != null)
        {
            if (visible && backpackContent?.ViewComponent?.UiTransform != null &&
                globalSearchPanel.ViewComponent.UiTransform != null)
            {
                Transform source = backpackContent.ViewComponent.UiTransform;
                Transform results = globalSearchPanel.ViewComponent.UiTransform;
                results.localPosition = source.localPosition;
                results.localScale = source.localScale;
            }
            globalSearchPanel.ViewComponent.IsVisible = visible;
        }
        if (backpackContent?.ViewComponent != null)
        {
            backpackContent.ViewComponent.IsVisible = !visible;
            if (!visible && backpackContent.ViewComponent.UiTransform != null)
                RestoreTransformPosition(backpackContent.ViewComponent.UiTransform, "InventorySlots");
        }
    }

    private void ShowLocateContext(int resultIndex)
    {
        locateResultIndex = resultIndex;
        if (locateContext?.ViewComponent != null) locateContext.ViewComponent.IsVisible = true;
    }

    private void HideLocateContext()
    {
        locateResultIndex = -1;
        if (locateContext?.ViewComponent != null) locateContext.ViewComponent.IsVisible = false;
    }

    private void LocateSelectedResult()
    {
        MultiInventoryState state = MultiInventoryClient.State;
        int index = locateResultIndex;
        HideLocateContext();
        if (state?.SearchInventories == null || state.SearchSlots == null || index < 0 ||
            index >= state.SearchInventories.Length || state.SearchInventories[index] < 0) return;
        MultiInventoryClient.Request(MultiInventoryService.LocateAction,
            state.SearchInventories[index], state.SearchSlots[index].ToString());
    }

    private void HighlightBackpackSlot(int slotNumber)
    {
        XUiC_ItemStack stack = FindItemStack(this, slotNumber);
        if (stack == null) return;
        if (highlightedStack != null) highlightedStack.IsSelected = false;
        highlightedStack = stack;
        highlightedStack.IsSelected = true;
        highlightTimer = 4f;
        SetGlobalSearchVisible(false);
        if (searchInput != null) searchInput.Text = string.Empty;
    }

    private static XUiC_ItemStack FindItemStack(XUiController root, int slotNumber)
    {
        XUiC_ItemStack own = root as XUiC_ItemStack;
        if (own != null && own.SlotNumber == slotNumber) return own;
        foreach (XUiController child in root.Children)
        {
            XUiC_ItemStack found = FindItemStack(child, slotNumber);
            if (found != null) return found;
        }
        return null;
    }

    private static T FindController<T>(XUiController root, string id) where T : XUiController
    {
        if (root == null) return null;
        if (root.ViewComponent != null && string.Equals(root.ViewComponent.ID, id, StringComparison.OrdinalIgnoreCase))
            return root as T;
        foreach (XUiController child in root.Children)
        {
            T found = FindController<T>(child, id);
            if (found != null) return found;
        }
        return null;
    }
}

public class XUiC_cHubGlobalSearchGrid : XUiC_ItemStackGrid
{
    private static int revision;
    private int appliedRevision = -1;

    public static void NotifyChanged() { revision++; }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (appliedRevision == revision) return;
        appliedRevision = revision;
        SetStacks(MultiInventoryClient.SearchResults);
        ApplyResultVisibility(this);
    }

    public override ItemStack[] GetSlots() => MultiInventoryClient.SearchResults;
    public override void UpdateBackend(ItemStack[] stackList) { }

    private static void ApplyResultVisibility(XUiController root)
    {
        XUiC_ItemStack stack = root as XUiC_ItemStack;
        if (stack?.ViewComponent != null)
            stack.ViewComponent.IsVisible = stack.ItemStack != null && !stack.ItemStack.IsEmpty();
        foreach (XUiController child in root.Children)
            if (child != null) ApplyResultVisibility(child);
    }
}

public class XUiC_cHubGlobalSearchItem : XUiC_ItemStack
{
    public override void Init()
    {
        base.Init();
        OnPress += (sender, mouseButton) =>
        {
            if (mouseButton != 1) return;
            MultiInventoryClient.RequestLocateContext(SlotNumber);
        };
    }
}

internal static class UiLayoutPresetStore
{
    private static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
        "Mods", "cHub", "Data", "ui-layout-presets.json");

    private static Dictionary<string, Dictionary<string, float>> Read()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase);
            return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, float>>>(
                File.ReadAllText(FilePath)) ??
                new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            StartupTerminal.Audit("UI_LAYOUT", "PRESET_READ", "FAILED", ex.Message);
            return new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static string[] GetNames() => Read().Keys.OrderBy(name => name,
        StringComparer.OrdinalIgnoreCase).Take(6).ToArray();

    public static void Save(string name, Dictionary<string, float> values)
    {
        var all = Read();
        all[name] = new Dictionary<string, float>(values, StringComparer.OrdinalIgnoreCase);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        File.WriteAllText(FilePath, JsonConvert.SerializeObject(all, Formatting.Indented));
        StartupTerminal.Audit("UI_LAYOUT", "PRESET_SAVE", "OK", "name=" + name);
    }

    public static Dictionary<string, float> Load(string name)
    {
        var all = Read();
        StartupTerminal.Audit("UI_LAYOUT", "PRESET_LOAD", all.ContainsKey(name) ? "OK" : "FAILED",
            "name=" + name);
        return all.TryGetValue(name, out Dictionary<string, float> values) ? values : null;
    }

    public static void Delete(string name)
    {
        var all = Read();
        if (!all.Remove(name)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        File.WriteAllText(FilePath, JsonConvert.SerializeObject(all, Formatting.Indented));
        StartupTerminal.Audit("UI_LAYOUT", "PRESET_DELETE", "OK", "name=" + name);
    }
}
