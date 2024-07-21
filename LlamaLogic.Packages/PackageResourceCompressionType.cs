namespace LlamaLogic.Packages;

[Flags]
enum PackageResourceCompressionType :
    ushort
{
    None = 0,
    Legacy = 0xffff,
    SmartSim = 0x5a42
}
