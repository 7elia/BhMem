using System.Diagnostics;

namespace BhMem;

public class BhManager
{
    private BhProcess? BhProcess { get; set; }

    public int UserId
    {
        get
        {
            if (BhProcess == null) return 0;
            var stackAddress = BhProcess.GetThreadStackAddress(0);
            var address = BhProcess.GetPointer(stackAddress - 0x00000A48, [0x150, 0x84, 0x4, 0x2C, 0x140]);
            return BhProcess.ReadInt32(address);
        }
    }

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
            var stackAddress = BhProcess.GetThreadStackAddress(0);
            var address = BhProcess.GetPointer(stackAddress - 0x00000A48, [0x150, 0x84, 0x4, 0x2C, 0x140]);
            Console.WriteLine(BhProcess.ReadInt32(address));
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
        }

        Console.WriteLine("\nScanning failed!");
        return false;
    }
}