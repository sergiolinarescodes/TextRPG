using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatGrid;
using TextRPG.Core.Encounter;
using TextRPG.Core.EnemyAI;
using TextRPG.Core.EntityStats;
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

            // Phase 3a — CombatGrid (depends on UnitRendering)
            installers.Add(new CombatGridSystemInstaller());

            // Phase 3b — Encounter + Enemy AI (depend on CombatGrid, ActionExecution, etc.)
            installers.Add(new EncounterSystemInstaller());
            installers.Add(new EnemyAISystemInstaller());
        }
    }
}
