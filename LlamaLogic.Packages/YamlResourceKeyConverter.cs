using YamlDotNet.Core.Events;

namespace LlamaLogic.Packages;

sealed class YamlResourceKeyConverter :
    IYamlTypeConverter
{
    public bool Accepts(Type type) =>
        type == typeof(ResourceKey);

    public object? ReadYaml(YamlDotNet.Core.IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        try
        {
            return parser.Current is Scalar scalar
                && scalar.Value is string versionString
                && !versionString.Equals("null", StringComparison.OrdinalIgnoreCase)
                && ResourceKey.TryParse(versionString, out var resourceKey)
                ? resourceKey
                : (object?)null;
        }
        finally
        {
            parser.MoveNext();
        }
    }

    public void WriteYaml(YamlDotNet.Core.IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is null)
        {
            emitter.Emit(new Scalar("null"));
            return;
        }
        if (value is ResourceKey resourceKey)
        {
            emitter.Emit(new Scalar(resourceKey.ToString()));
            return;
        }
        throw new NotSupportedException($"{value} ({value.GetType().FullName}) is not supported");
    }
}
