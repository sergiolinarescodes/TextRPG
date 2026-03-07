using TextRPG.Core.ActionExecution;

namespace TextRPG.Core.WordAction
{
    public readonly record struct WordActionMapping(
        string ActionId,
        int Value,
        string Target = null,
        int? Range = null,
        AreaShape? Area = null,
        string AssocWord = "");
}
