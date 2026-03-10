using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;

namespace TextRPG.Core.EncounterManager
{
    public interface IEncounterManager
    {
        void StartCombatEncounter(EncounterDefinition encounter, EntityId player);
        void StartEventEncounter(EventEncounterDefinition encounter, EntityId player);
        void TransitionToEvent(EventEncounterDefinition encounter);
        void TransitionToCombat(EncounterDefinition encounter);
        void ReturnToPrevious();
        bool IsInCombat { get; }
        bool IsInEvent { get; }
    }
}
