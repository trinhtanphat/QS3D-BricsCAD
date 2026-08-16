using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotOutputBoundSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OversizedSnapshotFailsClosedWithoutPublication();
            OrdinarySnapshotStillRoundTrips();
        }

        private static void OversizedSnapshotFailsClosedWithoutPublication()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-revision-bound-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "oversized.qs3drev");
            try
            {
                var snapshot = CreateSnapshot(new string('x', 4096));
                try
                {
                    InvokeBoundedSave(snapshot, path, 512L);
                    throw new InvalidOperationException("Revision snapshot output exceeding the configured byte limit must fail closed.");
                }
                catch (InvalidDataException ex) when (ex.Message.IndexOf("maximum supported file size", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                }

                if (File.Exists(path))
                    throw new InvalidOperationException("An over-budget revision snapshot must not publish a partial destination file.");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static void OrdinarySnapshotStillRoundTrips()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-revision-roundtrip-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "ordinary.qs3drev");
            try
            {
                var snapshot = CreateSnapshot("ordinary-value");
                InvokeBoundedSave(snapshot, path, 64L * 1024L);
                var loaded = new RevisionSnapshotStore().Load(path);
                if (!string.Equals(loaded.Id, snapshot.Id, StringComparison.Ordinal) ||
                    !string.Equals(loaded.ProjectId, snapshot.ProjectId, StringComparison.Ordinal) ||
                    loaded.CreatedUtc != snapshot.CreatedUtc ||
                    loaded.Elements.Count != 1 ||
                    !string.Equals(loaded.Elements[0].ElementId, "E-1", StringComparison.Ordinal) ||
                    !string.Equals(loaded.Elements[0].Properties["Note"], "ordinary-value", StringComparison.Ordinal) ||
                    Math.Abs(loaded.Elements[0].Quantities["Length"] - 12.5d) > 1e-12)
                    throw new InvalidOperationException("Bounded streaming persistence changed ordinary revision round-trip semantics.");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static RevisionSnapshot CreateSnapshot(string propertyValue)
        {
            var snapshot = new RevisionSnapshot
            {
                Id = "REV-BOUND",
                ProjectId = "PROJECT-1",
                CreatedUtc = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc)
            };
            var element = new RevisionElementSnapshot
            {
                ElementId = "E-1",
                Category = ElementCategory.Wall.ToString(),
                FamilyId = "F-1",
                FloorId = "L-1",
                ZoneId = "Z-1"
            };
            element.Properties["Note"] = propertyValue;
            element.Quantities["Length"] = 12.5d;
            element.SourceHandles.Add("A1");
            element.Dependencies.Add("E-0");
            snapshot.Elements.Add(element);
            return snapshot;
        }

        private static void InvokeBoundedSave(RevisionSnapshot snapshot, string path, long maximumBytes)
        {
            var method = typeof(RevisionSnapshotStore).GetMethod(
                "Save",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(RevisionSnapshot), typeof(string), typeof(long) },
                null) ?? throw new InvalidOperationException("RevisionSnapshotStore bounded Save overload was not found.");
            try
            {
                method.Invoke(new RevisionSnapshotStore(), new object[] { snapshot, path, maximumBytes });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }
    }
}
