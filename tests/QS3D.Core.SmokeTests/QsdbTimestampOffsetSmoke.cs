using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbTimestampOffsetSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExplicitOffsetNormalizesDeterministically();
            MissingOffsetIsRejected();
        }

        private static void ExplicitOffsetNormalizesDeterministically()
        {
            WithFile("2026-08-10T12:00:00+07:00", path =>
            {
                var project = new QsdbProjectStore().Load(path);
                var expected = new DateTime(2026, 8, 10, 5, 0, 0, DateTimeKind.Utc);
                if (project.UpdatedUtc != expected || project.UpdatedUtc.Kind != DateTimeKind.Utc)
                    throw new InvalidOperationException("QsdbTimestampOffsetSmoke: explicit +07:00 timestamp did not normalize deterministically to UTC.");
            });
        }

        private static void MissingOffsetIsRejected()
        {
            WithFile("2026-08-10T12:00:00", path =>
            {
                var rejected = false;
                try { new QsdbProjectStore().Load(path); }
                catch (InvalidDataException) { rejected = true; }
                if (!rejected)
                    throw new InvalidOperationException("QsdbTimestampOffsetSmoke: timestamp without Z/offset was accepted and may depend on machine timezone.");
            });
        }

        private static void WithFile(string updatedUtc, Action<string> action)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-ts-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var xml = "<qs3d schema=\"3\" projectId=\"P-ts\" name=\"Timestamp\" updatedUtc=\"" + updatedUtc + "\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                          "<metadata/><zones/><floors/><families/><rules/><elements/><audit/></qs3d>";
                File.WriteAllText(path, xml, Encoding.UTF8);
                action(path);
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
