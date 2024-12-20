namespace LlamaLogic.Packages;

[SuppressMessage("Style", "IDE1006: Naming Styles", Justification = "Original Maxis format naming.")]
enum CompressionTypeMethodNumber :
    ushort
{
    Uncompressed =              0x0000,
    Streamable_compression =    0xfffe,
    Internal_compression =      0xffff,
    Deleted_record =            0xffe0,
    ZLIB =                      0x5a42
}
