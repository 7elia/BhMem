using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BhMem;

public static class MemoryScanner
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint dwSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);


    private static IntPtr FindPattern(IntPtr processHandle, IntPtr startAddress, byte[] pattern, int regionSize)
    {
        var buffer = new byte[regionSize];

        for (var address = startAddress; address.ToInt64() < startAddress.ToInt64() + regionSize; address = IntPtr.Add(address, regionSize))
        {
            if (!ReadProcessMemory(processHandle, address, buffer, (uint) buffer.Length, out var bytesRead))
            {
                Console.WriteLine($"Failed to read memory at address: 0x{address.ToInt64():X}");
                continue;
            }
            
            for (var i = 0; i < bytesRead - pattern.Length; i++)
            {
                var found = !pattern.Where((t, j) => buffer[i + j] != t).Any();
                if (!found) continue;
                
                var foundAddress = IntPtr.Add(address, i);
                Console.WriteLine($"Pattern found at address: 0x{foundAddress.ToInt64():X}");
                return foundAddress;
            }
        }

        Console.WriteLine("Pattern not found in the given region.");
        return IntPtr.Zero;
    }

    public static void ScanMemory()
    {
        var process = Process.GetProcessesByName("BrawlhallaGame").FirstOrDefault();
        if (process == null)
        {
            Console.WriteLine("Process not found.");
            return;
        }

        var processHandle = OpenProcess(0x0400 | 0x0010, false, process.Id);
        if (processHandle == IntPtr.Zero)
        {
            Console.WriteLine("Failed to open process.");
            return;
        }

        if (process.MainModule == null) return;
        var currentAddress = process.MainModule.BaseAddress;
        var pattern = "THREADSTACK0"u8.ToArray();
        Console.WriteLine($"Searching for: {string.Join(" ", pattern)}");

        var status = VirtualQueryEx(processHandle, currentAddress, out var mbi, (uint) Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
        if (!status)
        {
            Console.WriteLine($"VirtualQueryEx failed at address: 0x{currentAddress.ToInt64():X}");
        }

        try
        {
            while (currentAddress.ToInt64() < process.MainModule.BaseAddress.ToInt64() + process.MainModule.ModuleMemorySize)
            {
                if (mbi.State == StateEnum.MEM_COMMIT || (mbi.Protect is AllocationProtectEnum.PAGE_EXECUTE_READ or AllocationProtectEnum.PAGE_EXECUTE_READWRITE or AllocationProtectEnum.PAGE_READONLY))
                {
                    var foundAddress = FindPattern(processHandle, mbi.BaseAddress, pattern, (int) mbi.RegionSize);
                    if (foundAddress != IntPtr.Zero)
                    {
                        Console.WriteLine($"Pattern found at: {foundAddress.ToInt64():X} (Offset adjusted: {foundAddress.ToInt64() - 0xA48:X})");
                        break;
                    }
                }

                currentAddress = IntPtr.Add(mbi.BaseAddress, (int) mbi.RegionSize);
                VirtualQueryEx(processHandle, currentAddress, out mbi, (uint) Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public AllocationProtectEnum AllocationProtect;
        public IntPtr RegionSize;
        public StateEnum State;
        public AllocationProtectEnum Protect;
        public TypeEnum Type;
    }

    private enum AllocationProtectEnum : uint
    {
        PAGE_EXECUTE = 0x00000010,
        PAGE_EXECUTE_READ = 0x00000020,
        PAGE_EXECUTE_READWRITE = 0x00000040,
        PAGE_EXECUTE_WRITECOPY = 0x00000080,
        PAGE_NOACCESS = 0x00000001,
        PAGE_READONLY = 0x00000002,
        PAGE_READWRITE = 0x00000004,
        PAGE_WRITECOPY = 0x00000008,
        PAGE_GUARD = 0x00000100,
        PAGE_NOCACHE = 0x00000200,
        PAGE_WRITECOMBINE = 0x00000400
    }

    private enum StateEnum : uint
    {
        MEM_COMMIT = 0x1000,
        MEM_FREE = 0x10000,
        MEM_RESERVE = 0x2000
    }

    private enum TypeEnum : uint
    {
        MEM_IMAGE = 0x1000000,
        MEM_MAPPED = 0x40000,
        MEM_PRIVATE = 0x20000
    }
}