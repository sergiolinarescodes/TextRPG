using Unidad.Core.Registry;

namespace TextRPG.Core.ActionExecution
{
    public interface IActionHandlerRegistry : IRegistry<string, IActionHandler> { }
}
