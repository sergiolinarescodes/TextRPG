namespace TextRPG.Core.ActionExecution
{
    public static class ActionNames
    {
        public const string Damage = "Damage";
        public const string MagicDamage = "MagicDamage";
        public const string WeaponDamage = "WeaponDamage";
        public const string Heal = "Heal";
        public const string Shield = "Shield";
        public const string Shock = "Shock";
        public const string Push = "Push";
        public const string Fire = "Fire";
        public const string Burn = "Burn";
        public const string Water = "Water";
        public const string Fear = "Fear";
        public const string Stun = "Stun";
        public const string Poison = "Poison";
        public const string Bleed = "Bleed";
        public const string Grow = "Grow";
        public const string Thorns = "Thorns";
        public const string Reflect = "Reflect";
        public const string Hardening = "Hardening";
        public const string Drunk = "Drunk";
        public const string Concentrate = "Concentrate";
        public const string Summon = "Summon";
        public const string Weapon = "Weapon";
        public const string Item = "Item";
        public const string Thinking = "Thinking";
        public const string Buff = "Buff";
        public const string Melt = "Melt";
        public const string Curse = "Curse";
        public const string Smash = "Smash";
        public const string Charm = "Charm";
        public const string Pay = "Pay";
        public const string Freeze = "Freeze";
        public const string Energize = "Energize";
        public const string Relax = "Relax";
        public const string SleepAction = "Sleep";
        public const string RestHeal = "RestHeal";

        public static readonly string[] InteractionActions =
        {
            "Enter", "Talk", "Steal", "Search", "Pray",
            "Rest", "Open", "Trade", "Recruit", "Leave",
            "Charm"
        };
    }
}
