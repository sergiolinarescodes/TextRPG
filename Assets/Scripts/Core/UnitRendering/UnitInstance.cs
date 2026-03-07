using Unidad.Core.Grid;

namespace TextRPG.Core.UnitRendering
{
    public sealed class UnitInstance
    {
        public UnitId Id { get; }
        public UnitDefinition Definition { get; }
        public int CurrentHp { get; set; }
        public GridPosition Position { get; set; }

        public UnitInstance(UnitDefinition definition, GridPosition position)
        {
            Id = definition.Id;
            Definition = definition;
            CurrentHp = definition.MaxHp;
            Position = position;
        }
    }
}
