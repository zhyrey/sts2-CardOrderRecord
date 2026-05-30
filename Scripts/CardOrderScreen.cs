using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace COR.Scripts;

public partial class CardOrderScreen : Control, ICapstoneScreen
{
    private const float CompactCardEntryWidth = 150f;
    private const float HistoryButtonWidth = 260f;
    private const float HistoryButtonHeight = 64f;

    private sealed record CardOrderEventGroup(CardOrderDisplayEvent Header, List<CardOrderDisplayEvent> Events);

    private readonly List<CardModel> _cards = [];
    private VBoxContainer _content = null!;
    private VBoxContainer _historyList = null!;
    private NBackButton _backButton = null!;
    private NPauseMenuButton _historyButton = null!;
    private bool _historyListVisible;
    private int? _selectedHistoryId;

    public NetScreenType ScreenType => NetScreenType.None;

    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => _backButton;

    public static void Open()
    {
        CardOrderScreen screen = new();

        if (NModalContainer.Instance != null)
        {
            NModalContainer.Instance.Add(screen);
            return;
        }

        NCapstoneContainer.Instance?.Open(screen);
    }

    public override void _Ready()
    {
        BuildLayout();
        Populate();
        _backButton.CallDeferred(Control.MethodName.GrabFocus);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed(MegaInput.cancel) || inputEvent.IsActionPressed(MegaInput.pauseAndBack))
        {
            GetViewport().SetInputAsHandled();
            Close();
        }
    }

    public void AfterCapstoneOpened()
    {
        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;
        _backButton.GrabFocus();
    }

    public void AfterCapstoneClosed()
    {
        Visible = false;
        this.QueueFreeSafely();
    }

    private void BuildLayout()
    {
        Name = "CardOrderScreen";
        MouseFilter = MouseFilterEnum.Stop;
        AnchorRight = 1f;
        AnchorBottom = 1f;

        Font? titleFont = ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres");
        ColorRect background = new()
        {
            Color = new Color(0f, 0f, 0f, 0.72f),
            MouseFilter = MouseFilterEnum.Stop,
            AnchorRight = 1f,
            AnchorBottom = 1f
        };
        this.AddChildSafely(background);

        MarginContainer margin = new()
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = MouseFilterEnum.Ignore
        };
        margin.AddThemeConstantOverride("margin_left", 250);
        margin.AddThemeConstantOverride("margin_top", 44);
        margin.AddThemeConstantOverride("margin_right", 250);
        margin.AddThemeConstantOverride("margin_bottom", 56);
        this.AddChildSafely(margin);

        VBoxContainer root = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.AddThemeConstantOverride("separation", 18);
        margin.AddChildSafely(root);

        HBoxContainer header = new();
        root.AddChildSafely(header);

        MegaLabel title = new()
        {
            Text = "Card Order",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        title.AddThemeColorOverride("font_color", new Color(1f, 0.964706f, 0.886275f));
        title.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
        title.AddThemeConstantOverride("shadow_offset_x", 4);
        title.AddThemeConstantOverride("shadow_offset_y", 3);
        title.AddThemeFontSizeOverride("font_size", 42);
        if (titleFont != null)
            title.AddThemeFontOverride("font", titleFont);
        header.AddChildSafely(title);

        PackedScene backButtonScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/back_button.tscn");
        _backButton = backButtonScene.Instantiate<NBackButton>(PackedScene.GenEditState.Disabled);
        _backButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => Close()));
        this.AddChildSafely(_backButton);
        _backButton.CallDeferred(NButton.MethodName.Enable);

        HBoxContainer body = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        body.AddThemeConstantOverride("separation", 24);
        root.AddChildSafely(body);

        ScrollContainer scroll = new()
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Stop,
            FollowFocus = true
        };
        body.AddChildSafely(scroll);

        _content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _content.AddThemeConstantOverride("separation", 18);
        scroll.AddChildSafely(_content);

        VBoxContainer historySidebar = new()
        {
            CustomMinimumSize = new Vector2(HistoryButtonWidth, 1f),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        historySidebar.AddThemeConstantOverride("separation", 8);
        body.AddChildSafely(historySidebar);

        _historyButton = CreateHistoryToggleButton("History");
        _historyButton.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ => ToggleHistoryList()));
        historySidebar.AddChildSafely(_historyButton);

        ScrollContainer historyScroll = new()
        {
            Visible = false,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Stop,
            FollowFocus = true
        };
        historySidebar.AddChildSafely(historyScroll);

        _historyList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _historyList.AddThemeConstantOverride("separation", 2);
        historyScroll.AddChildSafely(_historyList);
    }

    private void Populate()
    {
        _content.FreeChildren();
        _cards.Clear();

        if (_selectedHistoryId != null)
        {
            CardOrderHistoryEntry? selected = CardOrderRecorder.History
                .FirstOrDefault(entry => entry.Id == _selectedHistoryId.Value);
            if (selected != null)
            {
                _content.AddChildSafely(CreateHeaderLabel($"History {selected.Id}"));
                PopulateRecordSet(selected.Events);
                RefreshHistoryList();
                return;
            }

            _selectedHistoryId = null;
        }

        IReadOnlyList<CardOrderDisplayEvent> records = CardOrderRecorder.CurrentDisplayRecords();
        if (records.Count == 0)
        {
            _content.AddChildSafely(CreateHeaderLabel("No card events have been recorded in this combat."));
            RefreshHistoryList();
            return;
        }

        PopulateRecordSet(records);
        RefreshHistoryList();
    }

    private void RefreshHistoryList()
    {
        SetPauseMenuButtonLabel(_historyButton, _historyListVisible ? "Hide" : "History");

        _historyList.GetParent<Control>().Visible = _historyListVisible;
        _historyList.FreeChildren();

        if (!_historyListVisible)
            return;

        NPauseMenuButton currentButton = CreateHistoryListButton("Current");
        currentButton.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ =>
        {
            _selectedHistoryId = null;
            Populate();
        }));
        _historyList.AddChildSafely(currentButton);
        ApplyHistoryButtonSelection(currentButton, _selectedHistoryId == null);

        IReadOnlyList<CardOrderHistoryEntry> history = CardOrderRecorder.History;
        if (history.Count == 0)
        {
            _historyList.AddChildSafely(CreateSideLabel("No history"));
            return;
        }

        foreach (CardOrderHistoryEntry entry in history)
        {
            int historyId = entry.Id;
            NPauseMenuButton historyEntryButton = CreateHistoryListButton($"History {historyId}");
            historyEntryButton.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ =>
            {
                _selectedHistoryId = historyId;
                Populate();
            }));
            _historyList.AddChildSafely(historyEntryButton);
            ApplyHistoryButtonSelection(historyEntryButton, _selectedHistoryId == historyId);
        }
    }

    private void PopulateRecordSet(IReadOnlyList<CardOrderDisplayEvent> records)
    {
        foreach (IGrouping<int, CardOrderDisplayEvent> round in records.GroupBy(record => record.RoundNumber))
        {
            _content.AddChildSafely(CreateHeaderLabel($"Turn {round.Key}"));

            VBoxContainer eventContainer = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore
            };
            eventContainer.AddThemeConstantOverride("separation", 6);
            _content.AddChildSafely(eventContainer);

            foreach (CardOrderEventGroup group in GroupEvents(round.OrderBy(record => record.Sequence)))
                eventContainer.AddChildSafely(CreateEventRow(group));
        }
    }

    private static List<CardOrderEventGroup> GroupEvents(IEnumerable<CardOrderDisplayEvent> records)
    {
        List<CardOrderEventGroup> groups = [];

        foreach (CardOrderDisplayEvent record in records)
        {
            CardOrderEventGroup? previous = groups.LastOrDefault();
            if (CanGroup(previous, record))
            {
                previous!.Events.Add(record);
                continue;
            }

            groups.Add(new CardOrderEventGroup(record, [record]));
        }

        return groups;
    }

    private static bool CanGroup(CardOrderEventGroup? group, CardOrderDisplayEvent record)
    {
        if (group == null || record.Type == CardOrderEventType.Played)
            return false;

        CardOrderDisplayEvent header = group.Header;
        if (header.Type == CardOrderEventType.Played)
            return false;

        return header.Type == record.Type
            && header.FromHandDraw == record.FromHandDraw
            && header.DestinationPile == record.DestinationPile
            && header.SourceKind == record.SourceKind
            && header.SourceName == record.SourceName;
    }

    private Control CreateEventRow(CardOrderEventGroup group)
    {
        CardOrderDisplayEvent record = group.Header;
        HBoxContainer row = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddThemeConstantOverride("separation", 10);

        if (record.Type != CardOrderEventType.Played)
        {
            Control indent = new()
            {
                CustomMinimumSize = new Vector2(32f, 1f),
                MouseFilter = MouseFilterEnum.Ignore
            };
            row.AddChildSafely(indent);

            row.AddChildSafely(CreateSourceLabel(record.SourceName, record.SourceKind));
        }

        row.AddChildSafely(CreateEventBadge(record));

        HFlowContainer cardContainer = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        cardContainer.AddThemeConstantOverride("h_separation", 0);
        cardContainer.AddThemeConstantOverride("v_separation", 4);
        row.AddChildSafely(cardContainer);

        foreach (CardOrderDisplayEvent groupedRecord in group.Events)
            cardContainer.AddChildSafely(CreateCardEntry(groupedRecord.Card));

        return row;
    }

    private NDeckHistoryEntry CreateCardEntry(CardModel card)
    {
        int index = _cards.Count;
        _cards.Add(card);

        NDeckHistoryEntry entry = NDeckHistoryEntry.Create(card, 1);
        ApplyCompactCardEntryLayout(entry);
        entry.Connect(
            NDeckHistoryEntry.SignalName.Clicked,
            Callable.From<NDeckHistoryEntry>(_ => ShowCard(index))
        );
        return entry;
    }

    private static void ApplyCompactCardEntryLayout(NDeckHistoryEntry entry)
    {
        entry.CustomMinimumSize = new Vector2(CompactCardEntryWidth, 32f);
        entry.Size = new Vector2(CompactCardEntryWidth, 32f);
        entry.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        entry.SizeFlagsVertical = SizeFlags.ShrinkBegin;

        MarginContainer? labelContainer = entry.GetNodeOrNull<MarginContainer>("%MarginContainer");
        if (labelContainer != null)
            labelContainer.OffsetRight = CompactCardEntryWidth;
    }

    private Label CreateSourceLabel(string sourceName, CardOrderSourceKind sourceKind)
    {
        Font? font = ResourceLoader.Load<Font>("res://themes/kreon_regular_shared.tres");
        Label label = new()
        {
            Text = sourceName,
            CustomMinimumSize = new Vector2(230f, 32f),
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
        };
        label.AddThemeColorOverride("font_color", GetSourceColor(sourceKind));
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        label.AddThemeFontSizeOverride("font_size", 20);
        if (font != null)
            label.AddThemeFontOverride("font", font);
        return label;
    }

    private Label CreateEventBadge(CardOrderDisplayEvent record)
    {
        Font? font = ResourceLoader.Load<Font>("res://themes/kreon_regular_shared.tres");
        Label label = new()
        {
            Text = GetEventLabel(record),
            CustomMinimumSize = new Vector2(108f, 32f),
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        label.AddThemeColorOverride("font_color", GetEventColor(record.Type));
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        label.AddThemeFontSizeOverride("font_size", record.Type == CardOrderEventType.Played ? 24 : 20);
        if (font != null)
            label.AddThemeFontOverride("font", font);
        return label;
    }

    private static string GetEventLabel(CardOrderDisplayEvent record)
    {
        return record.Type switch
        {
            CardOrderEventType.Played => "Played",
            CardOrderEventType.Drew => "Drew",
            CardOrderEventType.Discarded => "Discarded",
            CardOrderEventType.Exhausted => "Exhausted",
            CardOrderEventType.Generated => "Generated",
            CardOrderEventType.SlyApplied => "Sly",
            CardOrderEventType.Retained => "Retained",
            CardOrderEventType.Enchanted => "Enchanted",
            CardOrderEventType.CostChanged => "Cost",
            CardOrderEventType.Transformed => "Transformed",
            CardOrderEventType.Moved => GetMoveLabel(record.DestinationPile),
            CardOrderEventType.Upgraded => "Upgraded",
            CardOrderEventType.Downgraded => "Downgraded",
            CardOrderEventType.Afflicted => "Afflicted",
            CardOrderEventType.KeywordApplied => "Keyword+",
            CardOrderEventType.KeywordRemoved => "Keyword-",
            CardOrderEventType.AfflictionCleared => "Afflict-",
            CardOrderEventType.EnchantmentCleared => "Enchant-",
            _ => record.Type.ToString()
        };
    }

    private static string GetMoveLabel(PileType? destinationPile)
    {
        return destinationPile switch
        {
            PileType.Hand => "To Hand",
            PileType.Draw => "To Draw",
            PileType.Discard => "To Discard",
            PileType.Exhaust => "To Exhaust",
            PileType.Deck => "To Deck",
            _ => "Moved"
        };
    }

    private static Color GetSourceColor(CardOrderSourceKind sourceKind)
    {
        return sourceKind switch
        {
            CardOrderSourceKind.Card => new Color(1f, 0.839216f, 0.211765f),
            CardOrderSourceKind.Power => new Color(0.65f, 0.95f, 1f),
            CardOrderSourceKind.Enchantment => new Color(0.75f, 0.65f, 1f),
            CardOrderSourceKind.System => new Color(0.8f, 0.8f, 0.8f),
            _ => new Color(1f, 0.964706f, 0.886275f)
        };
    }

    private static Color GetEventColor(CardOrderEventType type)
    {
        return type switch
        {
            CardOrderEventType.Played => new Color(1f, 0.839216f, 0.211765f),
            CardOrderEventType.Drew => new Color(0.392157f, 1f, 1f),
            CardOrderEventType.Discarded => new Color(1f, 0.964706f, 0.886275f),
            CardOrderEventType.Exhausted => new Color(0.901961f, 0.411765f, 1f),
            CardOrderEventType.Generated => new Color(0.384314f, 1f, 0.454902f),
            CardOrderEventType.SlyApplied => new Color(0.55f, 0.75f, 1f),
            CardOrderEventType.Retained => new Color(1f, 0.62f, 0.28f),
            CardOrderEventType.Enchanted => new Color(0.75f, 0.65f, 1f),
            CardOrderEventType.CostChanged => new Color(0.45f, 1f, 0.72f),
            CardOrderEventType.Transformed => new Color(1f, 0.55f, 0.38f),
            CardOrderEventType.Moved => new Color(0.85f, 0.92f, 1f),
            CardOrderEventType.Upgraded => new Color(1f, 0.9f, 0.38f),
            CardOrderEventType.Downgraded => new Color(0.95f, 0.58f, 0.38f),
            CardOrderEventType.Afflicted => new Color(0.95f, 0.45f, 0.55f),
            CardOrderEventType.KeywordApplied => new Color(0.7f, 0.9f, 1f),
            CardOrderEventType.KeywordRemoved => new Color(0.9f, 0.72f, 0.58f),
            CardOrderEventType.AfflictionCleared => new Color(0.72f, 1f, 0.72f),
            CardOrderEventType.EnchantmentCleared => new Color(0.9f, 0.72f, 1f),
            _ => Colors.White
        };
    }

    private Label CreateHeaderLabel(string text)
    {
        Font? font = ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres");
        Label label = new()
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeColorOverride("font_color", new Color(1f, 0.839216f, 0.211765f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
        label.AddThemeConstantOverride("shadow_offset_x", 3);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        label.AddThemeFontSizeOverride("font_size", 28);
        if (font != null)
            label.AddThemeFontOverride("font", font);
        return label;
    }

    private NPauseMenuButton CreateHistoryToggleButton(string text)
    {
        PackedScene buttonScene = ResourceLoader.Load<PackedScene>("res://scenes/pause_menu/pause_menu_button.tscn");
        NPauseMenuButton button = buttonScene.Instantiate<NPauseMenuButton>(PackedScene.GenEditState.Disabled);
        button.CustomMinimumSize = new Vector2(HistoryButtonWidth, HistoryButtonHeight);
        button.FocusMode = FocusModeEnum.All;
        button.MouseFilter = MouseFilterEnum.Stop;
        MakePauseMenuButtonVisualsUnique(button);
        SetPauseMenuButtonLabel(button, text);
        return button;
    }

    private NPauseMenuButton CreateHistoryListButton(string text)
    {
        NPauseMenuButton button = CreateHistoryToggleButton(text);
        button.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        return button;
    }

    private static void ApplyHistoryButtonSelection(NPauseMenuButton button, bool selected)
    {
        MegaLabel? label = button.GetNodeOrNull<MegaLabel>("Label");
        if (label == null)
            return;

        label.AddThemeColorOverride(
            "font_color",
            selected ? new Color(1f, 0.839216f, 0.211765f) : new Color(1f, 0.964706f, 0.886275f)
        );
    }

    private static void SetPauseMenuButtonLabel(NPauseMenuButton button, string text)
    {
        MegaLabel? label = button.GetNodeOrNull<MegaLabel>("Label");
        if (label != null)
            label.SetTextAutoSize(text);
    }

    private static void MakePauseMenuButtonVisualsUnique(NPauseMenuButton button)
    {
        TextureRect? buttonImage = button.GetNodeOrNull<TextureRect>("ButtonImage");
        if (buttonImage?.Material is ShaderMaterial material)
            buttonImage.Material = (ShaderMaterial)material.Duplicate();
    }

    private Label CreateSideLabel(string text)
    {
        Font? font = ResourceLoader.Load<Font>("res://themes/kreon_regular_shared.tres");
        Label label = new()
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeColorOverride("font_color", new Color(1f, 0.964706f, 0.886275f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        label.AddThemeFontSizeOverride("font_size", 18);
        if (font != null)
            label.AddThemeFontOverride("font", font);
        return label;
    }

    private void ToggleHistoryList()
    {
        _historyListVisible = !_historyListVisible;
        RefreshHistoryList();
    }

    private void ShowCard(int index)
    {
        NInspectCardScreen? inspectCardScreen = NGame.Instance?.GetInspectCardScreen();
        if (inspectCardScreen == null)
            return;

        inspectCardScreen.GetParent<Control>()?.MoveToFrontSafely();
        inspectCardScreen.Open(_cards, index);
    }

    private void Close()
    {
        if (NModalContainer.Instance?.OpenModal == this)
        {
            NModalContainer.Instance.Clear();
            return;
        }

        NCapstoneContainer.Instance?.Close();
    }
}
