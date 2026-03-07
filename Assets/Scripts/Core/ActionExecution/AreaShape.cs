namespace TextRPG.Core.ActionExecution
{
    public enum AreaShape
    {
        Single,     // 1 tile — no expansion (default)
        Cross,      // + shape — center + 4 cardinal neighbors
        Square3x3,  // 3x3 grid centered on target
        Diamond2,   // all tiles within Manhattan distance 2
        Line3,      // 3 tiles in line from caster through target
        VerticalLine, // full vertical line in front of caster through target
    }
}
