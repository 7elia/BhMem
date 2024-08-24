using System.Diagnostics;
using System.Text;

namespace BhMem;

public class BhManager
{
    public BhProcess BhProcess { get; private set; }

    public int UserId
    {
        get
        {
            BhProcess.ReadInt32(UserIdAddress);
            return 0;
        }
    }

    private UIntPtr UserIdAddress;

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
        bhProcess.Exited += (o, e) => Environment.Exit(0);

        BhProcess = new BhProcess(bhProcess);

        return ScanMemory();
    }

    private bool ScanMemory()
    {
        try
        {
            Console.WriteLine("\nScanning for memory addresses... (This may take a while)");

            ThreadStackScanner.FindThreadStack(BhProcess.Process, 0);


            // if (BhProcess.FindPattern(Signatures.UserId.Pattern, out UIntPtr result))
            // {
            //     UserIdAddress = (UIntPtr) BhProcess.ReadInt32(result + (nuint) Signatures.UserId.Offset);
            //     Console.WriteLine(UserIdAddress);
            //     return true;
            // }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        Console.WriteLine("\nScanning failed!");
        return false;
    }
}