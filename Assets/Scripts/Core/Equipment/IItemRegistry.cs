using Unidad.Core.Registry;

namespace TextRPG.Core.Equipment
{
    public interface IItemRegistry : IRegistry<string, EquipmentItemDefinition> { }
}
