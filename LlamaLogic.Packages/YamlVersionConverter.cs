using YamlDotNet.Core.Events;

namespace LlamaLogic.Packages;

sealed class YamlVersionConverter :
    IYamlTypeConverter
{
    public bool Accepts(Type type) =>
        type == typeof(Version);

    public object? ReadYaml(YamlDotNet.Core.IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        try
        {
            return parser.Current is Scalar scalar
                && scalar.Value is string versionString
                && !versionString.Equals("null", StringComparison.OrdinalIgnoreCase)
                && Version.TryParse(versionString, out var version)
                ? version
                : null;
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
        if (value is Version version)
        {
            emitter.Emit(new Scalar(version.ToString()));
            return;
        }
        throw new NotSupportedException($"{value} ({value.GetType().FullName}) is not supported");
    }
}
