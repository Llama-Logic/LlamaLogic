namespace LlamaLogic.Packages;

/// <summary>
/// Provides utility methods specific to The Sims 4
/// </summary>
public static class SmartSimUtilities
{
    static readonly Dictionary<LocaleFullInstancePrefix, CultureInfo> localeMap = new()
    {
        { LocaleFullInstancePrefix.en_US, new CultureInfo("en-US") },
        { LocaleFullInstancePrefix.zh_CN, new CultureInfo("zh-CN") },
        { LocaleFullInstancePrefix.zh_TW, new CultureInfo("zh-TW") },
        { LocaleFullInstancePrefix.cs, new CultureInfo("cs") },
        { LocaleFullInstancePrefix.da, new CultureInfo("da") },
        { LocaleFullInstancePrefix.nl, new CultureInfo("nl") },
        { LocaleFullInstancePrefix.fi, new CultureInfo("fi") },
        { LocaleFullInstancePrefix.fr_FR, new CultureInfo("fr-FR") },
        { LocaleFullInstancePrefix.de, new CultureInfo("de") },
        { LocaleFullInstancePrefix.it, new CultureInfo("it") },
        { LocaleFullInstancePrefix.ja, new CultureInfo("ja") },
        { LocaleFullInstancePrefix.ko, new CultureInfo("ko") },
        { LocaleFullInstancePrefix.nb, new CultureInfo("nb") },
        { LocaleFullInstancePrefix.pl, new CultureInfo("pl") },
        { LocaleFullInstancePrefix.pt_BR, new CultureInfo("pt-BR") },
        { LocaleFullInstancePrefix.ru, new CultureInfo("ru") },
        { LocaleFullInstancePrefix.es_ES, new CultureInfo("es-ES") },
        { LocaleFullInstancePrefix.sv, new CultureInfo("sv") }
    };

    /// <summary>
    /// Gets the locale of a string table resource in The Sims 4 based on its <paramref name="key"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The high order byte of the instance does not correspond to a locale supported by The Sims 4</exception>
    public static CultureInfo GetStringTableLocale(this ResourceKey key)
    {
        if (key.Type is not ResourceType.StringTable)
            throw new ArgumentOutOfRangeException(nameof(key), $"Resource key must be of type {nameof(ResourceType.StringTable)} (0x{ResourceType.StringTable:x8}) but is of type 0x{key.Type:x8}");
        var prefix = (LocaleFullInstancePrefix)((key.FullInstance & 0xFF00000000000000) >> 56);
        if (!localeMap.TryGetValue(prefix, out var locale))
            throw new ArgumentOutOfRangeException(nameof(key), $"Full instance local prefix 0x{prefix:x2} is unknown");
        return locale;
    }

    /// <summary>
    /// Gets the <see cref="ResourceKey"/> of a string table resource in The Sims 4 based on its <paramref name="locale"/>, <paramref name="group"/>, and <paramref name="fullInstance"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The locale is not supported by The Sims 4</exception>
    public static ResourceKey GetStringTableResourceKey(this CultureInfo locale, uint group, ulong fullInstance)
    {
        ArgumentNullException.ThrowIfNull(locale);
        if (!Enum.TryParse<LocaleFullInstancePrefix>(locale.Name.Replace("-", "_", StringComparison.Ordinal), out var prefix)
            || !localeMap.ContainsKey(prefix))
            throw new NotSupportedException($"Unknown or unsupported locale: {locale.Name}");
        return new ResourceKey(ResourceType.StringTable, group, ((ulong)prefix << 56) | (fullInstance & 0x00ffffffffffffff));
    }

    /// <summary>
    /// Gets the <see cref="ResourceKey"/> for this string table resource with the specified <paramref name="locale"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The locale is not supported by The Sims 4</exception>
    public static ResourceKey GetResourceKeyForLocale(this ResourceKey key, CultureInfo locale) =>
        key.Type switch
        {
            ResourceType.StringTable => GetStringTableResourceKey(locale, key.Group, key.FullInstance),
            _ => throw new ArgumentOutOfRangeException(nameof(key), "Resource key must be of type StringTable")
        };
}
