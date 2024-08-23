using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace LlamaLogic.Packages;

sealed class YamlHashHexConverter :
    IYamlTypeConverter
{
    public bool Accepts(Type type) =>
        type == typeof(byte[]);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        try
        {
            return parser.Current is Scalar scalar
                && scalar.Value is string hexString
                && !hexString.Equals("null", StringComparison.OrdinalIgnoreCase)
                ? hexString.ToByteArray()
                : null;
        }
        finally
        {
            parser.MoveNext();
        }
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is null)
        {
            emitter.Emit(new Scalar("null"));
            return;
        }
        if (value is byte[] byteArray)
        {
            emitter.Emit(new Scalar(byteArray.ToHexString()));
            return;
        }
        throw new NotSupportedException($"{value} ({value.GetType().FullName}) is not supported");
    }
}
