using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace COR.Scripts;

public static class QuickSaveLoad
{
    public static void QuickLoad()
    {
        TaskHelper.RunSafely(QuickLoadAsync());
    }

    private static async Task QuickLoadAsync()
    {
        try
        {
            ReadSaveResult<SerializableRun> result = SaveManager.Instance.LoadRunSave();
            if (!result.Success || result.SaveData == null)
            {
                Log.Error($"Card Order quick restart failed: could not read autosave. Status={result.Status}");
                return;
            }

            SerializableRun serializableRun = result.SaveData;
            RunState runState = RunState.FromSerializable(serializableRun);

            CardOrderRecorder.StartQuickRestart(CombatManager.Instance.DebugOnlyGetState());
            RunManager.Instance.ActionQueueSet.Reset();
            NRunMusicController.Instance?.StopMusic();

            await NGame.Instance!.Transition.FadeOut();
            RunManager.Instance.CleanUp();

            await RunManager.Instance.SetUpSavedSinglePlayer(runState, serializableRun);
            NGame.Instance.ReactionContainer.InitializeNetworking(new NetSingleplayerGameService());
            await NGame.Instance.LoadRun(runState, serializableRun.PreFinishedRoom);
            await NGame.Instance.Transition.FadeIn();

            Log.Info("Card Order quick restart completed.");
        }
        catch (Exception ex)
        {
            Log.Error($"Card Order quick restart failed: {ex}");
        }
        finally
        {
            CardOrderRecorder.FinishQuickRestart();
        }
    }
}
