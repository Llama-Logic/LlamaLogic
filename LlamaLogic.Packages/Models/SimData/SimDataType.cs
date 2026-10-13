namespace LlamaLogic.Packages.Models.SimData;

/// <summary>
/// A type of data within a <see cref="ResourceType.SimData"/> resource
/// </summary>
public enum SimDataType
{
    /// <summary>
    /// A <see cref="bool"/> expressed with an entire <see cref="byte"/> (`0` is <see langword="false"/>, any other value is <see langword="true"/>)
    /// </summary>
    BOOL = 0,

    /// <summary>
    /// A single byte of a UTF-8 string
    /// </summary>
    CHAR8 = 1,

    /// <summary>
    /// An <see cref="sbyte"/>
    /// </summary>
    INT8 = 2,

    /// <summary>
    /// A <see cref="byte"/>
    /// </summary>
    UINT8 = 3,

    /// <summary>
    /// A <see cref="short"/>
    /// </summary>
    INT16 = 4,

    /// <summary>
    /// A <see cref="ushort"/>
    /// </summary>
    UINT16 = 5,

    /// <summary>
    /// An <see cref="int"/>
    /// </summary>
    INT32 = 6,

    /// <summary>
    /// A <see cref="uint"/>
    /// </summary>
    UINT32 = 7,

    /// <summary>
    /// A <see cref="long"/>
    /// </summary>
    INT64 = 8,

    /// <summary>
    /// A <see cref="ulong"/>
    /// </summary>
    UINT64 = 9,

    /// <summary>
    /// A <see cref="float"/>
    /// </summary>
    FLOAT = 10,

    /// <summary>
    /// An <see cref="int"/> offset to a null-terminated UTF-8 <see cref="string"/>
    /// </summary>
    STRING8 = 11,

    /// <summary>
    /// An <see cref="int"/> offset to a null-terminated UTF-8 <see cref="string"/> and its accompanying <see cref="Fnv32"/> hash
    /// </summary>
    HASHEDSTRING8 = 12,

    /// <summary>
    /// An <see cref="int"/> offset to an object
    /// </summary>
    OBJECT = 13,

    /// <summary>
    /// An <see cref="int"/> offset to an array of objects and a <see cref="uint"/> that is its size
    /// </summary>
    VECTOR = 14,

    /// <summary>
    /// You might think it's a <see cref="double"/>, but nope: two <see cref="float"/>s
    /// </summary>
    FLOAT2 = 15,

    /// <summary>
    /// An obvious trend is developing, for this is three <see cref="float"/>s
    /// </summary>
    FLOAT3 = 16,

    /// <summary>
    /// (You know when, every once in a while, there's a great trilogy and someone messes it up?) Here are your four <see cref="float"/>s
    /// </summary>
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
    TABLESETREFERENCE = 18,

    /// <summary>
    /// A <see cref="ResourceKey"/> laid out by <see cref="ResourceKey.FullInstance"/>, then <see cref="ResourceKey.Type"/>, then <see cref="ResourceKey.Group"/>
    /// </summary>
    RESOURCEKEY = 19,

    /// <summary>
    /// A four-<see cref="byte"/> key used to uniquely identify localized strings in The Sims 4 (typically the <see cref="Fnv32"/> hash of the string in its original language)
    /// </summary>
    LOCKEY = 20,

    /// <summary>
    /// An <see cref="int"/> offset to an object accompanied by a <see cref="uint"/> type hash
    /// </summary>
    VARIANT = 21
}
