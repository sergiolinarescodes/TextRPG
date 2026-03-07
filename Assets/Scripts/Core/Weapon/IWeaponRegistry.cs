using Unidad.Core.Registry;

namespace TextRPG.Core.Weapon
{
    public interface IWeaponRegistry : IRegistry<string, WeaponDefinition> { }
}
