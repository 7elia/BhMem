using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace BhMem;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class NativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateToolhelp32Snapshot(SnapshotFlags dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Thread32First(IntPtr hSnapshot, ref THREADENTRY32 lpte);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Thread32Next(IntPtr hSnapshot, ref THREADENTRY32 lpte);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(IntPtr hProcess, uint lpBaseAddress, uint[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint dwSize, out UIntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, uint cb);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtQueryInformationThread(IntPtr ThreadHandle, THREADINFOCLASS ThreadInformationClass, ref THREAD_BASIC_INFORMATION ThreadInformation, int ThreadInformationLength, IntPtr ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, out NT_TIB lpBuffer, int dwSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr hModule);

    public enum THREADINFOCLASS
    {
        ThreadBasicInformation = 0
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NT_TIB
    {
        public IntPtr ExceptionList;
        public IntPtr StackBase;
        public IntPtr StackLimit;
        public IntPtr SubSystemTib;
        public IntPtr FiberData;
        public IntPtr ArbitraryUserPointer;
        public IntPtr Self;
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

    [StructLayout(LayoutKind.Sequential)]
    public struct LDT_ENTRY
    {
        public ushort LimitLow;
        public ushort BaseLow;
        public byte BaseMid;
        public byte Flags1;
        public byte Flags2;
        public byte BaseHi;
    }

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
    public struct MODULEINFO
    {
        public uint lpBaseOfDll;
        public uint SizeOfImage;
        public uint EntryPoint;
    }

    [Flags]
    public enum SnapshotFlags : uint
    {
        Thread = 0x00000004,
    }

    [Flags]
    public enum ProcessAccessFlags : uint
    {
        All = 0x001F0FFF,
    }

    [Flags]
    public enum ThreadAccess : uint
    {
        GET_CONTEXT = 0x0008,
        QUERY_INFORMATION = 0x0040,
    }
}