using Unidad.Core.Registry;

namespace TextRPG.Core.WordAction
{
    internal sealed class ActionRegistry : RegistryBase<string, ActionDefinition>, IActionRegistry
    {
    }
}
