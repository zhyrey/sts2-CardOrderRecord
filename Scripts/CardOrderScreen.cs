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
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace COR.Scripts;

public partial class CardOrderScreen : Control, ICapstoneScreen
{
    private const float CompactCardEntryWidth = 150f;

    private sealed record CardOrderEventGroup(CardOrderEvent Header, List<CardOrderEvent> Events);

    private readonly List<CardModel> _cards = [];
    private VBoxContainer _content = null!;
    private NBackButton _backButton = null!;

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

        ScrollContainer scroll = new()
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Stop,
            FollowFocus = true
        };
        root.AddChildSafely(scroll);

        _content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _content.AddThemeConstantOverride("separation", 18);
        scroll.AddChildSafely(_content);
    }

    private void Populate()
    {
        _content.FreeChildren();
        _cards.Clear();

        IReadOnlyList<CardOrderEvent> records = CardOrderRecorder.Records;
        if (records.Count == 0)
        {
            _content.AddChildSafely(CreateHeaderLabel("No card events have been recorded in this combat."));
            return;
        }

        foreach (IGrouping<int, CardOrderEvent> round in records.GroupBy(record => record.RoundNumber))
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

    private static List<CardOrderEventGroup> GroupEvents(IEnumerable<CardOrderEvent> records)
    {
        List<CardOrderEventGroup> groups = [];

        foreach (CardOrderEvent record in records)
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

    private static bool CanGroup(CardOrderEventGroup? group, CardOrderEvent record)
    {
        if (group == null || record.Type == CardOrderEventType.Played)
            return false;

        CardOrderEvent header = group.Header;
        if (header.Type == CardOrderEventType.Played)
            return false;

        return header.Type == record.Type
            && header.FromHandDraw == record.FromHandDraw
            && header.DestinationPile == record.DestinationPile
            && SameSource(header.Source, record.Source);
    }

    private static bool SameSource(AbstractModel? left, AbstractModel? right)
    {
        if (left == null || right == null)
            return left == right;

        return left.Id.Equals(right.Id);
    }

    private Control CreateEventRow(CardOrderEventGroup group)
    {
        CardOrderEvent record = group.Header;
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

            row.AddChildSafely(CreateSourceLabel(record.Source));
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

        foreach (CardOrderEvent groupedRecord in group.Events)
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

    private Label CreateSourceLabel(AbstractModel? source)
    {
        Font? font = ResourceLoader.Load<Font>("res://themes/kreon_regular_shared.tres");
        Label label = new()
        {
            Text = GetSourceName(source),
            CustomMinimumSize = new Vector2(230f, 32f),
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
        };
        label.AddThemeColorOverride("font_color", GetSourceColor(source));
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        label.AddThemeFontSizeOverride("font_size", 20);
        if (font != null)
            label.AddThemeFontOverride("font", font);
        return label;
    }

    private Label CreateEventBadge(CardOrderEvent record)
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

    private static string GetEventLabel(CardOrderEvent record)
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

    private static string GetSourceName(AbstractModel? source)
    {
        return source switch
        {
            CardModel card => card.Title,
            PowerModel power => power.Title.GetFormattedText(),
            EnchantmentModel enchantment => enchantment.Title.GetFormattedText(),
            null => "System",
            _ => source.Id.Entry
        };
    }

    private static Color GetSourceColor(AbstractModel? source)
    {
        return source switch
        {
            CardModel => new Color(1f, 0.839216f, 0.211765f),
            PowerModel => new Color(0.65f, 0.95f, 1f),
            EnchantmentModel => new Color(0.75f, 0.65f, 1f),
            null => new Color(0.8f, 0.8f, 0.8f),
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
