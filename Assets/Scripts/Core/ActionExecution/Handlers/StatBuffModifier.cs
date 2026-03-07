using Unidad.Core.Patterns.Modifier;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class StatBuffModifier : IModifier<int>
    {
        public string Id { get; }
        public int Priority => 0;
        public bool IsActive => true;

        private readonly int _amount;

        public StatBuffModifier(string id, int amount)
        {
            Id = id;
            _amount = amount;
        }

        public int Apply(int value) => value + _amount;
    }
}
