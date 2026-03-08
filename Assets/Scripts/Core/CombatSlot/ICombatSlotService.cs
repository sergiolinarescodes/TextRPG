using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.CombatSlot
{
    public interface ICombatSlotService
    {
        void Initialize(int maxEnemySlots = 3, int maxAllySlots = 2);
        void RegisterEnemy(EntityId entityId, int slotIndex);
        void RegisterAlly(EntityId entityId, int slotIndex);
        void RemoveEntity(EntityId entityId);
        CombatSlot? GetSlot(EntityId entityId);
        EntityId? GetEntityAt(SlotType type, int index);
        IReadOnlyList<EntityId> GetAllEnemies();
        IReadOnlyList<EntityId> GetAllAllies();
        EntityId? FindNearestOccupiedSlot(SlotType type, int targetIndex);
        int GetOccupiedEnemyCount();
        int GetOccupiedAllyCount();
        int? FindFirstEmptySlot(SlotType type);
    }
}
