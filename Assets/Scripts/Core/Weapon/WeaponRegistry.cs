using Unidad.Core.Registry;

namespace TextRPG.Core.Weapon
{
    internal sealed class WeaponRegistry : RegistryBase<string, WeaponDefinition>, IWeaponRegistry { }
}
