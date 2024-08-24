using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BhMem;

public class BhProcess(Process process)
{
    private Process Process { get; } = process;

    public IntPtr FindThreadStack()
    {
        var snapshot = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.TH32CS_SNAPTHREAD, (uint) Process.Id);

        if (snapshot == IntPtr.Zero)
        {
            Console.WriteLine($"Failed to take a snapshot of the threads: {Marshal.GetLastWin32Error()}");
            return IntPtr.Zero;
        }

        var entry = new NativeMethods.THREADENTRY32
        {
            dwSize = (uint) Marshal.SizeOf(typeof(NativeMethods.THREADENTRY32))
        };

        if (NativeMethods.Thread32First(snapshot, ref entry))
        {
            do
            {
                if (entry.th32OwnerProcessID != (uint) Process.Id) continue;

                var ptr = FindThreadStackBase(entry.th32ThreadID);
                Console.WriteLine($"0x{ptr:X}");
            } while (NativeMethods.Thread32Next(snapshot, ref entry));
        }
        else
        {
            Console.WriteLine("Failed to iterate over threads.");
        }

        NativeMethods.CloseHandle(snapshot);
        return IntPtr.Zero;
    }

    private IntPtr FindThreadStackBase(uint threadId)
    {
        var threadHandle = NativeMethods.OpenThread(0x0040, false, threadId);
        if (threadHandle == IntPtr.Zero)
        {
            Console.WriteLine($"Failed to open thread: {Marshal.GetLastWin32Error()}");
            return IntPtr.Zero;
        }

        var tebAddress = GetTebBaseAddress(threadHandle);
        if (tebAddress != IntPtr.Zero)
        {
            var stackBase = ReadStackBase(tebAddress);
            if (stackBase != IntPtr.Zero)
            {
                return stackBase;
            }
        }

        NativeMethods.CloseHandle(threadHandle);
        return IntPtr.Zero;
    }

    private static IntPtr GetTebBaseAddress(IntPtr threadHandle)
    {
        if (Environment.Is64BitProcess)
        {
            var tbi = new NativeMethods.THREAD_BASIC_INFORMATION();
            var status = NativeMethods.NtQueryInformationThread(
                threadHandle,
                NativeMethods.ThreadBasicInformation,
                ref tbi,
                (uint) Marshal.SizeOf(typeof(NativeMethods.THREAD_BASIC_INFORMATION)),
                IntPtr.Zero
            );
            if (status == 0)
            {
                return tbi.TebBaseAddress;
            }
        }
        else
        {
            if (NativeMethods.Wow64GetThreadSelectorEntry(threadHandle, 0x18, out var ldtEntry))
            {
                return (ldtEntry.BaseHi << 24) | (ldtEntry.BaseMid << 16) | ldtEntry.BaseLow;
            }
        }

        Console.WriteLine($"Failed to get TEB Base Address: {Marshal.GetLastWin32Error()}");
        return IntPtr.Zero;
    }

    private IntPtr ReadStackBase(IntPtr tebAddress)
    {
        var tebStackBaseOffset = Environment.Is64BitProcess ? 0x8 : 0x4;
        var buffer = new byte[IntPtr.Size];
        if (NativeMethods.ReadProcessMemory(Process.Handle, tebAddress + tebStackBaseOffset, buffer, (uint) buffer.Length, out var bytesRead) && bytesRead == buffer.Length)
        {
            return (IntPtr.Size == 4) ? new IntPtr(BitConverter.ToInt32(buffer, 0)) : new IntPtr(BitConverter.ToInt64(buffer, 0));
        }

        Console.WriteLine($"Failed to read stack base from TEB: {Marshal.GetLastWin32Error()} ({bytesRead} / {buffer.Length})");
        return IntPtr.Zero;
    }

    private byte[] ReadMemory(IntPtr address, uint size)
    {
        var result = new byte[size];
        NativeMethods.ReadProcessMemory(Process.Handle, address, result, size, out _);
        return result;
    }

    public IntPtr ReadMemory(IntPtr address, byte[] buffer, uint size)
    {
        NativeMethods.ReadProcessMemory(Process.Handle, address, buffer, size, out var bytesRead);
        return bytesRead;
    }

    public int ReadInt32(IntPtr address) => BitConverter.ToInt32(ReadMemory(address, sizeof(int)), 0);

    public uint ReadUInt32(IntPtr address) => BitConverter.ToUInt32(ReadMemory(address, sizeof(uint)), 0);

    public long ReadInt64(IntPtr address) => BitConverter.ToInt64(ReadMemory(address, sizeof(long)), 0);

    public ulong ReadUInt64(IntPtr address) => BitConverter.ToUInt64(ReadMemory(address, sizeof(ulong)), 0);

    public float ReadFloat(IntPtr address) => BitConverter.ToSingle(ReadMemory(address, sizeof(float)), 0);

    public double ReadDouble(IntPtr address) => BitConverter.ToDouble(ReadMemory(address, sizeof(double)), 0);

    public bool ReadBool(IntPtr address) => BitConverter.ToBoolean(ReadMemory(address, sizeof(bool)), 0);

    public string ReadString(IntPtr address, Encoding? encoding)
    {
        encoding ??= Encoding.UTF8;
        var stringAddress = ReadInt32(address);
        var length = ReadInt32(stringAddress + 0x4) * (encoding.Equals(Encoding.UTF8) ? 2 : 1);
        return encoding.GetString(ReadMemory(stringAddress + 0x8, (uint) length)).Replace("\0", string.Empty);
    }
}