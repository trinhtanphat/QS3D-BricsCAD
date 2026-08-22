using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionParsedStreamSizeSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OversizedParsedStreamFailsBeforeXmlParsing();
            ValidRevisionStillLoads();
        }

        private static void OversizedParsedStreamFailsBeforeXmlParsing()
        {
            var root = TempRoot("oversized");
            var path = Path.Combine(root, "snapshot.xml");
            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllBytes(path, new byte[4096]);
                var boundedLoad = typeof(RevisionSnapshotStore).GetMethod(
                    "LoadDocument",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    binder: null,
                    types: new[] { typeof(string), typeof(long) },
                    modifiers: null) ?? throw new InvalidOperationException("Revision bounded LoadDocument overload is unavailable.");

                try
                {
                    boundedLoad.Invoke(null, new object[] { path, 512L });
                }
                catch (TargetInvocationException ex) when (ex.InnerException is InvalidDataException dataError)
                {
                    const string expected = "QS3D revision exceeds the maximum supported file size of 64 MiB.";
                    if (!string.Equals(dataError.Message, expected, StringComparison.Ordinal))
                        throw new InvalidOperationException("Unexpected revision parsed-stream size error.", dataError);
                    return;
                }

                throw new InvalidOperationException("Oversized revision stream reached XML parsing instead of the byte-size guard.");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static void ValidRevisionStillLoads()
        {
            var root = TempRoot("valid");
            var path = Path.Combine(root, "snapshot.xml");
            var snapshot = new RevisionSnapshot
            {
                Id = "REV-STREAM-VALID",
                CreatedUtc = new DateTime(2026, 8, 12, 4, 12, 0, DateTimeKind.Utc)
            };
            snapshot.Elements.Add(new RevisionElementSnapshot
            {
                ElementId = "E1",
                Category = ElementCategory.CustomQuantity.ToString(),
                FamilyId = string.Empty,
                FloorId = string.Empty,
                ZoneId = string.Empty
            });

            try
            {
                var store = new RevisionSnapshotStore();
                store.Save(snapshot, path);
                var loaded = store.Load(path);
                Require(string.Equals(loaded.Id, snapshot.Id, StringComparison.Ordinal),
                    "Valid revision id did not load after parsed-stream size binding.");
                Require(loaded.Elements.Count == 1 && string.Equals(loaded.Elements[0].ElementId, "E1", StringComparison.Ordinal),
                    "Valid revision element did not load after parsed-stream size binding.");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static string TempRoot(string suffix) =>
            Path.Combine(Path.GetTempPath(), "QS3D-RevisionStreamSize-" + suffix + "-" + Guid.NewGuid().ToString("N"));

        private static void DeleteTree(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
