using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution
{
    // TODO: Consider ModifierStack<T> if more modifier sources are added (equipment, status effects, buffs)
    public interface IActionValueModifier
    {
        int ModifyValue(string actionId, int baseValue, string word, EntityId source);
    }
}
