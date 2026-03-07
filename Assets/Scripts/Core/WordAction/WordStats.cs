using TextRPG.Core.ActionExecution;

namespace TextRPG.Core.WordAction
{
    public readonly record struct WordMeta(string Target, int Cost, int Range = 0, AreaShape Area = AreaShape.Single);
}
