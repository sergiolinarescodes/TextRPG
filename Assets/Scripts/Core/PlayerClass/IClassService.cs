namespace TextRPG.Core.PlayerClass
{
    public interface IClassService
    {
        PlayerClass SelectedClass { get; }
        ClassDefinition Definition { get; }
    }
}
