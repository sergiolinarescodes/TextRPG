using System;
using TextRPG.Core.EntityStats;
using TextRPG.Core.UnitRendering;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;
using Unidad.Core.UI.TextAnimation;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.StatusEffect
{
    internal sealed class StatusEffectVisualService : SystemServiceBase
    {
        private StatusEffectFloatingTextPool _pool;
        private Func<EntityId, Vector3> _positionProvider;
        private bool _initialized;
        private TextAnimationService _textAnimService;
        private CombatSlotVisual _slotVisual;

        public StatusEffectVisualService(IEventBus eventBus) : base(eventBus)
        {
            Subscribe<StatusEffectAppliedEvent>(OnEffectApplied);
            Subscribe<StatusEffectDamageEvent>(OnEffectDamage);
            Subscribe<StatusEffectExpiredEvent>(OnEffectExpired);
            Subscribe<DamageTakenEvent>(OnDamageTaken);
        }

        public void Initialize(
            Func<EntityId, Vector3> positionProvider,
            VisualElement floatingTextLayer,
            CombatSlotVisual slotVisual)
        {
            _positionProvider = positionProvider;
            _slotVisual = slotVisual;

            _textAnimService = new TextAnimationService();
            _pool = new StatusEffectFloatingTextPool(_textAnimService);
            _pool.Initialize(floatingTextLayer);

            _initialized = true;
        }

        private void OnEffectApplied(StatusEffectAppliedEvent e)
        {
            if (!_initialized) return;
            var def = StatusEffectDefinitions.Get(e.Type);
            var pos = _positionProvider(e.Target);
            _pool.Spawn(new Vector2(pos.x, pos.y), def.DisplayName, def.DisplayColor, "heal");
        }

        private void OnEffectDamage(StatusEffectDamageEvent e)
        {
            if (!_initialized) return;
            var def = StatusEffectDefinitions.Get(e.Type);
            var pos = _positionProvider(e.Target);
            _pool.Spawn(new Vector2(pos.x, pos.y), $"-{e.Damage}", def.DisplayColor, "damage");
        }

        private void OnEffectExpired(StatusEffectExpiredEvent e)
        {
            if (!_initialized) return;
            var def = StatusEffectDefinitions.Get(e.Type);
            var pos = _positionProvider(e.Target);
            _pool.Spawn(new Vector2(pos.x, pos.y), $"{def.DisplayName} ENDED", new Color(0.6f, 0.6f, 0.6f), "heal");
        }

        private void OnDamageTaken(DamageTakenEvent e)
        {
            if (!_initialized || _slotVisual == null) return;

            var recipe = _textAnimService.GetRecipe("damage");
            _slotVisual.PlayHitAnimation(e.EntityId, recipe?.Duration ?? 1.5f, recipe?.MarkupTemplate ?? "<shake a=0.1 f=5>{0}</shake>");
        }

        public override void Dispose()
        {
            _pool?.Dispose();
            _pool = null;
            _textAnimService?.Dispose();
            _textAnimService = null;
            _slotVisual = null;
            _initialized = false;
            base.Dispose();
        }
    }
}
