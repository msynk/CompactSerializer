using System.Text;

namespace SerializerDemo;

internal sealed class CompactReader
{
    private readonly byte[] _buffer;
    private int _position;

    public CompactReader(ReadOnlySpan<byte> buffer) => _buffer = buffer.ToArray();

    public byte ReadByte()
    {
        EnsureAvailable(1);
        return _buffer[_position++];
    }

    public byte[] ReadBytes(int length)
    {
        EnsureAvailable(length);
        var bytes = _buffer.AsSpan(_position, length).ToArray();
        _position += length;
        return bytes;
    }

    public byte[] ReadByteArray()
    {
        var length = checked((int)ReadUInt32());
        return ReadBytes(length);
    }

    public string ReadString()
    {
        var length = checked((int)ReadUInt32());
        var bytes = ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    public int ReadInt32() => UnZigZag(ReadUInt32());

    public long ReadInt64() => UnZigZag(ReadUInt64());

    public uint ReadUInt32()
    {
        uint result = 0;
        var shift = 0;
        while (true)
        {
            var b = ReadByte();
            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
            if (shift > 35)
            {
                throw new InvalidOperationException("Invalid varint32 encoding.");
            }
        }
    }

    public ulong ReadUInt64()
    {
        ulong result = 0;
        var shift = 0;
        while (true)
        {
            var b = ReadByte();
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
            if (shift > 70)
            {
                throw new InvalidOperationException("Invalid varint64 encoding.");
            }
        }
    }

    public float ReadSingle()
    {
        var bytes = ReadBytes(sizeof(float));
        return BitConverter.ToSingle(bytes);
    }

    public double ReadDouble()
    {
        var bytes = ReadBytes(sizeof(double));
        return BitConverter.ToDouble(bytes);
    }

    public decimal ReadDecimal()
    {
        var bits = new int[4];
        for (var i = 0; i < bits.Length; i++)
        {
            var bytes = ReadBytes(sizeof(int));
            bits[i] = BitConverter.ToInt32(bytes);
        }

        return new decimal(bits);
    }

    private static int UnZigZag(uint value) => (int)((value >> 1) ^ (uint)-(int)(value & 1));

    private static long UnZigZag(ulong value) => (long)((value >> 1) ^ (ulong)-(long)(value & 1));

    private void EnsureAvailable(int length)
    {
        if (_position + length > _buffer.Length)
        {
            throw new InvalidOperationException("Unexpected end of payload.");
        }
    }
}
