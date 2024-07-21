namespace LlamaLogic.Packages;

[Flags]
enum PackageIndexType :
    uint
{
    Default = 0,
    NoMoreThanOneType = 0x01,
    NoMoreThanOneGroup = 0x02,
    NoMoreThanOneHighOrderInstance = 0x04
}
