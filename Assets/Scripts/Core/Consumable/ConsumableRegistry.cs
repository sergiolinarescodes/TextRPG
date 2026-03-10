using Unidad.Core.Registry;

namespace TextRPG.Core.Consumable
{
    internal sealed class ConsumableRegistry : RegistryBase<string, ConsumableDefinition>, IConsumableRegistry { }
}
