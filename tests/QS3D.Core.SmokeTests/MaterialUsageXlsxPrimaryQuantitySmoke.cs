using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageXlsxPrimaryQuantitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-material-primary-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                MaterialUsageXlsxExporter.Export(path, new List<MaterialUsageRow>
                {
                    new MaterialUsageRow
                    {
                        Floor = "L1",
                        MaterialName = "Tile",
                        UnitHint = "m2",
                        Component = "Material",
                        Category = "FloorFinish",
                        FamilyName = "Tile floor",
                        ElementCount = 1,
                        LengthM = 3d,
                        AreaM2 = 42.25d,
                        VolumeM3 = 99.5d,
                        MassKg = 7d
                    }
                });

                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml")
                        ?? throw new InvalidOperationException("Material XLSX worksheet entry is missing.");
                    using (var reader = new StreamReader(entry.Open(), Encoding.UTF8, true))
                    {
                        var xml = reader.ReadToEnd();
                        Contains(xml, "<c r=\"H2\" s=\"2\"><v>42.25</v></c>", "Material XLSX primary quantity did not preserve the unit-selected area quantity.");
                        NotContains(xml, "<c r=\"H2\" s=\"2\"><v>99.5</v></c>", "Material XLSX primary quantity regressed to the unrelated volume quantity.");
                    }
                }
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static void Contains(string value, string expected, string message)
        {
            if (value.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message);
        }

        private static void NotContains(string value, string unexpected, string message)
        {
            if (value.IndexOf(unexpected, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException(message);
        }
    }
}
