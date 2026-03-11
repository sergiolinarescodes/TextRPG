using TextRPG.Core.Services;
using Unidad.Core.Registry;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class StatusEffectHandlerRegistry
        : RegistryBase<StatusEffectType, IStatusEffectHandler>, IStatusEffectHandlerRegistry,
          IAutoScanRegistry<IStatusEffectHandler>
    {
        public void RegisterScanned(IStatusEffectHandler item) => Register(item.EffectType, item);
    }
}
