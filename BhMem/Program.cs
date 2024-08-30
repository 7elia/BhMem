using System.Reflection;

namespace BhMem.BhMem;

internal abstract class Program
{
    private static BhManager? _bhManager;

    private static void Main(string[] args)
    {
        _bhManager = new BhManager();

        if (!_bhManager.Initialize())
        {
            Console.WriteLine("\nBhMem will close in 5 seconds...");
            Thread.Sleep(5000);
            Environment.Exit(0);
            return;
        }

        DrawMainMenu();
    }

    private static void StartReading()
    {
        var shouldExit = false;
        var task = Task.Run(() =>
        {
            while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
            shouldExit = true;
        });

        while (!shouldExit)
        {
            if (_bhManager == null)
            {
                Console.WriteLine("Something went wrong.");
                shouldExit = true;
                break;
            }

            if (shouldExit) break;
            
            Thread.Sleep(100);
            
            Console.Clear();
            Console.WriteLine($"User ID: {_bhManager.UserId}");
            Console.WriteLine($"Opp. ID: {_bhManager.OpponentId}");
            Console.WriteLine($"Selected Legend: {_bhManager.SelectedLegend}");
            Console.WriteLine("\nESC. Back to Main Menu");
        }
        
        task.Dispose();
        DrawMainMenu();
    }

    private static void DrawPageHeader(string pageName)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        version = version.Remove(version.LastIndexOf(".0", StringComparison.Ordinal));
        Console.Clear();
        Console.WriteLine($"BhMem v{version}\n\n-- {pageName} --\n");
    }
    
    private static void DrawMainMenu()
    {
        DrawPageHeader("Main Menu");
        Console.WriteLine("1. Start Reading");
        Console.WriteLine("2. Settings (WIP)");

        switch (Console.ReadKey(true).Key)
        {
            case ConsoleKey.D1:
                StartReading();
                break;
            case ConsoleKey.D2:
                DrawSettings();
                break;
            default:
                DrawMainMenu();
                break;
        }
    }

    private static void DrawSettings()
    {
        DrawPageHeader("Settings");
        Console.WriteLine("!! WIP !!");
        Console.WriteLine("\nESC. Back to Main Menu");

        if (Console.ReadKey(true).Key == ConsoleKey.Escape)
        {
            DrawMainMenu();
        }
    }
}
