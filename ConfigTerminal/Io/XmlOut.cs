using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Magnetar.ConfigTerminal.Io;

/// <summary>
/// Shared XML output settings matching what the DS and Quasar write: UTF-8
/// without BOM, indented, LF newlines, XML declaration present. Keeping every DS
/// file on these settings keeps diffs clean across platforms and tools.
/// </summary>
internal static class XmlOut
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static XmlWriterSettings Settings() => new()
    {
        Indent = true,
        Encoding = Utf8NoBom,
        OmitXmlDeclaration = false,
        NewLineChars = "\n",
        NewLineHandling = NewLineHandling.Replace,
    };

    /// <summary>Serializes a document to a string using the shared settings.</summary>
    public static string ToXmlString(XDocument document)
    {
        using var stringWriter = new Utf8StringWriter();
        using (var xmlWriter = XmlWriter.Create(stringWriter, Settings()))
        {
            document.Save(xmlWriter);
            xmlWriter.Flush();
        }
        return stringWriter.ToString();
    }

    /// <summary>A StringWriter that reports UTF-8 so the declaration reads encoding="utf-8".</summary>
    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Utf8NoBom;
    }
}
