using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.StatusEffect.Scenarios
{
    internal sealed class StatusEffectScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter EffectTypeParam = new(
            "effectType", "Effect Type", typeof(string), "Burning");
        private static readonly ScenarioParameter DurationParam = new(
            "duration", "Duration", typeof(int), 3, 1, 10);

        private IStatusEffectService _statusService;
        private IEntityStatsService _entityStats;
        private ITurnService _turnService;
        private IEventBus _eventBus;
        private EntityId _hero;
        private EntityId _enemy;
        private int _duration;
        private StatusEffectType _effectType;
        private int _dotDamageTotal;
        private bool _effectExpired;
        private bool _effectApplied;
        private int _initialEnemyHealth;
        private readonly List<IDisposable> _subscriptions = new();

        public StatusEffectScenario() : base(new TestScenarioDefinition(
            "status-effect-flow",
            "Status Effect Flow",
            "Applies a status effect to an enemy, simulates turn ends, and displays DoT damage and effect expiry.",
            new[] { EffectTypeParam, DurationParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var effectTypeName = ResolveParam<string>(overrides, "effectType");
            _duration = ResolveParam<int>(overrides, "duration");
            _effectType = Enum.Parse<StatusEffectType>(effectTypeName);
            _dotDamageTotal = 0;
            _effectExpired = false;
            _effectApplied = false;

            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            _turnService = new TurnService(_eventBus);
            var handlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(_entityStats, _turnService, _eventBus);
            _statusService = new StatusEffectService(_eventBus, _entityStats, _turnService, handlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)_statusService;

            _hero = new EntityId("hero");
            _enemy = new EntityId("enemy");

            _entityStats.RegisterEntity(_hero, maxHealth: 100, strength: 10, magicPower: 8,
                physicalDefense: 5, magicDefense: 4, luck: 3);
            _entityStats.RegisterEntity(_enemy, maxHealth: 100, strength: 8, magicPower: 6,
                physicalDefense: 4, magicDefense: 3, luck: 2);

            _turnService.SetTurnOrder(new[] { _hero, _enemy });
            _initialEnemyHealth = _entityStats.GetCurrentHealth(_enemy);

            _subscriptions.Add(_eventBus.Subscribe<StatusEffectAppliedEvent>(e =>
            {
                _effectApplied = true;
                Debug.Log($"[StatusEffectScenario] {e.Type} applied to {e.Target.Value} {(e.Duration < 0 ? "permanently" : $"for {e.Duration} turns")}");
            }));
            _subscriptions.Add(_eventBus.Subscribe<StatusEffectDamageEvent>(e =>
            {
                _dotDamageTotal += e.Damage;
                Debug.Log($"[StatusEffectScenario] {e.Type} dealt {e.Damage} damage to {e.Target.Value}");
            }));
            _subscriptions.Add(_eventBus.Subscribe<StatusEffectTickedEvent>(e =>
                Debug.Log($"[StatusEffectScenario] {e.Type} ticked on {e.Target.Value}{(e.RemainingDuration < 0 ? "" : $", {e.RemainingDuration} turns left")}")));
            _subscriptions.Add(_eventBus.Subscribe<StatusEffectExpiredEvent>(e =>
            {
                _effectExpired = true;
                Debug.Log($"[StatusEffectScenario] {e.Type} expired on {e.Target.Value}");
            }));
            _subscriptions.Add(_eventBus.Subscribe<ExtraTurnGrantedEvent>(e =>
                Debug.Log($"[StatusEffectScenario] Extra turn granted to {e.EntityId.Value}")));

            _statusService.ApplyEffect(_enemy, _effectType, _duration, _hero);

            // Simulate turns — enemy ends turn each cycle
            for (int i = 0; i < _duration; i++)
            {
                _turnService.BeginTurn(); // hero
                _turnService.EndTurn();
                _turnService.BeginTurn(); // enemy
                _turnService.EndTurn();   // triggers status tick on enemy
            }

            BuildUI();
        }

        private void BuildUI()
        {
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            root.style.paddingTop = 20;
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;

            var title = new Label("Status Effect Flow");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            AddInfoRow(root, "Effect", _effectType.ToString(), new Color(1f, 0.6f, 0.2f));
            AddInfoRow(root, "Duration", _duration.ToString(), new Color(0.6f, 0.8f, 1f));
            AddInfoRow(root, "DoT Total", _dotDamageTotal.ToString(), new Color(1f, 0.3f, 0.3f));
            AddInfoRow(root, "Enemy HP", $"{_entityStats.GetCurrentHealth(_enemy)}/{_initialEnemyHealth}",
                new Color(0.2f, 0.8f, 0.2f));
            AddInfoRow(root, "Effect Expired", _effectExpired.ToString(), new Color(0.8f, 0.8f, 0.3f));

            var effectsTitle = new Label("Active Effects on Enemy:");
            effectsTitle.style.fontSize = 16;
            effectsTitle.style.color = Color.white;
            effectsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            effectsTitle.style.marginTop = 12;
            effectsTitle.style.marginBottom = 8;
            root.Add(effectsTitle);

            var effects = _statusService.GetEffects(_enemy);
            if (effects.Count == 0)
            {
                var noEffects = new Label("  (none)");
                noEffects.style.fontSize = 14;
                noEffects.style.color = new Color(0.5f, 0.5f, 0.5f);
                root.Add(noEffects);
            }
            else
            {
                foreach (var effect in effects)
                {
                    var durationText = effect.IsPermanent ? "permanent" : $"{effect.RemainingDuration} turns left";
                    var label = new Label($"  {effect.Type} — {durationText} (x{effect.StackCount})");
                    label.style.fontSize = 14;
                    label.style.color = new Color(0.8f, 0.6f, 0.2f);
                    label.style.marginBottom = 4;
                    root.Add(label);
                }
            }
        }

        private static void AddInfoRow(VisualElement parent, string label, string value, Color valueColor)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 6;

            var nameLabel = new Label($"{label}: ");
            nameLabel.style.fontSize = 16;
            nameLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            nameLabel.style.width = 150;
            row.Add(nameLabel);

            var valueLabel = new Label(value);
            valueLabel.style.fontSize = 16;
            valueLabel.style.color = valueColor;
            row.Add(valueLabel);

            parent.Add(row);
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var definition = StatusEffectDefinitions.Get(_effectType);
            var expectedDot = definition.DamagePerTick.HasValue ? definition.DamagePerTick.Value * _duration : 0;
            var expectedHealth = _initialEnemyHealth - expectedDot;
            if (expectedHealth < 0) expectedHealth = 0;

            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("Status service initialized", _statusService != null,
                    _statusService != null ? null : "Service is null"),
                new("Effect was applied", _effectApplied,
                    _effectApplied ? null : "Effect was never applied"),
                new("Effect expired after duration", _effectExpired,
                    _effectExpired ? null : "Effect did not expire"),
                new("DoT damage correct", _dotDamageTotal == expectedDot,
                    _dotDamageTotal == expectedDot
                        ? null : $"Expected {expectedDot} total DoT but got {_dotDamageTotal}"),
                new("Enemy health correct", _entityStats.GetCurrentHealth(_enemy) == expectedHealth,
                    _entityStats.GetCurrentHealth(_enemy) == expectedHealth
                        ? null : $"Expected {expectedHealth} HP but got {_entityStats.GetCurrentHealth(_enemy)}"),
                new("No effects remain", _statusService.GetEffects(_enemy).Count == 0,
                    _statusService.GetEffects(_enemy).Count == 0
                        ? null : $"Expected 0 effects but got {_statusService.GetEffects(_enemy).Count}"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();

            (_statusService as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            (_turnService as IDisposable)?.Dispose();
            _statusService = null;
            _entityStats = null;
            _turnService = null;
            _eventBus = null;
        }
    }
}
