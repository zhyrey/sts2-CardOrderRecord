using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
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

        IReadOnlyList<PlayedCardRecord> records = CardOrderRecorder.Records;
        if (records.Count == 0)
        {
            _content.AddChildSafely(CreateHeaderLabel("No cards have been played in this combat."));
            return;
        }

        foreach (IGrouping<int, PlayedCardRecord> round in records.GroupBy(record => record.RoundNumber))
        {
            _content.AddChildSafely(CreateHeaderLabel($"Turn {round.Key}"));

            HFlowContainer cardContainer = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore
            };
            cardContainer.AddThemeConstantOverride("h_separation", 16);
            cardContainer.AddThemeConstantOverride("v_separation", 8);
            _content.AddChildSafely(cardContainer);

            foreach (PlayedCardRecord record in round)
            {
                int index = _cards.Count;
                _cards.Add(record.Card);

                NDeckHistoryEntry entry = NDeckHistoryEntry.Create(record.Card, 1);
                entry.Connect(
                    NDeckHistoryEntry.SignalName.Clicked,
                    Callable.From<NDeckHistoryEntry>(_ => ShowCard(index))
                );
                cardContainer.AddChildSafely(entry);
            }
        }
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
