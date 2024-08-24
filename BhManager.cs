using System.Diagnostics;

namespace BhMem;

public class BhManager
{
    private BhProcess? BhProcess { get; set; }

    public int UserId
    {
        get
        {
            // BhProcess?.ReadInt32(_threadStackAddress);
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

            _threadStackAddress = BhProcess.GetThreadStackAddress(0);
            Console.WriteLine(_threadStackAddress.ToString("X"));
            var pointer = BhProcess?.GetPointer(_threadStackAddress - 0x00000A48, [0x150, 0x84, 0x4, 0x2C, 0x140]);
            Console.WriteLine(pointer.GetValueOrDefault().ToString("X"));
            var result = BhProcess?.ReadInt32(pointer.GetValueOrDefault());
            Console.WriteLine(result.GetValueOrDefault().ToString("X"));
            // _threadStackAddress = BhProcess.FindThreadStack();
            // Console.WriteLine(_threadStackAddress.ToString("X"));
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
        }

        Console.WriteLine("\nScanning failed!");
        return false;
    }
}