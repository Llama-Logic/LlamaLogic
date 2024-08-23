namespace LlamaLogic.Packages.Models.Data;

/// <summary>
/// A type of data within a data resource
/// </summary>
public enum DataModelType
{
    /// <summary>
    /// A <see cref="bool"/> expressed with an entire <see cref="byte"/> (`0` is <see langword="false"/>, any other value is <see langword="true"/>)
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.None)]
    BOOL = 0,

    /// <summary>
    /// A single ASCII character
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.None)]
    CHAR8 = 1,

    /// <summary>
    /// An <see cref="sbyte"/>
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.None)]
    INT8 = 2,

    /// <summary>
    /// A <see cref="byte"/>
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.None)]
    UINT8 = 3,

    /// <summary>
    /// A <see cref="short"/>
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.TwoBytes)]
    INT16 = 4,

    /// <summary>
    /// A <see cref="ushort"/>
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.TwoBytes)]
    UINT16 = 5,

    /// <summary>
    /// An <see cref="int"/>
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.FourBytes)]
    INT32 = 6,

    /// <summary>
    /// A <see cref="uint"/>
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.FourBytes)]
    UINT32 = 7,

    /// <summary>
    /// A <see cref="long"/>
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.EightBytes)]
    INT64 = 8,

    /// <summary>
    /// A <see cref="ulong"/>
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.EightBytes)]
    UINT64 = 9,

    /// <summary>
    /// A <see cref="float"/>
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.FourBytes)]
    FLOAT = 10,

    /// <summary>
    /// An <see cref="int"/> offset to a null-terminated ASCII <see cref="string"/>
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.FourBytes)]
    STRING8 = 11,

    /// <summary>
    /// An <see cref="int"/> offset to a null-terminated ASCII <see cref="string"/> and its accompanying <see cref="Fnv32"/> hash
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.FourBytes)]
    HASHEDSTRING8 = 12,

    /// <summary>
    /// An <see cref="int"/> offset to an object
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.FourBytes)]
    OBJECT = 13,

    /// <summary>
    /// An <see cref="int"/> offset to an array of objects and a <see cref="uint"/> that is its size
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.FourBytes)]
    VECTOR = 14,

    /// <summary>
    /// You might think it's a <see cref="double"/>, but nope: two <see cref="float"/>s
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.FourBytes)]
    FLOAT2 = 15,

    /// <summary>
    /// An obvious trend is developing, for this is three <see cref="float"/>s
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.FourBytes)]
    FLOAT3 = 16,

    /// <summary>
    /// (You know when, every once in a while, there's a great trilogy and someone messes it up?) Here are your four <see cref="float"/>s
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.FourBytes)]
    FLOAT4 = 17,

    /// <summary>
    /// <para>It's not just about good manners from "How to Serve and Not Be Served", it's really about this <see cref="ulong"/></para>
    /// <para>
    /// Seriously, though, why have these when we already have <see cref="UINT64"/>, you ask?
    /// <blockquote>
    /// you’re right that table set refs are just uint64s, but it is important to distinguish them because they mean different things.
    /// int64s are just literal numbers, table set refs are tuning IDs.
    /// so, when the game loads these, it knows that uint64 is just a number, so leave it alone, whereas a table set ref is tuning, so it needs to look that tuning up and use it as the value there rather than the number itself
    /// </blockquote>
    /// — frankk, August 16, 2024 in Lot 51's Discord
    /// </para>
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.EightBytes)]
    TABLESETREFERENCE = 18,

    /// <summary>
    /// A <see cref="ResourceKey"/> laid out by <see cref="ResourceKey.FullInstance"/>, then <see cref="ResourceKey.Type"/>, then <see cref="ResourceKey.Group"/>
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.EightBytes)]
    RESOURCEKEY = 19,

    /// <summary>
    /// A four-<see cref="byte"/> key used to uniquely identify localized strings in The Sims 4 (typically the <see cref="Fnv32"/> hash of the string in its original language)
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.FourBytes)]
    LOCKEY = 20,

    /// <summary>
    /// An <see cref="int"/> offset to an object accompanied by a <see cref="uint"/> type hash
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.FourBytes)]
    VARIANT = 21,

    /// <summary>
    /// ?
    /// </summary>
    [DataModelTypeAlignment(DataModelAlignment.None)]
    UNDEFINED = 22
}
