using System.Runtime.InteropServices;

namespace BhMem;

public static class NativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint dwSize, out UIntPtr lpNumberOfBytesRead);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool Thread32First(IntPtr hSnapshot, ref THREADENTRY32 lpte);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Thread32Next(IntPtr hSnapshot, ref THREADENTRY32 lpte);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtQueryInformationThread(
        IntPtr threadHandle, 
        int threadInformationClass, 
        ref THREAD_BASIC_INFORMATION threadInformation, 
        uint threadInformationLength, 
        IntPtr returnLength
    );
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);
    
    [StructLayout(LayoutKind.Sequential)]
    public struct THREADENTRY32
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
    public struct THREAD_BASIC_INFORMATION
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