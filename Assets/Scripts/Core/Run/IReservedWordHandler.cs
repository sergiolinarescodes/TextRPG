namespace TextRPG.Core.Run
{
    public interface IReservedWordHandler
    {
        bool TryHandleReservedWord(string word);
    }
}
