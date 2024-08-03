namespace LlamaLogic.Packages;

/// <summary>
/// A type of data within a data resource
/// </summary>
[SuppressMessage("Design", "CA1028: Enum Storage should be Int32", Justification = "Nope, this is the format which is used in data. Sorry, code analyzer.")]
public enum DataResourceType :
    ushort
{
    /// <summary>
    /// <see cref="bool"/>
    /// </summary>
    Boolean = 0,

    /// <summary>
    /// A single ASCII character
    /// </summary>
    Character = 1,

    /// <summary>
    /// A <see cref="sbyte"/>
    /// </summary>
    SignedByte = 2,

    /// <summary>
    /// A <see cref="byte"/>
    /// </summary>
    Byte = 3,

    /// <summary>
    /// A <see cref="short"/>
    /// </summary>
    ShortInteger = 4,

    /// <summary>
    /// A <see cref="ushort"/>
    /// </summary>
    UnsignedShortInteger = 5,

    /// <summary>
    /// An <see cref="int"/>
    /// </summary>
    Integer = 6,

    /// <summary>
    /// A <see cref="uint"/>
    /// </summary>
    UnsignedInteger = 7,

    /// <summary>
    /// A <see cref="long"/>
    /// </summary>
    LongInteger = 8,

    /// <summary>
    /// A <see cref="ulong"/>
    /// </summary>
    UnsignedLongInteger = 9,

    /// <summary>
    /// A <see cref="float"/>
    /// </summary>
    FloatingPoint = 10,

    /// <summary>
    /// A <see cref="string"/>
    /// </summary>
    String = 11,

    /// <summary>
    /// A <see cref="string"/> and accompanying hash
    /// </summary>
    HashedString = 12,

    /// <summary>
    /// An offset to an object
    /// </summary>
    Object = 13,

    /// <summary>
    /// An offset to an array of objects and its size
    /// </summary>
    Vector = 14,

    /// <summary>
    /// You might think it's a <see cref="double"/>, but nope: two <see cref="float"/>s
    /// </summary>
    FloatingPoint2 = 15,

    /// <summary>
    /// An obvious trend is developing, for this is three <see cref="float"/>s
    /// </summary>
    FloatingPoint3 = 16,

    /// <summary>
    /// (You know when, every once in a while, there's a great trilogy and someone messes it up?) Here are your four <see cref="float"/>s
    /// </summary>
    FloatingPoint4 = 17,

    /// <summary>
    /// It's not just about good manners from "How to Serve and Not Be Served", it's really about this <see cref="ulong"/>
    /// </summary>
    TableSetReference = 18,

    /// <summary>
    /// A <see cref="PackageResourceKey"/>
    /// </summary>
    ResourceKey = 19,

    /// <summary>
    /// A four-byte key used to uniquely identify localized strings in The Sims 4
    /// </summary>
    LocalizationKey = 20,

    /// <summary>
    /// ?
    /// </summary>
    Undefined = 21
}
