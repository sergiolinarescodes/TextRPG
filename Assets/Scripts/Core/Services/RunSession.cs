using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatAI;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Consumable;
using TextRPG.Core.Encounter;
using TextRPG.Core.Equipment;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.EventEncounterLoop;
using TextRPG.Core.LetterChallenge;
using TextRPG.Core.LetterReserve;
using TextRPG.Core.Lockpick;
using TextRPG.Core.Luck;
using TextRPG.Core.Passive;
using TextRPG.Core.PlayerClass;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Experience;
using TextRPG.Core.Run;
using TextRPG.Core.Scroll;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordCooldown;
using TextRPG.Core.WordInput;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Services
{
    /// <summary>
    /// Holds all run-lifetime services that persist across encounters.
    /// Created once per run via <see cref="RunSessionFactory"/>.
    /// </summary>
    internal sealed class RunSession : IDisposable
    {
        // Core services
        public IEventBus EventBus { get; init; }
        public EntityId PlayerId { get; init; }
        public IEntityStatsService EntityStats { get; init; }
        public IWordInputService WordInputService { get; init; }
        public DrunkLetterService DrunkLetterService { get; init; }
        public IUnitService UnitService { get; init; }

        // Word data
        public IWordResolver WordResolver { get; init; }
        public IWordResolver AmmoResolver { get; init; }
        public IActionRegistry ActionRegistry { get; init; }
        public IWordTagResolver WordTagResolver { get; init; }
        public IWordMatchService WordMatchService { get; init; }
        public IWordMatchService AmmoMatchService { get; init; }

        // Combat infrastructure
        public ICombatSlotService SlotService { get; init; }
        public ICombatContext CombatContext { get; init; }
        public ITurnService TurnService { get; init; }
        public IStatusEffectService StatusEffects { get; init; }
        public IActionHandlerRegistry HandlerRegistry { get; init; }
        public IActionExecutionService ActionExecution { get; init; }

        // Weapon/Consumable/Equipment
        public IWeaponService WeaponService { get; init; }
        public IWeaponActionExecutor WeaponExecutor { get; init; }
        public IConsumableService ConsumableService { get; init; }
        public ConsumableActionExecutor ConsumableExecutor { get; init; }
        public IEquipmentService EquipmentService { get; init; }
        public IItemRegistry ItemRegistry { get; init; }
        public IInventoryService InventoryService { get; init; }
        public InventoryId PlayerInventoryId { get; init; }

        // Animation
        public ActionAnimationService AnimationService { get; init; }
        public IAnimationResolver ScenarioAnimResolver { get; init; }

        // Preview/Targeting
        public ITargetingPreviewService PreviewService { get; init; }
        public ITargetingPreviewService AmmoPreviewService { get; init; }

        // Passive/Reaction
        public IPassiveService PassiveService { get; init; }
        public ReactionService ReactionService { get; init; }
        public EventEncounterContext ReactionContext { get; init; }

        // Class
        public IClassService ClassService { get; init; }

        // Letter Reserve / Challenge
        public ILetterReserveService LetterReserve { get; init; }
        public ILetterChallengeService LetterChallengeService { get; init; }

        // Lockpick
        public ILockpickService LockpickService { get; init; }

        // Run/Resource/Experience
        public IRunService RunService { get; init; }
        public Unidad.Core.Resource.IResourceService ResourceService { get; init; }
        public ILootRewardService LootRewardService { get; init; }
        public ExperienceService ExperienceService { get; init; }
        public StatusEffectVisualService StatusVisualService { get; init; }

        // Misc
        public IWordCooldownService WordCooldown { get; init; }
        public IGiveValidator GiveValidator { get; init; }
        public ISpellService SpellService { get; init; }
        public IWordResolver EnemyResolver { get; init; }
        public EnemyWordResolver SpellResolver { get; init; }
        public IAnxietyService AnxietyService { get; init; }
        public ILuckService LuckService { get; init; }

        // Static data
        public Dictionary<string, EntityDefinition> AllUnits { get; init; }
        public Dictionary<string, EventEncounterDefinition> AllEventEncounters { get; init; }

        // Active encounter scope (only one at a time)
        private IDisposable _currentScope;

        public CombatEncounterScope StartCombat(EncounterDefinition encounter)
        {
            var scope = CombatEncounterScope.Create(this, encounter);
            _currentScope = scope;
            return scope;
        }

        public EventEncounterScope StartEvent(EventEncounterDefinition encounter)
        {
            var scope = EventEncounterScope.Create(this, encounter);
            _currentScope = scope;
            return scope;
        }

        public void CleanupCurrentEncounter()
        {
            _currentScope?.Dispose();
            _currentScope = null;

            // Clear lockpick encounter ref
            if (LockpickService is Lockpick.LockpickService lockpick)
                lockpick.SetEncounterService(null);

            ReactionService?.ClearReactions();
            SlotService.Initialize();
            (EnemyResolver as EnemyWordResolver)?.Clear();
            ((CombatContext)CombatContext).SetEnemies(Array.Empty<EntityId>());
            ((CombatContext)CombatContext).SetAllies(Array.Empty<EntityId>());
            WordCooldown?.Reset();
            LetterReserve?.Clear();
            ExperienceService?.SetEncounterService(null);
        }

        public void Dispose()
        {
            _currentScope?.Dispose();
            _currentScope = null;

            (AnimationService as IDisposable)?.Dispose();
            (PassiveService as IDisposable)?.Dispose();
            (RunService as IDisposable)?.Dispose();
            (ReactionService as IDisposable)?.Dispose();
            (ResourceService as IDisposable)?.Dispose();
            (TurnService as IDisposable)?.Dispose();
            (StatusVisualService as IDisposable)?.Dispose();
            (DrunkLetterService as IDisposable)?.Dispose();
            (AnxietyService as IDisposable)?.Dispose();
            (ConsumableService as IDisposable)?.Dispose();
            (ConsumableExecutor as IDisposable)?.Dispose();
            (LootRewardService as IDisposable)?.Dispose();
            (ExperienceService as IDisposable)?.Dispose();
            (ClassService as IDisposable)?.Dispose();
            (LetterReserve as IDisposable)?.Dispose();
            (SpellService as IDisposable)?.Dispose();
            (LockpickService as IDisposable)?.Dispose();
            (EquipmentService as IDisposable)?.Dispose();
            (InventoryService as IDisposable)?.Dispose();
            EventBus?.ClearAllSubscriptions();
        }
    }
}
