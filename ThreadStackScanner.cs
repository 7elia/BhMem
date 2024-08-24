using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BhMem;

public class ThreadStackScanner
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool Thread32First(IntPtr hSnapshot, ref THREADENTRY32 lpte);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool Thread32Next(IntPtr hSnapshot, ref THREADENTRY32 lpte);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtQueryInformationThread(
        IntPtr threadHandle, 
        int threadInformationClass, 
        ref THREAD_BASIC_INFORMATION threadInformation, 
        uint threadInformationLength, 
        IntPtr returnLength
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
    private const uint TH32CS_SNAPTHREAD = 0x00000004;
    private const int ThreadBasicInformation = 0;

    public static IntPtr FindThreadStack(Process process, int id)
    {
        var snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, (uint) process.Id);

        if (snapshot == IntPtr.Zero)
        {
            Console.WriteLine($"Failed to take a snapshot of the threads: {Marshal.GetLastWin32Error()}");
            return IntPtr.Zero;
        }

        var entry = new THREADENTRY32
        {
            dwSize = (uint) Marshal.SizeOf(typeof(THREADENTRY32))
        };

        if (Thread32First(snapshot, ref entry))
        {
            do
            {
                if (entry.th32OwnerProcessID != (uint) process.Id) continue;
                
                var threadHandle = OpenThread(0x0040, false, entry.th32ThreadID);
                if (threadHandle == IntPtr.Zero)
                {
                    Console.WriteLine($"Couldn't open thread: {Marshal.GetLastWin32Error()}");
                    continue;
                }
                
                var tbi = new THREAD_BASIC_INFORMATION();
                var tdiLength = (uint) Marshal.SizeOf(typeof(THREAD_BASIC_INFORMATION)) - 1;
                var status = NtQueryInformationThread(threadHandle, ThreadBasicInformation, ref tbi, tdiLength, IntPtr.Zero);
                CloseHandle(threadHandle);
                
                if (status == 0)
                {
                    Console.WriteLine($"Thread ID: {entry.th32ThreadID}, StackBase: 0x{tbi.StackBase.ToInt64():X}");
                    return tbi.StackBase;
                }
                Console.WriteLine($"Failed to query information for thread ID: {entry.th32ThreadID}, Status: {status:X} / {Marshal.GetLastWin32Error()}");
            } while (Thread32Next(snapshot, ref entry));
        }
        else
        {
            Console.WriteLine("Failed to iterate over threads.");
        }

        CloseHandle(snapshot);
        return IntPtr.Zero;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct THREADENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ThreadID;
        public uint th32OwnerProcessID;
        public int tpBasePri;
        public int tpDeltaPri;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct THREAD_BASIC_INFORMATION
    {
        public int ExitStatus;
        public IntPtr TebBaseAddress;
        public IntPtr StackBase;
        public IntPtr StackLimit;
        public IntPtr SubSystemTib;
        public IntPtr FiberData;
        public IntPtr ArbitraryUserPointer;
        public IntPtr Self;
    }
}