namespace LlamaLogic.TuningArchive;

readonly record struct ResourceContent(long BlobId, string Pack, string? Name, long? ClassId);

readonly record struct StringContent(string Text, string Pack);