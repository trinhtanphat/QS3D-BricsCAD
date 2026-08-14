using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedStaleReasonPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var element = new ProjectElement("E-STALE", ElementCategory.CurtainWall);
            element.Properties["GeneratedCurtainFrameHandles"] = "AB12";
            element.MarkClean(ElementDirtyFlags.All);

            var propertiesBeforeRejectedReason = new Dictionary<string, string>(element.Properties, StringComparer.OrdinalIgnoreCase);
            var dirtyBeforeRejectedReason = element.Dirty;
            var updatedBeforeRejectedReason = element.UpdatedUtc;
            var rejected = false;
            try
            {
                element.MarkGeneratedCurtainFrameStale("bad\u0001reason");
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            if (!rejected)
                throw new InvalidOperationException("Generated stale marking accepted an XML-illegal reason.");
            EqualProperties(propertiesBeforeRejectedReason, element.Properties, "Rejected generated stale reason");
            if (element.Dirty != dirtyBeforeRejectedReason)
                throw new InvalidOperationException("Rejected generated stale reason changed Dirty state.");
            if (element.UpdatedUtc != updatedBeforeRejectedReason)
                throw new InvalidOperationException("Rejected generated stale reason changed UpdatedUtc.");

            const string normalizedReason = "Manual\nreview\tneeded";
            element.MarkGeneratedCurtainFrameStale("  " + normalizedReason + "  ");
            Equal("stale", element.Properties[ProjectElement.GeneratedCurtainFrameStateKey], "Curtain frame stale state");
            Equal("stale", element.Properties[ProjectElement.GeneratedGeometryStateKey], "Aggregate generated stale state");
            Equal(normalizedReason, element.Properties[ProjectElement.GeneratedGeometryStaleReasonKey], "Normalized generated stale reason");

            var project = new ProjectState("STALE-REASON", "Generated stale reason persistability");
            project.Elements.Add(element);
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-stale-reason-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);

            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var loadedElement = loaded.FindElement("E-STALE") ?? throw new InvalidOperationException("Saved stale element did not round-trip.");
                if (!loadedElement.Properties.TryGetValue(ProjectElement.GeneratedGeometryStaleReasonKey, out var loadedReason))
                    throw new InvalidOperationException("Generated stale reason did not round-trip through QSDB.");
                Equal(normalizedReason, loadedReason, "Generated stale reason QSDB round-trip");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void EqualProperties(
            IReadOnlyDictionary<string, string> expected,
            IDictionary<string, string> actual,
            string label)
        {
            if (expected.Count != actual.Count)
                throw new InvalidOperationException(label + " changed property count.");
            foreach (var pair in expected)
            {
                if (!actual.TryGetValue(pair.Key, out var value) || !string.Equals(pair.Value, value, StringComparison.Ordinal))
                    throw new InvalidOperationException(label + " changed property '" + pair.Key + "'.");
            }
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " mismatch.");
        }
    }
}
