namespace Core
{
    public interface IAutoCompleteHandler
    {
        char[] Separators { get; set; }
        string[] GetSuggestions(string text, int index);
    }
}
