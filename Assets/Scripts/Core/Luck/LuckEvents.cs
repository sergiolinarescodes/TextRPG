using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Luck
{
    public readonly record struct CriticalHitEvent(EntityId Source, EntityId Target, int OriginalDamage, int CritDamage);
}
