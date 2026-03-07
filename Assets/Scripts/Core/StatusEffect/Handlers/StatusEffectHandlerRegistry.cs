using Unidad.Core.Registry;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class StatusEffectHandlerRegistry : RegistryBase<StatusEffectType, IStatusEffectHandler>, IStatusEffectHandlerRegistry { }
}
