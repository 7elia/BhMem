namespace BhMem;

class Program
{
    private static BhManager BhManager;
    
    static void Main(string[] args)
    {
        BhManager = new BhManager();

        // MemoryScanner.ScanMemory();

        if (!BhManager.Initialize())
        {
            Console.WriteLine("\nBhMem will close in 5 seconds...");
            Thread.Sleep(5000);
            Environment.Exit(0);
        }
    }
}
