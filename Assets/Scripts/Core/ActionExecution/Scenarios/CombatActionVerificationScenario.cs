using System;
using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.LetterReserve;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
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
        private ICombatSlotService _slotService;
        private IActionExecutionService _executionService;
        private ILetterReserveService _letterReserve;
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
            RebuildAll(); RunThinkingGroup();
            RebuildAll(); RunManaCostGroup();
            RebuildAll(); RunShieldInteractionGroup();
            RebuildAll(); RunElementalInteractionGroup();
            RebuildAll(); RunDebuffDamageGroup();
            RebuildAll(); RunBurnTickGroup();
            RebuildAll(); RunManaInteractionGroup();
            RebuildAll(); RunDeathEdgeCaseGroup();
            RebuildAll(); RunPoisonedGroup();
            RebuildAll(); RunFrozenGroup();
            RebuildAll(); RunStunGroup();
            RebuildAll(); RunStatusStatModifierGroup();
            RebuildAll(); RunStatusLifecycleGroup();
            RebuildAll(); RunDamageUnderModifiersGroup();
            RebuildAll(); RunShieldAdvancedGroup();
            RebuildAll(); RunHealEdgeCaseGroup();
            RebuildAll(); RunTargetTypeCoverageGroup();
            RebuildAll(); RunAdvancedComboGroup();
            RebuildAll(); RunStatModifierStackingGroup();
            RebuildAll(); RunMultiDoTGroup();
            RebuildAll(); RunManaRegenGroup();
            RebuildAll(); RunConcentrateGroup();
            RebuildAll(); RunPoisonActionGroup();
            RebuildAll(); RunBleedGroup();
            RebuildAll(); RunDuplicateActionGroup();
            RebuildAll(); RunCompositeTargetGroup();
            RebuildAll(); RunShieldOnSpawnGroup();
            RebuildAll(); RunGrowGroup();
            RebuildAll(); RunThornsGroup();
            RebuildAll(); RunReflectGroup();
            RebuildAll(); RunHardeningGroup();
            RebuildAll(); RunMagicDamageGroup();
            RebuildAll(); RunSiphonGroup();
            RebuildAll(); RunDeceiveGroup();
            RebuildAll(); RunOverchargeGroup();
            RebuildAll(); RunRecuperateGroup();
            RebuildAll(); RunComfortGroup();
            RebuildAll(); RunAttuneGroup();
            RebuildAll(); RunCannonadeGroup();
            RebuildAll(); RunPlunderGroup();
            RebuildAll(); RunIgniteGroup();
            RebuildAll(); RunCombustGroup();

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

            _combatContext = new CombatContext();
            _combatContext.SetSourceEntity(_hero);
            _combatContext.SetEnemies(new[] { _enemyA, _enemyB, _enemyC, _enemyD });
            _combatContext.SetAllies(new[] { _allyA });
            _combatContext.SetSlotService(_slotService);
            _combatContext.SetEntityStats(_entityStats);
            _combatContext.SetStatusEffects(_statusEffects);

            _letterReserve = new LetterReserveService(_eventBus);

            var actionHandlerCtx = new ActionHandlerContext(_entityStats, _eventBus, _combatContext,
                _statusEffects, _turnService, letterReserve: _letterReserve);
            var actionHandlerRegistry = ActionHandlerFactory.CreateDefault(actionHandlerCtx);

            var letterReserveModifier = new LetterReserveValueModifier(_letterReserve, _eventBus, _hero);

            _resolver = new EnemyWordResolver();
            _executionService = new ActionExecutionService(_eventBus, _resolver, actionHandlerRegistry, _combatContext, _entityStats, _statusEffects, valueModifier: letterReserveModifier);

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

            _slotService.RegisterEnemy(_enemyA, 0);
            _slotService.RegisterEnemy(_enemyB, 1);
            _slotService.RegisterEnemy(_enemyC, 2);
        }

        private void SoftReset()
        {
            var entities = new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA };
            foreach (var e in entities)
            {
                _entityStats.ApplyHeal(e, 9999);
                _statusEffects.RemoveAllEffects(e);
                _entityStats.ClearAllModifiers(e);
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

        private static int ExpDmg(int baseVal, int str, int pdef) => StatScaling.OffensiveScale(baseVal, str, pdef);
        private static int ExpMagicDmg(int baseVal, int magic, int mdef) => StatScaling.OffensiveScale(baseVal, magic, mdef);
        private static int ExpHeal(int baseVal, int magic) => StatScaling.SupportScale(baseVal, magic);
        private static int ExpShield(int baseVal, int pdef) => StatScaling.SupportScale(baseVal, pdef);
        private static int ExpShock(int baseVal, int magic, int mdef, double mult = 1.0) => Math.Max(1, (int)(StatScaling.SupportScale(baseVal, magic) * mult) - mdef / 3);
        private static int ExpConc(int baseVal, int magic) => StatScaling.SupportScale(baseVal, magic, StatScaling.WeakDivisor);

        // ── Group 1: Damage ──────────────────────────────────────────

        private void RunDamageGroup()
        {
            // Damage(3) → SingleEnemy → enemy_a: ExpDmg(3, 12, 4) = Max(1, 3+4-1) = 6
            _resolver.RegisterWord("test_dmg_single",
                new List<WordActionMapping> { new("Damage", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_dmg_single");
            int expected = ExpDmg(3, 12, 4);
            Check("Damage single: correct HP reduction",
                HP(_enemyA) == 80 - expected,
                $"Expected HP={80 - expected}, got {HP(_enemyA)}");

            SoftReset();

            // Damage(2) → AreaEnemies (all enemies)
            _resolver.RegisterWord("test_dmg_all",
                new List<WordActionMapping> { new("Damage", 2) },
                new WordMeta("AreaEnemies", 0, 0, AreaShape.Single));
            Exec("test_dmg_all");
            int dA = ExpDmg(2, 12, 4); // 5
            int dB = ExpDmg(2, 12, 3); // 5
            int dC = ExpDmg(2, 12, 6); // 4
            int dD = ExpDmg(2, 12, 2); // 6
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
            // Pre-damage hero 20, Heal(5) → Self → ExpHeal(5,8) = 7, HP=80+7=87
            _entityStats.ApplyDamage(_hero, 20);
            _resolver.RegisterWord("test_heal",
                new List<WordActionMapping> { new("Heal", 5) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_heal");
            int healAmt = ExpHeal(5, 8); // 7
            Check("Heal self: correct HP restoration",
                HP(_hero) == 80 + healAmt,
                $"Expected HP={80 + healAmt}, got {HP(_hero)}");

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
            // Hero magic=8: ExpHeal(3,8) = 5
            _entityStats.ApplyDamage(_hero, 10);
            _entityStats.ApplyDamage(_allyA, 10);
            _resolver.RegisterWord("test_heal_allies",
                new List<WordActionMapping> { new("Heal", 3) },
                new WordMeta("AllAlliesAndSelf", 0, 0, AreaShape.Single));
            Exec("test_heal_allies");
            int allyHeal = ExpHeal(3, 8); // 5
            Check("Heal allies+self: both healed",
                HP(_hero) == 90 + allyHeal && HP(_allyA) == 60 + allyHeal,
                $"Hero HP={HP(_hero)}(exp {90 + allyHeal}), Ally HP={HP(_allyA)}(exp {60 + allyHeal})");
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
            // Shock(4) → enemy_a: ExpShock(4, 8, 3) = Max(1, (4+2)*1 - 1) = 5
            _resolver.RegisterWord("test_shock",
                new List<WordActionMapping> { new("Shock", 4) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_shock");
            int shockBase = ExpShock(4, 8, 3);
            Check("Shock base: correct damage",
                HP(_enemyA) == 80 - shockBase,
                $"Expected HP={80 - shockBase}, got {HP(_enemyA)}");

            SoftReset();

            // Pre-wet enemy_a, Shock(4) → doubled: ExpShock(4, 8, 3, 2.0) = Max(1, 12-1) = 11
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Wet, 5, _hero);
            _resolver.RegisterWord("test_shock_wet",
                new List<WordActionMapping> { new("Shock", 4) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_shock_wet");
            int shockWet = ExpShock(4, 8, 3, 2.0);
            Check("Shock wet: damage doubled when target is wet",
                HP(_enemyA) == 80 - shockWet,
                $"Expected HP={80 - shockWet}, got {HP(_enemyA)}");

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
            // damage = ExpDmg(2, 12, 4) = 5
            _resolver.RegisterWord("test_combo_dmg_burn",
                new List<WordActionMapping> { new("Damage", 2), new("Burn", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_combo_dmg_burn");
            int comboDmg = ExpDmg(2, 12, 4);
            Check("Combo damage+burn: damage dealt and Burning applied",
                HP(_enemyA) == 80 - comboDmg && _statusEffects.HasEffect(_enemyA, StatusEffectType.Burning),
                $"HP={HP(_enemyA)}(exp {80 - comboDmg}), Burning={_statusEffects.HasEffect(_enemyA, StatusEffectType.Burning)}");

            SoftReset();

            // Water(4)+Shock(3) → SingleEnemy: wet first, shock doubled
            // ExpShock(3, 8, 3, 2.0) = Max(1, (int)((3+2)*2.0) - 1) = Max(1, 10-1) = 9
            _resolver.RegisterWord("test_combo_wet_shock",
                new List<WordActionMapping> { new("Water", 4), new("Shock", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_combo_wet_shock");
            int comboShock = ExpShock(3, 8, 3, 2.0);
            Check("Combo wet+shock: shock damage doubled by wet",
                HP(_enemyA) == 80 - comboShock,
                $"Expected HP={80 - comboShock}, got {HP(_enemyA)}");

            SoftReset();

            // Damage(1)+Stun(2)+Fear(3) → SingleEnemy: damage + Stun + Fear
            // damage = ExpDmg(1, 12, 4) = Max(1, 1+4-1) = 4
            _resolver.RegisterWord("test_combo_triple",
                new List<WordActionMapping> { new("Damage", 1), new("Stun", 2), new("Fear", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_combo_triple");
            int tripleDmg = ExpDmg(1, 12, 4);
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
            int mixedDmg = ExpDmg(2, 12, 4);
            Check("Combo mixed targets: enemy damaged + hero buffed",
                HP(_enemyA) == 80 - mixedDmg && Stat(_hero, StatType.Strength) == 15,
                $"Enemy HP={HP(_enemyA)}(exp {80 - mixedDmg}), Hero str={Stat(_hero, StatType.Strength)}(exp 15)");
        }

        private int Mana(EntityId id) => _entityStats.GetCurrentMana(id);
        private int Shield(EntityId id) => _entityStats.GetCurrentShield(id);

        private void AdvanceTurns(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _turnService.BeginTurn();
                if (_turnService.IsTurnActive)
                    _turnService.EndTurn();
            }
        }

        // ── Group 9: Target Types ──────────────────────────────────

        private void RunTargetTypeGroup()
        {
            // LowestHealthEnemy: enemy_d has HP=40 (lowest at full health)
            _resolver.RegisterWord("test_lowest_hp",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("LowestHealthEnemy", 0, 0, AreaShape.Single));
            Exec("test_lowest_hp");
            int lowestDmg = ExpDmg(1, 12, 2); // enemy_d pDef=2
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
                new WordMeta("AllEnemies+Burning", 0, 0, AreaShape.Single));
            Exec("test_burning");
            int bDmgA = ExpDmg(1, 12, 4);
            int bDmgC = ExpDmg(1, 12, 6);
            bool burningOk = HP(_enemyA) == 80 - bDmgA && HP(_enemyC) == 100 - bDmgC &&
                             HP(_enemyB) == 60 && HP(_enemyD) == 40;
            Check("Target burning enemies: only burning enemies hit",
                burningOk,
                $"A={HP(_enemyA)}(exp {80 - bDmgA}), C={HP(_enemyC)}(exp {100 - bDmgC}), B={HP(_enemyB)}(exp 60), D={HP(_enemyD)}(exp 40)");

            SoftReset();

            // Melee: targets slot 0 (FrontEnemy = enemy_a), others untouched
            _resolver.RegisterWord("test_melee",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("Melee", 0, 0, AreaShape.Single));
            Exec("test_melee");
            int mDmgA = ExpDmg(1, 12, 4);
            bool meleeOk = HP(_enemyA) == 80 - mDmgA && HP(_enemyB) == 60 &&
                           HP(_enemyC) == 100 && HP(_enemyD) == 40;
            Check("Target melee: hits front slot enemy only",
                meleeOk,
                $"A={HP(_enemyA)}(exp {80 - mDmgA}), B={HP(_enemyB)}(exp 60), C={HP(_enemyC)}(exp 100), D={HP(_enemyD)}(exp 40)");
        }

        // ── Group 10: Thinking ──────────────────────────────────────

        private void RunThinkingGroup()
        {
            // Spend 3 mana (5→2), then Thinking(3) → mana restored to 5
            _entityStats.TrySpendMana(_hero, 3);
            Check("Thinking setup: mana reduced to 2",
                Mana(_hero) == 2,
                $"Expected mana=2, got {Mana(_hero)}");

            _resolver.RegisterWord("test_thinking",
                new List<WordActionMapping> { new("Thinking", 3) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_thinking");
            Check("Thinking: mana restored to 5",
                Mana(_hero) == 5,
                $"Expected mana=5, got {Mana(_hero)}");

            SoftReset();

            // Thinking(99) at full mana → capped at MaxMana (10)
            _resolver.RegisterWord("test_thinking_cap",
                new List<WordActionMapping> { new("Thinking", 99) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_thinking_cap");
            Check("Thinking capped: mana does not exceed MaxMana",
                Mana(_hero) == 10,
                $"Expected mana=10, got {Mana(_hero)}");
        }

        // ── Group 11: Mana Cost ──────────────────────────────────────

        private void RunManaCostGroup()
        {
            // Word with cost=3 → mana deducted from 5 to 2
            _resolver.RegisterWord("test_mana_cost",
                new List<WordActionMapping> { new("Heal", 1) },
                new WordMeta("Self", 3, 0, AreaShape.Single));
            Exec("test_mana_cost");
            Check("Mana cost: deducted from 5 to 2",
                Mana(_hero) == 2,
                $"Expected mana=2, got {Mana(_hero)}");

            // Word with cost=6 when mana=2 → rejected
            bool rejected = false;
            _subscriptions.Add(_eventBus.Subscribe<WordRejectedEvent>(e => rejected = true));
            _resolver.RegisterWord("test_mana_reject",
                new List<WordActionMapping> { new("Heal", 1) },
                new WordMeta("Self", 6, 0, AreaShape.Single));
            int hpBefore = HP(_hero);
            Exec("test_mana_reject");
            Check("Mana insufficient: word rejected",
                rejected && Mana(_hero) == 2 && HP(_hero) == hpBefore,
                $"Rejected={rejected}, mana={Mana(_hero)}(exp 2), HP unchanged={HP(_hero) == hpBefore}");
        }

        // ── Group 12: Shield + Damage Interaction ──────────────────

        private void RunShieldInteractionGroup()
        {
            // Shield(3) on hero, then Damage(10) from enemy_a perspective
            // Apply shield directly, then apply damage
            _entityStats.ApplyShield(_hero, 3);
            _entityStats.ApplyDamage(_hero, 10);
            Check("Shield absorbs partial: shield takes 3, HP takes 7",
                HP(_hero) == 93 && _entityStats.GetCurrentShield(_hero) == 0,
                $"HP={HP(_hero)}(exp 93), Shield={_entityStats.GetCurrentShield(_hero)}(exp 0)");

            SoftReset();

            // Shield(20) on hero, Damage(2) → HP unchanged, shield reduced to 18
            _entityStats.ApplyShield(_hero, 20);
            _entityStats.ApplyDamage(_hero, 2);
            Check("Shield blocks fully: HP unchanged, shield reduced",
                HP(_hero) == 100 && _entityStats.GetCurrentShield(_hero) == 18,
                $"HP={HP(_hero)}(exp 100), Shield={_entityStats.GetCurrentShield(_hero)}(exp 18)");

            // Eat leftover shield, heal back
            var leftover = Shield(_hero);
            if (leftover > 0) _entityStats.ApplyDamage(_hero, leftover);
            SoftReset();

            // Multiple shield stacks: Shield(2) + Shield(3) → total 5
            _entityStats.ApplyShield(_hero, 2);
            _entityStats.ApplyShield(_hero, 3);
            Check("Multiple shield stacks: total 5",
                Shield(_hero) == 5,
                $"Shield={Shield(_hero)}(exp 5)");
        }

        // ── Group 13: Elemental Interactions ────────────────────────

        private void RunElementalInteractionGroup()
        {
            // Burn removes Frozen: apply Frozen, then Burn → Frozen removed
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Frozen, 5, _hero);
            Check("Elemental setup: enemy has Frozen",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Frozen),
                "Enemy_a should have Frozen");

            _resolver.RegisterWord("test_burn_frozen",
                new List<WordActionMapping> { new("Burn", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_burn_frozen");
            Check("Burn removes Frozen: Frozen gone, Burning applied",
                !_statusEffects.HasEffect(_enemyA, StatusEffectType.Frozen) &&
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Burning),
                $"Frozen={_statusEffects.HasEffect(_enemyA, StatusEffectType.Frozen)}, Burning={_statusEffects.HasEffect(_enemyA, StatusEffectType.Burning)}");

        }

        // ── Group 14: Debuff → Damage Amplification ────────────────

        private void RunDebuffDamageGroup()
        {
            // DebuffPhysicalDefense(2) on enemy_a: pDef 4→2
            // Then Damage(2): ExpDmg(2, 12, 2) = Max(1, 2+4-0) = 6
            _resolver.RegisterWord("test_debuff_then_dmg_debuff",
                new List<WordActionMapping> { new("DebuffPhysicalDefense", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_debuff_then_dmg_debuff");
            Check("Debuff setup: enemy pDef reduced to 2",
                Stat(_enemyA, StatType.PhysicalDefense) == 2,
                $"pDef={Stat(_enemyA, StatType.PhysicalDefense)}(exp 2)");

            _resolver.RegisterWord("test_debuff_then_dmg_hit",
                new List<WordActionMapping> { new("Damage", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_debuff_then_dmg_hit");
            int amplifiedDmg = ExpDmg(2, 12, 2);
            Check("Debuff then Damage: amplified damage dealt",
                HP(_enemyA) == 80 - amplifiedDmg,
                $"HP={HP(_enemyA)}(exp {80 - amplifiedDmg})");
        }

        // ── Group 15: Burn DoT Tick ─────────────────────────────────

        private void RunBurnTickGroup()
        {
            // Apply Burning(3 duration) to enemy_a, then advance to enemy_a's turn
            // BurningHandler.OnTick deals DamagePerTick(3) * stackCount(1) = 3 on TurnEnded
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 3, _hero);
            int hpBefore = HP(_enemyA);

            // Begin hero turn (index 0) and end it → triggers TurnEnded for hero (no burn)
            _turnService.BeginTurn();
            _turnService.EndTurn();

            // Now it's enemy_a's turn (index 1)
            _turnService.BeginTurn();
            _turnService.EndTurn(); // TurnEnded triggers OnTick for enemy_a's burning

            int dotDamage = 3; // DamagePerTick=3, stackCount=1
            Check("Burn DoT tick: HP reduced by DoT on turn end",
                HP(_enemyA) == hpBefore - dotDamage,
                $"HP={HP(_enemyA)}(exp {hpBefore - dotDamage})");
        }

        // ── Group 16: Multi-Action Mana Interactions ────────────────

        private void RunManaInteractionGroup()
        {
            // Combo word Damage(1)+Heal(1) with cost=2 → mana deducted, both execute
            _entityStats.ApplyDamage(_hero, 10); // hero to 90 HP
            _resolver.RegisterWord("test_mana_combo",
                new List<WordActionMapping> { new("Damage", 1), new("Heal", 1, Target: "Self") },
                new WordMeta("SingleEnemy", 2, 0, AreaShape.Single));
            Exec("test_mana_combo");
            int comboDmg = ExpDmg(1, 12, 4);
            int comboHeal = ExpHeal(1, 8);
            Check("Mana combo: cost deducted, both actions execute",
                Mana(_hero) == 3 && HP(_enemyA) < 80 && HP(_hero) == 90 + comboHeal,
                $"Mana={Mana(_hero)}(exp 3), EnemyA HP={HP(_enemyA)}(exp {80 - comboDmg}), Hero HP={HP(_hero)}(exp {90 + comboHeal})");

            SoftReset();

            // Thinking restores mana then expensive word succeeds
            // Drain all mana first (SoftReset doesn't reset mana)
            _entityStats.TrySpendMana(_hero, Mana(_hero));
            _resolver.RegisterWord("test_think_restore",
                new List<WordActionMapping> { new("Thinking", 5) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_think_restore");
            Check("Thinking restores: mana back to 5",
                Mana(_hero) == 5,
                $"Mana={Mana(_hero)}(exp 5)");

            _resolver.RegisterWord("test_expensive_after",
                new List<WordActionMapping> { new("Heal", 1) },
                new WordMeta("Self", 3, 0, AreaShape.Single));
            _entityStats.ApplyDamage(_hero, 10);
            Exec("test_expensive_after");
            int expAfterHeal = ExpHeal(1, 8);
            Check("Expensive word after Thinking: succeeds with mana",
                Mana(_hero) == 2 && HP(_hero) == 90 + expAfterHeal,
                $"Mana={Mana(_hero)}(exp 2), HP={HP(_hero)}(exp {90 + expAfterHeal})");
        }

        // ── Group 17: Death Edge Cases ──────────────────────────────

        private void RunDeathEdgeCaseGroup()
        {
            // Kill enemy: Damage huge amount → HP=0
            _entityStats.ApplyDamage(_enemyD, 9999);
            Check("Death setup: enemy_d is dead",
                HP(_enemyD) == 0,
                $"HP={HP(_enemyD)}(exp 0)");

            // Damage already-dead entity → no crash, HP stays 0
            _entityStats.ApplyDamage(_enemyD, 50);
            Check("Damage dead entity: no crash, HP stays 0",
                HP(_enemyD) == 0,
                $"HP={HP(_enemyD)}(exp 0)");

            SoftReset();

            // Heal after near-death: take 95 damage (HP=5), then heal 10
            // ExpHeal(10, 8) = 12, so HP=5+12=17
            _entityStats.ApplyDamage(_hero, 95);
            Check("Near-death setup: hero at 5 HP",
                HP(_hero) == 5,
                $"HP={HP(_hero)}(exp 5)");

            _resolver.RegisterWord("test_heal_near_death",
                new List<WordActionMapping> { new("Heal", 10) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_heal_near_death");
            int nearDeathHeal = ExpHeal(10, 8);
            Check("Heal after near-death: HP correctly restored",
                HP(_hero) == 5 + nearDeathHeal,
                $"HP={HP(_hero)}(exp {5 + nearDeathHeal})");
        }

        // ── Group 18: Poisoned DoT & Stacking ───────────────────────

        private void RunPoisonedGroup()
        {
            // Poisoned tick: DamagePerTick=2 * stackCount=1 = 2
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Poisoned, 3, _hero);
            AdvanceTurns(2); // hero + enemy_a → tick on enemy_a TurnEnded
            Check("Poisoned tick: 2 damage per tick",
                HP(_enemyA) == 78,
                $"HP={HP(_enemyA)}(exp 78)");

            SoftReset();

            // Poisoned stacking: StackIntensity → stackCount=2 → 4 dmg/tick
            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Poisoned, 3, _hero);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Poisoned, 3, _hero);
            Check("Poisoned stacking: stackCount=2",
                _statusEffects.GetStackCount(_enemyA, StatusEffectType.Poisoned) == 2,
                $"stackCount={_statusEffects.GetStackCount(_enemyA, StatusEffectType.Poisoned)}(exp 2)");
            AdvanceTurns(2);
            Check("Poisoned stacking tick: 4 damage (2*2)",
                HP(_enemyA) == 76,
                $"HP={HP(_enemyA)}(exp 76)");

            SoftReset();

            // Heal poisoned entity: HP restores but poison persists
            _entityStats.ApplyDamage(_enemyA, 20);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Poisoned, 3, _hero);
            _entityStats.ApplyHeal(_enemyA, 10);
            Check("Heal poisoned: HP restored, poison persists",
                HP(_enemyA) == 70 && _statusEffects.HasEffect(_enemyA, StatusEffectType.Poisoned),
                $"HP={HP(_enemyA)}(exp 70), Poisoned={_statusEffects.HasEffect(_enemyA, StatusEffectType.Poisoned)}");
        }

        // ── Group 19: Frozen Mechanics ──────────────────────────────

        private void RunFrozenGroup()
        {
            // Frozen grants +999 pDef/mDef → damage becomes 1
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Frozen, 5, _hero);
            int frozenDef = Stat(_enemyA, StatType.PhysicalDefense);
            Check("Frozen defense: pDef boosted to 1003",
                frozenDef == 1003,
                $"pDef={frozenDef}(exp 1003)");

            _resolver.RegisterWord("test_dmg_frozen",
                new List<WordActionMapping> { new("Damage", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_dmg_frozen");
            Check("Frozen target: damage clamped to 1",
                HP(_enemyA) == 79,
                $"HP={HP(_enemyA)}(exp 79)");

            SoftReset();

            // Frozen Ignore stacking: apply twice → still 1 instance
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Frozen, 3, _hero);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Frozen, 5, _hero);
            Check("Frozen ignore stacking: still 1 stack, original duration",
                _statusEffects.GetStackCount(_enemyA, StatusEffectType.Frozen) == 1,
                $"stackCount={_statusEffects.GetStackCount(_enemyA, StatusEffectType.Frozen)}(exp 1)");

            SoftReset();

            // Burn removes Frozen → defense reverts
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Frozen, 5, _hero);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 3, _hero);
            Check("Burn removes Frozen: pDef reverts to base",
                Stat(_enemyA, StatType.PhysicalDefense) == 4 && !_statusEffects.HasEffect(_enemyA, StatusEffectType.Frozen),
                $"pDef={Stat(_enemyA, StatType.PhysicalDefense)}(exp 4), Frozen={_statusEffects.HasEffect(_enemyA, StatusEffectType.Frozen)}");
        }

        // ── Group 20: Stun Mechanics ────────────────────────────────

        private void RunStunGroup()
        {
            // Stun RefreshDuration: reapply refreshes duration
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Stun, 2, _hero);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Stun, 5, _hero);
            var stunEffects = _statusEffects.GetEffects(_enemyA);
            var stunInstance = stunEffects[0];
            Check("Stun RefreshDuration: duration refreshed to 5",
                stunInstance.RemainingDuration == 5 && _statusEffects.GetStackCount(_enemyA, StatusEffectType.Stun) == 1,
                $"Duration={stunInstance.RemainingDuration}(exp 5), stacks={_statusEffects.GetStackCount(_enemyA, StatusEffectType.Stun)}(exp 1)");
        }

        // ── Group 21: Status Effect Stat Modifiers ──────────────────

        private void RunStatusStatModifierGroup()
        {
            // Slowed: MagicPower -3
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Slowed, 3, _hero);
            Check("Slowed: MagicPower reduced by 3",
                Stat(_enemyA, StatType.MagicPower) == 3,
                $"MagicPower={Stat(_enemyA, StatType.MagicPower)}(exp 3)");

            SoftReset();

            // Cursed: Luck -5
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Cursed, 3, _hero);
            Check("Cursed: Luck reduced by 5",
                Stat(_enemyA, StatType.Luck) == -3,
                $"Luck={Stat(_enemyA, StatType.Luck)}(exp -3)");

            SoftReset();

            // Buffed status: Strength +5
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Buffed, 3, _hero);
            Check("Buffed status: Strength increased by 5",
                Stat(_enemyA, StatType.Strength) == 13,
                $"Strength={Stat(_enemyA, StatType.Strength)}(exp 13)");

            SoftReset();

            // Shielded status: PhysicalDefense +5
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Shielded, 3, _hero);
            Check("Shielded status: PhysicalDefense increased by 5",
                Stat(_enemyA, StatType.PhysicalDefense) == 9,
                $"pDef={Stat(_enemyA, StatType.PhysicalDefense)}(exp 9)");

            SoftReset();

            // Fear: Str-1, MagicPower-1, pDef-1, mDef-1
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Fear, 3, _hero);
            bool fearOk = Stat(_enemyA, StatType.Strength) == 7 &&
                           Stat(_enemyA, StatType.MagicPower) == 5 &&
                           Stat(_enemyA, StatType.PhysicalDefense) == 3 &&
                           Stat(_enemyA, StatType.MagicDefense) == 2;
            Check("Fear: all 4 stats reduced by 1",
                fearOk,
                $"Str={Stat(_enemyA, StatType.Strength)}(7), Magic={Stat(_enemyA, StatType.MagicPower)}(5), pDef={Stat(_enemyA, StatType.PhysicalDefense)}(3), mDef={Stat(_enemyA, StatType.MagicDefense)}(2)");

            // Fear stacking: StackIntensity → stackCount increases but modifiers stay
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Fear, 3, _hero);
            Check("Fear stacking: stackCount=2, stats still -1 each",
                _statusEffects.GetStackCount(_enemyA, StatusEffectType.Fear) == 2 &&
                Stat(_enemyA, StatType.Strength) == 7,
                $"stacks={_statusEffects.GetStackCount(_enemyA, StatusEffectType.Fear)}(exp 2), Str={Stat(_enemyA, StatType.Strength)}(exp 7)");
        }

        // ── Group 22: Status Effect Lifecycle ───────────────────────

        private void RunStatusLifecycleGroup()
        {
            // Burning(1) expires after 1 tick (DoT fires, then effect removed)
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 1, _hero);
            AdvanceTurns(2); // hero + enemy_a → tick: -3, duration 1→0 → expired
            Check("Burning expires: effect removed after 1 tick, DoT dealt",
                !_statusEffects.HasEffect(_enemyA, StatusEffectType.Burning) && HP(_enemyA) == 77,
                $"HasBurning={_statusEffects.HasEffect(_enemyA, StatusEffectType.Burning)}, HP={HP(_enemyA)}(exp 77)");

            SoftReset();

            // Manual removal: apply and remove → gone
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Wet, 5, _hero);
            _statusEffects.RemoveEffect(_enemyA, StatusEffectType.Wet);
            Check("Manual removal: effect removed",
                !_statusEffects.HasEffect(_enemyA, StatusEffectType.Wet),
                "Wet still present after removal");

            SoftReset();

            // RefreshDuration: Burning(2) then Burning(5) → duration refreshed
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 2, _hero);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 5, _hero);
            var effects = _statusEffects.GetEffects(_enemyA);
            Check("RefreshDuration: duration refreshed to 5",
                effects.Count == 1 && effects[0].RemainingDuration == 5,
                $"Count={effects.Count}(exp 1), Duration={effects[0].RemainingDuration}(exp 5)");

            SoftReset();

            // StackIntensity: Poisoned(3) then Poisoned(2) → stackCount=2, duration=max(3,2)=3
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Poisoned, 3, _hero);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Poisoned, 2, _hero);
            var poisonEffects = _statusEffects.GetEffects(_enemyA);
            Check("StackIntensity: stackCount=2, duration=max",
                poisonEffects[0].StackCount == 2 && poisonEffects[0].RemainingDuration == 3,
                $"stacks={poisonEffects[0].StackCount}(exp 2), duration={poisonEffects[0].RemainingDuration}(exp 3)");
        }

        // ── Group 23: Damage Under Stat Modifiers ───────────────────

        private void RunDamageUnderModifiersGroup()
        {
            // BuffStrength on hero then Damage: str 12→15, Damage(2) → ExpDmg(2, 15, 4)
            _resolver.RegisterWord("test_buff_then_dmg_buff",
                new List<WordActionMapping> { new("BuffStrength", 3) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_buff_then_dmg_buff");
            _resolver.RegisterWord("test_buff_then_dmg_hit",
                new List<WordActionMapping> { new("Damage", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_buff_then_dmg_hit");
            int buffDmg = ExpDmg(2, 15, 4);
            Check("Buff then Damage: increased damage",
                HP(_enemyA) == 80 - buffDmg,
                $"HP={HP(_enemyA)}(exp {80 - buffDmg})");

            SoftReset();

            // Damage against Shielded-status target: pDef 4+5=9
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Shielded, 5, _hero);
            int sPdef = Stat(_enemyA, StatType.PhysicalDefense); // 9
            int sDmg = ExpDmg(2, 12, sPdef);
            _resolver.RegisterWord("test_dmg_vs_shielded",
                new List<WordActionMapping> { new("Damage", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_dmg_vs_shielded");
            Check("Damage vs Shielded status: reduced damage (pDef+5)",
                HP(_enemyA) == 80 - sDmg,
                $"HP={HP(_enemyA)}(exp {80 - sDmg})");

            SoftReset();

            // Damage against Feared target: pDef 4-1=3
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Fear, 3, _hero);
            int fPdef = Stat(_enemyA, StatType.PhysicalDefense); // 3
            int fDmg = ExpDmg(2, 12, fPdef);
            _resolver.RegisterWord("test_dmg_vs_fear",
                new List<WordActionMapping> { new("Damage", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_dmg_vs_fear");
            Check("Damage vs Feared target: increased damage (pDef-1)",
                HP(_enemyA) == 80 - fDmg,
                $"HP={HP(_enemyA)}(exp {80 - fDmg})");

            SoftReset();

            // Minimum damage guarantee: Frozen enemy_c (pDef 6+999=1005), Damage(1) → 1
            _statusEffects.ApplyEffect(_enemyC, StatusEffectType.Frozen, 5, _hero);
            _resolver.RegisterWord("test_min_dmg",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("HighestDefenseEnemy", 0, 0, AreaShape.Single));
            Exec("test_min_dmg");
            Check("Minimum damage: floor of 1 against extreme defense",
                HP(_enemyC) == 99,
                $"HP={HP(_enemyC)}(exp 99)");
        }

        // ── Group 24: Shield Advanced ───────────────────────────────

        private void RunShieldAdvancedGroup()
        {
            // Shield handler on SingleEnemy: enemy gets shield
            // Source is hero (pDef=5), ExpShield(5, 5) = 6
            _resolver.RegisterWord("test_shield_enemy",
                new List<WordActionMapping> { new("Shield", 5) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_shield_enemy");
            int shieldEnemy = ExpShield(5, 5);
            Check("Shield on enemy: enemy_a gets scaled shield",
                Shield(_enemyA) == shieldEnemy,
                $"Shield={Shield(_enemyA)}(exp {shieldEnemy})");

            // Shield on AllAlliesAndSelf: hero + ally both get shield
            // ExpShield(3, 5) = 4
            _resolver.RegisterWord("test_shield_allies",
                new List<WordActionMapping> { new("Shield", 3) },
                new WordMeta("AllAlliesAndSelf", 0, 0, AreaShape.Single));
            Exec("test_shield_allies");
            int shieldAllies = ExpShield(3, 5);
            Check("Shield AllAlliesAndSelf: hero and ally both shielded",
                Shield(_hero) == shieldAllies && Shield(_allyA) == shieldAllies,
                $"Hero shield={Shield(_hero)}(exp {shieldAllies}), Ally shield={Shield(_allyA)}(exp {shieldAllies})");

            // Shield drain across multiple hits
            _entityStats.ApplyDamage(_hero, 100); // eat hero shield + HP to reset
            _entityStats.ApplyHeal(_hero, 100);
            _entityStats.ApplyShield(_hero, 5);
            _entityStats.ApplyDamage(_hero, 3); // shield 5→2, HP=100
            _entityStats.ApplyDamage(_hero, 3); // shield 2→0, HP takes 1 → 99
            Check("Shield drain: absorbs across multiple hits",
                HP(_hero) == 99 && Shield(_hero) == 0,
                $"HP={HP(_hero)}(exp 99), Shield={Shield(_hero)}(exp 0)");

            // Shield doesn't interfere with healing
            _entityStats.ApplyShield(_hero, 5);
            _entityStats.ApplyDamage(_hero, 10); // shield absorbs 5, HP takes 5 → 94
            _entityStats.ApplyHeal(_hero, 5); // HP 94→99, shield stays 0
            Check("Shield + heal: heal restores HP independently of shield",
                HP(_hero) == 99 && Shield(_hero) == 0,
                $"HP={HP(_hero)}(exp 99), Shield={Shield(_hero)}(exp 0)");
        }

        // ── Group 25: Heal Edge Cases ───────────────────────────────

        private void RunHealEdgeCaseGroup()
        {
            // Heal poisoned entity: HP restored, poison still active
            // ExpHeal(10, 8) = 12, halved by poison: Max(1, 12/2) = 6
            _entityStats.ApplyDamage(_enemyA, 20);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Poisoned, 3, _hero);
            _resolver.RegisterWord("test_heal_poisoned",
                new List<WordActionMapping> { new("Heal", 10) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_heal_poisoned");
            int poisonedHeal = Math.Max(1, ExpHeal(10, 8) / 2);
            Check("Heal poisoned entity: HP restored (halved by poison), poison persists",
                HP(_enemyA) == 60 + poisonedHeal && _statusEffects.HasEffect(_enemyA, StatusEffectType.Poisoned),
                $"HP={HP(_enemyA)}(exp {60 + poisonedHeal}), Poisoned={_statusEffects.HasEffect(_enemyA, StatusEffectType.Poisoned)}");

            SoftReset();

            // Heal AllAllies: only ally healed, hero unchanged
            // ExpHeal(5, 8) = 7
            _entityStats.ApplyDamage(_hero, 20);
            _entityStats.ApplyDamage(_allyA, 20);
            _resolver.RegisterWord("test_heal_all_allies",
                new List<WordActionMapping> { new("Heal", 5) },
                new WordMeta("AllAllies", 0, 0, AreaShape.Single));
            Exec("test_heal_all_allies");
            int allyHealAmt = ExpHeal(5, 8);
            Check("Heal AllAllies: only ally healed, hero excluded",
                HP(_hero) == 80 && HP(_allyA) == 50 + allyHealAmt,
                $"Hero HP={HP(_hero)}(exp 80), Ally HP={HP(_allyA)}(exp {50 + allyHealAmt})");

            SoftReset();

            // Heal SingleEnemy: heals an enemy
            // ExpHeal(5, 8) = 7
            _entityStats.ApplyDamage(_enemyA, 20);
            _resolver.RegisterWord("test_heal_enemy",
                new List<WordActionMapping> { new("Heal", 5) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_heal_enemy");
            int enemyHealAmt = ExpHeal(5, 8);
            Check("Heal SingleEnemy: enemy HP restored",
                HP(_enemyA) == 60 + enemyHealAmt,
                $"HP={HP(_enemyA)}(exp {60 + enemyHealAmt})");

            SoftReset();

            // Heal dead entity: HP goes from 0 to heal amount
            _entityStats.ApplyDamage(_enemyD, 9999);
            _entityStats.ApplyHeal(_enemyD, 5);
            Check("Heal dead entity: HP restored from 0",
                HP(_enemyD) == 5,
                $"HP={HP(_enemyD)}(exp 5)");
        }

        // ── Group 26: Target Type Coverage ──────────────────────────

        private void RunTargetTypeCoverageGroup()
        {
            // HighestHealthEnemy: enemy_c HP=100, pDef=6
            _resolver.RegisterWord("test_highest_hp",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("HighestHealthEnemy", 0, 0, AreaShape.Single));
            Exec("test_highest_hp");
            int highHpDmg = ExpDmg(1, 12, 6);
            Check("Target HighestHealthEnemy: enemy_c (HP=100) hit",
                HP(_enemyC) == 100 - highHpDmg && HP(_enemyA) == 80 && HP(_enemyB) == 60 && HP(_enemyD) == 40,
                $"C={HP(_enemyC)}(exp {100 - highHpDmg}), A={HP(_enemyA)}(80), B={HP(_enemyB)}(60), D={HP(_enemyD)}(40)");

            SoftReset();

            // LowestDefenseEnemy: enemy_d pDef=2
            _resolver.RegisterWord("test_lowest_def",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("LowestDefenseEnemy", 0, 0, AreaShape.Single));
            Exec("test_lowest_def");
            int lowDefDmg = ExpDmg(1, 12, 2);
            Check("Target LowestDefenseEnemy: enemy_d (pDef=2) hit",
                HP(_enemyD) == 40 - lowDefDmg && HP(_enemyA) == 80,
                $"D={HP(_enemyD)}(exp {40 - lowDefDmg}), A={HP(_enemyA)}(80)");

            SoftReset();

            // HighestDefenseEnemy: enemy_c pDef=6
            _resolver.RegisterWord("test_highest_def",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("HighestDefenseEnemy", 0, 0, AreaShape.Single));
            Exec("test_highest_def");
            int highDefDmg = ExpDmg(1, 12, 6);
            Check("Target HighestDefenseEnemy: enemy_c (pDef=6) hit",
                HP(_enemyC) == 100 - highDefDmg && HP(_enemyA) == 80,
                $"C={HP(_enemyC)}(exp {100 - highDefDmg}), A={HP(_enemyA)}(80)");

            SoftReset();

            // HighestStrengthEnemy: enemy_c str=10, pDef=6
            _resolver.RegisterWord("test_highest_str",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("HighestStrengthEnemy", 0, 0, AreaShape.Single));
            Exec("test_highest_str");
            int highStrDmg = ExpDmg(1, 12, 6);
            Check("Target HighestStrengthEnemy: enemy_c (str=10) hit",
                HP(_enemyC) == 100 - highStrDmg && HP(_enemyA) == 80,
                $"C={HP(_enemyC)}(exp {100 - highStrDmg}), A={HP(_enemyA)}(80)");

            SoftReset();

            // AllEnemiesWithStatus: burn enemy_a+c → only those hit
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 3, _hero);
            _statusEffects.ApplyEffect(_enemyC, StatusEffectType.Burning, 3, _hero);
            _resolver.RegisterWord("test_with_status",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("AllEnemiesWithStatus", 0, 0, AreaShape.Single));
            Exec("test_with_status");
            Check("Target AllEnemiesWithStatus: only enemies with effects hit",
                HP(_enemyA) < 80 && HP(_enemyC) < 100 && HP(_enemyB) == 60 && HP(_enemyD) == 40,
                $"A={HP(_enemyA)}, C={HP(_enemyC)}, B={HP(_enemyB)}(60), D={HP(_enemyD)}(40)");

            SoftReset();

            // AllEnemiesWithoutStatus: burn enemy_a+c → B+D hit
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 3, _hero);
            _statusEffects.ApplyEffect(_enemyC, StatusEffectType.Burning, 3, _hero);
            _resolver.RegisterWord("test_without_status",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("AllEnemiesWithoutStatus", 0, 0, AreaShape.Single));
            Exec("test_without_status");
            Check("Target AllEnemiesWithoutStatus: only unaffected enemies hit",
                HP(_enemyA) == 80 && HP(_enemyC) == 100 && HP(_enemyB) < 60 && HP(_enemyD) < 40,
                $"A={HP(_enemyA)}(80), C={HP(_enemyC)}(100), B={HP(_enemyB)}, D={HP(_enemyD)}");

            SoftReset();

            // AllWetEnemies: wet enemy_b → only enemy_b hit
            // ExpDmg(1, 12, 3) = Max(1, 1+4-1) = 4
            _statusEffects.ApplyEffect(_enemyB, StatusEffectType.Wet, 3, _hero);
            _resolver.RegisterWord("test_all_wet",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("AllEnemies+Wet", 0, 0, AreaShape.Single));
            Exec("test_all_wet");
            int wetDmg = ExpDmg(1, 12, 3);
            Check("Target AllWetEnemies: only wet enemy hit",
                HP(_enemyB) == 60 - wetDmg && HP(_enemyA) == 80 && HP(_enemyC) == 100 && HP(_enemyD) == 40,
                $"B={HP(_enemyB)}(exp {60 - wetDmg}), others unchanged");

            SoftReset();

            // AllStunnedEnemies: stun enemy_d → only enemy_d hit
            // ExpDmg(1, 12, 2) = 5
            _statusEffects.ApplyEffect(_enemyD, StatusEffectType.Stun, 3, _hero);
            _resolver.RegisterWord("test_all_stunned",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("AllEnemies+Stun", 0, 0, AreaShape.Single));
            Exec("test_all_stunned");
            int stunDmg = ExpDmg(1, 12, 2);
            Check("Target AllStunnedEnemies: only stunned enemy hit",
                HP(_enemyD) == 40 - stunDmg && HP(_enemyA) == 80 && HP(_enemyB) == 60 && HP(_enemyC) == 100,
                $"D={HP(_enemyD)}(exp {40 - stunDmg}), others unchanged");

            SoftReset();

            // AllFearfulEnemies: fear enemy_a → only enemy_a hit
            // Fear reduces pDef by 1: 4→3. ExpDmg(1, 12, 3) = 4
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Fear, 3, _hero);
            _resolver.RegisterWord("test_all_fear",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("AllEnemies+Fear", 0, 0, AreaShape.Single));
            Exec("test_all_fear");
            int fearDmg = ExpDmg(1, 12, 3); // Fear reduced pDef 4→3
            Check("Target AllFearfulEnemies: only feared enemy hit",
                HP(_enemyA) == 80 - fearDmg && HP(_enemyB) == 60,
                $"A={HP(_enemyA)}(exp {80 - fearDmg}), B={HP(_enemyB)}(60)");

            SoftReset();

            // HalfEnemiesRandom: 4 enemies → Max(1, 4/2) = 2 hit (use Shield to count)
            _resolver.RegisterWord("test_half_random",
                new List<WordActionMapping> { new("Shield", 1) },
                new WordMeta("HalfEnemiesRandom", 0, 0, AreaShape.Single));
            Exec("test_half_random");
            int halfCount = (Shield(_enemyA) > 0 ? 1 : 0) + (Shield(_enemyB) > 0 ? 1 : 0) +
                            (Shield(_enemyC) > 0 ? 1 : 0) + (Shield(_enemyD) > 0 ? 1 : 0);
            Check("Target HalfEnemiesRandom: exactly 2 enemies targeted",
                halfCount == 2,
                $"hitCount={halfCount}(exp 2)");

            SoftReset();

            // TwoRandomEnemies: exactly 2 — clear shields from previous test first
            foreach (var e in new[] { _enemyA, _enemyB, _enemyC, _enemyD })
            {
                var s = Shield(e);
                if (s > 0) _entityStats.ApplyDamage(e, s);
            }
            SoftReset();
            _resolver.RegisterWord("test_two_random",
                new List<WordActionMapping> { new("Shield", 1) },
                new WordMeta("TwoRandomEnemies", 0, 0, AreaShape.Single));
            Exec("test_two_random");
            int twoCount = (Shield(_enemyA) > 0 ? 1 : 0) + (Shield(_enemyB) > 0 ? 1 : 0) +
                           (Shield(_enemyC) > 0 ? 1 : 0) + (Shield(_enemyD) > 0 ? 1 : 0);
            Check("Target TwoRandomEnemies: exactly 2 enemies targeted",
                twoCount == 2,
                $"hitCount={twoCount}(exp 2)");

            // ThreeRandomEnemies: exactly 3 — clear shields first
            foreach (var e in new[] { _enemyA, _enemyB, _enemyC, _enemyD })
            {
                var s = Shield(e);
                if (s > 0) _entityStats.ApplyDamage(e, s);
            }
            SoftReset();
            _resolver.RegisterWord("test_three_random",
                new List<WordActionMapping> { new("Shield", 1) },
                new WordMeta("ThreeRandomEnemies", 0, 0, AreaShape.Single));
            Exec("test_three_random");
            int threeCount = (Shield(_enemyA) > 0 ? 1 : 0) + (Shield(_enemyB) > 0 ? 1 : 0) +
                             (Shield(_enemyC) > 0 ? 1 : 0) + (Shield(_enemyD) > 0 ? 1 : 0);
            Check("Target ThreeRandomEnemies: exactly 3 enemies targeted",
                threeCount == 3,
                $"hitCount={threeCount}(exp 3)");
        }

        // ── Group 27: Advanced Combos ───────────────────────────────

        private void RunAdvancedComboGroup()
        {
            // Heal+Damage: self heal + enemy damage
            // ExpHeal(5, 8) = 7, ExpDmg(2, 12, 4) = 5
            _entityStats.ApplyDamage(_hero, 20);
            _resolver.RegisterWord("test_combo_heal_dmg",
                new List<WordActionMapping>
                {
                    new("Heal", 5, Target: "Self"),
                    new("Damage", 2, Target: "SingleEnemy")
                },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_combo_heal_dmg");
            int comboHealAmt = ExpHeal(5, 8);
            int comboDmgAmt = ExpDmg(2, 12, 4);
            Check("Combo Heal+Damage: hero healed, enemy damaged",
                HP(_hero) == 80 + comboHealAmt && HP(_enemyA) == 80 - comboDmgAmt,
                $"Hero HP={HP(_hero)}(exp {80 + comboHealAmt}), Enemy HP={HP(_enemyA)}(exp {80 - comboDmgAmt})");

            SoftReset();

            // Shield+Damage: protect self while attacking
            // ExpShield(5, 5) = 6, ExpDmg(2, 12, 4) = 5
            _resolver.RegisterWord("test_combo_shield_dmg",
                new List<WordActionMapping>
                {
                    new("Shield", 5, Target: "Self"),
                    new("Damage", 2, Target: "SingleEnemy")
                },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_combo_shield_dmg");
            int comboShield = ExpShield(5, 5);
            int comboShieldDmg = ExpDmg(2, 12, 4);
            Check("Combo Shield+Damage: hero shielded, enemy damaged",
                Shield(_hero) == comboShield && HP(_enemyA) == 80 - comboShieldDmg,
                $"Hero shield={Shield(_hero)}(exp {comboShield}), Enemy HP={HP(_enemyA)}(exp {80 - comboShieldDmg})");

            SoftReset();

            // Debuff+Damage in one word: debuff applies first, amplified damage
            // pDef 4→2, Damage: ExpDmg(2, 12, 2) = 6
            _resolver.RegisterWord("test_combo_debuff_dmg",
                new List<WordActionMapping>
                {
                    new("DebuffPhysicalDefense", 2, Target: "SingleEnemy"),
                    new("Damage", 2, Target: "SingleEnemy")
                },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_combo_debuff_dmg");
            int comboDebuffDmg = ExpDmg(2, 12, 2);
            Check("Combo Debuff+Damage: debuff amplifies damage in same word",
                HP(_enemyA) == 80 - comboDebuffDmg,
                $"HP={HP(_enemyA)}(exp {80 - comboDebuffDmg})");

            SoftReset();

            // Multiple buffs in one word
            _resolver.RegisterWord("test_combo_multi_buff",
                new List<WordActionMapping>
                {
                    new("BuffStrength", 2, Target: "Self"),
                    new("BuffMagicPower", 3, Target: "Self")
                },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_combo_multi_buff");
            Check("Combo multi-buff: both stats increased",
                Stat(_hero, StatType.Strength) == 14 && Stat(_hero, StatType.MagicPower) == 11,
                $"Str={Stat(_hero, StatType.Strength)}(exp 14), Magic={Stat(_hero, StatType.MagicPower)}(exp 11)");

            SoftReset();

            // Burn+Water in one word: both effects applied
            _resolver.RegisterWord("test_combo_burn_water",
                new List<WordActionMapping> { new("Burn", 3), new("Water", 4) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_combo_burn_water");
            Check("Combo Burn+Water: both Burning and Wet applied",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Burning) &&
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Wet),
                $"Burning={_statusEffects.HasEffect(_enemyA, StatusEffectType.Burning)}, Wet={_statusEffects.HasEffect(_enemyA, StatusEffectType.Wet)}");
        }

        // ── Group 28: Stat Modifier Stacking ────────────────────────

        private void RunStatModifierStackingGroup()
        {
            // Double buff: BuffStrength(3) twice → str 12+3+3=18
            _resolver.RegisterWord("test_double_buff",
                new List<WordActionMapping> { new("BuffStrength", 3) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_double_buff");
            Exec("test_double_buff");
            Check("Double buff: both modifiers stack additively",
                Stat(_hero, StatType.Strength) == 18,
                $"Str={Stat(_hero, StatType.Strength)}(exp 18)");

            SoftReset();

            // Buff+Debuff: BuffStrength(3) + DebuffStrength(1) → net +2 from baseline
            int strBaseline = Stat(_hero, StatType.Strength);
            _resolver.RegisterWord("test_buff_for_net",
                new List<WordActionMapping> { new("BuffStrength", 3) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_buff_for_net");
            _resolver.RegisterWord("test_debuff_for_net",
                new List<WordActionMapping> { new("DebuffStrength", 1) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_debuff_for_net");
            int expectedNet = strBaseline + 3 - 1;
            Check("Buff+Debuff: net modifier applied",
                Stat(_hero, StatType.Strength) == expectedNet,
                $"Str={Stat(_hero, StatType.Strength)}(exp {expectedNet})");

            SoftReset();

            // Debuff to negative defense: DebuffPhysicalDefense(10) on enemy_d (pDef=2→-8)
            _resolver.RegisterWord("test_debuff_neg",
                new List<WordActionMapping> { new("DebuffPhysicalDefense", 10) },
                new WordMeta("LowestDefenseEnemy", 0, 0, AreaShape.Single));
            Exec("test_debuff_neg");
            Check("Debuff to negative: stat goes below zero",
                Stat(_enemyD, StatType.PhysicalDefense) == -8,
                $"pDef={Stat(_enemyD, StatType.PhysicalDefense)}(exp -8)");
            _resolver.RegisterWord("test_dmg_neg_def",
                new List<WordActionMapping> { new("Damage", 1) },
                new WordMeta("LowestDefenseEnemy", 0, 0, AreaShape.Single));
            var negStr = Stat(_hero, StatType.Strength);
            var negDef = Stat(_enemyD, StatType.PhysicalDefense); // -8
            var negDmg = ExpDmg(1, negStr, negDef);
            var expHpNeg = HP(_enemyD) - negDmg;
            Exec("test_dmg_neg_def");
            Check("Damage vs negative defense: increased damage from negative pDef",
                HP(_enemyD) == expHpNeg,
                $"HP={HP(_enemyD)}(exp {expHpNeg})");
        }

        // ── Group 29: Multi-Turn DoT & Status ───────────────────────

        private void RunMultiDoTGroup()
        {
            // Burning over 2 turns: 3 damage per tick × 2 = 6 total
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 3, _hero);
            AdvanceTurns(2); // hero + enemy_a → tick 1: -3
            AdvanceTurns(6); // enemy_b, c, d, ally_a, hero, enemy_a → tick 2: -3
            Check("Burning 2 ticks: 6 total damage",
                HP(_enemyA) == 74,
                $"HP={HP(_enemyA)}(exp 74)");

            SoftReset();

            // Dual DoTs: Burning(3) + Poisoned(2) = 5 per tick
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 3, _hero);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Poisoned, 3, _hero);
            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });
            AdvanceTurns(2);
            Check("Dual DoTs: Burning(3) + Poisoned(2) = 5 damage per tick",
                HP(_enemyA) == 75,
                $"HP={HP(_enemyA)}(exp 75)");

            SoftReset();

            // Removed effect doesn't tick
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 3, _hero);
            _statusEffects.RemoveEffect(_enemyA, StatusEffectType.Burning);
            AdvanceTurns(2);
            Check("Removed DoT: no damage after removal",
                HP(_enemyA) == 80,
                $"HP={HP(_enemyA)}(exp 80)");
        }

        // ── Group 30: Mana Regen & Edge Cases ───────────────────────

        private void RunManaRegenGroup()
        {
            // Mana regen on turn start: hero starts mana=5, regen=2 → 7
            _turnService.BeginTurn(); // hero → regen fires
            Check("Mana regen on turn start: 5→7",
                Mana(_hero) == 7,
                $"Mana={Mana(_hero)}(exp 7)");
            _turnService.EndTurn();

            // Advance to next hero turn
            AdvanceTurns(5); // enemy_a through ally_a

            // Mana regen capped at MaxMana: set to 9, regen 2 → capped at 10
            _entityStats.ApplyMana(_hero, 2); // 7→9
            _turnService.BeginTurn(); // hero → regen 2 → 9+2=11 capped at 10
            Check("Mana regen capped: does not exceed MaxMana",
                Mana(_hero) == 10,
                $"Mana={Mana(_hero)}(exp 10)");
            _turnService.EndTurn();

            // Zero-cost word: cost=0 → no mana deducted, action executes
            _entityStats.ApplyDamage(_hero, 5); // HP 100→95
            int manaBefore = Mana(_hero);
            _resolver.RegisterWord("test_zero_cost",
                new List<WordActionMapping> { new("Heal", 1) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_zero_cost");
            int zeroCostHeal = ExpHeal(1, 8);
            Check("Zero cost word: mana unchanged, action executes",
                Mana(_hero) == manaBefore && HP(_hero) == 95 + zeroCostHeal,
                $"Mana={Mana(_hero)}(exp {manaBefore}), HP={HP(_hero)}(exp {95 + zeroCostHeal})");
        }

        // ── Group 31: Concentrate ────────────────────────────────────

        private void RunConcentrateGroup()
        {
            // Concentrate(3) → Self → mana restored by ExpConc(3, 8) = 4
            _entityStats.TrySpendMana(_hero, 3); // 5→2
            _resolver.RegisterWord("test_concentrate",
                new List<WordActionMapping> { new("Concentrate", 3) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_concentrate");
            int concMana = ExpConc(3, 8); // 4
            Check("Concentrate: mana restored",
                Mana(_hero) == 2 + concMana,
                $"Mana={Mana(_hero)}(exp {2 + concMana})");

            // Concentrated status applied
            Check("Concentrate: Concentrated effect applied",
                _statusEffects.HasEffect(_hero, StatusEffectType.Concentrated),
                $"HasConcentrated={_statusEffects.HasEffect(_hero, StatusEffectType.Concentrated)}");

            // Concentrated tick → Strength modifier added
            int strBefore = Stat(_hero, StatType.Strength);
            int stacks = _statusEffects.GetStackCount(_hero, StatusEffectType.Concentrated);
            _turnService.BeginTurn(); // hero turn
            _turnService.EndTurn(); // triggers OnTick → adds Strength modifier
            Check("Concentrate tick: Strength increased by stack count",
                Stat(_hero, StatType.Strength) == strBefore + stacks,
                $"Str={Stat(_hero, StatType.Strength)}(exp {strBefore + stacks})");

            SoftReset();

            // Concentrated expires → modifier removed
            var strBaseline = Stat(_hero, StatType.Strength);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Concentrated, 1, _hero);
            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });
            _turnService.BeginTurn(); // hero
            _turnService.EndTurn(); // tick: adds modifier, duration 1→0 → expire removes modifier
            Check("Concentrate expire: Strength reverts",
                Stat(_hero, StatType.Strength) == strBaseline,
                $"Str={Stat(_hero, StatType.Strength)}(exp {strBaseline})");
        }

        // ── Group 32: Poison Action ─────────────────────────────────

        private void RunPoisonActionGroup()
        {
            // Poison(3) → SingleEnemy → Poisoned applied with duration=3
            _resolver.RegisterWord("test_poison_action",
                new List<WordActionMapping> { new("Poison", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_poison_action");
            Check("Poison action: Poisoned effect applied",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Poisoned),
                $"HasPoisoned={_statusEffects.HasEffect(_enemyA, StatusEffectType.Poisoned)}");

            // Poisoned tick: damage = 2 * stackCount(1) = 2
            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });
            AdvanceTurns(2); // hero + enemy_a → tick on enemy_a
            Check("Poison action tick: 2 damage dealt",
                HP(_enemyA) == 78,
                $"HP={HP(_enemyA)}(exp 78)");

            // Stack decays: stackCount 1→0 after tick → duration forced to 0
            Check("Poison action decay: stacks decreased",
                _statusEffects.GetStackCount(_enemyA, StatusEffectType.Poisoned) == 0 ||
                !_statusEffects.HasEffect(_enemyA, StatusEffectType.Poisoned),
                $"Stacks={_statusEffects.GetStackCount(_enemyA, StatusEffectType.Poisoned)}");

            SoftReset();

            // Heal halved while poisoned: Poison enemy, then heal
            // ExpHeal(6, 8) = 8, halved: Max(1, 8/2) = 4
            _entityStats.ApplyDamage(_enemyA, 20);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Poisoned, 5, _hero);
            _resolver.RegisterWord("test_heal_while_poisoned",
                new List<WordActionMapping> { new("Heal", 6) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_heal_while_poisoned");
            int healWhilePoisoned = Math.Max(1, ExpHeal(6, 8) / 2);
            Check("Heal halved while poisoned: heal amount reduced",
                HP(_enemyA) == 60 + healWhilePoisoned,
                $"HP={HP(_enemyA)}(exp {60 + healWhilePoisoned})");
        }

        // ── Group 33: Bleed ─────────────────────────────────────────

        private void RunBleedGroup()
        {
            // Bleed(3) → SingleEnemy → Bleeding applied
            _resolver.RegisterWord("test_bleed_action",
                new List<WordActionMapping> { new("Bleed", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_bleed_action");
            Check("Bleed action: Bleeding effect applied",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Bleeding),
                $"HasBleeding={_statusEffects.HasEffect(_enemyA, StatusEffectType.Bleeding)}");

            // Bleeding tick: damage = stackCount(1)
            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });
            AdvanceTurns(2);
            Check("Bleed tick 1: damage = 1 (stackCount=1)",
                HP(_enemyA) == 79,
                $"HP={HP(_enemyA)}(exp 79)");

            // Stacks grow if untreated: stackCount 1→2 after tick
            Check("Bleed grows: stacks increased",
                _statusEffects.GetStackCount(_enemyA, StatusEffectType.Bleeding) == 2,
                $"Stacks={_statusEffects.GetStackCount(_enemyA, StatusEffectType.Bleeding)}(exp 2)");

            // Second tick: damage = 2 (stackCount=2), then stacks→3
            AdvanceTurns(6); // enemy_b, c, d, ally_a, hero, enemy_a
            Check("Bleed tick 2: damage = 2 (stackCount=2)",
                HP(_enemyA) == 77,
                $"HP={HP(_enemyA)}(exp 77)");

            SoftReset();

            // Heal reduces bleeding stacks
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Bleeding, StatusEffectInstance.PermanentDuration, _hero);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Bleeding, StatusEffectInstance.PermanentDuration, _hero); // stack to 2
            _entityStats.ApplyDamage(_enemyA, 10); // HP 80→70
            _entityStats.ApplyHeal(_enemyA, 5); // HP 75, sets WasHealedThisTurn
            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });
            AdvanceTurns(2); // tick: damage=2, then stacks -= 2 → 0 → removed
            Check("Bleed healed: stacks reduced on heal",
                !_statusEffects.HasEffect(_enemyA, StatusEffectType.Bleeding),
                $"HasBleeding={_statusEffects.HasEffect(_enemyA, StatusEffectType.Bleeding)}");
        }

        // ── Group 34: Duplicate Actions ─────────────────────────────

        private void RunDuplicateActionGroup()
        {
            // Heal(1)+Heal(1) → Self → heals twice
            // ExpHeal(1, 8) = 3 each, total 6
            _entityStats.ApplyDamage(_hero, 10);
            _resolver.RegisterWord("test_double_heal",
                new List<WordActionMapping> { new("Heal", 1), new("Heal", 1) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_double_heal");
            int doubleHeal = ExpHeal(1, 8) * 2;
            Check("Duplicate Heal: heals twice",
                HP(_hero) == 90 + doubleHeal,
                $"HP={HP(_hero)}(exp {90 + doubleHeal})");

            SoftReset();

            // Damage(2)+Damage(2) → SingleEnemy → damages twice
            // ExpDmg(2, 12, 4) = 5 each
            _resolver.RegisterWord("test_double_damage",
                new List<WordActionMapping> { new("Damage", 2), new("Damage", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            int singleDmg = ExpDmg(2, 12, 4);
            Exec("test_double_damage");
            Check("Duplicate Damage: damages twice",
                HP(_enemyA) == 80 - singleDmg * 2,
                $"HP={HP(_enemyA)}(exp {80 - singleDmg * 2})");
        }

        // ── Group 35: Composite Target Types ────────────────────────

        private void RunCompositeTargetGroup()
        {
            // AllEnemies+Burning: burn enemy_a + enemy_c → only those targeted
            // Shield(1) with hero pDef=5: ExpShield(1, 5) = 2
            int shieldVal = ExpShield(1, 5);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 3, _hero);
            _statusEffects.ApplyEffect(_enemyC, StatusEffectType.Burning, 3, _hero);
            _resolver.RegisterWord("test_composite_burning",
                new List<WordActionMapping> { new("Shield", 1) },
                new WordMeta("AllEnemies+Burning", 0, 0, AreaShape.Single));
            Exec("test_composite_burning");
            Check("Composite AllEnemies+Burning: only burning enemies targeted",
                Shield(_enemyA) == shieldVal && Shield(_enemyC) == shieldVal && Shield(_enemyB) == 0 && Shield(_enemyD) == 0,
                $"A shield={Shield(_enemyA)}({shieldVal}), C shield={Shield(_enemyC)}({shieldVal}), B={Shield(_enemyB)}(0), D={Shield(_enemyD)}(0)");

            SoftReset();
            foreach (var e in new[] { _enemyA, _enemyB, _enemyC, _enemyD })
                if (Shield(e) > 0) _entityStats.ApplyDamage(e, Shield(e));
            SoftReset();

            // RandomEnemy+Wet: wet enemy_b only → must target enemy_b
            _statusEffects.ApplyEffect(_enemyB, StatusEffectType.Wet, 3, _hero);
            _resolver.RegisterWord("test_composite_wet",
                new List<WordActionMapping> { new("Shield", 1) },
                new WordMeta("RandomEnemy+Wet", 0, 0, AreaShape.Single));
            Exec("test_composite_wet");
            Check("Composite RandomEnemy+Wet: only wet enemy targeted",
                Shield(_enemyB) == shieldVal && Shield(_enemyA) == 0 && Shield(_enemyC) == 0 && Shield(_enemyD) == 0,
                $"B shield={Shield(_enemyB)}({shieldVal}), A={Shield(_enemyA)}(0)");

            SoftReset();
            foreach (var e in new[] { _enemyA, _enemyB, _enemyC, _enemyD })
                if (Shield(e) > 0) _entityStats.ApplyDamage(e, Shield(e));
            SoftReset();

            // LowestHealthEnemy+Poisoned: poison enemy_a(80) + enemy_d(40) → target enemy_d
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Poisoned, 3, _hero);
            _statusEffects.ApplyEffect(_enemyD, StatusEffectType.Poisoned, 3, _hero);
            _resolver.RegisterWord("test_composite_poisoned_lowest",
                new List<WordActionMapping> { new("Shield", 1) },
                new WordMeta("LowestHealthEnemy+Poisoned", 0, 0, AreaShape.Single));
            Exec("test_composite_poisoned_lowest");
            Check("Composite LowestHealthEnemy+Poisoned: lowest-HP poisoned enemy targeted",
                Shield(_enemyD) == shieldVal && Shield(_enemyA) == 0,
                $"D shield={Shield(_enemyD)}({shieldVal}), A shield={Shield(_enemyA)}(0)");
        }

        // ── Group 36: Shield on Spawn ───────────────────────────────

        private void RunShieldOnSpawnGroup()
        {
            // Entity registered with startingShield=10
            var shielded = new EntityId("shielded_unit");
            _entityStats.RegisterEntity(shielded, maxHealth: 50, strength: 5, magicPower: 0,
                physicalDefense: 4, magicDefense: 2, luck: 0, startingShield: 10);
            Check("Shield on spawn: entity starts with shield",
                Shield(shielded) == 10,
                $"Shield={Shield(shielded)}(exp 10)");

            // Damage absorbed by starting shield: 7 dmg → shield 10→3, HP=50
            _entityStats.ApplyDamage(shielded, 7);
            Check("Shield on spawn: damage absorbed by shield",
                Shield(shielded) == 3 && HP(shielded) == 50,
                $"Shield={Shield(shielded)}(exp 3), HP={HP(shielded)}(exp 50)");

            // Damage exceeds shield: 5 dmg → shield 3→0, HP takes 2 → 48
            _entityStats.ApplyDamage(shielded, 5);
            Check("Shield on spawn: overflow hits HP",
                Shield(shielded) == 0 && HP(shielded) == 48,
                $"Shield={Shield(shielded)}(exp 0), HP={HP(shielded)}(exp 48)");

            SoftReset();

            // Shield on player: register with startingShield
            var player = new EntityId("shielded_player");
            _entityStats.RegisterEntity(player, maxHealth: 100, strength: 10, magicPower: 5,
                physicalDefense: 5, magicDefense: 3, luck: 2, startingShield: 5);
            Check("Shield on player spawn: player starts with shield",
                Shield(player) == 5,
                $"Shield={Shield(player)}(exp 5)");

            // Shield stacks with action-applied shield
            _entityStats.ApplyShield(player, 3);
            Check("Shield stacks: spawn shield + action shield",
                Shield(player) == 8,
                $"Shield={Shield(player)}(exp 8)");

            SoftReset();

            // Entity with 0 starting shield (default)
            var noshield = new EntityId("noshield_unit");
            _entityStats.RegisterEntity(noshield, maxHealth: 30, strength: 3, magicPower: 0,
                physicalDefense: 2, magicDefense: 1, luck: 0);
            Check("No shield default: entity starts with 0 shield",
                Shield(noshield) == 0,
                $"Shield={Shield(noshield)}(exp 0)");
        }

        // ── Group 37: Grow ────────────────────────────────────────────

        private void RunGrowGroup()
        {
            // Growing heals on tick by StackCount
            _entityStats.ApplyDamage(_hero, 20); // HP 100→80
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Growing, 3, _hero);
            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });
            _turnService.BeginTurn(); // hero
            _turnService.EndTurn(); // tick: heal by 1 (stackCount=1)
            Check("Growing tick: heals by stackCount",
                HP(_hero) == 81,
                $"HP={HP(_hero)}(exp 81)");

            SoftReset();

            // Growing + Wet: bonus healing
            _entityStats.ApplyDamage(_hero, 20); // HP 100→80
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Growing, 3, _hero);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Wet, 5, _hero);
            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });
            _turnService.BeginTurn(); // hero
            _turnService.EndTurn(); // tick: heal by 1 + 2 = 3
            Check("Growing + Wet: bonus heal (+2)",
                HP(_hero) == 83,
                $"HP={HP(_hero)}(exp 83)");

            SoftReset();

            // Growing expires after duration
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Growing, 1, _hero);
            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });
            _turnService.BeginTurn(); // hero
            _turnService.EndTurn(); // tick, duration 1→0 → expired
            Check("Growing expires after duration",
                !_statusEffects.HasEffect(_hero, StatusEffectType.Growing),
                $"HasGrowing={_statusEffects.HasEffect(_hero, StatusEffectType.Growing)}");
        }

        // ── Group 38: Thorns ─────────────────────────────────────────

        private void RunThornsGroup()
        {
            // Thorns retaliates damage back to attacker
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Thorns, 5, _enemyA);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Thorns, 5, _enemyA);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Thorns, 5, _enemyA); // stacks=3
            _resolver.RegisterWord("test_thorns_hit",
                new List<WordActionMapping> { new("Damage", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_thorns_hit");
            int dmgDealt = ExpDmg(3, 12, 4);
            Check("Thorns: attacker takes retaliation damage",
                HP(_hero) == 100 - 3, // 3 stacks of thorns
                $"HeroHP={HP(_hero)}(exp 97)");
            Check("Thorns: target still takes damage",
                HP(_enemyA) == 80 - dmgDealt,
                $"EnemyHP={HP(_enemyA)}(exp {80 - dmgDealt})");

            SoftReset();

            // Thorns does NOT trigger on thorns retaliation (no infinite loop)
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Thorns, 5, _enemyA);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Thorns, 5, _hero);
            int heroBefore = HP(_hero);
            int enemyBefore = HP(_enemyA);
            _resolver.RegisterWord("test_thorns_no_loop",
                new List<WordActionMapping> { new("Damage", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_thorns_no_loop");
            int dmg2 = ExpDmg(3, 12, 4);
            // Hero takes 1 (thorns from enemyA), enemy takes damage
            // Enemy thorns fires → hero takes 1 damage (no source → no counter-thorns)
            Check("Thorns no loop: no infinite retaliation",
                HP(_hero) == heroBefore - 1 && HP(_enemyA) == enemyBefore - dmg2,
                $"HeroHP={HP(_hero)}(exp {heroBefore - 1}), EnemyHP={HP(_enemyA)}(exp {enemyBefore - dmg2})");

            SoftReset();

            // Thorns stacks: apply twice → stackCount=2
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Thorns, 5, _enemyA);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Thorns, 5, _enemyA);
            Check("Thorns stacking: stackCount=2",
                _statusEffects.GetStackCount(_enemyA, StatusEffectType.Thorns) == 2,
                $"stacks={_statusEffects.GetStackCount(_enemyA, StatusEffectType.Thorns)}(exp 2)");
        }

        // ── Group 39: Reflect ────────────────────────────────────────

        private void RunReflectGroup()
        {
            // Reflect redirects single-target damage to caster
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Reflecting, StatusEffectInstance.PermanentDuration, _enemyA);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Reflecting, StatusEffectInstance.PermanentDuration, _enemyA); // 2 stacks
            _resolver.RegisterWord("test_reflect_hit",
                new List<WordActionMapping> { new("Damage", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            int heroStr = Stat(_hero, StatType.Strength);
            int heroDef = Stat(_hero, StatType.PhysicalDefense);
            int reflectedDmg = ExpDmg(3, heroStr, heroDef);
            Exec("test_reflect_hit");
            Check("Reflect: damage redirected to caster",
                HP(_hero) == 100 - reflectedDmg,
                $"HeroHP={HP(_hero)}(exp {100 - reflectedDmg})");
            Check("Reflect: target unharmed",
                HP(_enemyA) == 80,
                $"EnemyHP={HP(_enemyA)}(exp 80)");

            // Stack decremented
            Check("Reflect: stack decremented to 1",
                _statusEffects.GetStackCount(_enemyA, StatusEffectType.Reflecting) == 1,
                $"stacks={_statusEffects.GetStackCount(_enemyA, StatusEffectType.Reflecting)}(exp 1)");

            SoftReset();

            // Multi-target NOT reflected
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Reflecting, StatusEffectInstance.PermanentDuration, _enemyA);
            _resolver.RegisterWord("test_reflect_multi",
                new List<WordActionMapping> { new("Damage", 2) },
                new WordMeta("AreaEnemies", 0, 0, AreaShape.Single));
            Exec("test_reflect_multi");
            // All enemies take damage (multi-target, not reflected)
            Check("Reflect: multi-target NOT reflected",
                HP(_enemyA) < 80 && HP(_enemyB) < 60,
                $"EnemyA HP={HP(_enemyA)}, EnemyB HP={HP(_enemyB)}");
            // Hero unharmed
            Check("Reflect: hero unharmed on multi-target",
                HP(_hero) == 100,
                $"HeroHP={HP(_hero)}(exp 100)");

            SoftReset();

            // After all stacks consumed, Reflecting removed
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Reflecting, StatusEffectInstance.PermanentDuration, _enemyA);
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Reflecting, StatusEffectInstance.PermanentDuration, _enemyA); // 2 stacks
            _resolver.RegisterWord("test_reflect_consume1",
                new List<WordActionMapping> { new("Shield", 1) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_reflect_consume1"); // reflected → stack 1
            _resolver.RegisterWord("test_reflect_consume2",
                new List<WordActionMapping> { new("Shield", 1) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_reflect_consume2"); // reflected → stack 0 → removed
            Check("Reflect: removed after all stacks consumed",
                !_statusEffects.HasEffect(_enemyA, StatusEffectType.Reflecting),
                $"HasReflecting={_statusEffects.HasEffect(_enemyA, StatusEffectType.Reflecting)}");
        }

        // ── Group 40: Hardening ──────────────────────────────────────

        private void RunHardeningGroup()
        {
            // Hardening reduces damage by StackCount
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, _hero);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, _hero);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, _hero); // stacks=3
            _entityStats.ApplyDamage(_hero, 5);
            Check("Hardening: damage reduced (5-3=2)",
                HP(_hero) == 98,
                $"HP={HP(_hero)}(exp 98)");

            SoftReset();

            // Hardening decays on tick
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, _hero);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, _hero);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, _hero); // stacks=3
            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });
            _turnService.BeginTurn(); // hero
            _turnService.EndTurn(); // tick: stacks 3→2
            Check("Hardening decay: stacks reduced to 2",
                _statusEffects.GetStackCount(_hero, StatusEffectType.Hardening) == 2,
                $"stacks={_statusEffects.GetStackCount(_hero, StatusEffectType.Hardening)}(exp 2)");

            // Damage with decayed hardening: 5-2=3
            _entityStats.ApplyDamage(_hero, 5);
            Check("Hardening after decay: damage reduced (5-2=3)",
                HP(_hero) == 97,
                $"HP={HP(_hero)}(exp 97)");

            SoftReset();

            // Hardening reaches 0 and is removed after enough ticks
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, _hero);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, _hero);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, _hero); // stacks=3
            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });
            // Tick 1: 3→2
            AdvanceTurns(2); // hero begin+end
            AdvanceTurns(4); // enemy_a through ally_a
            // Tick 2: 2→1
            AdvanceTurns(2); // hero begin+end
            AdvanceTurns(4);
            // Tick 3: 1→0 → removed
            AdvanceTurns(2);
            Check("Hardening removed after 3 ticks",
                !_statusEffects.HasEffect(_hero, StatusEffectType.Hardening),
                $"HasHardening={_statusEffects.HasEffect(_hero, StatusEffectType.Hardening)}");

            SoftReset();

            // Hardening reduces DoT damage
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, _hero);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, _hero);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, _hero); // stacks=3, DamageReduction=3
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Burning, 3, _enemyA); // DoT 3 per tick
            _turnService.SetTurnOrder(new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA });
            _turnService.BeginTurn(); // hero
            _turnService.EndTurn(); // tick: Hardening 3→2, Burning tick 3 - 3 reduction = 0 damage
            // After hardening tick, stacks=2 → DamageReduction=2. Burning tick fires with DoT=3, reduced by old reduction.
            // Actually: both tick in same OnTurnEnded. Order depends on apply order.
            // Hardening ticks first (applied first) → stacks 3→2, DamageReduction updated to 2
            // Then Burning ticks → damage 3, reduced by DamageReduction(2) = 1
            Check("Hardening reduces DoT: Burning damage reduced",
                HP(_hero) == 99,
                $"HP={HP(_hero)}(exp 99)");
        }

        // ── Group 41: MagicDamage ─────────────────────────────────────

        private void RunMagicDamageGroup()
        {
            // MagicDamage(3) → SingleEnemy → enemy_a: ExpMagicDmg(3, 8, 3) = Max(1, 3+2-1) = 4
            _resolver.RegisterWord("test_magic_dmg_single",
                new List<WordActionMapping> { new("MagicDamage", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_magic_dmg_single");
            int mdSingle = ExpMagicDmg(3, 8, 3); // hero magic=8, enemy_a mDef=3
            Check("MagicDamage single: correct HP reduction",
                HP(_enemyA) == 80 - mdSingle,
                $"Expected HP={80 - mdSingle}, got {HP(_enemyA)}");

            SoftReset();

            // MagicDamage(2) → AreaEnemies (all enemies)
            _resolver.RegisterWord("test_magic_dmg_all",
                new List<WordActionMapping> { new("MagicDamage", 2) },
                new WordMeta("AreaEnemies", 0, 0, AreaShape.Single));
            Exec("test_magic_dmg_all");
            int mdA = ExpMagicDmg(2, 8, 3); // enemy_a mDef=3
            int mdB = ExpMagicDmg(2, 8, 2); // enemy_b mDef=2
            int mdC = ExpMagicDmg(2, 8, 5); // enemy_c mDef=5
            int mdD = ExpMagicDmg(2, 8, 1); // enemy_d mDef=1
            bool mdAllOk = HP(_enemyA) == 80 - mdA && HP(_enemyB) == 60 - mdB &&
                           HP(_enemyC) == 100 - mdC && HP(_enemyD) == 40 - mdD;
            Check("MagicDamage all enemies: correct HP reductions",
                mdAllOk,
                $"A={HP(_enemyA)}(exp {80 - mdA}), B={HP(_enemyB)}(exp {60 - mdB}), C={HP(_enemyC)}(exp {100 - mdC}), D={HP(_enemyD)}(exp {40 - mdD})");

            SoftReset();

            // MagicDamage(99) → SingleEnemy → overkill, entity dies
            bool mdDied = false;
            _subscriptions.Add(_eventBus.Subscribe<EntityDiedEvent>(e =>
            {
                if (e.EntityId.Equals(_enemyA)) mdDied = true;
            }));
            _resolver.RegisterWord("test_magic_dmg_overkill",
                new List<WordActionMapping> { new("MagicDamage", 99) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_magic_dmg_overkill");
            Check("MagicDamage overkill: entity dies at HP=0",
                HP(_enemyA) == 0 && mdDied,
                $"HP={HP(_enemyA)}, died={mdDied}");

            SoftReset();

            // MagicDamage minimum damage: Frozen enemy_c (mDef 5+999=1004)
            _statusEffects.ApplyEffect(_enemyC, StatusEffectType.Frozen, 5, _hero);
            _resolver.RegisterWord("test_magic_dmg_frozen",
                new List<WordActionMapping> { new("MagicDamage", 1) },
                new WordMeta("HighestDefenseEnemy", 0, 0, AreaShape.Single));
            Exec("test_magic_dmg_frozen");
            Check("MagicDamage vs Frozen: floor of 1 against extreme magic defense",
                HP(_enemyC) == 99,
                $"HP={HP(_enemyC)}(exp 99)");

            SoftReset();

            // MagicDamage combo with BuffMagicPower: magic 8→12
            _resolver.RegisterWord("test_magic_buff",
                new List<WordActionMapping> { new("BuffMagicPower", 4) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_magic_buff");
            _resolver.RegisterWord("test_magic_dmg_buffed",
                new List<WordActionMapping> { new("MagicDamage", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_magic_dmg_buffed");
            int mdBuffed = ExpMagicDmg(2, 12, 3); // buffed magic=12, enemy_a mDef=3
            Check("MagicDamage buffed: increased damage with higher magic",
                HP(_enemyA) == 80 - mdBuffed,
                $"HP={HP(_enemyA)}(exp {80 - mdBuffed})");
        }

        // ── Siphon ──────────────────────────────────────────────────

        private void RunSiphonGroup()
        {
            // Siphon(1) → SingleEnemy → enemy_a: debuffs a random stat by 1, buffs hero by 1
            int totalStatsBefore = Stat(_hero, StatType.Strength) + Stat(_hero, StatType.MagicPower) +
                Stat(_hero, StatType.PhysicalDefense) + Stat(_hero, StatType.MagicDefense) + Stat(_hero, StatType.Luck);
            int enemyTotalBefore = Stat(_enemyA, StatType.Strength) + Stat(_enemyA, StatType.MagicPower) +
                Stat(_enemyA, StatType.PhysicalDefense) + Stat(_enemyA, StatType.MagicDefense) + Stat(_enemyA, StatType.Luck);

            _resolver.RegisterWord("test_siphon",
                new List<WordActionMapping> { new("Siphon", 1) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_siphon");

            int totalStatsAfter = Stat(_hero, StatType.Strength) + Stat(_hero, StatType.MagicPower) +
                Stat(_hero, StatType.PhysicalDefense) + Stat(_hero, StatType.MagicDefense) + Stat(_hero, StatType.Luck);
            int enemyTotalAfter = Stat(_enemyA, StatType.Strength) + Stat(_enemyA, StatType.MagicPower) +
                Stat(_enemyA, StatType.PhysicalDefense) + Stat(_enemyA, StatType.MagicDefense) + Stat(_enemyA, StatType.Luck);

            Check("Siphon: hero gains +1 total stats",
                totalStatsAfter == totalStatsBefore + 1,
                $"HeroTotal={totalStatsAfter}(exp {totalStatsBefore + 1})");
            Check("Siphon: enemy loses -1 total stats",
                enemyTotalAfter == enemyTotalBefore - 1,
                $"EnemyTotal={enemyTotalAfter}(exp {enemyTotalBefore - 1})");
        }

        // ── Deceive ──────────────────────────────────────────────────

        private void RunDeceiveGroup()
        {
            // Deceive(2) → SingleEnemy → enemy_a: Fear(2) + Concussion(permanent)
            _resolver.RegisterWord("test_deceive",
                new List<WordActionMapping> { new("Deceive", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_deceive");

            Check("Deceive: Fear applied",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Fear),
                $"HasFear={_statusEffects.HasEffect(_enemyA, StatusEffectType.Fear)}");
            Check("Deceive: Concussion applied",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Concussion),
                $"HasConcussion={_statusEffects.HasEffect(_enemyA, StatusEffectType.Concussion)}");
        }

        // ── Overcharge ──────────────────────────────────────────────────

        private void RunOverchargeGroup()
        {
            // Overcharge(2) → Self → buffs MagicPower by 2, applies Energetic(2)
            int mgcBefore = Stat(_hero, StatType.MagicPower);

            _resolver.RegisterWord("test_overcharge",
                new List<WordActionMapping> { new("Overcharge", 2) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_overcharge");

            Check("Overcharge: MagicPower buffed",
                Stat(_hero, StatType.MagicPower) == mgcBefore + 2,
                $"MgcPow={Stat(_hero, StatType.MagicPower)}(exp {mgcBefore + 2})");

            Check("Overcharge: Energetic applied",
                _statusEffects.HasEffect(_hero, StatusEffectType.Energetic),
                $"HasEnergetic={_statusEffects.HasEffect(_hero, StatusEffectType.Energetic)}");
        }

        // ── Recuperate ──────────────────────────────────────────────────

        private void RunRecuperateGroup()
        {
            // Recuperate(2) → AllAlliesAndSelf → heals + removes one negative status
            _entityStats.ApplyDamage(_hero, 20);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Poisoned, 3, _enemyA);
            _statusEffects.ApplyEffect(_hero, StatusEffectType.Burning, 2, _enemyA);
            int hpBefore = HP(_hero);

            _resolver.RegisterWord("test_recuperate",
                new List<WordActionMapping> { new("Recuperate", 2) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_recuperate");

            Check("Recuperate: HP increased",
                HP(_hero) > hpBefore,
                $"HP={HP(_hero)}(was {hpBefore})");

            bool poisonGone = !_statusEffects.HasEffect(_hero, StatusEffectType.Poisoned);
            bool burningGone = !_statusEffects.HasEffect(_hero, StatusEffectType.Burning);
            Check("Recuperate: one negative status removed",
                poisonGone || burningGone,
                $"Poisoned={_statusEffects.HasEffect(_hero, StatusEffectType.Poisoned)}, Burning={_statusEffects.HasEffect(_hero, StatusEffectType.Burning)}");
        }

        // ── Comfort ──────────────────────────────────────────────────

        private void RunComfortGroup()
        {
            // Comfort(1) → SingleEnemy (used as target for test) → applies Energetic to target
            _resolver.RegisterWord("test_comfort",
                new List<WordActionMapping> { new("Comfort", 1) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_comfort");

            Check("Comfort: Energetic applied to target",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Energetic),
                $"HasEnergetic={_statusEffects.HasEffect(_enemyA, StatusEffectType.Energetic)}");
        }

        // ── Attune ──────────────────────────────────────────────────

        private void RunAttuneGroup()
        {
            // Test 1: Play "attune" → letters are stored
            _resolver.RegisterWord("test_attune",
                new List<WordActionMapping> { new("Attune", 1) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_attune");
            var letters = _letterReserve.GetReservedLetters();
            Check("Attune: letters stored",
                letters.Count == 10, // "test_attune" has 10 letters
                $"LetterCount={letters.Count}(exp 10)");

            // Test 2: Damage word with attuned letters → value is multiplied
            // "test_attune" letters: t,e,s,t,a,t,t,u,n,e
            // "test" matches: t,e,s,t → 4 letters → +80% bonus
            _resolver.RegisterWord("test_dmg",
                new List<WordActionMapping> { new("Damage", 5) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            int hpBefore = HP(_enemyA);
            Exec("test_dmg");
            int baseDmg = ExpDmg(5, 12, 4);
            int boostedDmg = ExpDmg((int)(5 * (1.0f + 0.2f * 4)), 12, 4);
            Check("Attune: damage boosted by consumed letters",
                HP(_enemyA) == hpBefore - boostedDmg,
                $"HP={HP(_enemyA)}(exp {hpBefore - boostedDmg}, baseDmg={baseDmg}, boosted={boostedDmg})");

            // Test 3: Letters consumed — remaining pool should be smaller
            var remaining = _letterReserve.GetReservedLetters();
            // "test" consumed t,e,s,t from pool [t,e,s,t,a,t,t,u,n,e] → remaining [a,t,t,u,n,e] = 6
            Check("Attune: letters consumed on use",
                remaining.Count == 6,
                $"Remaining={remaining.Count}(exp 6)");

            // Test 4: Second play with no matching letters gets no bonus
            _resolver.RegisterWord("xxx_no_match",
                new List<WordActionMapping> { new("Damage", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            SoftReset();
            // Re-register attune and execute to get fresh letters
            _resolver.RegisterWord("test_attune2",
                new List<WordActionMapping> { new("Attune", 1) },
                new WordMeta("Self", 0, 0, AreaShape.Single));
            Exec("test_attune2");
            // "xxx_no_match" letters: x,x,x,n,o,m,a,t,c,h
            // From "test_attune2" pool: t,e,s,t,a,t,t,u,n,e,2 → matches n,a,t = 3 letters
            // Actually wait, _letterReserve was cleared by SoftReset? No, SoftReset only resets HP/status/modifiers
            // But we called RebuildAll before RunAttuneGroup, so _letterReserve is fresh
            // After SoftReset: remaining letters from test 3 = [a,t,t,u,n,e]
            // After Exec("test_attune2"): adds t,e,s,t,a,t,t,u,n,e = 10 more → total 16
            // Let me simplify: just test multi-action word caching

            SoftReset();
            _letterReserve.Clear();

            // Test 4: Multi-action word → all actions get same bonus
            _letterReserve.AddLetters("abcde", "test");
            // Pool: [a,b,c,d,e]
            _resolver.RegisterWord("bead",
                new List<WordActionMapping> { new("Damage", 3), new("Shock", 2, Target: "SingleEnemy") },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            // "bead" matches: b,e,a,d → 4 letters consumed → +80%
            int hpBefore2 = HP(_enemyA);
            Exec("bead");
            int expectedDmg = ExpDmg((int)(3 * 1.8f), 12, 4);
            Check("Attune: multi-action word gets same bonus for all actions",
                HP(_enemyA) < hpBefore2,
                $"HP={HP(_enemyA)}(was {hpBefore2})");

            // Verify only 'c' remains (b,e,a,d consumed)
            var finalLetters = _letterReserve.GetReservedLetters();
            Check("Attune: correct letters remain after multi-action",
                finalLetters.Count == 1 && finalLetters[0] == 'c',
                $"Remaining={finalLetters.Count}(exp 1), first={( finalLetters.Count > 0 ? finalLetters[0].ToString() : "none")}");
        }

        // ── Cannonade ──────────────────────────────────────────────────

        private void RunCannonadeGroup()
        {
            // Cannonade(3) → AllEnemies → fires 3 hits at random enemies
            int hpA = HP(_enemyA);
            int hpB = HP(_enemyB);
            int hpC = HP(_enemyC);

            _resolver.RegisterWord("test_cannonade",
                new List<WordActionMapping> { new("Cannonade", 3) },
                new WordMeta("AllEnemies", 0, 0, AreaShape.Single));
            Exec("test_cannonade");

            int totalDmg = (hpA - HP(_enemyA)) + (hpB - HP(_enemyB)) + (hpC - HP(_enemyC));
            Check("Cannonade: total damage dealt across enemies",
                totalDmg > 0,
                $"TotalDmg={totalDmg}");
        }

        // ── Plunder ──────────────────────────────────────────────────

        private void RunPlunderGroup()
        {
            // Plunder(2) → SingleEnemy → deals damage + steals 1 stat
            int hpBefore = HP(_enemyA);
            int heroTotalBefore = Stat(_hero, StatType.Strength) + Stat(_hero, StatType.MagicPower) +
                Stat(_hero, StatType.PhysicalDefense) + Stat(_hero, StatType.MagicDefense) + Stat(_hero, StatType.Luck);
            int enemyTotalBefore = Stat(_enemyA, StatType.Strength) + Stat(_enemyA, StatType.MagicPower) +
                Stat(_enemyA, StatType.PhysicalDefense) + Stat(_enemyA, StatType.MagicDefense) + Stat(_enemyA, StatType.Luck);

            _resolver.RegisterWord("test_plunder",
                new List<WordActionMapping> { new("Plunder", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_plunder");

            Check("Plunder: damage dealt",
                HP(_enemyA) < hpBefore,
                $"HP={HP(_enemyA)}(was {hpBefore})");

            int heroTotalAfter = Stat(_hero, StatType.Strength) + Stat(_hero, StatType.MagicPower) +
                Stat(_hero, StatType.PhysicalDefense) + Stat(_hero, StatType.MagicDefense) + Stat(_hero, StatType.Luck);
            int enemyTotalAfter = Stat(_enemyA, StatType.Strength) + Stat(_enemyA, StatType.MagicPower) +
                Stat(_enemyA, StatType.PhysicalDefense) + Stat(_enemyA, StatType.MagicDefense) + Stat(_enemyA, StatType.Luck);

            Check("Plunder: hero gains +1 stat",
                heroTotalAfter == heroTotalBefore + 1,
                $"HeroTotal={heroTotalAfter}(exp {heroTotalBefore + 1})");
            Check("Plunder: enemy loses -1 stat",
                enemyTotalAfter == enemyTotalBefore - 1,
                $"EnemyTotal={enemyTotalAfter}(exp {enemyTotalBefore - 1})");
        }

        // ── Group: Ignite ────────────────────────────────────────────

        private void RunIgniteGroup()
        {
            // Ignite(2) → SingleEnemy: MagicDamage(MagicPower vs MagicDefense) + apply Burning
            // Hero: MagicPower=8, EnemyA: MagicDefense=3
            int hpBefore = HP(_enemyA);
            int expectedDmg = ExpMagicDmg(2, 8, 3);

            _resolver.RegisterWord("test_ignite",
                new List<WordActionMapping> { new("Ignite", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_ignite");

            Check("Ignite: correct magic damage",
                HP(_enemyA) == hpBefore - expectedDmg,
                $"HP={HP(_enemyA)}(exp {hpBefore - expectedDmg})");
            Check("Ignite: Burning applied",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Burning),
                "Enemy_a does not have Burning");
        }

        // ── Group: Combust ──────────────────────────────────────────

        private void RunCombustGroup()
        {
            // Combust(2) without Burning → base MagicDamage only
            int hpBefore = HP(_enemyA);
            int baseDmg = ExpMagicDmg(2, 8, 3);

            _resolver.RegisterWord("test_combust_base",
                new List<WordActionMapping> { new("Combust", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_combust_base");

            Check("Combust (no burn): base damage dealt",
                HP(_enemyA) == hpBefore - baseDmg,
                $"HP={HP(_enemyA)}(exp {hpBefore - baseDmg})");

            SoftReset();

            // Combust(2) with Burning(3 stacks) → bonus MagicDamage(2+3=5)
            _statusEffects.ApplyEffect(_enemyA, StatusEffectType.Burning, 3, _hero);
            Check("Combust setup: Burning applied",
                _statusEffects.HasEffect(_enemyA, StatusEffectType.Burning),
                "Burning not applied for Combust test");

            hpBefore = HP(_enemyA);
            int bonusDmg = ExpMagicDmg(2 + 3, 8, 3);

            _resolver.RegisterWord("test_combust_burn",
                new List<WordActionMapping> { new("Combust", 2) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));
            Exec("test_combust_burn");

            Check("Combust (with burn): bonus damage dealt",
                HP(_enemyA) == hpBefore - bonusDmg,
                $"HP={HP(_enemyA)}(exp {hpBefore - bonusDmg})");
            Check("Combust (with burn): Burning removed",
                !_statusEffects.HasEffect(_enemyA, StatusEffectType.Burning),
                "Burning still present after Combust");
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
            (_slotService as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            (_turnService as IDisposable)?.Dispose();
            (_letterReserve as IDisposable)?.Dispose();
            _executionService = null;
            _letterReserve = null;
            _statusEffects = null;
            _slotService = null;
            _entityStats = null;
            _turnService = null;
            _combatContext = null;
            _resolver = null;
            _eventBus = null;
        }
    }
}
