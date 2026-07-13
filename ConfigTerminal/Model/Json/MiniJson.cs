using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Magnetar.ConfigTerminal.Model.Json;

/// <summary>
/// A tiny, self-contained JSON reader — enough to parse the Steam Web API
/// responses the Workshop resolver consumes, with zero third-party dependencies.
/// Produces a lenient <see cref="JsonValue"/> tree; malformed input throws
/// <see cref="FormatException"/>.
/// </summary>
internal static class MiniJson
{
    public static JsonValue Parse(string text)
    {
        var p = new Parser(text ?? string.Empty);
        JsonValue v = p.ParseValue();
        p.SkipWhitespace();
        return v;
    }

    private sealed class Parser
    {
        private readonly string s;
        private int i;

        public Parser(string s) => this.s = s;

        public void SkipWhitespace()
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
        }

        public JsonValue ParseValue()
        {
            SkipWhitespace();
            if (i >= s.Length)
                throw new FormatException("Unexpected end of JSON.");
            char c = s[i];
            switch (c)
            {
                case '{': return ParseObject();
                case '[': return ParseArray();
                case '"': return JsonValue.Of(ParseString());
                case 't': Expect("true"); return JsonValue.Of(true);
                case 'f': Expect("false"); return JsonValue.Of(false);
                case 'n': Expect("null"); return JsonValue.Null();
                default: return ParseNumber();
            }
        }

        private JsonValue ParseObject()
        {
            var obj = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            i++; // {
            SkipWhitespace();
            if (i < s.Length && s[i] == '}') { i++; return JsonValue.Of(obj); }
            while (true)
            {
                SkipWhitespace();
                if (i >= s.Length || s[i] != '"')
                    throw new FormatException("Expected object key.");
                string key = ParseString();
                SkipWhitespace();
                if (i >= s.Length || s[i] != ':')
                    throw new FormatException("Expected ':' in object.");
                i++;
                obj[key] = ParseValue();
                SkipWhitespace();
                if (i >= s.Length)
                    throw new FormatException("Unterminated object.");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; break; }
                throw new FormatException("Expected ',' or '}' in object.");
            }
            return JsonValue.Of(obj);
        }

        private JsonValue ParseArray()
        {
            var arr = new List<JsonValue>();
            i++; // [
            SkipWhitespace();
            if (i < s.Length && s[i] == ']') { i++; return JsonValue.Of(arr); }
            while (true)
            {
                arr.Add(ParseValue());
                SkipWhitespace();
                if (i >= s.Length)
                    throw new FormatException("Unterminated array.");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; break; }
                throw new FormatException("Expected ',' or ']' in array.");
            }
            return JsonValue.Of(arr);
        }

        private string ParseString()
        {
            var sb = new StringBuilder();
            i++; // opening quote
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"')
                    return sb.ToString();
                if (c == '\\')
                {
                    if (i >= s.Length) break;
                    char e = s[i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 <= s.Length &&
                                ushort.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code))
                            {
                                sb.Append((char)code);
                                i += 4;
                            }
                            break;
                        default: sb.Append(e); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            throw new FormatException("Unterminated string.");
        }

        private JsonValue ParseNumber()
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0)
                i++;
            string token = s.Substring(start, i - start);
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                return JsonValue.Of(d);
            throw new FormatException("Invalid number: " + token);
        }

        private void Expect(string literal)
        {
            if (i + literal.Length > s.Length || s.Substring(i, literal.Length) != literal)
                throw new FormatException("Expected '" + literal + "'.");
            i += literal.Length;
        }
    }
}

/// <summary>A parsed JSON value with lenient, null-safe accessors.</summary>
internal sealed class JsonValue
{
    private enum Kind { Null, Bool, Number, String, Array, Object }

    private readonly Kind kind;
    private readonly bool boolValue;
    private readonly double numberValue;
    private readonly string stringValue;
    private readonly List<JsonValue> arrayValue;
    private readonly Dictionary<string, JsonValue> objectValue;

    private JsonValue(Kind k, bool b = false, double n = 0, string s = null,
        List<JsonValue> a = null, Dictionary<string, JsonValue> o = null)
    {
        kind = k; boolValue = b; numberValue = n; stringValue = s; arrayValue = a; objectValue = o;
    }

    public static JsonValue Null() => new(Kind.Null);
    public static JsonValue Of(bool b) => new(Kind.Bool, b: b);
    public static JsonValue Of(double n) => new(Kind.Number, n: n);
    public static JsonValue Of(string s) => new(Kind.String, s: s);
    public static JsonValue Of(List<JsonValue> a) => new(Kind.Array, a: a);
    public static JsonValue Of(Dictionary<string, JsonValue> o) => new(Kind.Object, o: o);

    public bool IsNull => kind == Kind.Null;
    public bool IsArray => kind == Kind.Array;

    /// <summary>Object member lookup; returns a null value when absent or not an object.</summary>
    public JsonValue this[string key] =>
        kind == Kind.Object && objectValue.TryGetValue(key, out JsonValue v) ? v : Null();

    /// <summary>Array items (empty when not an array).</summary>
    public IReadOnlyList<JsonValue> Items => arrayValue ?? EmptyList;

    private static readonly List<JsonValue> EmptyList = new();

    public string AsString()
    {
        switch (kind)
        {
            case Kind.String: return stringValue;
            case Kind.Number: return numberValue.ToString(CultureInfo.InvariantCulture);
            case Kind.Bool: return boolValue ? "true" : "false";
            default: return null;
        }
    }

    public long AsLong(long fallback = 0)
    {
        switch (kind)
        {
            case Kind.Number: return (long)numberValue;
            case Kind.String:
                return long.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : fallback;
            default: return fallback;
        }
    }

    public int AsInt(int fallback = 0) => (int)AsLong(fallback);
}
