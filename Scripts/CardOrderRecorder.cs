using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace COR.Scripts;

public enum CardOrderEventType
{
    Played,
    Drew,
    Discarded,
    Exhausted,
    Generated,
    SlyApplied,
    Retained,
    Enchanted,
    CostChanged,
    Transformed,
    Moved,
    Upgraded,
    Downgraded,
    Afflicted,
    KeywordApplied,
    KeywordRemoved,
    AfflictionCleared,
    EnchantmentCleared
}

public sealed record CardOrderEvent(
    int Sequence,
    int RoundNumber,
    CardOrderEventType Type,
    CardModel Card,
    AbstractModel? Source = null,
    bool FromHandDraw = false,
    Player? Creator = null,
    PileType? DestinationPile = null
);

public static class CardOrderRecorder
{
    private static readonly List<CardOrderEvent> Events = [];
    private static readonly Dictionary<CardModel, AbstractModel> SelectionSources = [];
    private static int _sequence;
    private static CardModel? _activePlayedCard;

    public static IReadOnlyList<CardOrderEvent> Records => Events;
    public static AbstractModel? CurrentSource => _activePlayedCard;

    public static void Clear()
    {
        Events.Clear();
        SelectionSources.Clear();
        _sequence = 0;
        _activePlayedCard = null;
    }

    public static void RecordPlayed(ICombatState combatState, CardPlay cardPlay)
    {
        _activePlayedCard = cardPlay.Card;
        Add(combatState, CardOrderEventType.Played, cardPlay.Card, cardPlay.Card);
    }

    public static void FinishPlayed(CardPlay cardPlay)
    {
        if (_activePlayedCard == cardPlay.Card)
            _activePlayedCard = null;
    }

    public static void RecordDrew(ICombatState combatState, CardModel card, bool fromHandDraw)
    {
        Add(combatState, CardOrderEventType.Drew, card, ResolveSource(card), fromHandDraw);
    }

    public static void RecordDiscarded(ICombatState combatState, CardModel card)
    {
        Add(combatState, CardOrderEventType.Discarded, card, ResolveSource(card));
    }

    public static void RecordExhausted(ICombatState combatState, CardModel card)
    {
        Add(combatState, CardOrderEventType.Exhausted, card, ResolveSource(card));
    }

    public static void RecordGenerated(ICombatState combatState, CardModel card, Player? creator)
    {
        AbstractModel? source = ResolveSource(card);
        if (ShouldSuppressGenerated(card, source))
            return;

        Add(combatState, CardOrderEventType.Generated, card, source, creator: creator);
    }

    public static void RecordSlyApplied(CardModel card)
    {
        if (!IsInPile(card))
            return;

        ICombatState? combatState = card.CombatState ?? card.Owner?.Creature.CombatState;
        if (combatState == null)
            return;

        Add(combatState, CardOrderEventType.SlyApplied, card, ResolveSource(card));
    }

    public static void RecordRetained(CardModel card)
    {
        if (!IsInPile(card))
            return;

        ICombatState? combatState = card.CombatState ?? card.Owner?.Creature.CombatState;
        if (combatState == null)
            return;

        Add(combatState, CardOrderEventType.Retained, card, ResolveSource(card));
    }

    public static void RecordEnchanted(CardModel card)
    {
        if (!IsInPile(card))
            return;

        ICombatState? combatState = card.CombatState ?? card.Owner?.Creature.CombatState;
        if (combatState == null)
            return;

        Add(combatState, CardOrderEventType.Enchanted, card, ResolveSource(card));
    }

    public static void RecordCostChanged(CardModel card)
    {
        if (!IsInPile(card))
            return;

        ICombatState? combatState = card.CombatState ?? card.Owner?.Creature.CombatState;
        if (combatState == null)
            return;

        Add(combatState, CardOrderEventType.CostChanged, card, ResolveSource(card));
    }

    public static void RecordCostChanged(CardEnergyCost energyCost)
    {
        if (EnergyCostCardField.GetValue(energyCost) is CardModel card)
            RecordCostChanged(card);
    }

    public static void RecordMovedToPile(ICombatState combatState, CardModel card, PileType? oldPile, PileType destinationPile)
    {
        bool hasSelectionSource = SelectionSources.ContainsKey(card);
        if (!ShouldRecordPileMove(card, oldPile, destinationPile, hasSelectionSource))
            return;

        AbstractModel? source = hasSelectionSource ? ResolveSource(card) : _activePlayedCard;
        if (source == null)
            return;

        Add(combatState, CardOrderEventType.Moved, card, source, destinationPile: destinationPile);
    }

