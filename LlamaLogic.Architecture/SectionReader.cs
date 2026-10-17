namespace LlamaLogic.Architecture;

ref struct SectionReader
{
    public SectionReader(ReadOnlySpan<byte> span, LotArchitectureSectionType type)
    {
        this.span = span;
        this.type = type;
    }

    int position;
    readonly ReadOnlySpan<byte> span;
    readonly LotArchitectureSectionType type;

    public readonly int Position =>
        position;

    public readonly int Remaining =>
        span.Length - position;

    public readonly LotArchitectureSectionType Type =>
        type;

    public void ExpectEnd()
    {
        if (position != span.Length)
            throw new InvalidDataException($"{span.Length - position} bytes follow the end of the {type} section at offset {position}");
    }

    public readonly InvalidDataException Invalid(string message) =>
        new($"The {type} section is invalid at offset {position}: {message}");

    public bool ReadBoolean()
    {
        var value = ReadByte();
        if (value > 1)
            throw Invalid($"a Boolean is {value}, expected 0 or 1");
        return value is 1;
    }

    public byte ReadByte() =>
        Take(1)[0];

    public byte[] ReadBytes(int count) =>
        Take(count).ToArray();

    public int ReadCount() =>
        ReadCount(ReadInt32());

    public int ReadCount(long count)
    {
        if (count < 0 || count > Remaining)
            throw Invalid($"a count of {count} cannot fit in the {Remaining} remaining bytes");
        return (int)count;
    }

    public short ReadInt16() =>
        BinaryPrimitives.ReadInt16LittleEndian(Take(2));

    public int ReadInt32() =>
        BinaryPrimitives.ReadInt32LittleEndian(Take(4));

    public long ReadInt64() =>
        BinaryPrimitives.ReadInt64LittleEndian(Take(8));

    public ResourceKey ReadKeyInstanceTypeGroup()
    {
        var instance = ReadUInt64();
        var type = ReadUInt32();
        var group = ReadUInt32();
        return new((ResourceType)type, group, instance);
    }

    public ResourceKey ReadKeyTypeGroupInstance()
    {
        var type = ReadUInt32();
        var group = ReadUInt32();
        var instance = ReadUInt64();
        return new((ResourceType)type, group, instance);
    }

    public sbyte ReadSByte() =>
        unchecked((sbyte)Take(1)[0]);

    public float ReadSingle() =>
        BinaryPrimitives.ReadSingleLittleEndian(Take(4));

    public ushort ReadUInt16() =>
        BinaryPrimitives.ReadUInt16LittleEndian(Take(2));

    public uint ReadUInt32() =>
        BinaryPrimitives.ReadUInt32LittleEndian(Take(4));

    public ulong ReadUInt64() =>
        BinaryPrimitives.ReadUInt64LittleEndian(Take(8));

    public string ReadUtf16String(int characterCount)
    {
        if (characterCount < 0 || characterCount > Remaining / 2)
            throw Invalid($"a string of {characterCount} UTF-16 code units cannot fit in the {Remaining} remaining bytes");
        return Encoding.Unicode.GetString(Take(characterCount * 2));
    }

    public uint ReadVersion(uint minimum, uint maximum)
    {
        var version = ReadUInt32();
        if (version < minimum || version > maximum)
            throw UnsupportedVersion($"version {version} is not supported, expected {minimum} through {maximum}");
        return version;
    }

    public readonly UnsupportedSectionVersionException UnsupportedVersion(string message) =>
        new($"The {type} section is not supported at offset {position}: {message}");

    ReadOnlySpan<byte> Take(int count)
    {
        if (count < 0 || count > span.Length - position)
            throw new InvalidDataException($"The {type} section ends at offset {span.Length}, before the {count} bytes needed at offset {position}");
        var result = span.Slice(position, count);
        position += count;
        return result;
    }
}
