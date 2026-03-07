using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Weapon
{
    public interface IWeaponService
    {
        void EquipWeapon(EntityId entity, string weaponWord);
        bool UseWeapon(EntityId entity);
        bool HasWeapon(EntityId entity);
        WeaponDefinition? GetWeaponDefinition(EntityId entity);
        int GetCurrentDurability(EntityId entity);
        bool IsAmmoForEquipped(EntityId entity, string ammoWord);
        IReadOnlyList<string> GetAmmoWords(EntityId entity);
    }
}
