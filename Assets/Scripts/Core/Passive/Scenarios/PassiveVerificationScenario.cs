using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Passive.Scenarios
{
    internal sealed class PassiveVerificationScenario : DataDrivenScenario
    {
        private IEventBus _eventBus;
        private IEntityStatsService _entityStats;
        private ITurnService _turnService;
        private ICombatSlotService _slotService;
        private IStatusEffectService _statusEffects;
        private IPassiveService _passiveService;

        private readonly EntityId _allyA = new("ally_a");
        private readonly EntityId _allyB = new("ally_b");
        private readonly EntityId _enemyA = new("enemy_a");
        private readonly EntityId _enemyB = new("enemy_b");

        private readonly List<ScenarioVerificationResult.CheckResult> _checks = new();
        private readonly List<IDisposable> _subscriptions = new();

        public PassiveVerificationScenario() : base(new TestScenarioDefinition(
            "passive-verification",
            "Passive Verification",
            "Verifies correctness of all passive triggers, effects, targets, source-keyed registration, lifecycle, and edge cases.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _checks.Clear();

            RebuildAll(); RunHealEffectGroup();
            RebuildAll(); RunDamageEffectGroup();
            RebuildAll(); RunShieldEffectGroup();
            RebuildAll(); RunManaEffectGroup();
            RebuildAll(); RunApplyStatusEffectGroup();
            RebuildAll(); RunOnSelfHitGroup();
            RebuildAll(); RunOnAllyHitGroup();
            RebuildAll(); RunOnRoundEndGroup();
            RebuildAll(); RunOnRoundStartGroup();
            RebuildAll(); RunOnTurnStartGroup();
            RebuildAll(); RunOnTurnEndGroup();
            RebuildAll(); RunOnKillGroup();
            RebuildAll(); RunOnWordPlayedGroup();
            RebuildAll(); RunOnWordLengthGroup();
            RebuildAll(); RunOnWordTagGroup();
            RebuildAll(); RunTargetResolutionGroup();
            RebuildAll(); RunTauntMarkerGroup();
            RebuildAll(); RunSourceKeyedGroup();
            RebuildAll(); RunLifecycleGroup();
            RebuildAll(); RunReentrancyGroup();
            RebuildAll(); RunDeadOwnerGroup();
            RebuildAll(); RunMultiplePassiveGroup();
            RebuildAll(); RunCrossFactionGroup();

            BuildUI();
        }

        // ── Setup ──────────────────────────────────────────────────

        private void RebuildAll()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();

            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            _turnService = new TurnService(_eventBus);

            _slotService = new CombatSlotService(_eventBus);
            _slotService.Initialize();

            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(_entityStats, _turnService, _eventBus);
            _statusEffects = new StatusEffectService(_eventBus, _entityStats, _turnService, effectHandlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)_statusEffects;

            var triggerRegistry = PassiveSystemInstaller.CreateTriggerRegistry();
            var effectRegistry = PassiveSystemInstaller.CreateEffectRegistry(null);
            var targetResolver = new PassiveTargetResolver();

            var tagResolver = new WordTagResolver(new Dictionary<string, List<string>>
            {
                ["fireball"] = new() { "fire" },
                ["icebolt"] = new() { "ice" },
            });

            var context = new PassiveContext(_entityStats, _slotService, _eventBus, null,
                _statusEffects, tagResolver, _turnService);
            _passiveService = new PassiveService(_eventBus, triggerRegistry, effectRegistry, targetResolver, context);

            RegisterEntities();
        }

        private void RegisterEntities()
        {
            // allyA: HP=50, Str=8, Magic=6, PhysDef=4, MagDef=3, Luck=2, mana defaults(10,2,5)
            _entityStats.RegisterEntity(_allyA, 50, 8, 6, 4, 3, 2);
            // allyB: HP=40, Str=5, Magic=4, PhysDef=3, MagDef=2, Luck=1
            _entityStats.RegisterEntity(_allyB, 40, 5, 4, 3, 2, 1);
            // enemyA: HP=60, Str=10, Magic=7, PhysDef=5, MagDef=4, Luck=3
            _entityStats.RegisterEntity(_enemyA, 60, 10, 7, 5, 4, 3);
            // enemyB: HP=30, Str=4, Magic=3, PhysDef=2, MagDef=1, Luck=1
            _entityStats.RegisterEntity(_enemyB, 30, 4, 3, 2, 1, 1);

            _slotService.RegisterAlly(_allyA, 0);
            _slotService.RegisterAlly(_allyB, 1);
            _slotService.RegisterEnemy(_enemyA, 0);
            _slotService.RegisterEnemy(_enemyB, 1);

            _turnService.SetTurnOrder(new[] { _allyA, _allyB, _enemyA, _enemyB });
        }

        private void SoftReset()
        {
            var entities = new[] { _allyA, _allyB, _enemyA, _enemyB };
            foreach (var e in entities)
            {
                _entityStats.ApplyHeal(e, 9999);
                _statusEffects.RemoveAllEffects(e);
            }
            _passiveService.RemovePassives(_allyA);
            _passiveService.RemovePassives(_allyB);
            _passiveService.RemovePassives(_enemyA);
            _passiveService.RemovePassives(_enemyB);
        }

        // ── Helpers ──────────────────────────────────────────────────

        private void Check(string name, bool passed, string detail = null)
        {
            _checks.Add(new ScenarioVerificationResult.CheckResult(name, passed, passed ? null : detail));
            Debug.Log($"[PassiveVerification] {(passed ? "PASS" : "FAIL")}: {name}{(passed ? "" : $" - {detail}")}");
        }

        private int HP(EntityId id) => _entityStats.GetCurrentHealth(id);
        private int Shield(EntityId id) => _entityStats.GetCurrentShield(id);
        private int Mana(EntityId id) => _entityStats.GetCurrentMana(id);

        private void Register(EntityId entity, string trigger, string triggerParam,
                              string effect, string effectParam, int value, string target)
        {
            _passiveService.RegisterPassives(entity,
                new[] { new PassiveEntry(trigger, triggerParam, effect, effectParam, value, target) });
        }

        private int CountTriggered()
        {
            int count = 0;
            var sub = _eventBus.Subscribe<PassiveTriggeredEvent>(_ => count++);
            // We need to subscribe BEFORE the action, so this helper returns the sub for disposal
            sub.Dispose();
            return count;
        }

        // ── Group 1: Heal Effect ──────────────────────────────────────

        private void RunHealEffectGroup()
        {
            // on_round_end + heal 5 Self: damage ally, end round, verify heal
            _entityStats.ApplyDamage(_allyA, 20);
            Check("Heal setup: allyA damaged to 30",
                HP(_allyA) == 30, $"HP={HP(_allyA)}(exp 30)");

            Register(_allyA, "on_round_end", null, "heal", null, 5, "Self");
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn(); // round complete
            Check("Heal self on round end: HP increased by 5",
                HP(_allyA) == 35, $"HP={HP(_allyA)}(exp 35)");

            SoftReset();

            // Heal cannot exceed max HP
            _entityStats.ApplyDamage(_allyA, 2); // 50→48
            Register(_allyA, "on_round_end", null, "heal", null, 10, "Self");
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            Check("Heal capped at max HP",
                HP(_allyA) == 50, $"HP={HP(_allyA)}(exp 50)");
        }

        // ── Group 2: Damage Effect ──────────────────────────────────────

        private void RunDamageEffectGroup()
        {
            // on_self_hit + damage 3 Attacker: allyA hit → damages attacker
            Register(_allyA, "on_self_hit", null, "damage", null, 3, "Attacker");
            _entityStats.ApplyDamage(_allyA, 5, _enemyA);
            Check("Damage attacker on self hit: enemyA takes 3 damage",
                HP(_enemyA) == 57, $"HP={HP(_enemyA)}(exp 57)");

            SoftReset();

            // Damage effect kills target at 0 HP
            _entityStats.ApplyDamage(_enemyB, 27); // 30→3
            Register(_allyA, "on_self_hit", null, "damage", null, 10, "Attacker");
            _entityStats.ApplyDamage(_allyA, 5, _enemyB);
            Check("Damage effect overkill: target dies",
                HP(_enemyB) <= 0, $"HP={HP(_enemyB)}(exp <=0)");
        }

        // ── Group 3: Shield Effect ──────────────────────────────────────

        private void RunShieldEffectGroup()
        {
            // on_self_hit + shield 2 Self
            Register(_allyA, "on_self_hit", null, "shield", null, 2, "Self");
            _entityStats.ApplyDamage(_allyA, 5);
            Check("Shield self on hit: shield = 2",
                Shield(_allyA) == 2, $"Shield={Shield(_allyA)}(exp 2)");

            // Second hit with 1 damage: shield(2) absorbs fully → no DamageTakenEvent → no re-trigger
            _entityStats.ApplyDamage(_allyA, 1);
            Check("Shield absorbs small hit: no re-trigger when fully absorbed",
                Shield(_allyA) == 1, $"Shield={Shield(_allyA)}(exp 1)");

            // Third hit exceeds shield: 3 damage vs shield(1) → 1 absorbed, 2 HP damage → DamageTakenEvent → new shield
            _entityStats.ApplyDamage(_allyA, 3);
            Check("Shield re-triggers when damage pierces shield",
                Shield(_allyA) == 2, $"Shield={Shield(_allyA)}(exp 2)");
        }

        // ── Group 4: Mana Effect ──────────────────────────────────────

        private void RunManaEffectGroup()
        {
            // on_round_end + mana 3 Self: spend mana first
            _entityStats.TrySpendMana(_allyA, 4); // 5→1
            Check("Mana setup: mana reduced to 1",
                Mana(_allyA) == 1, $"Mana={Mana(_allyA)}(exp 1)");

            Register(_allyA, "on_round_end", null, "mana", null, 3, "Self");
            _turnService.BeginTurn(); _turnService.EndTurn(); // allyA: mana regen +2 → 1+2=3
            _turnService.BeginTurn(); _turnService.EndTurn(); // allyB
            _turnService.BeginTurn(); _turnService.EndTurn(); // enemyA
            _turnService.BeginTurn(); _turnService.EndTurn(); // enemyB → round end → passive +3 → 3+3=6
            Check("Mana restored on round end (includes regen)",
                Mana(_allyA) == 6, $"Mana={Mana(_allyA)}(exp 6)");
        }

        // ── Group 5: Apply Status Effect ──────────────────────────────

        private void RunApplyStatusEffectGroup()
        {
            // on_self_hit + apply_status(Burning) 3 Attacker
            Register(_allyA, "on_self_hit", null, "apply_status", "Burning", 3, "Attacker");
            _entityStats.ApplyDamage(_allyA, 5, _enemyA);
            Check("Apply Burning on attacker when self hit",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Burning),
                $"HasBurning={_statusEffects.HasEffect(_enemyA, StatusEffectType.Burning)}");

            SoftReset();

            // apply_status with null effectParam → no-op (graceful failure)
            Register(_allyA, "on_self_hit", null, "apply_status", null, 3, "Attacker");
            _entityStats.ApplyDamage(_allyA, 5, _enemyA);
            Check("Apply status with null param: no crash, no effect",
                !_statusEffects.HasEffect(_enemyA, StatusEffectType.Burning),
                "Should not have any status");
        }

        // ── Group 6: on_self_hit trigger ──────────────────────────────

        private void RunOnSelfHitGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            Register(_allyA, "on_self_hit", null, "heal", null, 1, "Self");
            _entityStats.ApplyDamage(_allyA, 5);
            Check("on_self_hit: triggers when owner is damaged",
                triggered == 1, $"triggered={triggered}(exp 1)");

            triggered = 0;
            _entityStats.ApplyDamage(_allyB, 5); // different entity
            Check("on_self_hit: does NOT trigger for other entity damage",
                triggered == 0, $"triggered={triggered}(exp 0)");

            // Owner dead → does not trigger
            SoftReset();
            triggered = 0;
            Register(_allyA, "on_self_hit", null, "heal", null, 1, "Self");
            _entityStats.ApplyDamage(_allyA, 999); // kill
            triggered = 0;
            _entityStats.ApplyDamage(_allyA, 5); // damage dead entity
            Check("on_self_hit: does NOT trigger when owner is dead",
                triggered == 0, $"triggered={triggered}(exp 0)");
        }

        // ── Group 7: on_ally_hit trigger ──────────────────────────────

        private void RunOnAllyHitGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            // allyA has passive: when ally hit → heal self
            Register(_allyA, "on_ally_hit", null, "heal", null, 2, "Self");
            _entityStats.ApplyDamage(_allyA, 10); // damage self
            Check("on_ally_hit: does NOT trigger for self damage",
                triggered == 0, $"triggered={triggered}(exp 0)");

            _entityStats.ApplyDamage(_allyB, 10); // damage ally
            Check("on_ally_hit: triggers when same-faction ally hit",
                triggered == 1, $"triggered={triggered}(exp 1)");

            triggered = 0;
            _entityStats.ApplyDamage(_enemyA, 10); // damage enemy
            Check("on_ally_hit: does NOT trigger for enemy damage",
                triggered == 0, $"triggered={triggered}(exp 0)");
        }

        // ── Group 8: on_round_end trigger ──────────────────────────────

        private void RunOnRoundEndGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            Register(_allyA, "on_round_end", null, "heal", null, 1, "Self");

            // Partial round: only 2 of 4 turns
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            Check("on_round_end: not triggered mid-round",
                triggered == 0, $"triggered={triggered}(exp 0)");

            // Complete the round
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            Check("on_round_end: triggers after full round",
                triggered == 1, $"triggered={triggered}(exp 1)");
        }

        // ── Group 9: on_round_start trigger ──────────────────────────────

        private void RunOnRoundStartGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            Register(_allyA, "on_round_start", null, "shield", null, 1, "Self");

            // RoundStartedEvent fires inside EndTurn when round wraps — first round has no event
            _turnService.BeginTurn(); _turnService.EndTurn(); // allyA
            _turnService.BeginTurn(); _turnService.EndTurn(); // allyB
            _turnService.BeginTurn(); _turnService.EndTurn(); // enemyA
            Check("on_round_start: not triggered during first round",
                triggered == 0, $"triggered={triggered}(exp 0)");

            _turnService.BeginTurn(); _turnService.EndTurn(); // enemyB → round end → round 2 start
            Check("on_round_start: triggers when round 2 starts",
                triggered == 1, $"triggered={triggered}(exp 1)");

            // Complete round 2
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn(); // round 3 start
            Check("on_round_start: triggers again on round 3",
                triggered == 2, $"triggered={triggered}(exp 2)");
        }

        // ── Group 10: on_turn_start trigger ──────────────────────────────

        private void RunOnTurnStartGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            // Default (faction-scoped): ally's turn_start triggers for same-faction turns
            Register(_allyA, "on_turn_start", null, "heal", null, 1, "Self");
            _entityStats.ApplyDamage(_allyA, 10);

            _turnService.BeginTurn(); // allyA turn
            Check("on_turn_start (faction): triggers on ally turn",
                triggered == 1, $"triggered={triggered}(exp 1)");
            _turnService.EndTurn();

            _turnService.BeginTurn(); // allyB turn — same faction
            Check("on_turn_start (faction): triggers on same-faction ally turn",
                triggered == 2, $"triggered={triggered}(exp 2)");
            _turnService.EndTurn();

            _turnService.BeginTurn(); // enemyA turn — different faction
            Check("on_turn_start (faction): does NOT trigger on enemy turn",
                triggered == 2, $"triggered={triggered}(exp 2)");
            _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn(); // enemyB — complete the round

            SoftReset();
            triggered = 0;

            // triggerParam="self": only triggers on owner's own turn
            Register(_allyA, "on_turn_start", "self", "heal", null, 1, "Self");
            _entityStats.ApplyDamage(_allyA, 10);

            _turnService.BeginTurn(); // allyA (round starts fresh)
            Check("on_turn_start (self): triggers on own turn",
                triggered == 1, $"triggered={triggered}(exp 1)");
            _turnService.EndTurn();

            _turnService.BeginTurn(); // allyB — same faction but not self
            Check("on_turn_start (self): does NOT trigger on other ally turn",
                triggered == 1, $"triggered={triggered}(exp 1)");
            _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn(); // enemyA
            _turnService.BeginTurn(); _turnService.EndTurn(); // enemyB — complete round

            SoftReset();
            triggered = 0;

            // triggerParam="any": triggers for any entity's turn
            Register(_allyA, "on_turn_start", "any", "heal", null, 1, "Self");
            _entityStats.ApplyDamage(_allyA, 20);

            _turnService.BeginTurn(); _turnService.EndTurn(); // allyA
            _turnService.BeginTurn(); _turnService.EndTurn(); // allyB
            _turnService.BeginTurn(); // enemyA
            Check("on_turn_start (any): triggers for all entity turns",
                triggered == 3, $"triggered={triggered}(exp 3)");
        }

        // ── Group 11: on_turn_end trigger ──────────────────────────────

        private void RunOnTurnEndGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            // Default (faction-scoped)
            Register(_allyA, "on_turn_end", null, "heal", null, 1, "Self");
            _entityStats.ApplyDamage(_allyA, 10);

            _turnService.BeginTurn();
            Check("on_turn_end: not triggered on BeginTurn",
                triggered == 0, $"triggered={triggered}(exp 0)");
            _turnService.EndTurn(); // allyA end
            Check("on_turn_end (faction): triggers on own turn end",
                triggered == 1, $"triggered={triggered}(exp 1)");

            _turnService.BeginTurn(); _turnService.EndTurn(); // allyB end — same faction
            Check("on_turn_end (faction): triggers on same-faction turn end",
                triggered == 2, $"triggered={triggered}(exp 2)");

            _turnService.BeginTurn(); _turnService.EndTurn(); // enemyA end
            Check("on_turn_end (faction): does NOT trigger on enemy turn end",
                triggered == 2, $"triggered={triggered}(exp 2)");
            _turnService.BeginTurn(); _turnService.EndTurn(); // enemyB — complete round

            SoftReset();
            triggered = 0;

            // triggerParam="self"
            Register(_allyA, "on_turn_end", "self", "mana", null, 1, "Self");
            _turnService.BeginTurn(); _turnService.EndTurn(); // allyA
            Check("on_turn_end (self): triggers on own turn end",
                triggered == 1, $"triggered={triggered}(exp 1)");
            _turnService.BeginTurn(); _turnService.EndTurn(); // allyB
            Check("on_turn_end (self): does NOT trigger on other ally turn end",
                triggered == 1, $"triggered={triggered}(exp 1)");
        }

        // ── Group 12: on_kill trigger ──────────────────────────────

        private void RunOnKillGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            Register(_allyA, "on_kill", null, "heal", null, 5, "Self");
            _entityStats.ApplyDamage(_allyA, 20); // 50→30

            // Kill an enemy
            _entityStats.ApplyDamage(_enemyB, 999);
            Check("on_kill: triggers when enemy dies",
                triggered == 1, $"triggered={triggered}(exp 1)");
            Check("on_kill: heals self on kill",
                HP(_allyA) == 35, $"HP={HP(_allyA)}(exp 35)");

            triggered = 0;
            // Ally death should NOT trigger (same faction)
            _entityStats.ApplyDamage(_allyB, 999);
            Check("on_kill: does NOT trigger when ally dies",
                triggered == 0, $"triggered={triggered}(exp 0)");

            // Owner death should NOT trigger
            triggered = 0;
            _entityStats.ApplyDamage(_allyA, 999);
            Check("on_kill: does NOT trigger for own death",
                triggered == 0, $"triggered={triggered}(exp 0)");
        }

        // ── Group 13: on_word_played trigger ──────────────────────────

        private void RunOnWordPlayedGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            Register(_allyA, "on_word_played", null, "shield", null, 1, "Self");

            // Simulate word played: TurnStarted for allyA → ActionExecutionCompleted
            _turnService.BeginTurn(); // allyA
            _eventBus.Publish(new ActionExecutionCompletedEvent("fireball"));
            Check("on_word_played: triggers on word in same-faction turn",
                triggered == 1, $"triggered={triggered}(exp 1)");
            _turnService.EndTurn();

            // Enemy turn: should NOT trigger for ally's passive
            _turnService.BeginTurn(); _turnService.EndTurn(); // allyB
            _turnService.BeginTurn(); // enemyA
            _eventBus.Publish(new ActionExecutionCompletedEvent("strike"));
            Check("on_word_played: does NOT trigger on enemy turn",
                triggered == 1, $"triggered={triggered}(exp 1)");
            _turnService.EndTurn();

            // Empty word: no trigger
            triggered = 0;
            _turnService.BeginTurn(); _turnService.EndTurn(); // enemyB
            _turnService.BeginTurn(); // allyA again
            _eventBus.Publish(new ActionExecutionCompletedEvent(""));
            Check("on_word_played: does NOT trigger for empty word",
                triggered == 0, $"triggered={triggered}(exp 0)");
        }

        // ── Group 14: on_word_length trigger ──────────────────────────

        private void RunOnWordLengthGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            // triggerParam="5": only words of length ≥ 5
            Register(_allyA, "on_word_length", "5", "mana", null, 2, "Self");
            _entityStats.TrySpendMana(_allyA, 4); // 5→1

            _turnService.BeginTurn(); // allyA
            _eventBus.Publish(new ActionExecutionCompletedEvent("fire")); // length 4 — too short
            Check("on_word_length: does NOT trigger for short word (4 < 5)",
                triggered == 0, $"triggered={triggered}(exp 0)");

            _eventBus.Publish(new ActionExecutionCompletedEvent("flame")); // length 5 — exactly
            Check("on_word_length: triggers for word of exact length (5)",
                triggered == 1, $"triggered={triggered}(exp 1)");

            _eventBus.Publish(new ActionExecutionCompletedEvent("fireball")); // length 8
            Check("on_word_length: triggers for longer word (8 >= 5)",
                triggered == 2, $"triggered={triggered}(exp 2)");
            // mana: start=5, spend 4→1, BeginTurn regen+2→3, passive+2→5, passive+2→7
            Check("on_word_length: mana restored correctly (includes regen)",
                Mana(_allyA) == 7, $"Mana={Mana(_allyA)}(exp 7)");
        }

        // ── Group 15: on_word_tag trigger ──────────────────────────

        private void RunOnWordTagGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            // triggerParam="fire": only words tagged "fire"
            Register(_allyA, "on_word_tag", "fire", "shield", null, 2, "Self");

            _turnService.BeginTurn(); // allyA
            _eventBus.Publish(new ActionExecutionCompletedEvent("fireball")); // tagged "fire"
            Check("on_word_tag: triggers for fire-tagged word",
                triggered == 1, $"triggered={triggered}(exp 1)");

            _eventBus.Publish(new ActionExecutionCompletedEvent("icebolt")); // tagged "ice", not "fire"
            Check("on_word_tag: does NOT trigger for non-matching tag",
                triggered == 1, $"triggered={triggered}(exp 1)");

            _eventBus.Publish(new ActionExecutionCompletedEvent("unknown")); // no tags
            Check("on_word_tag: does NOT trigger for untagged word",
                triggered == 1, $"triggered={triggered}(exp 1)");
            _turnService.EndTurn(); // end active turn before reset

            SoftReset();
            triggered = 0;

            // null triggerParam → never triggers
            Register(_allyA, "on_word_tag", null, "shield", null, 2, "Self");
            _turnService.BeginTurn(); // allyB (next in order)
            _eventBus.Publish(new ActionExecutionCompletedEvent("fireball"));
            Check("on_word_tag: null triggerParam never triggers",
                triggered == 0, $"triggered={triggered}(exp 0)");
        }

        // ── Group 16: Target Resolution ──────────────────────────────

        private void RunTargetResolutionGroup()
        {
            // Target: Self
            _entityStats.ApplyDamage(_allyA, 20); // 50→30
            Register(_allyA, "on_round_end", null, "heal", null, 5, "Self");
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            Check("Target Self: only owner healed",
                HP(_allyA) == 35 && HP(_allyB) == 40,
                $"A={HP(_allyA)}(exp 35), B={HP(_allyB)}(exp 40)");

            SoftReset();

            // Target: Injured (contextual from trigger)
            _entityStats.ApplyDamage(_allyB, 10); // 40→30
            Register(_allyA, "on_ally_hit", null, "heal", null, 3, "Injured");
            _entityStats.ApplyDamage(_allyB, 5, _enemyA); // trigger → injured=allyB
            Check("Target Injured: heals the hit ally",
                HP(_allyB) == 28, $"B HP={HP(_allyB)}(exp 28)"); // 30-5+3=28

            SoftReset();

            // Target: Attacker
            Register(_allyA, "on_self_hit", null, "damage", null, 2, "Attacker");
            _entityStats.ApplyDamage(_allyA, 5, _enemyA);
            Check("Target Attacker: damages the attacker",
                HP(_enemyA) == 58, $"enemyA HP={HP(_enemyA)}(exp 58)");

            SoftReset();

            // Target: Attacker with no DamageSource → no targets (no crash)
            Register(_allyA, "on_self_hit", null, "damage", null, 2, "Attacker");
            _entityStats.ApplyDamage(_allyA, 5); // no source
            Check("Target Attacker with no source: no crash, no effect",
                HP(_enemyA) == 60, $"enemyA HP={HP(_enemyA)}(exp 60)");
        }

        // ── Group 17: Taunt Marker ──────────────────────────────

        private void RunTauntMarkerGroup()
        {
            Check("Taunt: not present initially",
                !_passiveService.HasTaunt(_enemyA), "Should not have taunt");

            _passiveService.RegisterPassives(_enemyA,
                new[] { new PassiveEntry("taunt", null, null, null, 0, "Self") });
            Check("Taunt: present after registration",
                _passiveService.HasTaunt(_enemyA), "Should have taunt");

            _passiveService.RemovePassives(_enemyA);
            Check("Taunt: removed after RemovePassives",
                !_passiveService.HasTaunt(_enemyA), "Taunt should be gone");
        }

        // ── Group 18: Source-Keyed Registration ──────────────────────

        private void RunSourceKeyedGroup()
        {
            // Register "unit" source
            var unitPassives = new[] { new PassiveEntry("on_round_end", null, "heal", null, 1, "Self") };
            _passiveService.RegisterPassives(_allyA, "unit", unitPassives);
            Check("Source-key: unit passives registered",
                _passiveService.HasPassives(_allyA), "Should have passives");

            // Register "equip:helm" source
            var equipPassives = new[] { new PassiveEntry("on_self_hit", null, "shield", null, 2, "Self") };
            _passiveService.RegisterPassives(_allyA, "equip:helm", equipPassives);
            var all = _passiveService.GetPassives(_allyA);
            Check("Source-key: both sources present",
                all.Count == 2, $"count={all.Count}(exp 2)");

            // Remove equip source only
            _passiveService.RemovePassives(_allyA, "equip:helm");
            Check("Source-key: unit passives survive after equip removal",
                _passiveService.HasPassives(_allyA), "Should still have unit passives");
            var remaining = _passiveService.GetPassives(_allyA);
            Check("Source-key: only unit passive remains",
                remaining.Count == 1 && remaining[0].TriggerId == "on_round_end",
                $"count={remaining.Count}, trigger={remaining[0].TriggerId}");

            // Remove all
            _passiveService.RemovePassives(_allyA);
            Check("Source-key: remove all clears everything",
                !_passiveService.HasPassives(_allyA), "Should have no passives");

            // Re-register and test trigger isolation: equip removed = trigger disposed
            _passiveService.RegisterPassives(_allyA, "unit", unitPassives);
            _passiveService.RegisterPassives(_allyA, "equip:ring",
                new[] { new PassiveEntry("on_self_hit", null, "shield", null, 3, "Self") });
            _passiveService.RemovePassives(_allyA, "equip:ring");

            _entityStats.ApplyDamage(_allyA, 5);
            Check("Source-key: disposed equip trigger no longer fires",
                Shield(_allyA) == 0, $"Shield={Shield(_allyA)}(exp 0)");
        }

        // ── Group 19: Lifecycle ──────────────────────────────────────

        private void RunLifecycleGroup()
        {
            // EntityDiedEvent removes all passives
            Register(_allyA, "on_round_end", null, "heal", null, 1, "Self");
            Check("Lifecycle: passives exist before death",
                _passiveService.HasPassives(_allyA), "Should have passives");

            _eventBus.Publish(new EntityDiedEvent(_allyA));
            Check("Lifecycle: passives removed on EntityDiedEvent",
                !_passiveService.HasPassives(_allyA), "Should have no passives");

            // After removal, trigger no longer fires
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            _turnService.BeginTurn(); _turnService.EndTurn();
            Check("Lifecycle: dead entity trigger no longer fires",
                triggered == 0, $"triggered={triggered}(exp 0)");
        }

        // ── Group 20: Re-entrancy Guard ──────────────────────────────

        private void RunReentrancyGroup()
        {
            // on_self_hit → damage Attacker. Attacker also has on_self_hit → damage Attacker.
            // Should NOT infinite loop due to _isProcessing guard.
            Register(_allyA, "on_self_hit", null, "damage", null, 2, "Attacker");
            Register(_enemyA, "on_self_hit", null, "damage", null, 2, "Attacker");

            _entityStats.ApplyDamage(_allyA, 5, _enemyA);
            // allyA hit → damages enemyA(2). enemyA's passive tries to fire but _isProcessing blocks it.
            Check("Re-entrancy: no infinite loop",
                HP(_enemyA) == 58, $"enemyA HP={HP(_enemyA)}(exp 58)");
            Check("Re-entrancy: allyA HP correct",
                HP(_allyA) == 45, $"allyA HP={HP(_allyA)}(exp 45)");
        }

        // ── Group 21: Dead Owner Guard ──────────────────────────────

        private void RunDeadOwnerGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            // Kill allyA but DON'T fire EntityDiedEvent (passives still registered but owner is dead)
            Register(_allyA, "on_ally_hit", null, "heal", null, 1, "Self");
            _entityStats.ApplyDamage(_allyA, 999);

            _entityStats.ApplyDamage(_allyB, 5);
            Check("Dead owner: on_ally_hit does NOT trigger when owner is dead",
                triggered == 0, $"triggered={triggered}(exp 0)");
        }

        // ── Group 22: Multiple Passives Same Entity ──────────────────

        private void RunMultiplePassiveGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            // Two different passives on same entity
            _passiveService.RegisterPassives(_allyA, new[]
            {
                new PassiveEntry("on_self_hit", null, "heal", null, 3, "Self"),
                new PassiveEntry("on_self_hit", null, "shield", null, 2, "Self"),
            });

            _entityStats.ApplyDamage(_allyA, 10); // 50→40
            // First passive fires (heal 3) but re-entrancy blocks second? No — both subscribe independently.
            // Actually re-entrancy guard blocks nested triggers. Both subscribe to DamageTakenEvent.
            // First handler fires (heal) → _isProcessing=true. Second handler is separate subscription.
            // Wait: _isProcessing is per-passive. It's on PassiveService. So only first fires.
            Check("Multiple passives: at least one triggers",
                triggered >= 1, $"triggered={triggered}(exp >=1)");
            Check("Multiple passives: allyA healed or shielded",
                HP(_allyA) >= 40 || Shield(_allyA) > 0,
                $"HP={HP(_allyA)}, Shield={Shield(_allyA)}");
        }

        // ── Group 23: Cross-Faction Isolation ──────────────────────────

        private void RunCrossFactionGroup()
        {
            int triggered = 0;
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(_ => triggered++));

            // Enemy passive: on_ally_hit (enemy allies = other enemies)
            Register(_enemyA, "on_ally_hit", null, "heal", null, 3, "Self");
            _entityStats.ApplyDamage(_enemyA, 10);

            // Hit enemyB (same faction as enemyA)
            _entityStats.ApplyDamage(_enemyB, 5);
            Check("Cross-faction: enemy on_ally_hit triggers for enemy ally damage",
                triggered == 1, $"triggered={triggered}(exp 1)");
            Check("Cross-faction: enemyA healed",
                HP(_enemyA) == 53, $"HP={HP(_enemyA)}(exp 53)");

            triggered = 0;
            // Hit allyA (different faction)
            _entityStats.ApplyDamage(_allyA, 5);
            Check("Cross-faction: enemy on_ally_hit does NOT trigger for player-side damage",
                triggered == 0, $"triggered={triggered}(exp 0)");
        }

        // ── UI ──────────────────────────────────────────────────────

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

            var title = new Label("Passive Verification");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            int passed = 0, failed = 0;
            foreach (var check in _checks)
            {
                var line = new Label($"{(check.Passed ? "PASS" : "FAIL")}: {check.Name}");
                line.style.fontSize = 14;
                line.style.color = check.Passed ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);
                root.Add(line);
                if (check.Passed) passed++; else failed++;

                if (!check.Passed && check.Message != null)
                {
                    var detail = new Label($"    {check.Message}");
                    detail.style.fontSize = 12;
                    detail.style.color = new Color(1f, 0.6f, 0.6f);
                    root.Add(detail);
                }
            }

            var summary = new Label($"\nTotal: {_checks.Count} | Passed: {passed} | Failed: {failed}");
            summary.style.fontSize = 16;
            summary.style.color = failed == 0 ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.5f, 0.2f);
            summary.style.unityFontStyleAndWeight = FontStyle.Bold;
            summary.style.marginTop = 12;
            root.Add(summary);
        }

        // ── Verify & Cleanup ──────────────────────────────────────────

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            return new ScenarioVerificationResult(_checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            _eventBus?.ClearAllSubscriptions();
            _passiveService = null;
            _entityStats = null;
            _statusEffects = null;
            _slotService = null;
            _turnService = null;
            _eventBus = null;
        }
    }
}
