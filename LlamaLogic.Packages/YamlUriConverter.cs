using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace LlamaLogic.Packages;

sealed class YamlUriConverter :
    IYamlTypeConverter
{
    public bool Accepts(Type type) =>
        type == typeof(Uri);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        try
        {
            return parser.Current is Scalar scalar
                && scalar.Value is string uriString
                && !uriString.Equals("null", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(uriString, UriKind.Absolute, out var uri)
                ? uri
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
        if (value is Uri uri)
        {
            emitter.Emit(new Scalar(uri.AbsoluteUri.ToString()));
            return;
        }
        throw new NotSupportedException($"{value} ({value.GetType().FullName}) is not supported");
    }
}
