using TextRPG.Core.ActionExecution;
using TextRPG.Core.WordAction;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.Consumable
{
    internal sealed class ConsumableActionExecutor : SystemServiceBase
    {
        private readonly IConsumableService _consumableService;
        private readonly IWordResolver _ammoResolver;
        private readonly IActionHandlerRegistry _handlerRegistry;
        private readonly ICombatContext _combatContext;
        private readonly IAnimationResolver _animationResolver;

        public ConsumableActionExecutor(
            IEventBus eventBus,
            IConsumableService consumableService,
            IWordResolver ammoResolver,
            IActionHandlerRegistry handlerRegistry,
            ICombatContext combatContext,
            IAnimationResolver animationResolver = null)
            : base(eventBus)
        {
            _consumableService = consumableService;
            _ammoResolver = ammoResolver;
            _handlerRegistry = handlerRegistry;
            _combatContext = combatContext;
            _animationResolver = animationResolver;

            Subscribe<ConsumableAmmoSubmittedEvent>(OnAmmoSubmitted);
        }

        private void OnAmmoSubmitted(ConsumableAmmoSubmittedEvent e)
        {
            var word = e.AmmoWord.ToLowerInvariant();

            if (!_consumableService.IsAmmoForEquipped(e.Source, word))
                return;

            if (!_ammoResolver.HasWord(word))
                return;

            var actions = _ammoResolver.Resolve(word);
            var meta = _ammoResolver.GetStats(word);

            _consumableService.UseConsumable(e.Source);

            var resolved = AmmoActionResolver.ResolveActions(e.Source, word, actions, meta, _handlerRegistry, _combatContext);

            Publish(new ActionExecutionStartedEvent(word, resolved.Count));

            bool isInstant = _animationResolver == null || _animationResolver.IsInstant;

            if (isInstant)
            {
                AmmoActionResolver.ExecuteAllImmediately(resolved, _handlerRegistry, EventBus);
                Publish(new ActionResolvedEvent(word, resolved, e.Source, true));
                Publish(new ActionExecutionCompletedEvent(word));
            }
            else
            {
                Publish(new ActionResolvedEvent(word, resolved, e.Source, false));
            }
        }
    }
}
