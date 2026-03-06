using Unidad.Core.Registry;

namespace TextRPG.Core.WordAction
{
    public interface IActionRegistry : IRegistry<string, ActionDefinition>
    {
    }
}
