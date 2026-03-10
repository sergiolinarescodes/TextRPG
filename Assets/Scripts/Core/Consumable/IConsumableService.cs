using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Consumable
{
    public interface IConsumableService
    {
        void EquipConsumable(EntityId entity, string itemWord);
        bool UseConsumable(EntityId entity);
        bool HasConsumable(EntityId entity);
        ConsumableDefinition? GetDefinition(EntityId entity);
        int GetDurability(EntityId entity);
        bool IsAmmoForEquipped(EntityId entity, string ammoWord);
        IReadOnlyList<string> GetAmmoWords(EntityId entity);
    }
}
