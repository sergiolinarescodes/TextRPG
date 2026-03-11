using TextRPG.Core.Services;
using Unidad.Core.Registry;

namespace TextRPG.Core.Effects
{
    internal sealed class GameEffectRegistry
        : RegistryBase<string, IGameEffect>,
          IAutoScanRegistry<IGameEffect>
    {
        public void RegisterScanned(IGameEffect item) => Register(item.EffectId, item);
    }
}
