using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BhMem.BhMem.Memory;

internal class ThreadInspector(Process process)
{
    private Process Process { get; } = process;
    private readonly Dictionary<int, UIntPtr> _threadStackCache = [];

    public UIntPtr GetThreadStackAddress(int stack)
    {
        if (_threadStackCache.TryGetValue(stack, out var value))
        {
            return value;
        }
        var dwProcId = (uint) Process.Id;
        var hProcHandle = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, dwProcId);

        if (hProcHandle == IntPtr.Zero)
        {
            Console.WriteLine("Failed to open process -- invalid handle");
            Console.WriteLine($"Error code: {Marshal.GetLastWin32Error()}");
            return UIntPtr.Zero;
        }

        var threadIds = ThreadList(dwProcId);
        var stackNum = 0;

        foreach (var threadStartAddress in threadIds.Select(threadId => NativeMethods.OpenThread(NativeMethods.ThreadAccess.GET_CONTEXT | NativeMethods.ThreadAccess.QUERY_INFORMATION, false, threadId)).Select(threadHandle => GetThreadStartAddress(hProcHandle, threadHandle)))
        {
            if (stackNum == stack)
            {
                NativeMethods.CloseHandle(hProcHandle);
                _threadStackCache.Add(stack, threadStartAddress);
                return threadStartAddress;
            }

            stackNum++;
        }

        NativeMethods.CloseHandle(hProcHandle);
        return UIntPtr.Zero;
    }

    private static List<uint> ThreadList(uint pid)
    {
        List<uint> threadIds = [];
        var snapshot = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.SnapshotFlags.Thread, 0);

        if (snapshot == IntPtr.Zero)
            return threadIds;

        var threadEntry = new NativeMethods.THREADENTRY32
        {
            dwSize = (uint) Marshal.SizeOf(typeof(NativeMethods.THREADENTRY32))
        };

        if (NativeMethods.Thread32First(snapshot, ref threadEntry))
        {
            do
            {
                if (threadEntry.th32OwnerProcessID != pid) continue;
                threadIds.Add(threadEntry.th32ThreadID);
            } while (NativeMethods.Thread32Next(snapshot, ref threadEntry));
        }

        NativeMethods.CloseHandle(snapshot);
        return threadIds;
    }

    private static uint GetThreadStartAddress(IntPtr processHandle, IntPtr hThread)
    {
        var stackTop = (uint) GetThreadStackTopAddress_x86(processHandle, hThread);
        uint result = 0;

        NativeMethods.GetModuleInformation(processHandle, NativeMethods.GetModuleHandle("kernel32.dll"), out var mi, (uint) Marshal.SizeOf(typeof(NativeMethods.MODULEINFO)));

        var buffer = new uint[4096 / sizeof(uint)];
        if (stackTop != 0)
        {
            if (NativeMethods.ReadProcessMemory(processHandle, stackTop - 4096, buffer, buffer.Length * sizeof(uint), out _))
            {
                for (var i = buffer.Length - 1; i >= 0; i--)
                {
                    if (buffer[i] < mi.lpBaseOfDll || buffer[i] > mi.lpBaseOfDll + mi.SizeOfImage) continue;
                    result = stackTop - 4096 + (uint) (i * sizeof(uint));
                    break;
                }
            }
        }

        NativeMethods.CloseHandle(hThread);
        return result;
    }

    private static IntPtr GetThreadStackTopAddress_x86(IntPtr hProcess, IntPtr hThread)
    {
        var module = NativeMethods.GetModuleHandle("ntdll.dll");

        var loadedManually = false;
        if (module == IntPtr.Zero)
        {
            module = NativeMethods.LoadLibrary("ntdll.dll");
            loadedManually = true;
        }

        var tbi = new NativeMethods.THREAD_BASIC_INFORMATION();
        var status = NativeMethods.NtQueryInformationThread(hThread, NativeMethods.THREADINFOCLASS.ThreadBasicInformation, ref tbi, Marshal.SizeOf(typeof(NativeMethods.THREAD_BASIC_INFORMATION)), IntPtr.Zero);

        if (status >= 0)
        {
            if (NativeMethods.ReadProcessMemory(hProcess, tbi.TebBaseAddress, out var tib, Marshal.SizeOf(typeof(NativeMethods.NT_TIB)), out var bytesRead) && bytesRead == Marshal.SizeOf(typeof(NativeMethods.NT_TIB)))
            {
                if (loadedManually)
                {
                    NativeMethods.FreeLibrary(module);
                }

                return tib.StackBase;
            }
        }

        if (loadedManually)
        {
            NativeMethods.FreeLibrary(module);
        }

        return IntPtr.Zero;
    }
}