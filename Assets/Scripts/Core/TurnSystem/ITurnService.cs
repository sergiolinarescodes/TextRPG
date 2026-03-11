using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.TurnSystem
{
    public interface ITurnService
    {
        EntityId CurrentEntity { get; }
        int CurrentTurnNumber { get; }
        int CurrentRoundNumber { get; }
        bool IsTurnActive { get; }

        void SetTurnOrder(IReadOnlyList<EntityId> order);
        void AddToTurnOrder(EntityId entityId);
        void BeginTurn();
        void EndTurn();
        void RemoveFromTurnOrder(EntityId entityId);
        void GrantExtraTurn(EntityId entityId);
        void MoveToLastInRound(EntityId entityId);
    }
}
