namespace Commands
{
    public interface ITerminalCommand
    {
        string Name { get; }
        void Execute(string[] args);
    }
}