using Unidad.Core.Patterns.Modifier;

namespace TextRPG.Core.StatusEffect
{
    internal sealed class StatusEffectModifier : IModifier<int>
    {
        public string Id { get; }
        public int Priority => 0;
        public bool IsActive => true;
        private readonly int _amount;

        public StatusEffectModifier(string id, int amount)
        {
            Id = id;
            _amount = amount;
        }

        public int Apply(int value) => value + _amount;
    }
}
