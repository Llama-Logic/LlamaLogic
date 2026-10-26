namespace LlamaLogic.Packages;

[AttributeUsage(AttributeTargets.Field)]
sealed class StandardCompressionAttribute :
    Attribute
{
    public StandardCompressionAttribute(CompressionTypeMethodNumber methodNumber) =>
        MethodNumber = methodNumber;

    public CompressionTypeMethodNumber MethodNumber { get; }
}
