using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace COR.Scripts;

[HarmonyPatch(typeof(NPauseMenu), nameof(NPauseMenu._Ready))]
public static class PauseMenuPatch
{
    [HarmonyPostfix]
    public static void Postfix(NPauseMenu __instance)
    {
        try
        {
            Control buttonContainer = __instance.GetNode<Control>("%ButtonContainer");
            NPauseMenuButton saveAndQuitButton = buttonContainer.GetNode<NPauseMenuButton>("SaveAndQuit");
            NPauseMenuButton giveUpButton = buttonContainer.GetNode<NPauseMenuButton>("GiveUp");
            int giveUpIndex = giveUpButton.GetIndex();
            int cardOrderIndex = giveUpIndex;

            if (RunManager.Instance.NetService.Type == NetGameType.Singleplayer)
            {
                NPauseMenuButton quickRestartButton = (NPauseMenuButton)saveAndQuitButton.Duplicate();
                quickRestartButton.Name = "QuickRestart";
                MakeButtonVisualsUnique(quickRestartButton);
                quickRestartButton.GetNode<MegaLabel>("Label").SetTextAutoSize("Quick Restart");

                buttonContainer.AddChild(quickRestartButton);
                buttonContainer.MoveChild(quickRestartButton, giveUpIndex);

                quickRestartButton.Connect(
                    NClickableControl.SignalName.Released,
                    Callable.From<NButton>(OnQuickRestartPressed)
                );

                if (!SaveManager.Instance.HasRunSave)
                    quickRestartButton.Disable();

                cardOrderIndex = giveUpIndex + 1;
            }

            NPauseMenuButton cardOrderButton = (NPauseMenuButton)saveAndQuitButton.Duplicate();
            cardOrderButton.Name = "CardOrder";
            MakeButtonVisualsUnique(cardOrderButton);
            cardOrderButton.GetNode<MegaLabel>("Label").SetTextAutoSize("Card Order");

            buttonContainer.AddChild(cardOrderButton);
            buttonContainer.MoveChild(cardOrderButton, cardOrderIndex);

            cardOrderButton.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => OnCardOrderPressed())
            );

            RebuildFocusNeighbors(buttonContainer);
            Log.Info("Card Order button added to pause menu.");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to add Card Order button: {ex}");
        }
    }

    private static void OnCardOrderPressed()
    {
        Log.Info("Card Order button pressed.");
        CardOrderScreen.Open();
    }

    private static void OnQuickRestartPressed(NButton button)
    {
        button.Disable();
        Log.Info("Card Order quick restart button pressed.");
        QuickSaveLoad.QuickLoad();
    }

    private static void MakeButtonVisualsUnique(NPauseMenuButton cardOrderButton)
    {
        TextureRect buttonImage = cardOrderButton.GetNode<TextureRect>("ButtonImage");
        if (buttonImage.Material is not ShaderMaterial material)
            return;

        cardOrderButton.GetNode<TextureRect>("ButtonImage").Material = (ShaderMaterial)material.Duplicate();
    }

    private static void RebuildFocusNeighbors(Control buttonContainer)
    {
        List<NPauseMenuButton> buttons = [];
        foreach (Node child in buttonContainer.GetChildren())
        {
            if (child is NPauseMenuButton { Visible: true } button)
                buttons.Add(button);
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            NPauseMenuButton button = buttons[i];
            button.FocusNeighborLeft = button.GetPath();
            button.FocusNeighborRight = button.GetPath();
            button.FocusNeighborTop = i > 0 ? buttons[i - 1].GetPath() : button.GetPath();
            button.FocusNeighborBottom = i < buttons.Count - 1 ? buttons[i + 1].GetPath() : button.GetPath();
        }
    }
}
