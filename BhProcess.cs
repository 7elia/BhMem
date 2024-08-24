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

                var threadHandle = NativeMethods.OpenThread(0x0040, false, entry.th32ThreadID);
                if (threadHandle == IntPtr.Zero)
                {
                    Console.WriteLine($"Couldn't open thread: {Marshal.GetLastWin32Error()}");
                    continue;
                }

                var tbi = new NativeMethods.THREAD_BASIC_INFORMATION();
                var tdiLength = (uint) Marshal.SizeOf(typeof(NativeMethods.THREAD_BASIC_INFORMATION));
                var status = NativeMethods.NtQueryInformationThread(threadHandle, NativeMethods.ThreadBasicInformation, ref tbi, tdiLength, IntPtr.Zero);
                NativeMethods.CloseHandle(threadHandle);

                if (status == 0)
                {
                    var stackBase = ReadStackBase(process.Handle, tbi.TebBaseAddress);
                    if (stackBase != IntPtr.Zero)
                    {
                        Console.WriteLine($"0x{stackBase - 0xA48:X}");
                    }

                    continue;
                }

                Console.WriteLine($"Failed to query information for thread ID: {entry.th32ThreadID}, Status: {status:X} / {Marshal.GetLastWin32Error()}");
            } while (NativeMethods.Thread32Next(snapshot, ref entry));
        }
        else
        {
            Console.WriteLine("Failed to iterate over threads.");
        }

        NativeMethods.CloseHandle(snapshot);
        return IntPtr.Zero;
    }

    private static IntPtr ReadStackBase(IntPtr processHandle, IntPtr tebAddress)
    {
        const int tebStackBaseOffset = 0x8;

        var buffer = new byte[IntPtr.Size];
        if (NativeMethods.ReadProcessMemory(processHandle, tebAddress + tebStackBaseOffset, buffer, (uint) buffer.Length, out var bytesRead) && bytesRead == buffer.Length)
        {
            return new IntPtr(BitConverter.ToInt64(buffer, 0));
        }

        Console.WriteLine($"Failed to read stack base from TEB: {Marshal.GetLastWin32Error()}");
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