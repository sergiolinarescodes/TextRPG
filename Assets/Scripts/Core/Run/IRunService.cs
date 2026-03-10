using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Run
{
    public interface IRunService
    {
        RunDefinition CurrentRun { get; }
        int CurrentNodeIndex { get; }
        RunNode CurrentNode { get; }
        bool IsRunActive { get; }
        bool IsRunComplete { get; }
        bool IsAwaitingAdvance { get; }
        void StartRun(RunDefinition run, EntityId player);
        void AdvanceToNextNode();
        bool TryEscape();
    }
}
