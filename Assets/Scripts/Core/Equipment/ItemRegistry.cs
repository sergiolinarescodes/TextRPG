using Unidad.Core.Registry;

namespace TextRPG.Core.Equipment
{
    internal sealed class ItemRegistry : RegistryBase<string, EquipmentItemDefinition>, IItemRegistry { }
}
