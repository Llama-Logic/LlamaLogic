namespace LlamaLogic.Packages.Formats;

static class ImageDecoding
{
    public static DecoderOptions Options { get; } = new()
    {
        Configuration = new(new PngConfigurationModule(), new JpegConfigurationModule()),
        SkipMetadata = true
    };
}
