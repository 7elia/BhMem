using System.Diagnostics;
using BhMem.BhMem.Helpers;
using BhMem.BhMem.Memory;

namespace BhMem.BhMem;

public class BhManager
{
    private BhProcess? BhProcess { get; set; }

    public int UserId
    {
        get
        {
            if (BhProcess == null) return 0;
            
            var stackAddress = BhProcess.GetThreadStackAddress(0);
            var address = BhProcess.GetPointer(stackAddress - 0xA48, [0x150, 0x84, 0x4, 0x2C, 0x140]);
            return BhProcess.ReadInt32(address);
        }
    }

    public int OpponentId
    {
        get
        {
            if (BhProcess == null) return 0;
            
            var stackAddress = BhProcess.GetThreadStackAddress(0);
            var address = BhProcess.GetPointer(stackAddress - 0xA48, [0x158, 0x98, 0x4, 0xC, 0x8, 0x18, 0x10, 0x714]);
            var id = BhProcess.ReadInt32(address);
            
            // The list of player ID's in a game isn't ordered consistently, so ~50% of the time the first address will yield the same result as `UserId`.
            if (id != UserId) return id;
            
            address = BhProcess.GetPointer(stackAddress - 0xA48, [0x158, 0x98, 0x8, 0xC34]);
            return BhProcess.ReadInt32(address);
        }
    }

    public Legend SelectedLegend
    {
        get
        {
            if (BhProcess == null) return Legend.Unknown;
            var stackAddress = BhProcess.GetThreadStackAddress(0);
            var address = BhProcess.GetPointer(stackAddress - 0xA48, [0x148, 0x7C, 0x8, 0xFC]);
            return (Legend) BhProcess.ReadInt32(address);
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

        Console.WriteLine(UserId);
        
        return true;
    }
}