    public static void RecordTransformed(ICombatState combatState, CardModel original)
    {
        Add(combatState, CardOrderEventType.Transformed, original, ResolveSource(original));
    }

    public static void RecordUpgraded(CardModel card)
    {
        if (!IsInPile(card))
            return;

        ICombatState? combatState = card.CombatState ?? card.Owner?.Creature.CombatState;
        if (combatState == null)
            return;

        Add(combatState, CardOrderEventType.Upgraded, card, ResolveSource(card));
    }

    public static void RecordDowngraded(CardModel card)
    {
        if (!IsInPile(card))
            return;

        ICombatState? combatState = card.CombatState ?? card.Owner?.Creature.CombatState;
        if (combatState == null)
            return;

        Add(combatState, CardOrderEventType.Downgraded, card, ResolveSource(card));
    }

    public static void RecordAfflicted(ICombatState combatState, CardModel card)
    {
        Add(combatState, CardOrderEventType.Afflicted, card, ResolveSource(card));
    }

    public static void RecordKeywordApplied(CardModel card)
    {
        if (!IsInPile(card))
            return;

        ICombatState? combatState = card.CombatState ?? card.Owner?.Creature.CombatState;
        if (combatState == null)
            return;

        Add(combatState, CardOrderEventType.KeywordApplied, card, ResolveSource(card));
    }

    public static void RecordKeywordRemoved(CardModel card)
    {
        if (!IsInPile(card))
            return;

        ICombatState? combatState = card.CombatState ?? card.Owner?.Creature.CombatState;
        if (combatState == null)
            return;

        Add(combatState, CardOrderEventType.KeywordRemoved, card, ResolveSource(card));
    }

    public static void RecordAfflictionCleared(CardModel card)
    {
        if (!IsInPile(card))
            return;

        ICombatState? combatState = card.CombatState ?? card.Owner?.Creature.CombatState;
        if (combatState == null)
            return;

        Add(combatState, CardOrderEventType.AfflictionCleared, card, ResolveSource(card));
    }

    public static void RecordEnchantmentCleared(CardModel card)
    {
        if (!IsInPile(card))
            return;

        ICombatState? combatState = card.CombatState ?? card.Owner?.Creature.CombatState;
        if (combatState == null)
            return;

        Add(combatState, CardOrderEventType.EnchantmentCleared, card, ResolveSource(card));
    }

    public static void RememberSelectionSource(IEnumerable<CardModel> selectedCards, AbstractModel source)
    {
        foreach (CardModel card in selectedCards)
            SelectionSources[card] = source;
    }

    private static void Add(
        ICombatState combatState,
        CardOrderEventType type,
        CardModel card,
        AbstractModel? source = null,
        bool fromHandDraw = false,
        Player? creator = null,
        PileType? destinationPile = null
    )
    {
        Events.Add(new CardOrderEvent(
            _sequence++,
            combatState.RoundNumber,
            type,
            card,
            source,
            fromHandDraw,
            creator,
            destinationPile
        ));
    }

    private static AbstractModel? ResolveSource(CardModel card)
    {
        if (SelectionSources.Remove(card, out AbstractModel? source))
            return source;

        return _activePlayedCard;
    }

    private static bool ShouldSuppressGenerated(CardModel card, AbstractModel? source)
    {
        return card.Id.Entry == "SOUL"
            && source is CardModel sourceCard
            && sourceCard.Id.Entry == "SEANCE";
    }

    private static bool IsInPile(CardModel card)
    {
        return card.Pile != null;
    }

    private static bool ShouldRecordPileMove(CardModel card, PileType? oldPile, PileType destinationPile, bool hasSelectionSource)
    {
        if (oldPile == destinationPile || oldPile == null)
            return false;

        if (destinationPile == PileType.Play || destinationPile == PileType.Exhaust)
            return false;

        if (destinationPile == PileType.Discard && (oldPile == PileType.Play || oldPile == PileType.Hand))
            return false;

        if (destinationPile == PileType.Hand && oldPile == PileType.Draw && !hasSelectionSource)
            return false;

        return hasSelectionSource || _activePlayedCard != null || card == _activePlayedCard;
    }

