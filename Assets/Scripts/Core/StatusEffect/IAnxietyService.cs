using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect
{
    public interface IAnxietyService
    {
        bool TryInterceptTurn(EntityId entityId, out string word);
        void SetStatusEffects(IStatusEffectService statusEffects);
    }
}
