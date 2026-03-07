using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Weapon
{
    public readonly record struct WeaponEquippedEvent(EntityId Entity, WeaponDefinition Weapon);
    public readonly record struct WeaponDurabilityChangedEvent(EntityId Entity, string WeaponWord, int CurrentDurability, int MaxDurability);
    public readonly record struct WeaponDestroyedEvent(EntityId Entity, string WeaponWord);
    public readonly record struct WeaponAmmoSubmittedEvent(EntityId Source, string AmmoWord);
}