    private static readonly System.Reflection.FieldInfo EnergyCostCardField =
        AccessTools.Field(typeof(CardEnergyCost), "_card");
}

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayStarted))]
public static class CardPlayStartedPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState combatState, CardPlay cardPlay)
    {
        CardOrderRecorder.RecordPlayed(combatState, cardPlay);
    }
}

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayFinished))]
public static class CardPlayFinishedPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardPlay cardPlay)
    {
        CardOrderRecorder.FinishPlayed(cardPlay);
    }
}

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardDrawn))]
public static class CardDrawnPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState combatState, CardModel card, bool fromHandDraw)
    {
        CardOrderRecorder.RecordDrew(combatState, card, fromHandDraw);
    }
}

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardDiscarded))]
public static class CardDiscardedPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState combatState, CardModel card)
    {
        CardOrderRecorder.RecordDiscarded(combatState, card);
    }
}

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardExhausted))]
public static class CardExhaustedPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState combatState, CardModel card)
    {
        CardOrderRecorder.RecordExhausted(combatState, card);
    }
}

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardGenerated))]
public static class CardGeneratedPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState combatState, CardModel card, Player? creator)
    {
        CardOrderRecorder.RecordGenerated(combatState, card, creator);
    }
}

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardAfflicted))]
public static class CardAfflictedPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState combatState, CardModel card)
    {
        CardOrderRecorder.RecordAfflicted(combatState, card);
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

[HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromHand), typeof(PlayerChoiceContext), typeof(Player), typeof(CardSelectorPrefs), typeof(Func<CardModel, bool>), typeof(AbstractModel))]
public static class CardSelectFromHandPatch
{
    [HarmonyPostfix]
    public static void Postfix(AbstractModel source, ref Task<IEnumerable<CardModel>> __result)
    {
        __result = RememberSelectionSourceAsync(__result, source);
    }

    private static async Task<IEnumerable<CardModel>> RememberSelectionSourceAsync(
        Task<IEnumerable<CardModel>> resultTask,
        AbstractModel source
    )
    {
        IEnumerable<CardModel> selectedCards = await resultTask;
        CardOrderRecorder.RememberSelectionSource(selectedCards, source);
        return selectedCards;
    }
}

[HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromCombatPile), typeof(PlayerChoiceContext), typeof(CardPile), typeof(Player), typeof(CardSelectorPrefs), typeof(Func<CardModel, bool>))]
public static class CardSelectFromCombatPilePatch
{
    [HarmonyPostfix]
    public static void Postfix(ref Task<IEnumerable<CardModel>> __result)
    {
        if (CardOrderRecorder.CurrentSource is not AbstractModel source)
            return;

        __result = RememberSelectionSourceAsync(__result, source);
    }

    private static async Task<IEnumerable<CardModel>> RememberSelectionSourceAsync(
        Task<IEnumerable<CardModel>> resultTask,
        AbstractModel source
    )
    {
        IEnumerable<CardModel> selectedCards = await resultTask;
        CardOrderRecorder.RememberSelectionSource(selectedCards, source);
        return selectedCards;
    }
}

[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileAddPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref Task<IReadOnlyList<CardPileAddResult>> __result)
    {
        __result = RecordMovedToPileAsync(__result);
    }

    private static async Task<IReadOnlyList<CardPileAddResult>> RecordMovedToPileAsync(
        Task<IReadOnlyList<CardPileAddResult>> resultTask
    )
    {
        IReadOnlyList<CardPileAddResult> results = await resultTask;
        foreach (CardPileAddResult result in results)
        {
            if (!result.success)
                continue;

            CardModel card = result.cardAdded;
            ICombatState? combatState = card.CombatState ?? card.Owner?.Creature.CombatState;
            PileType? destinationPile = card.Pile?.Type;
            if (combatState == null || destinationPile == null)
                continue;

            CardOrderRecorder.RecordMovedToPile(combatState, card, result.oldPile?.Type, destinationPile.Value);
        }

        return results;
    }
}

[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Transform), typeof(CardModel), typeof(CardModel), typeof(CardPreviewStyle))]
public static class CardTransformPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel original, ref Task<CardPileAddResult?> __result)
    {
        ICombatState? combatState = original.CombatState ?? original.Owner?.Creature.CombatState;
        if (combatState == null)
            return;

        __result = RecordTransformedAsync(__result, combatState, original);
    }

    private static async Task<CardPileAddResult?> RecordTransformedAsync(
        Task<CardPileAddResult?> resultTask,
        ICombatState combatState,
        CardModel original
    )
    {
        CardPileAddResult? result = await resultTask;
        if (result?.success == true)
            CardOrderRecorder.RecordTransformed(combatState, original);

        return result;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.UpgradeInternal))]
