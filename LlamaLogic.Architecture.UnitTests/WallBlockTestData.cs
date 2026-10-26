namespace LlamaLogic.Architecture.UnitTests;

static class WallBlockTestData
{
    public const uint FoundationType = 0x2FAE983E;
    public const uint WallPatternType = 0xD5F0F921;

    public static readonly ushort[] Versions = [17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36];

    public static TestBytes Block(this TestBytes bytes, ushort version, ushort id = 7, sbyte level = 1, int loopCount = 1, bool hasFloor = true, byte segmentKind = 0)
    {
        bytes.U16(version).U16(id).S8(level);
        if (version == 17)
            bytes.S8(level);
        if (version > 24)
            bytes.U8(3);
        if (version > 35)
            bytes.U16(300);
        bytes.U8(hasFloor ? (byte)1 : (byte)0);
        if (version > 27)
            bytes.U8(1);
        bytes.F32(3.25f);
        if (version > 33)
            bytes.F32(0.25f);
        if (version is 31 or 32)
            bytes.U8(0);
        if (version < 27)
        {
            bytes.U8(2);
            if (version < 22)
                bytes.U8(0);
            var keyCount = version < 21 ? 15 : 9;
            for (var i = 0; i < keyCount; ++i)
                bytes.KeyTypeGroupInstance(FoundationType, 0, (ulong)(0x100 + i));
            bytes.U8(1).U8(0);
        }
        if (version > 23)
            bytes.U16((ushort)loopCount);
        var slotCount = version switch
        {
            < 20 => 0,
            < 27 => 4,
            < 29 => 7,
            30 or 31 or 35 or 36 => 9,
            _ => 8
        };
        for (var loop = 0; loop < loopCount; ++loop)
        {
            bytes.U16(2).U8(4).U8(5).U8(8).U8(5);
            for (var segment = 0; segment < 2; ++segment)
            {
                bytes.U8(segmentKind);
                if (version < 23)
                    bytes.U8(9);
                for (var slot = 0; slot < slotCount; ++slot)
                    bytes.U32((uint)(slot is 0 or 4 ? 1 : 0));
                if (version > 25)
                    bytes.U32(11).U32(12);
            }
        }
        if (version < 19)
            bytes.U8(3);
        if (version > 19)
            bytes.KeyInstanceTypeGroup(0, 0, 0).U32(1).KeyInstanceTypeGroup(WallPatternType, 0, 0x1234).U32(1).U32(2);
        if (version is >= 12 and <= 25)
            bytes.U32(13);
        if (version < 27)
            bytes.U16(4).U8(1).U8(1);
        return bytes;
    }
}
