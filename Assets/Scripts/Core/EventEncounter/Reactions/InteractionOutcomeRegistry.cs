using TextRPG.Core.Services;
using Unidad.Core.Registry;

namespace TextRPG.Core.EventEncounter.Reactions
{
    internal sealed class InteractionOutcomeRegistry
        : RegistryBase<string, IInteractionOutcome>,
          IAutoScanRegistry<IInteractionOutcome>
    {
        public void RegisterScanned(IInteractionOutcome item) => Register(item.OutcomeId, item);
    }
}
