using ICSharpCode.SharpZipLib.Zip;

namespace LlamaLogic.Packages;

class StreamStaticDataSource(Stream stream) :
    IStaticDataSource
{
    public Stream GetSource() =>
        stream;
}
