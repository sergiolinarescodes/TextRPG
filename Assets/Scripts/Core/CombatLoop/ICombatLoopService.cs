namespace TextRPG.Core.CombatLoop
{
    public interface ICombatLoopService
    {
        bool IsPlayerTurn { get; }
        bool IsGameOver { get; }
        void Start();
        WordSubmitResult SubmitWord(string word);
        bool FireWeapon();
        bool UseConsumable();
    }
}
