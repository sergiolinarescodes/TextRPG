using TextRPG.Core.EntityStats;

namespace TextRPG.Core.CombatLoop
{
    public readonly record struct PlayerTurnStartedEvent(int TurnNumber, int RoundNumber);
    public readonly record struct PlayerTurnEndedEvent();
    public readonly record struct GameOverEvent(EntityId PlayerId);

    public enum WordSubmitResult { Accepted, InvalidWord, InsufficientMana, NotPlayerTurn, GameOver }
}
