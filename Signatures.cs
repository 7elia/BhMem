using System.Text;

namespace BhMem;

public static class Signatures
{
    public static readonly Signature UserId = new()
    {
        Pattern = "54 48 52 45 41 44 53 54 41 43 4B 30 30",
        Offset = -0xA48
    };
}

public class Signature
{
    public string Pattern;
    public int Offset;
}
