using System.Collections.Generic;

namespace TextRPG.Core.WordAction
{
    public readonly record struct WordResolvedEvent(string Word, IReadOnlyList<WordActionMapping> Actions, WordMeta Stats);
}
