using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BhMem;

public class BhProcess(Process process)
{
    private const uint TH32CS_SNAPTHREAD = 0x00000004;
    private const int ThreadBasicInformation = 0x0;

    private Process Process { get; } = process;

    public UIntPtr FindThreadStack()
    {
        var snapshot = NativeMethods.CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, (uint) Process.Id);

        if (snapshot == IntPtr.Zero)
        {
            Console.WriteLine($"Failed to take a snapshot of the threads: {Marshal.GetLastWin32Error()}");
            return UIntPtr.Zero;
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
                var tdiLength = (uint) Marshal.SizeOf(typeof(NativeMethods.THREAD_BASIC_INFORMATION)) - 1;
                var status = NativeMethods.NtQueryInformationThread(threadHandle, ThreadBasicInformation, ref tbi, tdiLength, IntPtr.Zero);
                NativeMethods.CloseHandle(threadHandle);
                
                if (status == 0)
                {
                    Console.WriteLine($"Thread ID: {entry.th32ThreadID}, StackBase: 0x{tbi.StackBase.ToInt64():X}");
                    return (UIntPtr) tbi.StackBase;
                }
                Console.WriteLine($"Failed to query information for thread ID: {entry.th32ThreadID}, Status: {status:X} / {Marshal.GetLastWin32Error()}");
            } while (NativeMethods.Thread32Next(snapshot, ref entry));
        }
        else
        {
            Console.WriteLine("Failed to iterate over threads.");
        }

        NativeMethods.CloseHandle(snapshot);
        return UIntPtr.Zero;
    }

    private byte[] ReadMemory(UIntPtr address, uint size)
    {
        var result = new byte[size];
        NativeMethods.ReadProcessMemory(Process.Handle, address, result, size, out UIntPtr bytesRead);
        return result;
    }

    public UIntPtr ReadMemory(UIntPtr address, byte[] buffer, uint size)
    {
        NativeMethods.ReadProcessMemory(Process.Handle, address, buffer, size, out var bytesRead);
        return bytesRead;
    }

    public int ReadInt32(UIntPtr address) => BitConverter.ToInt32(ReadMemory(address, sizeof(int)), 0);

    public uint ReadUInt32(UIntPtr address) => BitConverter.ToUInt32(ReadMemory(address, sizeof(uint)), 0);

    public long ReadInt64(UIntPtr address) => BitConverter.ToInt64(ReadMemory(address, sizeof(long)), 0);

    public ulong ReadUInt64(UIntPtr address) => BitConverter.ToUInt64(ReadMemory(address, sizeof(ulong)), 0);

    public float ReadFloat(UIntPtr address) => BitConverter.ToSingle(ReadMemory(address, sizeof(float)), 0);

    public double ReadDouble(UIntPtr address) => BitConverter.ToDouble(ReadMemory(address, sizeof(double)), 0);

    public bool ReadBool(UIntPtr address) => BitConverter.ToBoolean(ReadMemory(address, sizeof(bool)), 0);

    public string ReadString(UIntPtr address, Encoding? encoding)
    {
        encoding ??= Encoding.UTF8;
        var stringAddress = (UIntPtr)ReadInt32(address);
        var length = ReadInt32(stringAddress + 0x4) * (encoding.Equals(Encoding.UTF8) ? 2 : 1);
        return encoding.GetString(ReadMemory(stringAddress + 0x8, (uint)length)).Replace("\0", string.Empty);
    }
}
