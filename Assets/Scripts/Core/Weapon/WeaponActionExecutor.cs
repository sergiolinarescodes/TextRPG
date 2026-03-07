using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.Weapon
{
    internal sealed class WeaponActionExecutor : SystemServiceBase, IWeaponActionExecutor
    {
        private readonly IWeaponService _weaponService;
        private readonly IWordResolver _ammoResolver;
        private readonly IActionHandlerRegistry _handlerRegistry;
        private readonly ICombatContext _combatContext;

        public WeaponActionExecutor(
            IEventBus eventBus,
            IWeaponService weaponService,
            IWordResolver ammoResolver,
            IActionHandlerRegistry handlerRegistry,
            ICombatContext combatContext)
            : base(eventBus)
        {
            _weaponService = weaponService;
            _ammoResolver = ammoResolver;
            _handlerRegistry = handlerRegistry;
            _combatContext = combatContext;

            Subscribe<WeaponAmmoSubmittedEvent>(OnAmmoSubmitted);
        }

        public void ExecuteAmmoWord(EntityId source, string ammoWord)
        {
            var word = ammoWord.ToLowerInvariant();

            if (!_weaponService.IsAmmoForEquipped(source, word))
                return;

            if (!_ammoResolver.HasWord(word))
                return;

            var actions = _ammoResolver.Resolve(word);
            var meta = _ammoResolver.GetStats(word);

            Publish(new ActionExecutionStartedEvent(word, actions.Count));

            for (int i = 0; i < actions.Count; i++)
            {
                var mapping = actions[i];
                var actionTarget = mapping.Target ?? meta.Target;
                var actionRange = mapping.Range ?? meta.Range;
                var actionArea = mapping.Area ?? meta.Area;

                var targetType = TargetTypeClassifier.ParseTargetType(actionTarget);
                var primaryTargets = _combatContext.GetTargets(targetType, actionRange);
                var targets = _combatContext.ExpandArea(primaryTargets, actionArea);

                if (_handlerRegistry.TryGet(mapping.ActionId, out var handler))
                {
                    var context = new ActionContext(source, targets, mapping.Value, word);
                    handler.Execute(context);
                    Publish(new ActionHandlerExecutedEvent(mapping.ActionId, mapping.Value, source, targets));
                }
            }

            _weaponService.UseWeapon(source);
            Publish(new ActionExecutionCompletedEvent(word));
        }

        private void OnAmmoSubmitted(WeaponAmmoSubmittedEvent e)
        {
            ExecuteAmmoWord(e.Source, e.AmmoWord);
        }
    }
}
