namespace TextRPG.Core.Effects
{
    public interface IGameEffect
    {
        string EffectId { get; }
        void Execute(EffectContext context);
    }
}
