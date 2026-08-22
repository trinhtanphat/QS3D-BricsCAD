using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace QS3D.Core.Export
{
    internal static class XlsxPackageValidator
    {
        private const long MaxXmlEntryBytes = 32L * 1024L * 1024L;

        public static void Validate(string path, params string[] requiredEntries)
        {
            using (var archive = ZipFile.OpenRead(path))
            {
                foreach (var name in requiredEntries)
                {
                    var entry = archive.GetEntry(name) ?? throw new InvalidDataException("Generated XLSX package is missing " + name + ".");
                    if (entry.Length > MaxXmlEntryBytes) throw new InvalidDataException("Generated XLSX XML entry is too large: " + name + ".");
                    var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxXmlEntryBytes };
                    using (var stream = entry.Open())
                    using (var reader = XmlReader.Create(stream, settings))
                        while (reader.Read()) { }
                }
            }
        }
    }
}
