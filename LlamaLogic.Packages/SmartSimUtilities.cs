namespace LlamaLogic.Packages;

/// <summary>
/// Provides utility methods specific to The Sims 4
/// </summary>
public static class SmartSimUtilities
{
    const byte en_US = 0x00;
    const byte zh_TW = 0x02;
    const byte cs_CZ = 0x03;
    const byte da_DK = 0x04;
    const byte nl_NL = 0x05;
    const byte fi_FI = 0x06;
    const byte fr_FR = 0x07;
    const byte de_DE = 0x08;
    const byte it_IT = 0x0b;
    const byte ja_JP = 0x0c;
    const byte nb_NO = 0x0e;
    const byte pl_PL = 0x0f;
    const byte pt_BR = 0x11;
    const byte ru_RU = 0x12;
    const byte es_ES = 0x13;
    const byte sv_SE = 0x15;

    /// <summary>
    /// Gets the locale of a string table resource in The Sims 4 based on its <paramref name="key"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The high order byte of the instance does not correspond to a locale supported by The Sims 4</exception>
    public static CultureInfo GetStringTableLocale(this ResourceKey key)
    {
        if (key.Type is not ResourceType.StringTable)
            throw new ArgumentOutOfRangeException(nameof(key), $"Resource key must be of type {nameof(ResourceType.StringTable)}");
        return (byte)((key.FullInstance & 0xFF00000000000000) >> 56) switch
        {
            en_US => new CultureInfo("en-US"),
            zh_TW => new CultureInfo("zh-TW"),
            cs_CZ => new CultureInfo("cs-CZ"),
            da_DK => new CultureInfo("da-DK"),
            nl_NL => new CultureInfo("nl-NL"),
            fi_FI => new CultureInfo("fi-FI"),
            fr_FR => new CultureInfo("fr-FR"),
            de_DE => new CultureInfo("de-DE"),
            it_IT => new CultureInfo("it-IT"),
            ja_JP => new CultureInfo("ja-JP"),
            nb_NO => new CultureInfo("nb-NO"),
            pl_PL => new CultureInfo("pl-PL"),
            pt_BR => new CultureInfo("pt-BR"),
            ru_RU => new CultureInfo("ru-RU"),
            es_ES => new CultureInfo("es-ES"),
            sv_SE => new CultureInfo("sv-SE"),
            _ => throw new ArgumentOutOfRangeException(nameof(key), "Unknown locale")
        };
    }

    /// <summary>
    /// Gets the <see cref="ResourceKey"/> of a string table resource in The Sims 4 based on its <paramref name="locale"/>, <paramref name="group"/>, and <paramref name="fullInstance"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The locale is not supported by The Sims 4</exception>
    public static ResourceKey GetStringTableResourceKey(this CultureInfo locale, uint group, ulong fullInstance)
    {
        ArgumentNullException.ThrowIfNull(locale);
        return new ResourceKey(ResourceType.StringTable, group, ((ulong)(locale.Name switch
        {
            "en-US" => en_US,
            "zh-TW" => zh_TW,
            "cs-CZ" => cs_CZ,
            "da-DK" => da_DK,
            "nl-NL" => nl_NL,
            "fi-FI" => fi_FI,
            "fr-FR" => fr_FR,
            "de-DE" => de_DE,
            "it-IT" => it_IT,
            "ja-JP" => ja_JP,
            "nb-NO" => nb_NO,
            "pl-PL" => pl_PL,
            "pt-BR" => pt_BR,
            "ru-RU" => ru_RU,
            "es-ES" => es_ES,
            "sv-SE" => sv_SE,
            _ => throw new ArgumentOutOfRangeException(nameof(locale), "Unknown locale")
        }) << 56) | fullInstance);
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
