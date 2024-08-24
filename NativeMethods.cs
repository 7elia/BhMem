using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace BhMem;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class NativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint dwSize, out IntPtr lpNumberOfBytesRead);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessId);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool Thread32First(IntPtr hSnapshot, ref THREADENTRY32 lpte);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Thread32Next(IntPtr hSnapshot, ref THREADENTRY32 lpte);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
    [DllImport("ntdll.dll")]
    public static extern int NtQueryInformationThread(
        IntPtr ThreadHandle,
        int ThreadInformationClass,
        ref THREAD_BASIC_INFORMATION ThreadInformation,
        uint ThreadInformationLength,
        IntPtr ReturnLength
    );
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
    
    public const uint TH32CS_SNAPTHREAD = 0x00000004;
    public const uint PROCESS_VM_READ = 0x0010;
    public const int ThreadBasicInformation = 0x0;
    
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

    [StructLayout(LayoutKind.Sequential)]
    public struct THREAD_BASIC_INFORMATION
    {
        public uint ExitStatus;
        public IntPtr TebBaseAddress;
        public CLIENT_ID ClientId;
        public IntPtr AffinityMask;
        public int Priority;
        public int BasePriority;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct CLIENT_ID
    {
        public IntPtr UniqueProcess;
        public IntPtr UniqueThread;
    }
}