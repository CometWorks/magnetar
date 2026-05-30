using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using VRage;
using VRageMath;

namespace PluginSdk.Config
{
    /// <summary>
    /// Read/write helpers for the small set of VRage value types
    /// (<see cref="Color"/>, <see cref="Vector2D"/>, <see cref="Vector3D"/>,
    /// <see cref="Vector2I"/>, <see cref="Vector3I"/>,
    /// <see cref="Base6Directions.Direction"/>, <see cref="MyPositionAndOrientation"/>)
    /// that are first-class configuration values.
    ///
    /// <para>
    /// These types cannot be handed straight to <see cref="System.Xml.Serialization.XmlSerializer"/>:
    /// <see cref="Color"/> exposes aliased X/R, Y/G, Z/B properties on top of its
    /// <see cref="Color.PackedValue"/> field, and <see cref="MyPositionAndOrientation"/>
    /// has a derived <see cref="MyPositionAndOrientation.Orientation"/> property whose
    /// getter is undefined for a zero-initialized instance.
    /// </para>
    /// </summary>
    internal static class TypeSerialization
    {
        /// <summary>
        /// True if <paramref name="t"/> is one of the supported VRage value
        /// types — used by <see cref="PluginConfig"/> to bypass both the
        /// generic <c>XmlSerializer</c> path and the deep-struct equality
        /// path. These types all implement <c>IEquatable&lt;T&gt;</c>, so
        /// equality falls through to their own <c>Equals</c>.
        /// </summary>
        public static bool IsHandled(Type t)
        {
            return t == typeof(Color)
                || t == typeof(Vector2D) || t == typeof(Vector3D)
                || t == typeof(Vector2I) || t == typeof(Vector3I)
                || t == typeof(Base6Directions.Direction)
                || t == typeof(MyPositionAndOrientation);
        }

        // ---------- XML -------------------------------------------------

        public static void WriteXml(XmlWriter writer, string elementName, object value)
        {
            writer.WriteStartElement(elementName);
            WriteXmlBody(writer, value);
            writer.WriteEndElement();
        }

        private static void WriteXmlBody(XmlWriter writer, object value)
        {
            switch (value)
            {
                case Color c:
                    writer.WriteString(FormatColor(c));
                    break;
                case Vector2D v:
                    writer.WriteString(FormatDoubles(v.X, v.Y));
                    break;
                case Vector3D v:
                    writer.WriteString(FormatDoubles(v.X, v.Y, v.Z));
                    break;
                case Vector2I v:
                    writer.WriteString(FormatInts(v.X, v.Y));
                    break;
                case Vector3I v:
                    writer.WriteString(FormatInts(v.X, v.Y, v.Z));
                    break;
                case Base6Directions.Direction d:
                    writer.WriteString(d.ToString());
                    break;
                case MyPositionAndOrientation pose:
                    writer.WriteElementString("Position", FormatDoubles(pose.Position.X, pose.Position.Y, pose.Position.Z));
                    writer.WriteElementString("Forward",  FormatDoubles(pose.Forward.X,  pose.Forward.Y,  pose.Forward.Z));
                    writer.WriteElementString("Up",       FormatDoubles(pose.Up.X,       pose.Up.Y,       pose.Up.Z));
                    break;
                default:
                    throw new InvalidOperationException($"TypeSerialization.WriteXml: unsupported type {value?.GetType().FullName}");
            }
        }

        public static object ReadXml(XmlReader reader, Type type)
        {
            if (type == typeof(MyPositionAndOrientation))
                return ReadPoseXml(reader);

            // All other supported types are leaf elements with a single text value.
            bool isEmpty = reader.IsEmptyElement;
            reader.ReadStartElement();
            var text = isEmpty ? string.Empty : reader.ReadContentAsString();
            if (!isEmpty) reader.ReadEndElement();

            if (type == typeof(Color)) return ParseColor(text);
            if (type == typeof(Vector2D))
            {
                var d = ParseDoubles(text, 2);
                return new Vector2D(d[0], d[1]);
            }
            if (type == typeof(Vector3D))
            {
                var d = ParseDoubles(text, 3);
                return new Vector3D(d[0], d[1], d[2]);
            }
            if (type == typeof(Vector2I))
            {
                var i = ParseInts(text, 2);
                return new Vector2I(i[0], i[1]);
            }
            if (type == typeof(Vector3I))
            {
                var i = ParseInts(text, 3);
                return new Vector3I(i[0], i[1], i[2]);
            }
            if (type == typeof(Base6Directions.Direction))
                return (Base6Directions.Direction)Enum.Parse(typeof(Base6Directions.Direction), text.Trim(), ignoreCase: false);

