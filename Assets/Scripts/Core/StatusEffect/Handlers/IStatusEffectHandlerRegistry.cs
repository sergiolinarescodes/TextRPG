using Unidad.Core.Registry;

namespace TextRPG.Core.StatusEffect.Handlers
{
    public interface IStatusEffectHandlerRegistry : IRegistry<StatusEffectType, IStatusEffectHandler> { }
}
