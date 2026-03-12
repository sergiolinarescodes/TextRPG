using TextRPG.Core.EntityStats;
using Unidad.Core.Inventory;

namespace TextRPG.Core.Equipment
{
    public interface IEquipmentService
    {
        bool Equip(EntityId entity, string itemWord);
        bool Unequip(EntityId entity, EquipmentSlotType slot);
        bool HasEquipped(EntityId entity, EquipmentSlotType slot);
        EquipmentItemDefinition GetEquipped(EntityId entity, EquipmentSlotType slot);
        bool IsSlotEmpty(EntityId entity, EquipmentSlotType slot);
        EquipmentSlotType? GetSlotTypeForItem(string itemWord);

        /// <summary>
        /// Equip from inventory: validates slot type, handles swap if occupied, removes from inventory.
        /// </summary>
        bool EquipFromInventory(EntityId entity, string itemWord, IInventoryService inventory, InventoryId inventoryId);

        /// <summary>
        /// Unequip to inventory: unequips and adds item back to inventory.
        /// </summary>
        bool UnequipToInventory(EntityId entity, EquipmentSlotType slot, IInventoryService inventory, InventoryId inventoryId);

        bool IsInBattle { get; }
        bool IsBattleSetupPhase { get; }
        bool CanEquipSlotInBattle(EquipmentSlotType slot);
        bool CanUnequipInBattle();
        void EnterBattle();
        void ExitBattle();
    }
}
