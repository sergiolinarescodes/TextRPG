using System;
using System.Collections.Generic;
using TextRPG.Core.Equipment;
using TextRPG.Core.EventEncounter.Reactions.Tags;
using TextRPG.Core.WordAction;
using Unidad.Core.Inventory;
using Unidad.Core.Resource;

namespace TextRPG.Core.ActionExecution
{
    internal sealed class GiveValidator : IGiveValidator
    {
        private static readonly HashSet<string> GiveableItemTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "SILVER", "VALUABLE"
        };

        private readonly IWordTagResolver _wordTagResolver;
        private readonly IInventoryService _inventoryService;
        private readonly InventoryId _inventoryId;
        private readonly IItemRegistry _itemRegistry;
        private readonly IWordResolver _wordResolver;
        private readonly IResourceService _resourceService;

        public GiveValidator(
            IWordTagResolver wordTagResolver,
            IInventoryService inventoryService,
            InventoryId inventoryId,
            IItemRegistry itemRegistry,
            IWordResolver wordResolver,
            IResourceService resourceService)
        {
            _wordTagResolver = wordTagResolver;
            _inventoryService = inventoryService;
            _inventoryId = inventoryId;
            _itemRegistry = itemRegistry;
            _wordResolver = wordResolver;
            _resourceService = resourceService;
        }

        public bool RequiresItemForGive(string word)
        {
            if (HasGiveableItemTags(word)) return true;
            if (GetPayValue(word) > 0) return true;
            return false;
        }

        public bool TryConsumeForGive(string word)
        {
            // Item tags take priority (SILVER, VALUABLE)
            if (HasGiveableItemTags(word))
                return TryConsumeItemByTag(word);

            // Fall through to gold resource for Pay words
            int payValue = GetPayValue(word);
            if (payValue > 0)
                return _resourceService != null && _resourceService.TrySpend(ResourceIds.Gold, payValue);

            return false;
        }

        private bool HasGiveableItemTags(string word)
        {
            // Check if the word itself is a giveable tag name (e.g., "silver" matches "SILVER")
            if (GiveableItemTags.Contains(word)) return true;

            var tags = _wordTagResolver.GetTags(word);
            for (int i = 0; i < tags.Count; i++)
                if (GiveableItemTags.Contains(tags[i])) return true;
            return false;
        }

        private int GetPayValue(string word)
        {
            if (_wordResolver == null || !_wordResolver.HasWord(word)) return 0;
            var actions = _wordResolver.Resolve(word);
            for (int i = 0; i < actions.Count; i++)
            {
                if (string.Equals(actions[i].ActionId, ActionNames.Pay, StringComparison.OrdinalIgnoreCase))
                    return actions[i].Value;
            }
            return 0;
        }

        private bool TryConsumeItemByTag(string word)
        {
            var wordTags = _wordTagResolver.GetTags(word);
            var matchingTags = new List<string>();

            // Word itself as a tag name (e.g., "silver" → "SILVER")
            if (GiveableItemTags.Contains(word))
                matchingTags.Add(word);

            for (int i = 0; i < wordTags.Count; i++)
                if (GiveableItemTags.Contains(wordTags[i])) matchingTags.Add(wordTags[i]);

            if (matchingTags.Count == 0) return false;

            int slotCount = _inventoryService.GetSlotCount(_inventoryId);
            for (int s = 0; s < slotCount; s++)
            {
                var slot = _inventoryService.GetSlot(_inventoryId, s);
                if (slot.IsEmpty) continue;

                if (!_itemRegistry.TryGet(slot.ItemId.Value, out var itemDef)) continue;
                if (itemDef.Tags == null || itemDef.Tags.Length == 0) continue;

                for (int t = 0; t < itemDef.Tags.Length; t++)
                {
                    for (int m = 0; m < matchingTags.Count; m++)
                    {
                        if (string.Equals(itemDef.Tags[t], matchingTags[m], StringComparison.OrdinalIgnoreCase))
                        {
                            _inventoryService.TryRemove(_inventoryId, slot.ItemId, 1);
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
