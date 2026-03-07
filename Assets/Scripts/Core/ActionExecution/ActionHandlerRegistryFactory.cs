using TextRPG.Core.ActionExecution.Handlers;
using TextRPG.Core.Weapon;

using static TextRPG.Core.EntityStats.StatType;

namespace TextRPG.Core.ActionExecution
{
    internal static class ActionHandlerRegistryFactory
    {
        public static ActionHandlerRegistry CreateDefault(IActionHandlerContext ctx)
        {
            var registry = new ActionHandlerRegistry();

            registry.Register("Damage", new DamageActionHandler(ctx));
            registry.Register("Heal", new HealActionHandler(ctx));
            registry.Register("Push", new PushActionHandler(ctx));
            registry.Register("Fire", new FireActionHandler(ctx));
            registry.Register("Shield", new ShieldActionHandler(ctx));

            registry.Register("Move", new MoveActionHandler(ctx));
            registry.Register("Teleport", new TeleportActionHandler(ctx));
            registry.Register("MoveRandom", new MoveRandomActionHandler(ctx));
            registry.Register("MoveNearAlly", new MoveNearAllyActionHandler(ctx));
            registry.Register("MoveNearEnemy", new MoveNearEnemyActionHandler(ctx));
            registry.Register("MoveFlank", new MoveFlankActionHandler(ctx));

            var stats = new[]
            {
                (Strength, "Strength"), (MagicPower, "MagicPower"),
                (PhysicalDefense, "PhysicalDefense"), (MagicDefense, "MagicDefense"),
                (Luck, "Luck"), (MovementPoints, "Movement"),
            };
            foreach (var (stat, name) in stats)
            {
                registry.Register($"Buff{name}", new StatModifierActionHandler($"Buff{name}", ctx, stat, true));
                registry.Register($"Debuff{name}", new StatModifierActionHandler($"Debuff{name}", ctx, stat, false));
            }
            registry.Register("Buff", new StatModifierActionHandler("Buff", ctx, Strength, true));

            if (ctx.StatusEffects != null)
            {
                registry.Register("Burn", new BurnActionHandler(ctx));
                registry.Register("Water", new WetActionHandler(ctx));
                registry.Register("Shock", new ShockActionHandler(ctx));
                registry.Register("Fear", new FearActionHandler(ctx));
                registry.Register("Stun", new StunActionHandler(ctx));
            }

            if (ctx.StatusEffects != null && ctx.TurnService != null)
                registry.Register("Summon", new SummonActionHandler(ctx));

            if (ctx.WeaponService != null)
                registry.Register("Weapon", new WeaponActionHandler(ctx));

            return registry;
        }
    }
}
