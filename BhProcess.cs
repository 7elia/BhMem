using System.Diagnostics;
using System.Text;

namespace BhMem;

public class BhProcess(Process process)
{
    private Process Process { get; } = process;
    private ThreadInspector ThreadInspector { get; } = new(process);

    public UIntPtr GetThreadStackAddress(int stack)
    {
        return ThreadInspector.GetThreadStackAddress(stack);
    }

    public UIntPtr GetPointer(UIntPtr baseAddress, int[] offsets)
    {
        var current = baseAddress;

        foreach (var offset in offsets)
        {
            current = ReadPointer(current);
            if (current == UIntPtr.Zero)
            {
                return UIntPtr.Zero;
            }

            current = UIntPtr.Add(current, offset);
        }

        return current;
    }

    private byte[] ReadMemory(UIntPtr address, uint size)
    {
        var result = new byte[size];
        NativeMethods.ReadProcessMemory(Process.Handle, address, result, size, out _);
        return result;
    }

    public UIntPtr ReadPointer(UIntPtr address)
    {
        var buffer = new byte[IntPtr.Size];
        var success = NativeMethods.ReadProcessMemory(Process.Handle, address, buffer, buffer.Length, out var bytesRead);
    
        if (!success || bytesRead != buffer.Length)
        {
            return UIntPtr.Zero;
        }

        return BitConverter.ToUInt32(buffer, 0);
    }

    public int ReadInt32(UIntPtr address) => BitConverter.ToInt32(ReadMemory(address, sizeof(int)), 0);

    public uint ReadUInt32(UIntPtr address) => BitConverter.ToUInt32(ReadMemory(address, sizeof(uint)), 0);

    public long ReadInt64(UIntPtr address) => BitConverter.ToInt64(ReadMemory(address, sizeof(long)), 0);

    public ulong ReadUInt64(UIntPtr address) => BitConverter.ToUInt64(ReadMemory(address, sizeof(ulong)), 0);

    public float ReadFloat(UIntPtr address) => BitConverter.ToSingle(ReadMemory(address, sizeof(float)), 0);

    public double ReadDouble(UIntPtr address) => BitConverter.ToDouble(ReadMemory(address, sizeof(double)), 0);

    public bool ReadBool(UIntPtr address) => BitConverter.ToBoolean(ReadMemory(address, sizeof(bool)), 0);

    public string ReadString(UIntPtr address, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var stringAddress = (UIntPtr) ReadInt32(address);
        var length = ReadInt32(stringAddress + 0x4) * (encoding.Equals(Encoding.UTF8) ? 2 : 1);
        return encoding.GetString(ReadMemory(stringAddress + 0x8, (uint) length)).Replace("\0", string.Empty);
    }
}