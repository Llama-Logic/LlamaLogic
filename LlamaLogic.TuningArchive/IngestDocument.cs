namespace LlamaLogic.TuningArchive;

sealed record IngestDocument(ResourceType Type, uint Group, ulong Instance, string? Name, string? Class, string? Module, string Pack, string? TypeName, byte[] Content, BlobHash Hash);