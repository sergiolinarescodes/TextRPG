using TextRPG.Core.Scroll;
using UnityEngine;

namespace TextRPG.Core.Equipment
{
    public sealed record LootRewardOption(
        EquipmentItemDefinition Equipment,
        ScrollDefinition Scroll)
    {
        public bool IsScroll => Scroll != null;
        public string DisplayName => IsScroll ? Scroll.DisplayName : Equipment.DisplayName;
        public Color Color => IsScroll ? Scroll.Color : Equipment.Color;
    }
}
