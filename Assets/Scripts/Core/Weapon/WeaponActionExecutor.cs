using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using TextRPG.Core.WordAction;
using Unidad.Core.Abstractions;
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
        private readonly IAnimationResolver _animationResolver;

        public WeaponActionExecutor(
            IEventBus eventBus,
            IWeaponService weaponService,
            IWordResolver ammoResolver,
            IActionHandlerRegistry handlerRegistry,
            ICombatContext combatContext,
            IAnimationResolver animationResolver = null)
            : base(eventBus)
        {
            _weaponService = weaponService;
            _ammoResolver = ammoResolver;
            _handlerRegistry = handlerRegistry;
            _combatContext = combatContext;
            _animationResolver = animationResolver;

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

            // Consume weapon durability during resolve (turn-level cost)
            _weaponService.UseWeapon(source);

            var resolved = ResolveActions(source, word, actions, meta);

            Publish(new ActionExecutionStartedEvent(word, resolved.Count));

            bool isInstant = _animationResolver == null || _animationResolver.IsInstant;

            if (isInstant)
            {
                ExecuteAllImmediately(resolved);
                Publish(new ActionResolvedEvent(word, resolved, source, true));
                Publish(new ActionExecutionCompletedEvent(word));
            }
            else
            {
                Publish(new ActionResolvedEvent(word, resolved, source, false));
            }
        }

        private List<ResolvedAction> ResolveActions(EntityId source, string word,
            IReadOnlyList<WordActionMapping> actions, WordMeta meta)
        {
            return AmmoActionResolver.ResolveActions(source, word, actions, meta, _handlerRegistry, _combatContext);
        }

        private void ExecuteAllImmediately(List<ResolvedAction> resolved)
        {
            AmmoActionResolver.ExecuteAllImmediately(resolved, _handlerRegistry, EventBus);
        }

        private void OnAmmoSubmitted(WeaponAmmoSubmittedEvent e)
        {
            ExecuteAmmoWord(e.Source, e.AmmoWord);
        }
    }
}
