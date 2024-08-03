using System;
using System.Collections.Generic;
using System.Text;

namespace LlamaLogic.Packages.Binary;

interface IStringOffsetContainer
{
    bool IsExpectingHash { get; }

    void Commit(in Memory<byte> memory);

    void SetStringOffset(ArrayBufferWriter<byte> writer, in Range structRange);

    void SetStringOffsetAndHash(ArrayBufferWriter<byte> writer, in Range structRange, uint hash);
}
