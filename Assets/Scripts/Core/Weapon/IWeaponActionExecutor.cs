using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Weapon
{
    public interface IWeaponActionExecutor
    {
        void ExecuteAmmoWord(EntityId source, string ammoWord);
    }
}
