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

            registry.Register(ActionNames.Damage, new DamageActionHandler(ctx));
            registry.Register(ActionNames.Smash, new SmashActionHandler(ctx));
            registry.Register(ActionNames.MagicDamage, new MagicDamageActionHandler(ctx));
            registry.Register(ActionNames.WeaponDamage, new WeaponDamageActionHandler(ctx));
            registry.Register(ActionNames.Heal, new HealActionHandler(ctx));
            registry.Register(ActionNames.Push, new PushActionHandler(ctx));
            registry.Register(ActionNames.Fire, new FireActionHandler(ctx));
            registry.Register(ActionNames.Shield, new ShieldActionHandler(ctx));
            registry.Register(ActionNames.Thinking, new ThinkingActionHandler(ctx));
            registry.Register(ActionNames.Pay, new PayActionHandler());

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
            registry.Register(ActionNames.Buff, new StatModifierActionHandler(ActionNames.Buff, ctx, Strength, true));
            registry.Register(ActionNames.Melt, new StatModifierActionHandler(ActionNames.Melt, ctx, PhysicalDefense, false));

            if (ctx.StatusEffects != null)
            {
                registry.Register(ActionNames.Burn, new BurnActionHandler(ctx));
                registry.Register(ActionNames.Water, new WetActionHandler(ctx));
                registry.Register(ActionNames.Shock, new ShockActionHandler(ctx));
                registry.Register(ActionNames.Fear, new FearActionHandler(ctx));
                registry.Register(ActionNames.Stun, new StunActionHandler(ctx));
                registry.Register(ActionNames.Poison, new ApplyStatusEffectHandler(ActionNames.Poison, ctx, StatusEffectType.Poisoned, false, FromValue));
                registry.Register(ActionNames.Bleed, new ApplyStatusEffectHandler(ActionNames.Bleed, ctx, StatusEffectType.Bleeding, false, Permanent));
                registry.Register(ActionNames.Grow, new ApplyStatusEffectHandler(ActionNames.Grow, ctx, StatusEffectType.Growing, true, FromValue));
                registry.Register(ActionNames.Thorns, new ApplyStatusEffectHandler(ActionNames.Thorns, ctx, StatusEffectType.Thorns, true, FromValue));
                registry.Register(ActionNames.Reflect, new ApplyStatusEffectHandler(ActionNames.Reflect, ctx, StatusEffectType.Reflecting, true, StackByValue));
                registry.Register(ActionNames.Hardening, new ApplyStatusEffectHandler(ActionNames.Hardening, ctx, StatusEffectType.Hardening, true, StackByValue));
                registry.Register(ActionNames.Drunk, new ApplyStatusEffectHandler(ActionNames.Drunk, ctx, StatusEffectType.Drunk, true, StackByValue));
            }

            if (ctx.StatusEffects != null && ctx.EntityStats != null)
                registry.Register(ActionNames.Concentrate, new ConcentrateActionHandler(ctx));

            if (ctx.StatusEffects != null && ctx.TurnService != null)
                registry.Register(ActionNames.Summon, new SummonActionHandler(ctx));

            if (ctx.WeaponService != null)
                registry.Register(ActionNames.Weapon, new WeaponActionHandler(ctx));

            foreach (var action in ActionNames.InteractionActions)
                registry.Register(action, new InteractionActionHandler(action, ctx));

            return registry;
        }
    }
}
