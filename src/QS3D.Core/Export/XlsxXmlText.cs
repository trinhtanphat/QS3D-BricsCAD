using System;
using System.Security;
using System.Xml;

namespace QS3D.Core.Export
{
    internal static class XlsxXmlText
    {
        public static string Escape(string? value)
        {
            var text = value ?? string.Empty;
            try
            {
                XmlConvert.VerifyXmlChars(text);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException("XLSX text contains a character that is not valid in XML.", nameof(value), ex);
            }
            return SecurityElement.Escape(text) ?? string.Empty;
        }
    }
}
