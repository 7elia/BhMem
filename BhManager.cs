using System.Diagnostics;

namespace BhMem;

public class BhManager
{
    private BhProcess? BhProcess { get; set; }

    public int UserId
    {
        get
        {
            BhProcess?.ReadInt32(_threadStackAddress);
            return 0;
        }
    }

    private UIntPtr _threadStackAddress;

    public bool Initialize()
    {
        Console.WriteLine("Initializing...");

        var bhProcess = Process.GetProcessesByName("BrawlhallaGame").FirstOrDefault();

        if (bhProcess == default)
        {
            Console.WriteLine("Brawlhalla process not found! Please launch Brawlhalla first!");
            return false;
        }

        bhProcess.EnableRaisingEvents = true;
        bhProcess.Exited += (_, _) => Environment.Exit(0);

        BhProcess = new BhProcess(bhProcess);

        return ScanMemory();
    }

    private bool ScanMemory()
    {
        if (BhProcess == null)
        {
            Console.WriteLine("Process not initialized.");
            return false;
        }
        try
        {
            Console.WriteLine("\nScanning for memory addresses... (This may take a while)");

            _threadStackAddress = BhProcess.FindThreadStack();
            Console.WriteLine(_threadStackAddress.ToUInt64().ToString("X"));
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
        }

        Console.WriteLine("\nScanning failed!");
        return false;
    }
}