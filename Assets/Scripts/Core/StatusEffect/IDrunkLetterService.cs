using System.Collections.Generic;

namespace TextRPG.Core.StatusEffect
{
    public interface IDrunkLetterService
    {
        bool IsActive { get; }
        int CurrentStacks { get; }
        char RemapInput(char input);
        bool IsLetterDrunk(char letter);
        bool IsRemappedChar(char c);
        IReadOnlyDictionary<char, char> GetScrambleMap();
    }
}
