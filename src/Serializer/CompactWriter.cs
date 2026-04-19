using System.Text;

namespace CompactSerializer;

internal sealed class CompactWriter
{
    private readonly MemoryStream _stream = new();

    public void WriteByte(byte value) => _stream.WriteByte(value);

    public void WriteBytes(byte[] value) => _stream.Write(value, 0, value.Length);

    public void WriteByteArray(byte[] value)
    {
        WriteUInt32((uint)value.Length);
        WriteBytes(value);
    }

    public void WriteString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteUInt32((uint)bytes.Length);
        WriteBytes(bytes);
    }

    public void WriteInt32(int value) => WriteUInt32(ZigZag(value));

    public void WriteInt64(long value) => WriteUInt64(ZigZag(value));

    public void WriteUInt32(uint value)
    {
        while (value >= 0x80)
        {
            WriteByte((byte)(value | 0x80));
            value >>= 7;
        }

        WriteByte((byte)value);
    }

    public void WriteUInt64(ulong value)
    {
        while (value >= 0x80)
        {
            WriteByte((byte)(value | 0x80));
            value >>= 7;
        }

        WriteByte((byte)value);
    }

    public void WriteSingle(float value)
    {
        var bytes = BitConverter.GetBytes(value);
        WriteBytes(bytes);
    }

    public void WriteDouble(double value)
    {
        var bytes = BitConverter.GetBytes(value);
        WriteBytes(bytes);
    }

    public void WriteDecimal(decimal value)
    {
        var bits = decimal.GetBits(value);
        foreach (var bit in bits)
        {
            var bytes = BitConverter.GetBytes(bit);
            WriteBytes(bytes);
        }
    }

    public byte[] ToArray() => _stream.ToArray();

    private static uint ZigZag(int value) => (uint)((value << 1) ^ (value >> 31));

    private static ulong ZigZag(long value) => (ulong)((value << 1) ^ (value >> 63));
}
