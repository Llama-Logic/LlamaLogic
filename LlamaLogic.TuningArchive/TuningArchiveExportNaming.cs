namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies how <see cref="TuningArchiveFile.ExportAsync"/> names the files it writes, following the conventions of Scumbumbo's XML Extractor
/// </summary>
public enum TuningArchiveExportNaming
{
    /// <summary>
    /// The type, group and instance, for importing into s4pe (such as <c>S4_E882D22F_00000000_000000000001A2BC.xml</c>)
    /// </summary>
    TypeGroupInstance = 0,

    /// <summary>
    /// The type, group and instance followed by the name, which s4pe turns into a <c>_KEY</c> name on import (such as <c>S4_B61DE6B4_00000001_000000000001C8C5_object_drink_Cider.xml</c>)
    /// </summary>
    TypeGroupInstanceName = 1,

    /// <summary>
    /// The name alone (such as <c>object_drink_Cider.xml</c>)
    /// </summary>
    Name = 2,

    /// <summary>
    /// The game's own naming, without an extension (such as <c>0x00000000!0x00000000000029c3.0x2451c101</c>)
    /// </summary>
    Native = 3,

    /// <summary>
    /// The naming Sims 4 Studio uses (such as <c>b61de6b4!00000001!000000000001c8c5.object_drink_Cider.object.xml</c>)
    /// </summary>
    Sims4Studio = 4
}