using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace COR.Scripts;

public sealed record PlayedCardRecord(int RoundNumber, CardModel Card);

public static class CardOrderRecorder
{
    private static readonly List<PlayedCardRecord> PlayedCards = [];

    public static IReadOnlyList<PlayedCardRecord> Records => PlayedCards;

    public static void Clear()
    {
        PlayedCards.Clear();
    }

    public static void Record(ICombatState combatState, CardPlay cardPlay)
    {
        PlayedCards.Add(new PlayedCardRecord(combatState.RoundNumber, cardPlay.Card));
    }
}

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayStarted))]
public static class CardPlayStartedPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState combatState, CardPlay cardPlay)
    {
        CardOrderRecorder.Record(combatState, cardPlay);
    }
}

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.Clear))]
public static class CombatHistoryClearPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        CardOrderRecorder.Clear();
    }
}