            throw new InvalidOperationException($"TypeSerialization.ReadXml: unsupported type {type.FullName}");
        }

        private static MyPositionAndOrientation ReadPoseXml(XmlReader reader)
        {
            var pose = new MyPositionAndOrientation(Vector3.Zero, Vector3.Forward, Vector3.Up);

            bool isEmpty = reader.IsEmptyElement;
            reader.ReadStartElement();
            if (isEmpty) return pose;

            reader.MoveToContent();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    var name = reader.Name;
                    var text = reader.ReadElementContentAsString();
                    var d = ParseDoubles(text, 3);
                    switch (name)
                    {
                        case "Position": pose.Position = new Vector3D(d[0], d[1], d[2]); break;
                        case "Forward":  pose.Forward  = new Vector3((float)d[0], (float)d[1], (float)d[2]); break;
                        case "Up":       pose.Up       = new Vector3((float)d[0], (float)d[1], (float)d[2]); break;
                    }
                }
                else
                {
                    reader.Skip();
                }
                reader.MoveToContent();
            }
            reader.ReadEndElement();
            return pose;
        }

        // ---------- Formatting helpers ----------------------------------

        private static string FormatColor(Color c)
            => $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";

        private static Color ParseColor(string text)
        {
            text = text.Trim();
            if (text.StartsWith("#")) text = text.Substring(1);

            if (text.Length == 6)
                return new Color(
                    byte.Parse(text.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(text.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(text.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    (byte)255);

            if (text.Length == 8)
                return new Color(
                    byte.Parse(text.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(text.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(text.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(text.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));

            throw new FormatException($"Color must be #RRGGBB or #RRGGBBAA hex; got '{text}'.");
        }

        private static string FormatDoubles(params double[] values)
        {
            var parts = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                parts[i] = values[i].ToString("G17", CultureInfo.InvariantCulture);
            return string.Join(" ", parts);
        }

        private static string FormatInts(params int[] values)
        {
            var parts = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                parts[i] = values[i].ToString(CultureInfo.InvariantCulture);
            return string.Join(" ", parts);
        }

        private static double[] ParseDoubles(string text, int expectedCount)
        {
            var parts = text.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != expectedCount)
                throw new FormatException($"Expected {expectedCount} numbers, got {parts.Length}: '{text}'.");
            var result = new double[expectedCount];
            for (int i = 0; i < expectedCount; i++)
                result[i] = double.Parse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture);
            return result;
        }

        private static int[] ParseInts(string text, int expectedCount)
        {
            var parts = text.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != expectedCount)
                throw new FormatException($"Expected {expectedCount} integers, got {parts.Length}: '{text}'.");
            var result = new int[expectedCount];
            for (int i = 0; i < expectedCount; i++)
                result[i] = int.Parse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
            return result;
        }

        // ---------- JSON converters -------------------------------------

        public static readonly JsonConverter[] JsonConverters = new JsonConverter[]
        {
            new ColorJsonConverter(),
            new Vector2DJsonConverter(),
            new Vector3DJsonConverter(),
            new Vector2IJsonConverter(),
            new Vector3IJsonConverter(),
            new PositionAndOrientationJsonConverter(),
            // Direction is an enum, handled by the existing JsonStringEnumConverter.
        };

        private sealed class ColorJsonConverter : JsonConverter<Color>
        {
            public override Color Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions __)
                => ParseColor(reader.GetString());

            public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions _)
                => writer.WriteStringValue(FormatColor(value));
        }

        private sealed class Vector2DJsonConverter : JsonConverter<Vector2D>
        {
            public override Vector2D Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions __)
            {
                double x = 0, y = 0;
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    var name = reader.GetString();
                    reader.Read();
                    switch (name)
                    {
                        case "x": case "X": x = reader.GetDouble(); break;
                        case "y": case "Y": y = reader.GetDouble(); break;
                    }
                }
                return new Vector2D(x, y);
            }

            public override void Write(Utf8JsonWriter writer, Vector2D value, JsonSerializerOptions _)
            {
                writer.WriteStartObject();
                writer.WriteNumber("x", value.X);
                writer.WriteNumber("y", value.Y);
                writer.WriteEndObject();
            }
        }

        private sealed class Vector3DJsonConverter : JsonConverter<Vector3D>
        {
            public override Vector3D Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions __)
            {
                double x = 0, y = 0, z = 0;
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    var name = reader.GetString();
                    reader.Read();
                    switch (name)
                    {
                        case "x": case "X": x = reader.GetDouble(); break;
                        case "y": case "Y": y = reader.GetDouble(); break;
                        case "z": case "Z": z = reader.GetDouble(); break;
                    }
                }
                return new Vector3D(x, y, z);
            }

            public override void Write(Utf8JsonWriter writer, Vector3D value, JsonSerializerOptions _)
            {
                writer.WriteStartObject();
                writer.WriteNumber("x", value.X);
                writer.WriteNumber("y", value.Y);
                writer.WriteNumber("z", value.Z);
                writer.WriteEndObject();
            }
        }

        private sealed class Vector2IJsonConverter : JsonConverter<Vector2I>
        {
            public override Vector2I Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions __)
            {
                int x = 0, y = 0;
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    var name = reader.GetString();
                    reader.Read();
                    switch (name)
                    {
                        case "x": case "X": x = reader.GetInt32(); break;
                        case "y": case "Y": y = reader.GetInt32(); break;
                    }
                }
                return new Vector2I(x, y);
            }

            public override void Write(Utf8JsonWriter writer, Vector2I value, JsonSerializerOptions _)
            {
                writer.WriteStartObject();
                writer.WriteNumber("x", value.X);
                writer.WriteNumber("y", value.Y);
                writer.WriteEndObject();
            }
        }

        private sealed class Vector3IJsonConverter : JsonConverter<Vector3I>
        {
            public override Vector3I Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions __)
            {
                int x = 0, y = 0, z = 0;
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    var name = reader.GetString();
                    reader.Read();
                    switch (name)
                    {
                        case "x": case "X": x = reader.GetInt32(); break;
                        case "y": case "Y": y = reader.GetInt32(); break;
                        case "z": case "Z": z = reader.GetInt32(); break;
                    }
                }
                return new Vector3I(x, y, z);
            }

            public override void Write(Utf8JsonWriter writer, Vector3I value, JsonSerializerOptions _)
            {
                writer.WriteStartObject();
                writer.WriteNumber("x", value.X);
                writer.WriteNumber("y", value.Y);
                writer.WriteNumber("z", value.Z);
                writer.WriteEndObject();
            }
        }

        private sealed class PositionAndOrientationJsonConverter : JsonConverter<MyPositionAndOrientation>
        {
            public override MyPositionAndOrientation Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions options)
            {
                Vector3D position = Vector3D.Zero;
                Vector3 forward = Vector3.Forward;
                Vector3 up = Vector3.Up;

                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    var name = reader.GetString();
                    reader.Read();
                    switch (name)
                    {
                        case "position":
                        case "Position":
                            position = JsonSerializer.Deserialize<Vector3D>(ref reader, options);
                            break;
                        case "forward":
                        case "Forward":
                            forward = (Vector3)JsonSerializer.Deserialize<Vector3D>(ref reader, options);
                            break;
                        case "up":
                        case "Up":
                            up = (Vector3)JsonSerializer.Deserialize<Vector3D>(ref reader, options);
                            break;
                    }
                }
                return new MyPositionAndOrientation(position, forward, up);
            }

            public override void Write(Utf8JsonWriter writer, MyPositionAndOrientation value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("position");
                JsonSerializer.Serialize(writer, (Vector3D)value.Position, options);

                writer.WritePropertyName("forward");
                JsonSerializer.Serialize(writer, new Vector3D(value.Forward.X, value.Forward.Y, value.Forward.Z), options);

                writer.WritePropertyName("up");
                JsonSerializer.Serialize(writer, new Vector3D(value.Up.X, value.Up.Y, value.Up.Z), options);

                writer.WriteEndObject();
            }
        }
    }
}