public static class CardUpgradeInternalPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance)
    {
        CardOrderRecorder.RecordUpgraded(__instance);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.DowngradeInternal))]
public static class CardDowngradeInternalPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance)
    {
        CardOrderRecorder.RecordDowngraded(__instance);
    }
}

[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.ApplyKeyword))]
public static class ApplyKeywordPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel card)
    {
        CardOrderRecorder.RecordKeywordApplied(card);
    }
}

[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.RemoveKeyword))]
public static class RemoveKeywordPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel card)
    {
        CardOrderRecorder.RecordKeywordRemoved(card);
    }
}

[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.ClearAffliction))]
public static class ClearAfflictionPatch
{
    [HarmonyPrefix]
    public static void Prefix(CardModel card, out bool __state)
    {
        __state = card.Affliction != null;
    }

    [HarmonyPostfix]
    public static void Postfix(CardModel card, bool __state)
    {
        if (__state)
            CardOrderRecorder.RecordAfflictionCleared(card);
    }
}

[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.ClearEnchantment))]
public static class ClearEnchantmentPatch
{
    [HarmonyPrefix]
    public static void Prefix(CardModel card, out bool __state)
    {
        __state = card.Enchantment != null;
    }

    [HarmonyPostfix]
    public static void Postfix(CardModel card, bool __state)
    {
        if (__state)
            CardOrderRecorder.RecordEnchantmentCleared(card);
    }
}

[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.ApplySingleTurnSly))]
public static class ApplySingleTurnSlyPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel card)
    {
        CardOrderRecorder.RecordSlyApplied(card);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.GiveSingleTurnRetain))]
public static class GiveSingleTurnRetainPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance)
    {
        CardOrderRecorder.RecordRetained(__instance);
    }
}

[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Enchant), typeof(EnchantmentModel), typeof(CardModel), typeof(decimal))]
public static class EnchantPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel card)
    {
        CardOrderRecorder.RecordEnchanted(card);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.SetStarCostUntilPlayed))]
public static class SetStarCostUntilPlayedPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance)
    {
        CardOrderRecorder.RecordCostChanged(__instance);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.SetStarCostThisTurn))]
public static class SetStarCostThisTurnPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance)
    {
        CardOrderRecorder.RecordCostChanged(__instance);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.SetStarCostThisCombat))]
public static class SetStarCostThisCombatPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance)
    {
        CardOrderRecorder.RecordCostChanged(__instance);
    }
}

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.SetUntilPlayed))]
public static class EnergySetUntilPlayedPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardEnergyCost __instance)
    {
        CardOrderRecorder.RecordCostChanged(__instance);
    }
}

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.SetThisTurnOrUntilPlayed))]
public static class EnergySetThisTurnOrUntilPlayedPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardEnergyCost __instance)
    {
        CardOrderRecorder.RecordCostChanged(__instance);
    }
}

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.SetThisTurn))]
public static class EnergySetThisTurnPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardEnergyCost __instance)
    {
        CardOrderRecorder.RecordCostChanged(__instance);
    }
}

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.SetThisCombat))]
public static class EnergySetThisCombatPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardEnergyCost __instance)
    {
        CardOrderRecorder.RecordCostChanged(__instance);
    }
}

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.AddUntilPlayed))]
public static class EnergyAddUntilPlayedPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardEnergyCost __instance)
    {
        CardOrderRecorder.RecordCostChanged(__instance);
    }
}

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.AddThisTurnOrUntilPlayed))]
public static class EnergyAddThisTurnOrUntilPlayedPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardEnergyCost __instance)
    {
        CardOrderRecorder.RecordCostChanged(__instance);
    }
}

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.AddThisTurn))]
public static class EnergyAddThisTurnPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardEnergyCost __instance)
    {
        CardOrderRecorder.RecordCostChanged(__instance);
    }
}

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.AddThisCombat))]
public static class EnergyAddThisCombatPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardEnergyCost __instance)
    {
        CardOrderRecorder.RecordCostChanged(__instance);
    }
}
