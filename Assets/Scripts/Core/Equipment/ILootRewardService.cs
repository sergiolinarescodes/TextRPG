namespace TextRPG.Core.Equipment
{
    public interface ILootRewardService
    {
        void GenerateAndOffer();
        void SelectReward(int index);
        bool IsAwaitingSelection { get; }
    }
}
