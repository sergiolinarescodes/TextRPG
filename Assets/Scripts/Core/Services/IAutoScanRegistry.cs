namespace TextRPG.Core.Services
{
    public interface IAutoScanRegistry<in T> where T : class
    {
        void RegisterScanned(T item);
    }
}
