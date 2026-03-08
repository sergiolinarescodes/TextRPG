using TextRPG.Core.ActionExecution.Handlers;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.Weapon;

using static TextRPG.Core.EntityStats.StatType;
using static TextRPG.Core.ActionExecution.Handlers.ApplyStatusEffectHandler.DurationMode;

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
            registry.Register("Thinking", new ThinkingActionHandler(ctx));

            var stats = new[]
            {
                (Strength, "Strength"), (MagicPower, "MagicPower"),
                (PhysicalDefense, "PhysicalDefense"), (MagicDefense, "MagicDefense"),
                (Luck, "Luck"),
                (MaxMana, "MaxMana"), (ManaRegen, "ManaRegen"),
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
                registry.Register("Poison", new ApplyStatusEffectHandler("Poison", ctx, StatusEffectType.Poisoned, false, FromValue));
                registry.Register("Bleed", new ApplyStatusEffectHandler("Bleed", ctx, StatusEffectType.Bleeding, false, Permanent));
                registry.Register("Grow", new ApplyStatusEffectHandler("Grow", ctx, StatusEffectType.Growing, true, FromValue));
                registry.Register("Thorns", new ApplyStatusEffectHandler("Thorns", ctx, StatusEffectType.Thorns, true, FromValue));
                registry.Register("Reflect", new ApplyStatusEffectHandler("Reflect", ctx, StatusEffectType.Reflecting, true, StackByValue));
                registry.Register("Hardening", new ApplyStatusEffectHandler("Hardening", ctx, StatusEffectType.Hardening, true, StackByValue));
            }

            if (ctx.StatusEffects != null && ctx.EntityStats != null)
                registry.Register("Concentrate", new ConcentrateActionHandler(ctx));

            if (ctx.StatusEffects != null && ctx.TurnService != null)
                registry.Register("Summon", new SummonActionHandler(ctx));

            if (ctx.WeaponService != null)
                registry.Register("Weapon", new WeaponActionHandler(ctx));

            return registry;
        }
    }
}
