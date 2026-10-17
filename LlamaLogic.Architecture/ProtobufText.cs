namespace LlamaLogic.Architecture;

static class ProtobufText
{
    static readonly ConcurrentDictionary<Type, Member[]> membersByType = new();

    sealed record Member(int Tag, string Name, PropertyInfo Property, MethodInfo? ShouldSerialize, Type? ElementType);

    enum TokenKind
    {
        End,
        Identifier,
        Number,
        String,
        Symbol
    }

    readonly record struct Token(TokenKind Kind, string? Text, byte[]? Bytes, int Line)
    {
        public InvalidDataException Invalid(string expected) =>
            new($"Line {Line} of the text has {(Kind is TokenKind.End ? "no more tokens" : $"'{Text}'")} where {expected} was expected");
    }

    ref struct Tokenizer
    {
        public Tokenizer(string text) =>
            this.text = text;

        int line = 1;
        int position;
        readonly string text;

        public Token Next()
        {
            while (position < text.Length)
            {
                var c = text[position];
                if (c == '\n')
                {
                    ++line;
                    ++position;
                }
                else if (char.IsWhiteSpace(c))
                    ++position;
                else if (c == '#')
                    while (position < text.Length && text[position] != '\n')
                        ++position;
                else
                    break;
            }
            if (position >= text.Length)
                return new(TokenKind.End, null, null, line);
            var start = position;
            var first = text[position];
            if (first is '{' or '}' or '<' or '>' or ':' or ',' or ';' or '[' or ']')
            {
                ++position;
                return new(TokenKind.Symbol, first.ToString(), null, line);
            }
            if (first is '"' or '\'')
                return ReadString(first);
            while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] is '_' or '.' or '-' or '+'))
                ++position;
            if (position == start)
                throw new InvalidDataException($"Line {line} of the text has an unexpected character '{first}'");
            var word = text[start..position];
            return new(char.IsLetter(word[0]) || word[0] == '_' ? TokenKind.Identifier : TokenKind.Number, word, null, line);
        }

        Token ReadString(char quote)
        {
            var startLine = line;
            ++position;
            var bytes = new List<byte>();
            while (true)
            {
                if (position >= text.Length || text[position] == '\n')
                    throw new InvalidDataException($"Line {startLine} of the text has an unterminated string");
                var c = text[position++];
                if (c == quote)
                    break;
                if (c != '\\')
                {
                    if (char.IsHighSurrogate(c) && position < text.Length && char.IsLowSurrogate(text[position]))
                        bytes.AddRange(Encoding.UTF8.GetBytes([c, text[position++]]));
                    else
                        bytes.AddRange(Encoding.UTF8.GetBytes(c.ToString()));
                    continue;
                }
                if (position >= text.Length)
                    throw new InvalidDataException($"Line {startLine} of the text has an unterminated string");
                var escape = text[position++];
                switch (escape)
                {
                    case 'n':
                        bytes.Add((byte)'\n');
                        break;
                    case 'r':
                        bytes.Add((byte)'\r');
                        break;
                    case 't':
                        bytes.Add((byte)'\t');
                        break;
                    case 'a':
                        bytes.Add(7);
                        break;
                    case 'b':
                        bytes.Add(8);
                        break;
                    case 'f':
                        bytes.Add(12);
                        break;
                    case 'v':
                        bytes.Add(11);
                        break;
                    case 'x':
                    {
                        var value = 0;
                        var count = 0;
                        while (count < 2 && position < text.Length && Uri.IsHexDigit(text[position]))
                        {
                            value = value * 16 + Convert.ToInt32(text[position].ToString(), 16);
                            ++position;
                            ++count;
                        }
                        if (count == 0)
                            throw new InvalidDataException($"Line {startLine} of the text has an invalid \\x escape");
                        bytes.Add((byte)value);
                        break;
                    }
                    case >= '0' and <= '7':
                    {
                        var value = escape - '0';
                        var count = 1;
                        while (count < 3 && position < text.Length && text[position] is >= '0' and <= '7')
                        {
                            value = value * 8 + (text[position] - '0');
                            ++position;
                            ++count;
                        }
                        if (value > 255)
                            throw new InvalidDataException($"Line {startLine} of the text has an octal escape beyond 255");
                        bytes.Add((byte)value);
                        break;
                    }
                    default:
                        bytes.AddRange(Encoding.UTF8.GetBytes(escape.ToString()));
                        break;
                }
            }
            return new(TokenKind.String, quote + "…" + quote, [.. bytes], startLine);
        }
    }

    static void AddValue(ref Tokenizer tokenizer, object message, Member member, Type valueType, Token token)
    {
        object value;
        if (IsMessage(valueType))
        {
            if (token.Kind is not TokenKind.Symbol || token.Text is not "{" and not "<")
                throw token.Invalid("'{'");
            value = Activator.CreateInstance(valueType)!;
            ParseMessage(ref tokenizer, value, token.Text is "{" ? '}' : '>');
        }
        else
            value = ConvertScalar(valueType, token);
        if (member.ElementType is not null)
            ((System.Collections.IList)member.Property.GetValue(message)!).Add(value);
        else
            member.Property.SetValue(message, value);
    }

    static int Compare(BigInteger numerator, BigInteger denominator, int decimalExponent) =>
        decimalExponent >= 0
            ? numerator.CompareTo(denominator * BigInteger.Pow(10, decimalExponent))
            : (numerator * BigInteger.Pow(10, -decimalExponent)).CompareTo(denominator);

    static object ConvertScalar(Type type, Token token)
    {
        if (type == typeof(string))
            return token.Kind is TokenKind.String ? Encoding.UTF8.GetString(token.Bytes!) : throw token.Invalid("a string");
        if (type == typeof(byte[]))
            return token.Kind is TokenKind.String ? token.Bytes! : throw token.Invalid("a string");
        if (token.Kind is not TokenKind.Identifier and not TokenKind.Number)
            throw token.Invalid("a scalar");
        var text = token.Text!;
        if (type == typeof(bool))
            return text switch
            {
                "true" or "True" or "t" or "1" => true,
                "false" or "False" or "f" or "0" => false,
                _ => throw token.Invalid("a Boolean")
            };
        if (type.IsEnum)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                if ((field.GetCustomAttribute<ProtoBuf.ProtoEnumAttribute>()?.Name ?? field.Name) == text)
                    return field.GetValue(null)!;
            if (TryParseInteger(text, out var negative, out var magnitude))
                return Enum.ToObject(type, negative ? -(long)magnitude : (long)magnitude);
            throw token.Invalid($"a value of {type.Name}");
        }
        if (type == typeof(float) || type == typeof(double))
        {
            var upper = text.ToUpperInvariant();
            var isHexadecimal = upper.StartsWith("0X", StringComparison.Ordinal) || upper.StartsWith("-0X", StringComparison.Ordinal);
            var number = !isHexadecimal && upper is not ("INF" or "+INF" or "-INF") && upper.EndsWith('F') ? text[..^1] : text;
            double value;
            upper = number.ToUpperInvariant();
            if (upper is "INF" or "INFINITY" or "+INF" or "+INFINITY")
                value = double.PositiveInfinity;
            else if (upper is "-INF" or "-INFINITY")
                value = double.NegativeInfinity;
            else if (upper is "NAN" or "-NAN")
                value = double.NaN;
            else if (isHexadecimal && TryParseInteger(number, out var negative, out var magnitude))
                value = negative ? -(double)magnitude : magnitude;
            else if (type == typeof(float))
                return float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var single) ? single : throw token.Invalid("a number");
            else if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw token.Invalid("a number");
            return type == typeof(float) ? (object)(float)value : value;
        }
        if (!TryParseInteger(text, out var isNegative, out var integer))
            throw token.Invalid("an integer");
        try
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Int32 => isNegative ? checked((int)-(long)integer) : checked((int)integer),
                TypeCode.Int64 => isNegative ? (integer == 0x8000000000000000 ? long.MinValue : checked(-(long)integer)) : checked((long)integer),
                TypeCode.UInt32 => isNegative ? throw token.Invalid("an unsigned integer") : checked((uint)integer),
                TypeCode.UInt64 => isNegative ? throw token.Invalid("an unsigned integer") : integer,
                _ => throw token.Invalid($"a value of type {type.Name}, which is not supported")
            };
        }
        catch (OverflowException)
        {
            throw token.Invalid($"a value that fits in {type.Name}");
        }
    }

    public static string Format(object message)
    {
        var builder = new StringBuilder();
        FormatMessage(builder, message, 0);
        return builder.ToString();
    }

    static void FormatBytes(StringBuilder builder, byte[] bytes)
    {
        builder.Append('"');
        foreach (var b in bytes)
            _ = b switch
            {
                (byte)'\n' => builder.Append("\\n"),
                (byte)'\r' => builder.Append("\\r"),
                (byte)'\t' => builder.Append("\\t"),
                (byte)'"' or (byte)'\'' or (byte)'\\' => builder.Append('\\').Append((char)b),
                >= 32 and < 127 => builder.Append((char)b),
                _ => builder.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'))
            };
        builder.Append('"');
    }

    static string FormatDouble(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    static void FormatMessage(StringBuilder builder, object message, int depth)
    {
        foreach (var member in GetMembers(message.GetType()))
        {
            var value = member.Property.GetValue(message);
            if (value is null)
                continue;
            if (member.ElementType is not null)
            {
                foreach (var element in (System.Collections.IEnumerable)value)
                    FormatValue(builder, member.Name, member.ElementType, element, depth);
                continue;
            }
            if (member.ShouldSerialize is not null && !(bool)member.ShouldSerialize.Invoke(message, null)!)
                continue;
            FormatValue(builder, member.Name, member.Property.PropertyType, value, depth);
        }
    }

    static string FormatSignificant(float value, int precision)
    {
        var bits = BitConverter.DoubleToInt64Bits(value);
        var negative = bits < 0;
        var exponent = (int)((bits >> 52) & 0x7ff);
        var mantissa = bits & 0xfffffffffffffL;
        if (exponent == 0)
            exponent = 1;
        else
            mantissa |= 1L << 52;
        exponent -= 1075;
        var numerator = new BigInteger(mantissa);
        var denominator = BigInteger.One;
        if (exponent >= 0)
            numerator <<= exponent;
        else
            denominator <<= -exponent;
        var decimalExponent = (int)Math.Floor(BigInteger.Log10(numerator) - BigInteger.Log10(denominator));
        if (Compare(numerator, denominator, decimalExponent) < 0)
            --decimalExponent;
        else if (Compare(numerator, denominator, decimalExponent + 1) >= 0)
            ++decimalExponent;
        var scale = decimalExponent - precision + 1;
        var scaledNumerator = scale < 0 ? numerator * BigInteger.Pow(10, -scale) : numerator;
        var scaledDenominator = scale > 0 ? denominator * BigInteger.Pow(10, scale) : denominator;
        var quotient = BigInteger.DivRem(scaledNumerator, scaledDenominator, out var remainder);
        if (remainder * 2 >= scaledDenominator)
            ++quotient;
        if (quotient >= BigInteger.Pow(10, precision))
        {
            quotient /= 10;
            ++decimalExponent;
        }
        var digits = quotient.ToString(CultureInfo.InvariantCulture).TrimEnd('0');
        if (digits.Length == 0)
            digits = "0";
        var sign = negative ? "-" : string.Empty;
        if (decimalExponent >= precision || decimalExponent < -4)
            return $"{sign}{digits[0]}{(digits.Length > 1 ? "." + digits[1..] : string.Empty)}E{(decimalExponent < 0 ? '-' : '+')}{Math.Abs(decimalExponent):00}";
        if (decimalExponent >= 0)
        {
            var integerPart = digits.Length > decimalExponent + 1 ? digits[..(decimalExponent + 1)] : digits.PadRight(decimalExponent + 1, '0');
            var fractionPart = digits.Length > decimalExponent + 1 ? digits[(decimalExponent + 1)..] : string.Empty;
            return $"{sign}{integerPart}{(fractionPart.Length > 0 ? "." + fractionPart : string.Empty)}";
        }
        return $"{sign}0.{new string('0', -decimalExponent - 1)}{digits}";
    }

    static string FormatSingle(float value)
    {
        if (value == 0)
            return "0";
        if (float.IsNaN(value))
            return "NaN";
        if (float.IsInfinity(value))
            return value > 0 ? "Infinity" : "-Infinity";
        var text = FormatSignificant(value, 7);
        if (float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture) == value)
            return text;
        return FormatSignificant(value, 9);
    }

    static void FormatValue(StringBuilder builder, string name, Type type, object value, int depth)
    {
        builder.Append(' ', depth * 2).Append(name);
        if (IsMessage(type))
        {
            builder.Append(" {\r\n");
            FormatMessage(builder, value, depth + 1);
            builder.Append(' ', depth * 2).Append("}\r\n");
            return;
        }
        builder.Append(": ");
        switch (value)
        {
            case bool boolean:
                builder.Append(boolean ? "true" : "false");
                break;
            case float single:
                builder.Append(FormatSingle(single));
                break;
            case double @double:
                builder.Append(FormatDouble(@double));
                break;
            case string text:
                FormatBytes(builder, Encoding.UTF8.GetBytes(text));
                break;
            case byte[] bytes:
                FormatBytes(builder, bytes);
                break;
            case Enum enumeration:
                var field = type.GetField(enumeration.ToString());
                builder.Append(field?.GetCustomAttribute<ProtoBuf.ProtoEnumAttribute>()?.Name ?? enumeration.ToString());
                break;
            case IFormattable formattable:
                builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                break;
            default:
                throw new InvalidOperationException($"A value of type {type.Name} cannot be written as text");
        }
        builder.Append("\r\n");
    }

    static Member[] GetMembers(Type type) =>
        membersByType.GetOrAdd(type, static type =>
        [
            .. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => (property, attribute: property.GetCustomAttribute<ProtoBuf.ProtoMemberAttribute>()))
                .Where(pair => pair.attribute is not null)
                .Select(pair => new Member
                (
                    pair.attribute!.Tag,
                    pair.attribute.Name ?? pair.property.Name,
                    pair.property,
                    type.GetMethod($"ShouldSerialize{pair.property.Name}", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes),
                    pair.property.PropertyType.IsGenericType && pair.property.PropertyType.GetGenericTypeDefinition() == typeof(List<>) ? pair.property.PropertyType.GetGenericArguments()[0] : null
                ))
                .OrderBy(member => member.Tag)
        ]);

    static bool IsMessage(Type type) =>
        type.GetCustomAttribute<ProtoBuf.ProtoContractAttribute>() is not null;

    public static T Parse<T>(string text)
        where T : new()
    {
        var tokenizer = new Tokenizer(text);
        var message = new T();
        ParseMessage(ref tokenizer, message, null);
        return message;
    }

    static void ParseMessage(ref Tokenizer tokenizer, object message, char? closing)
    {
        var members = GetMembers(message.GetType());
        while (true)
        {
            var token = tokenizer.Next();
            if (token.Kind is TokenKind.End)
            {
                if (closing is not null)
                    throw token.Invalid($"'{closing}'");
                return;
            }
            if (token.Kind is TokenKind.Symbol && closing is not null && token.Text![0] == closing)
                return;
            if (token.Kind is TokenKind.Symbol && token.Text is "," or ";")
                continue;
            if (token.Kind is not TokenKind.Identifier)
                throw token.Invalid("a field name");
            var member = Array.Find(members, candidate => candidate.Name == token.Text) ?? throw token.Invalid($"a field of {message.GetType().Name}");
            var valueType = member.ElementType ?? member.Property.PropertyType;
            var next = tokenizer.Next();
            if (next.Kind is TokenKind.Symbol && next.Text is ":")
                next = tokenizer.Next();
            if (next.Kind is TokenKind.Symbol && next.Text is "[" && member.ElementType is not null)
            {
                while (true)
                {
                    var element = tokenizer.Next();
                    if (element.Kind is TokenKind.Symbol && element.Text is "]")
                        break;
                    if (element.Kind is TokenKind.Symbol && element.Text is ",")
                        continue;
                    AddValue(ref tokenizer, message, member, valueType, element);
                }
                continue;
            }
            AddValue(ref tokenizer, message, member, valueType, next);
        }
    }

    static bool TryParseInteger(string text, out bool negative, out ulong magnitude)
    {
        negative = text.StartsWith('-');
        var digits = negative || text.StartsWith('+') ? text[1..] : text;
        if (digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ulong.TryParse(digits[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out magnitude);
        if (digits.Length > 1 && digits[0] == '0' && digits.All(c => c is >= '0' and <= '7'))
        {
            magnitude = 0;
            foreach (var c in digits)
            {
                if (magnitude > ulong.MaxValue / 8)
                    return false;
                magnitude = magnitude * 8 + (ulong)(c - '0');
            }
            return true;
        }
        return ulong.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out magnitude);
    }
}
