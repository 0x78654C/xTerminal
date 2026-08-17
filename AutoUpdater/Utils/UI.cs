namespace AutoUpdater
{
    public static class UI
    {
        /// <summary>
        /// Write error output in color Red.
        /// </summary>
        /// <param name="text"></param>
        public static void ErrorWriteLine(object data)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error: {data}");
            Console.ForegroundColor = currentForeground;
        }
    }
}
