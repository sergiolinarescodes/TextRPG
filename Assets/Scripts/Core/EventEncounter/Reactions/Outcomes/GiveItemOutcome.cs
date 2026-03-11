using TextRPG.Core.EntityStats;
using TextRPG.Core.Equipment;
using Unidad.Core.Inventory;

namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    internal sealed class GiveItemOutcome : IInteractionOutcome
    {
        private static readonly string[] FruitPool = { "apple", "pear", "grape", "orange", "banana" };
        private readonly System.Random _rng = new();

        public string OutcomeId => "give_item";

        public void Execute(InteractionOutcomeContext context)
        {
            var inventoryService = context.Ctx.InventoryService;
            var inventoryId = context.Ctx.PlayerInventoryId;
            var itemRegistry = context.Ctx.ItemRegistry;
            if (inventoryService == null || itemRegistry == null) return;

            var itemWord = context.OutcomeParam;
            if (string.IsNullOrEmpty(itemWord)) return;

            // Handle random fruit pool
            if (itemWord == "random_fruit")
                itemWord = FruitPool[_rng.Next(FruitPool.Length)];

            if (!itemRegistry.TryGet(itemWord, out var itemDef)) return;

            var itemId = new ItemId(itemWord);
            inventoryService.Add(inventoryId, itemId);
            context.Ctx.EventBus.Publish(new ItemAcquiredEvent(context.Source, itemWord));
            context.Ctx.EventBus.Publish(new InteractionMessageEvent(
                $"Received {itemDef.DisplayName}!", context.Target));
        }
    }
}
