using UnityEngine;

namespace TextRPG.Core.ActionAnimation
{
    public static class ProjectileStyle
    {
        public static Color GetColor(string actionId)
        {
            return actionId?.ToLowerInvariant() switch
            {
                "damage" => new Color(1f, 0.2f, 0.2f),
                "magicdamage" => new Color(0.6f, 0.3f, 1f),
                "heal" => new Color(0.2f, 1f, 0.3f),
                "burn" => new Color(1f, 0.6f, 0f),
                "poison" => new Color(0.6f, 0.1f, 0.9f),
                "bleed" => new Color(0.6f, 0f, 0f),
                "shield" => new Color(0.6f, 0.6f, 0.6f),
                "stun" => new Color(1f, 1f, 0.2f),
                "shock" => new Color(0.3f, 0.7f, 1f),
                "fear" => new Color(0.4f, 0f, 0.5f),
                "freeze" => new Color(0.5f, 0.9f, 1f),
                "grow" => new Color(0.3f, 0.8f, 0.2f),
                "thorns" => new Color(0.4f, 0.6f, 0.1f),
                "reflect" => new Color(0.8f, 0.8f, 1f),
                "hardening" => new Color(0.5f, 0.4f, 0.3f),
                "concentrate" => new Color(1f, 0.9f, 0.4f),
                "summon" => new Color(0.7f, 0.5f, 1f),
                "mana" => new Color(0.3f, 0.5f, 1f),
                "apply_status" => Color.yellow,
                _ => Color.white
            };
        }
    }
}
