using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;

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

            NPauseMenuButton cardOrderButton = (NPauseMenuButton)saveAndQuitButton.Duplicate();
            cardOrderButton.Name = "CardOrder";
            MakeButtonVisualsUnique(cardOrderButton);
            cardOrderButton.GetNode<MegaLabel>("Label").SetTextAutoSize("Card Order");

            NPauseMenuButton giveUpButton = buttonContainer.GetNode<NPauseMenuButton>("GiveUp");
            int giveUpIndex = giveUpButton.GetIndex();
            buttonContainer.AddChild(cardOrderButton);
            buttonContainer.MoveChild(cardOrderButton, giveUpIndex);

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
