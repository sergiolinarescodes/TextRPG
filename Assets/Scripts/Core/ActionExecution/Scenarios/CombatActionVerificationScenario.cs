using System;
using System.Collections.Generic;
using TextRPG.Core.CombatGrid;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Grid;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionExecution.Scenarios
{
    internal sealed class CombatActionVerificationScenario : DataDrivenScenario
    {
        private IEventBus _eventBus;
        private IEntityStatsService _entityStats;
        private ITurnService _turnService;
        private IStatusEffectService _statusEffects;
        private ICombatGridService _combatGrid;
        private IActionExecutionService _executionService;
        private CombatContext _combatContext;
        private EnemyWordResolver _resolver;

        private readonly EntityId _hero = new("hero");
        private readonly EntityId _enemyA = new("enemy_a");
        private readonly EntityId _enemyB = new("enemy_b");
        private readonly EntityId _enemyC = new("enemy_c");
        private readonly EntityId _enemyD = new("enemy_d");
        private readonly EntityId _allyA = new("ally_a");

        private readonly List<ScenarioVerificationResult.CheckResult> _checks = new();
        private readonly List<IDisposable> _subscriptions = new();

        public CombatActionVerificationScenario() : base(new TestScenarioDefinition(
            "combat-action-verification",
            "Combat Action Verification",
            "Verifies correctness of all action handlers: damage formulas, healing caps, status effects, shock interactions, stat modifiers, combos, and target types.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _checks.Clear();

            RebuildAll(); RunDamageGroup();
            RebuildAll(); RunHealGroup();
            RebuildAll(); RunStatusEffectGroup();
            RebuildAll(); RunEventActionGroup();
            RebuildAll(); RunShockGroup();
            RebuildAll(); RunSummonGroup();
            RebuildAll(); RunStatModifierGroup();
            RebuildAll(); RunComboGroup();
            RebuildAll(); RunTargetTypeGroup();

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

            var unitService = new UnitService(_eventBus);
            _combatGrid = new CombatGridService(_eventBus, unitService);
            _combatGrid.Initialize(8, 8);

            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(_entityStats, _turnService, _eventBus);
            _statusEffects = new StatusEffectService(_eventBus, _entityStats, _turnService, effectHandlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)_statusEffects;

            _combatContext = new CombatContext();
            _combatContext.SetSourceEntity(_hero);
            _combatContext.SetEnemies(new[] { _enemyA, _enemyB, _enemyC, _enemyD });
            _combatContext.SetAllies(new[] { _allyA });
            _combatContext.SetGrid(_combatGrid);
            _combatContext.SetEntityStats(_entityStats);
            _combatContext.SetStatusEffects(_statusEffects);

            var actionHandlerRegistry = ActionExecutionTestFactory.CreateHandlerRegistry(
                _eventBus, _entityStats, _statusEffects, _combatContext, _turnService);

            _resolver = new EnemyWordResolver();
            _executionService = new ActionExecutionService(_eventBus, _resolver, actionHandlerRegistry, _combatContext);

            RegisterEntities();
        }

        private void RegisterEntities()
        {
            _entityStats.RegisterEntity(_hero, maxHealth: 100, strength: 12, magicPower: 8,
                physicalDefense: 5, magicDefense: 4, luck: 3);
            _entityStats.RegisterEntity(_enemyA, maxHealth: 80, strength: 8, magicPower: 6,
                physicalDefense: 4, magicDefense: 3, luck: 2);
            _entityStats.RegisterEntity(_enemyB, maxHealth: 60, strength: 6, magicPower: 4,
                physicalDefense: 3, magicDefense: 2, luck: 1);
            _entityStats.RegisterEntity(_enemyC, maxHealth: 100, strength: 10, magicPower: 8,
                physicalDefense: 6, magicDefense: 5, luck: 4);
            _entityStats.RegisterEntity(_enemyD, maxHealth: 40, strength: 4, magicPower: 3,
                physicalDefense: 2, magicDefense: 1, luck: 1);
            _entityStats.RegisterEntity(_allyA, maxHealth: 70, strength: 7, magicPower: 5,
                physicalDefense: 4, magicDefense: 3, luck: 2);

            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });

            _combatGrid.RegisterCombatant(_hero, new UnitDefinition(new UnitId("hero"), "HERO", 100, 12, 8, 8, Color.cyan), new GridPosition(3, 3));
            _combatGrid.RegisterCombatant(_enemyA, new UnitDefinition(new UnitId("enemy_a"), "ENEMY_A", 80, 8, 6, 6, Color.red), new GridPosition(4, 3));
            _combatGrid.RegisterCombatant(_enemyB, new UnitDefinition(new UnitId("enemy_b"), "ENEMY_B", 60, 6, 4, 4, Color.red), new GridPosition(3, 4));
            _combatGrid.RegisterCombatant(_enemyC, new UnitDefinition(new UnitId("enemy_c"), "ENEMY_C", 100, 10, 8, 8, Color.red), new GridPosition(5, 3));
            _combatGrid.RegisterCombatant(_enemyD, new UnitDefinition(new UnitId("enemy_d"), "ENEMY_D", 40, 4, 3, 3, Color.red), new GridPosition(3, 5));
            _combatGrid.RegisterCombatant(_allyA, new UnitDefinition(new UnitId("ally_a"), "ALLY_A", 70, 7, 5, 5, Color.green), new GridPosition(2, 3));
        }

        private void SoftReset()
        {
            var entities = new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA };
            foreach (var e in entities)
            {
                _entityStats.ApplyHeal(e, 9999);
                _statusEffects.RemoveAllEffects(e);
            }
            _resolver.Clear();
        }

        // ── Helpers ──────────────────────────────────────────────────

        private void Check(string name, bool passed, string detail = null)
        {
            _checks.Add(new ScenarioVerificationResult.CheckResult(name, passed, passed ? null : detail));
            Debug.Log($"[ActionVerification] {(passed ? "PASS" : "FAIL")}: {name}{(passed ? "" : $" - {detail}")}");
        }

        private void Exec(string word) => _executionService.ExecuteWord(word);
        private int HP(EntityId id) => _entityStats.GetCurrentHealth(id);
        private int Stat(EntityId id, StatType stat) => _entityStats.GetStat(id, stat);

        // ── Group 1: Damage ──────────────────────────────────────────

        private void RunDamageGroup()
        {
            // Damage(3) → SingleEnemy → enemy_a: Max(1, 3*12/4) = 9
            _resolver.RegisterWord("test_dmg_single",
                new List<WordActionMapping> { new("Damage", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_dmg_single");
            int expected = Math.Max(1, 3 * 12 / Math.Max(1, 4));
            Check("Damage single: correct HP reduction",
                HP(_enemyA) == 80 - expected,
                $"Expected HP={80 - expected}, got {HP(_enemyA)}");

            SoftReset();

            // Damage(2) → AreaEnemies (all enemies)
            _resolver.RegisterWord("test_dmg_all",
                new List<WordActionMapping> { new("Damage", 2) },
                new WordMeta("AreaEnemies", 0, 0, AreaShape.Single));
            Exec("test_dmg_all");
            int dA = Math.Max(1, 2 * 12 / 4); // 6
            int dB = Math.Max(1, 2 * 12 / 3); // 8
            int dC = Math.Max(1, 2 * 12 / 6); // 4
            int dD = Math.Max(1, 2 * 12 / 2); // 12
            bool allOk = HP(_enemyA) == 80 - dA && HP(_enemyB) == 60 - dB &&
                         HP(_enemyC) == 100 - dC && HP(_enemyD) == 40 - dD;
            Check("Damage all enemies: correct HP reductions",
                allOk,
                $"A={HP(_enemyA)}(exp {80 - dA}), B={HP(_enemyB)}(exp {60 - dB}), C={HP(_enemyC)}(exp {100 - dC}), D={HP(_enemyD)}(exp {40 - dD})");

            SoftReset();

            // Damage(99) → SingleEnemy → overkill, entity dies
            bool died = false;
            _subscriptions.Add(_eventBus.Subscribe<EntityDiedEvent>(e =>
            {
                if (e.EntityId.Equals(_enemyA)) died = true;
            }));
            _resolver.RegisterWord("test_dmg_overkill",
                new List<WordActionMapping> { new("Damage", 99) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_dmg_overkill");
            Check("Damage overkill: entity dies at HP=0",
                HP(_enemyA) == 0 && died,
                $"HP={HP(_enemyA)}, died={died}");
        }

        // ── Group 2: Heal ──────────────────────────────────────────

        private void RunHealGroup()
        {
            // Pre-damage hero 20, Heal(5) → Self → HP=85
            _entityStats.ApplyDamage(_hero, 20);
            _resolver.RegisterWord("test_heal",
                new List<WordActionMapping> { new("Heal", 5) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_heal");
            Check("Heal self: correct HP restoration",
                HP(_hero) == 85,
                $"Expected HP=85, got {HP(_hero)}");

            SoftReset();

            // Hero at full, Heal(10) → HP stays 100
            _resolver.RegisterWord("test_heal_cap",
                new List<WordActionMapping> { new("Heal", 10) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_heal_cap");
            Check("Heal capped: HP does not exceed max",
                HP(_hero) == 100,
                $"Expected HP=100, got {HP(_hero)}");

            SoftReset();

            // Pre-damage hero+ally 10, Heal(3) → AllAlliesAndSelf
            _entityStats.ApplyDamage(_hero, 10);
            _entityStats.ApplyDamage(_allyA, 10);
            _resolver.RegisterWord("test_heal_allies",
                new List<WordActionMapping> { new("Heal", 3) },
                new WordMeta("AllAlliesAndSelf", 0, 0, AreaShape.Single));
            Exec("test_heal_allies");
            Check("Heal allies+self: both healed",
                HP(_hero) == 93 && HP(_allyA) == 63,
                $"Hero HP={HP(_hero)}(exp 93), Ally HP={HP(_allyA)}(exp 63)");
        }

        // ── Group 3: Status Effects ──────────────────────────────────

        private void RunStatusEffectGroup()
        {
            _resolver.RegisterWord("test_burn",
                new List<WordActionMapping> { new("Burn", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_burn");
            Check("Burn: enemy has Burning effect",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Burning),
                "Enemy_a does not have Burning");

            SoftReset();

            _resolver.RegisterWord("test_wet",
                new List<WordActionMapping> { new("Water", 4) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_wet");
            Check("Wet: enemy has Wet effect",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Wet),
                "Enemy_a does not have Wet");

            SoftReset();

            _resolver.RegisterWord("test_fear",
                new List<WordActionMapping> { new("Fear", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_fear");
            Check("Fear: enemy has Fear effect",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Fear),
                "Enemy_a does not have Fear");

            SoftReset();

            _resolver.RegisterWord("test_stun",
                new List<WordActionMapping> { new("Stun", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_stun");
            Check("Stun: enemy has Stun effect",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Stun),
                "Enemy_a does not have Stun");
        }

        // ── Group 4: Event-based Actions ──────────────────────────────

        private void RunEventActionGroup()
        {
            int fireDuration = -1;
            _subscriptions.Add(_eventBus.Subscribe<FireGridStatusEvent>(e => fireDuration = e.Duration));
            _resolver.RegisterWord("test_fire",
                new List<WordActionMapping> { new("Fire", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_fire");
            Check("Fire: FireGridStatusEvent with correct duration",
                fireDuration == 3,
                $"Expected duration=3, got {fireDuration}");

            SoftReset();

            int pushValue = -1;
            EntityId pushTarget = default;
            _subscriptions.Add(_eventBus.Subscribe<PushActionEvent>(e =>
            {
                pushValue = e.Value;
                pushTarget = e.Target;
            }));
            _resolver.RegisterWord("test_push",
                new List<WordActionMapping> { new("Push", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_push");
            Check("Push: PushActionEvent with correct value and target",
                pushValue == 2 && pushTarget.Equals(_enemyA),
                $"Value={pushValue}(exp 2), target={pushTarget.Value}(exp enemy_a)");
        }

        // ── Group 5: Shock ──────────────────────────────────────────

        private void RunShockGroup()
        {
            // Shock(4) → enemy_a: direct 4 damage (no defense calc)
            _resolver.RegisterWord("test_shock",
                new List<WordActionMapping> { new("Shock", 4) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_shock");
            Check("Shock base: correct damage (no defense calc)",
                HP(_enemyA) == 76,
                $"Expected HP=76, got {HP(_enemyA)}");

            SoftReset();

            // Pre-wet enemy_a, Shock(4) → doubled to 8
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Wet, 5, _hero);
            _resolver.RegisterWord("test_shock_wet",
                new List<WordActionMapping> { new("Shock", 4) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_shock_wet");
            Check("Shock wet: damage doubled when target is wet",
                HP(_enemyA) == 72,
                $"Expected HP=72, got {HP(_enemyA)}");

            SoftReset();

            // Shock enemy_a at (4,3) → chains to enemy_c at (5,3)
            _resolver.RegisterWord("test_shock_chain",
                new List<WordActionMapping> { new("Shock", 4) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_shock_chain");
            Check("Shock chain: adjacent enemy takes secondary damage",
                HP(_enemyA) == 76 && HP(_enemyC) == 96,
                $"A HP={HP(_enemyA)}(exp 76), C HP={HP(_enemyC)}(exp 96)");
        }

        // ── Group 6: Summon ──────────────────────────────────────────

        private void RunSummonGroup()
        {
            int summonMaxHp = -1;
            _subscriptions.Add(_eventBus.Subscribe<EntityRegisteredEvent>(e =>
            {
                if (e.EntityId.Value.StartsWith("summon"))
                    summonMaxHp = e.MaxHealth;
            }));
            _resolver.RegisterWord("test_summon",
                new List<WordActionMapping> { new("Summon", 4) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_summon");
            Check("Summon: new entity with correct HP",
                summonMaxHp == 20,
                $"Expected summon HP=20, got {summonMaxHp}");
        }

        // ── Group 7: Stat Modifiers ──────────────────────────────────

        private void RunStatModifierGroup()
        {
            // BuffStrength(3) → Self → hero str 12→15
            _resolver.RegisterWord("test_buff_str",
                new List<WordActionMapping> { new("BuffStrength", 3) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_buff_str");
            Check("BuffStrength: hero strength increased",
                Stat(_hero, StatType.Strength) == 15,
                $"Expected str=15, got {Stat(_hero, StatType.Strength)}");

            SoftReset();

            // DebuffPhysicalDefense(2) → SingleEnemy → enemy_a pDef 4→2
            _resolver.RegisterWord("test_debuff_pdef",
                new List<WordActionMapping> { new("DebuffPhysicalDefense", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_debuff_pdef");
            Check("DebuffPhysicalDefense: enemy defense reduced",
                Stat(_enemyA, StatType.PhysicalDefense) == 2,
                $"Expected pDef=2, got {Stat(_enemyA, StatType.PhysicalDefense)}");

            SoftReset();

            // BuffMagicPower(4) → Self → hero magic 8→12
            _resolver.RegisterWord("test_buff_magic",
                new List<WordActionMapping> { new("BuffMagicPower", 4) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_buff_magic");
            Check("BuffMagicPower: hero magic power increased",
                Stat(_hero, StatType.MagicPower) == 12,
                $"Expected magic=12, got {Stat(_hero, StatType.MagicPower)}");
        }

        // ── Group 8: Combos ──────────────────────────────────────────

        private void RunComboGroup()
        {
            // Damage(2)+Burn(3) → SingleEnemy: damage + Burning
            // damage = Max(1, 2*12/4) = 6
            _resolver.RegisterWord("test_combo_dmg_burn",
                new List<WordActionMapping> { new("Damage", 2), new("Burn", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_combo_dmg_burn");
            int comboDmg = Math.Max(1, 2 * 12 / 4);
            Check("Combo damage+burn: damage dealt and Burning applied",
                HP(_enemyA) == 80 - comboDmg && _statusEffects.HasEffect(_enemyA, StatusEffectType.Burning),
                $"HP={HP(_enemyA)}(exp {80 - comboDmg}), Burning={_statusEffects.HasEffect(_enemyA, StatusEffectType.Burning)}");

            SoftReset();

            // Water(4)+Shock(3) → SingleEnemy: wet first, shock doubled → 6
            _resolver.RegisterWord("test_combo_wet_shock",
                new List<WordActionMapping> { new("Water", 4), new("Shock", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_combo_wet_shock");
            Check("Combo wet+shock: shock damage doubled by wet",
                HP(_enemyA) == 74,
                $"Expected HP=74, got {HP(_enemyA)}");

            SoftReset();

            // Damage(1)+Stun(2)+Fear(3) → SingleEnemy: damage + Stun + Fear
            // damage = Max(1, 1*12/4) = 3
            _resolver.RegisterWord("test_combo_triple",
                new List<WordActionMapping> { new("Damage", 1), new("Stun", 2), new("Fear", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_combo_triple");
            int tripleDmg = Math.Max(1, 1 * 12 / 4);
            bool tripleOk = HP(_enemyA) == 80 - tripleDmg &&
                            _statusEffects.HasEffect(_enemyA, StatusEffectType.Stun) &&
                            _statusEffects.HasEffect(_enemyA, StatusEffectType.Fear);
            Check("Combo triple: damage + Stun + Fear",
                tripleOk,
                $"HP={HP(_enemyA)}(exp {80 - tripleDmg}), Stun={_statusEffects.HasEffect(_enemyA, StatusEffectType.Stun)}, Fear={_statusEffects.HasEffect(_enemyA, StatusEffectType.Fear)}");

            SoftReset();

            // Mixed targets: Damage(2)→SingleEnemy + BuffStrength(3)→Self
            _resolver.RegisterWord("test_combo_mixed",
                new List<WordActionMapping>
                {
                    new("Damage", 2, Target: "SingleEnemy"),
                    new("BuffStrength", 3, Target: "Self")
                },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_combo_mixed");
            int mixedDmg = Math.Max(1, 2 * 12 / 4);
            Check("Combo mixed targets: enemy damaged + hero buffed",
                HP(_enemyA) == 80 - mixedDmg && Stat(_hero, StatType.Strength) == 15,
                $"Enemy HP={HP(_enemyA)}(exp {80 - mixedDmg}), Hero str={Stat(_hero, StatType.Strength)}(exp 15)");
        }

        // ── Group 9: Target Types ──────────────────────────────────

        private void RunTargetTypeGroup()
        {
            // LowestHealthEnemy: enemy_d has HP=40 (lowest at full health)
            _resolver.RegisterWord("test_lowest_hp",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("LowestHealthEnemy", 0, 0, AreaShape.Single));
            Exec("test_lowest_hp");
            int lowestDmg = Math.Max(1, 1 * 12 / 2); // 6, enemy_d pDef=2
            bool othersOk = HP(_enemyA) == 80 && HP(_enemyB) == 60 && HP(_enemyC) == 100;
            Check("Target lowest HP: only lowest-HP enemy hit",
                HP(_enemyD) == 40 - lowestDmg && othersOk,
                $"D HP={HP(_enemyD)}(exp {40 - lowestDmg}), others unchanged={othersOk}");

            SoftReset();

            // AllBurningEnemies: pre-burn enemy_a + enemy_c
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 3, _hero);
            _statusEffects.ApplyEffect(_enemyC, StatusEffectType.Burning, 3, _hero);
            _resolver.RegisterWord("test_burning",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("AllBurningEnemies", 0, 0, AreaShape.Single));
            Exec("test_burning");
            int bDmgA = Math.Max(1, 1 * 12 / 4); // 3
            int bDmgC = Math.Max(1, 1 * 12 / 6); // 2
            bool burningOk = HP(_enemyA) == 80 - bDmgA && HP(_enemyC) == 100 - bDmgC &&
                             HP(_enemyB) == 60 && HP(_enemyD) == 40;
            Check("Target burning enemies: only burning enemies hit",
                burningOk,
                $"A={HP(_enemyA)}(exp {80 - bDmgA}), C={HP(_enemyC)}(exp {100 - bDmgC}), B={HP(_enemyB)}(exp 60), D={HP(_enemyD)}(exp 40)");

            SoftReset();

            // Melee: hero at (3,3), adjacent enemies: enemy_a(4,3), enemy_b(3,4)
            _resolver.RegisterWord("test_melee",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("Melee", 0, 0, AreaShape.Single));
            Exec("test_melee");
            int mDmgA = Math.Max(1, 1 * 12 / 4); // 3
            int mDmgB = Math.Max(1, 1 * 12 / 3); // 4
            bool meleeOk = HP(_enemyA) == 80 - mDmgA && HP(_enemyB) == 60 - mDmgB &&
                           HP(_enemyC) == 100 && HP(_enemyD) == 40;
            Check("Target melee: only adjacent enemies hit",
                meleeOk,
                $"A={HP(_enemyA)}(exp {80 - mDmgA}), B={HP(_enemyB)}(exp {60 - mDmgB}), C={HP(_enemyC)}(exp 100), D={HP(_enemyD)}(exp 40)");
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

            var title = new Label("Combat Action Verification");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            int passed = 0, failed = 0;
            foreach (var check in _checks)
            {
                if (check.Passed) passed++; else failed++;

                var row = new Label($"{(check.Passed ? "PASS" : "FAIL")}: {check.Name}");
                row.style.fontSize = 14;
                row.style.color = check.Passed ? new Color(0.3f, 0.9f, 0.3f) : new Color(1f, 0.3f, 0.3f);
                row.style.marginBottom = 2;
                root.Add(row);

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
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();

            (_executionService as IDisposable)?.Dispose();
            (_statusEffects as IDisposable)?.Dispose();
            (_combatGrid as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            (_turnService as IDisposable)?.Dispose();
            _executionService = null;
            _statusEffects = null;
            _combatGrid = null;
            _entityStats = null;
            _turnService = null;
            _combatContext = null;
            _resolver = null;
            _eventBus = null;
        }
    }
}
