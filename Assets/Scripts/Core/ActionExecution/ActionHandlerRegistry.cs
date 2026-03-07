using Unidad.Core.Registry;

namespace TextRPG.Core.ActionExecution
{
    internal sealed class ActionHandlerRegistry : RegistryBase<string, IActionHandler>, IActionHandlerRegistry { }
}
