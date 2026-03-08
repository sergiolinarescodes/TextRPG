using System.Collections.Generic;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.CombatAI;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordInput;
using Unidad.Core.Bootstrap;

namespace TextRPG.Core.Bootstrap
{
    /// <summary>
    /// Game-specific bootstrap. The single MonoBehaviour in the scene.
    /// Registers all game system installers in dependency order.
    /// </summary>
    public sealed class GameBootstrap : UnidadBootstrap
    {
        protected override void RegisterInstallers(List<ISystemInstaller> installers)
        {
            installers.Add(new WordInputSystemInstaller());
            installers.Add(new WordActionSystemInstaller());
            installers.Add(new UnitRenderingSystemInstaller());

            // Phase 1 — no cross-dependencies
            installers.Add(new EntityStatsSystemInstaller());
            installers.Add(new TurnSystemInstaller());

            // Phase 2 — StatusEffect before ActionExecution (handlers depend on IStatusEffectService)
            installers.Add(new StatusEffectSystemInstaller());
            installers.Add(new ActionExecutionSystemInstaller());

            // Weapon system — depends on ActionExecution (IActionHandlerRegistry, ICombatContext)
            installers.Add(new WeaponSystemInstaller());

            // Action Animation — depends on ActionExecution events, no service dependencies
            installers.Add(new ActionAnimationSystemInstaller());

            // CombatSlot (simple slot system)
            installers.Add(new CombatSlotSystemInstaller());

            // Encounter + Combat AI (depend on CombatSlot, ActionExecution, etc.)
            installers.Add(new EncounterSystemInstaller());
            installers.Add(new CombatAISystemInstaller());

            // Passive system — depends on Encounter, CombatSlot, EntityStats
            installers.Add(new PassiveSystemInstaller());

            // CombatLoop — orchestrates turn loop, word submission, game-over detection
            installers.Add(new CombatLoopSystemInstaller());
        }
    }
}
