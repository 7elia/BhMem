namespace BhMem;

class Program
{
    private static BhManager? _bhManager;

    private static void Main(string[] args)
    {
        _bhManager = new BhManager();

        if (_bhManager.Initialize()) return;
        
        Console.WriteLine("\nBhMem will close in 5 seconds...");
        Thread.Sleep(5000);
        Environment.Exit(0);
    }
}
