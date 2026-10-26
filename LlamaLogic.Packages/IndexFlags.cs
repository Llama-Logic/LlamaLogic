namespace LlamaLogic.Packages;

[Flags]
enum IndexFlags :
    uint
{
    constantType =          0b_00000000_00000000_00000000_00000001,
    constantGroup =         0b_00000000_00000000_00000000_00000010,
    constantInstanceEx =    0b_00000000_00000000_00000000_00000100
}
