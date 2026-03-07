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
        void BeginTurn();
        void EndTurn();
        void GrantExtraTurn(EntityId entityId);
    }
}
