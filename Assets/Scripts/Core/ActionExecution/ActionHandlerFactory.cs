using System;
using TextRPG.Core.ActionExecution.Handlers;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.Weapon;

using DurationMode = TextRPG.Core.ActionExecution.Handlers.ApplyStatusEffectHandler.DurationMode;

namespace TextRPG.Core.ActionExecution
{
    internal static class ActionHandlerFactory
    {
        public static IActionHandler CreateFromDefinition(ActionTemplateDef def, IActionHandlerContext ctx)
        {
            return def.Template switch
            {
                "scaled_damage" => new ScaledDamageHandler(def.ActionId, ctx,
                    Enum.Parse<StatType>(def.Param1), Enum.Parse<StatType>(def.Param2)),

                "apply_status" => new ApplyStatusEffectHandler(def.ActionId, ctx,
                    Enum.Parse<StatusEffectType>(def.Param1), def.ApplySelf,
                    Enum.Parse<DurationMode>(def.Param2)),

                "stat_modifier" => new StatModifierActionHandler(def.ActionId, ctx,
                    Enum.Parse<StatType>(def.Param1), def.Param2 == "buff"),

                "heal" => new HealActionHandler(ctx),
                "shield" => new ShieldActionHandler(ctx),
                "mana_self" => new ThinkingActionHandler(ctx),
                "noop" => new PayActionHandler(),
                "push" => new PushActionHandler(ctx),
                "fire" => new FireActionHandler(ctx),

                _ => throw new ArgumentException($"Unknown action template: {def.Template}")
            };
        }

        public static ActionHandlerRegistry CreateDefault(IActionHandlerContext ctx)
        {
            var registry = new ActionHandlerRegistry();

            // 1. Data-driven handlers from definition table
            foreach (var def in ActionTemplateDefTable.Definitions)
            {
                if (def.Template == "apply_status" && ctx.StatusEffects == null)
                    continue;
                registry.Register(def.ActionId, CreateFromDefinition(def, ctx));
            }

            // 2. Complex handlers (require custom logic beyond template parameters)
            if (ctx.StatusEffects != null)
                registry.Register(ActionNames.Shock, new ShockActionHandler(ctx));

            if (ctx.StatusEffects != null && ctx.EntityStats != null)
                registry.Register(ActionNames.Concentrate, new ConcentrateActionHandler(ctx));

            if (ctx.StatusEffects != null && ctx.TurnService != null)
                registry.Register(ActionNames.Summon, new SummonActionHandler(ctx));

            if (ctx.WeaponService != null)
                registry.Register(ActionNames.Weapon, new WeaponActionHandler(ctx));

            // 3. RestHeal
            registry.Register(ActionNames.RestHeal, new RestHealActionHandler(ctx));

            // 4. Scramble (slot position swap)
            if (ctx.SlotService != null)
                registry.Register(ActionNames.Scramble, new ScrambleActionHandler(ctx));

            // 5. Creature actions (peck, screech) and cleansing actions (purify, awaken)
            if (ctx.StatusEffects != null)
            {
                registry.Register(ActionNames.Peck, new PeckActionHandler(ctx));
                registry.Register(ActionNames.Screech, new ScreechActionHandler(ctx));
                registry.Register(ActionNames.Purify, new PurifyActionHandler(ctx));
                registry.Register(ActionNames.Awaken, new AwakenActionHandler(ctx));
            }

            // 6. Interaction actions
            foreach (var action in ActionNames.InteractionActions)
                registry.Register(action, new InteractionActionHandler(action, ctx));

            return registry;
        }
    }
}
