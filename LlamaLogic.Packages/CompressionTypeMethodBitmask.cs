namespace LlamaLogic.Packages;

[Flags]
[SuppressMessage("Style", "IDE1006: Naming Styles", Justification = "Original Maxis format naming.")]
enum CompressionTypeMethodBitmask :
    uint
{
    mnSizeMask =                0b_01111111_11111111_11111111_11111111,
    mbExtendedCompressionType = 0b_10000000_00000000_00000000_00000000
}
