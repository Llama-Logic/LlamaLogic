namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.Weather"/> section: the lot's seasonal weather interpolation value (🔓)
/// </summary>
/// <remarks>
/// <see cref="Value"/> is the value of the game's <c>request_season_weather_interpolation</c> Python function for interpolation type 1; which weather quantity that is has not been established.
/// It is 0 in 2,087 of the 2,102 sections examined and up to about 0.86 in the rest.
/// Version 1 is the only one.
/// </remarks>
public sealed class WeatherSection :
    LotArchitectureSection
{
    const uint supportedVersion = 1;

    internal static WeatherSection Read(ref SectionReader reader, SectionContext context)
    {
        reader.ReadVersion(supportedVersion, supportedVersion);
        return new WeatherSection { Value = reader.ReadSingle() };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherSection"/> class
    /// </summary>
    public WeatherSection() :
        base(LotArchitectureSectionType.Weather)
    {
    }

    /// <summary>
    /// Gets/sets the interpolation value
    /// </summary>
    public float Value { get; set; }

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        writer.Write(supportedVersion);
        writer.Write(Value);
    }
}
