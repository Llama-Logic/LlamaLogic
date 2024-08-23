namespace LlamaLogic.Packages.Models.Data;

/// <summary>
/// Known versions of the format for <see cref="ResourceType.SimData"/> and <see cref="ResourceType.CombinedTuning"/> resources
/// </summary>
[SuppressMessage("Design", "CA1008: Enums should have zero value", Justification = "We do not know what the proper name of such a value would be.")]
[SuppressMessage("Design", "CA1028: Enum Storage should be Int32", Justification = "This is determined by the binary format created by Maxis. So sorry to clearly agitate you so much.")]
public enum DataModelVersion :
    uint
{
    /// <summary>
    /// The edition of the format which shipped with the original release of The Sims 4 on September 2, 2014
    /// </summary>
    SmartSimOriginalEdition = 0x100,

    /// <summary>
    /// The edition of the format which shipped with The Sims 4 (v1.5.139) on February 17, 2015, introducing support for <see cref="DataModelType.VARIANT"/> and allowing the creation of <see cref="ResourceType.CombinedTuning"/> resources
    /// </summary>
    VariantSupport = 0x101
}
