using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantityReportPersistedGeometrySnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            PersistedGeometryReportsWithoutMutation();
            SnapshotPreservesPropertyCapacityGuard();
        }

        private static void PersistedGeometryReportsWithoutMutation()
        {
            var directory = Path.Combine(Path.GetTempPath(), "QS3D-report-persisted-geometry-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "synthetic.qsdb");
                var store = new QsdbProjectStore();
                var warm = CreateProject();
                AssertRows(warm);
                store.Save(warm, path);
                var savedBytes = File.ReadAllBytes(path);
                var cold = store.Load(path);
                var keys = cold.Elements[0].Properties.Keys.ToList();
                Require(keys.IndexOf("GeneratedSolidHandle") < keys.IndexOf("LengthM"), "persistence no longer exercises output-before-geometry ordering");
                Require(warm.Elements[0].Properties.Keys.ToList().IndexOf("GeneratedSolidHandle") >
                    warm.Elements[0].Properties.Keys.ToList().IndexOf("LengthM"), "warm fixture must have geometry-before-output ordering");

                var before = Describe(cold);
                var preview = ProjectStateSnapshot.CreateDetachedCopy(cold);
                var previewBefore = Describe(preview);
                var count = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(preview);
                Require(count == 0, "saved generated elements must already be clean");
                AssertRows(preview);
                AssertRows(preview);
                Require(Describe(preview) == previewBefore, "report mutated the detached semantic generation");
                Require(Describe(cold) == before, "report mutated the loaded project");
                Require(savedBytes.SequenceEqual(File.ReadAllBytes(path)), "report changed persisted bytes");

                // The fix must preserve, not bypass, the genuine generation fence.
                RejectsActualPostCaptureDrift(ProjectStateSnapshot.CreateDetachedCopy(cold), false);
                RejectsActualPostCaptureDrift(ProjectStateSnapshot.CreateDetachedCopy(cold), true);
                Require(savedBytes.SequenceEqual(File.ReadAllBytes(path)), "drift refusal changed persisted bytes");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("P-PERSISTED-BQ", "Synthetic persisted geometry BQ") { DrawingFingerprint = "FP-PERSISTED-BQ" };
            for (var index = 0; index < 2; index++)
            {
                var element = new ProjectElement("E" + index, ElementCategory.Foundation) { DrawingFingerprint = project.DrawingFingerprint };
                element.SourceHandles.Add(index == 0 ? "A1" : "A2");
                element.SetProperty("LengthM", "4");
                element.SetProperty("ThicknessM", "2");
                element.SetProperty("Material", "Concrete");
                element.SetProperty("GeneratedSolidHandle", index == 0 ? "B1" : "B2");
                element.SetQuantity("GrossVolumeM3", 38d / 3d);
                element.SetQuantity("NetVolumeM3", 38d / 3d);
                element.MarkClean(ElementDirtyFlags.All);
                Require(!element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStateKey), "fixture unexpectedly starts stale");
                project.Elements.Add(element);
            }
            return project;
        }

        private static void AssertRows(ProjectState project)
        {
            var rows = ProjectQuantityReportBuilder.Group(project);
            Require(rows.Count == 1 && rows[0].Count == 2, "summary cardinality");
            Require(Math.Abs(rows[0].GrossConcreteM3 - 76d / 3d) < 1e-12 && Math.Abs(rows[0].NetConcreteM3 - 76d / 3d) < 1e-12, "summary quantity");
            Require(rows[0].SourceHandles.SequenceEqual(new[] { "A1", "A2" }), "summary source provenance");
            var details = ProjectQuantityReportBuilder.Detail(project);
            Require(details.Count == 2 && details.All(row => row.Count == 1 &&
                Math.Abs(row.GrossConcreteM3 - 38d / 3d) < 1e-12 &&
                Math.Abs(row.NetConcreteM3 - 38d / 3d) < 1e-12), "detail quantity");
        }

        private static void SnapshotPreservesPropertyCapacityGuard()
        {
            var project = new ProjectState("P-BQ-CAPACITY", "Synthetic report capacity");
            var element = new ProjectElement("E-CAPACITY", ElementCategory.Foundation);
            project.Elements.Add(element);
            // SetProperty is a legal direct route that predates the dictionary
            // facade's capacity guard; reporting must retain its own refusal.
            for (var index = 0; index < 10000; index++) element.SetProperty("Key" + index, "Value");
            Require(ProjectQuantityReportBuilder.Group(project).Count == 1, "capacity boundary summary rejected");
            Require(ProjectQuantityReportBuilder.Detail(project).Count == 1, "capacity boundary detail rejected");
            element.SetProperty("Overflow", "Value");
            foreach (var detail in new[] { false, true })
            {
                try
                {
                    if (detail) _ = ProjectQuantityReportBuilder.Detail(project);
                    else _ = ProjectQuantityReportBuilder.Group(project);
                }
                catch (InvalidOperationException error) when (error.Message.Contains("maximum supported cardinality"))
                {
                    Require(element.Properties.Count == 10001 && element.Properties["Overflow"] == "Value", "capacity refusal mutated source");
                    continue;
                }
                throw new InvalidOperationException("Quantity snapshot accepted an oversized public property collection.");
            }
        }

        private static void RejectsActualPostCaptureDrift(ProjectState project, bool detail)
        {
            var hook = typeof(ProjectQuantityReportBuilder).GetField("GenerationSnapshotCaptured", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Missing quantity snapshot hook.");
            hook.SetValue(null, (Action<ProjectState>)(current => current.Elements[0].SetProperty("LengthM", "5")));
            try
            {
                if (detail) _ = ProjectQuantityReportBuilder.Detail(project);
                else _ = ProjectQuantityReportBuilder.Group(project);
            }
            catch (InvalidOperationException error) when (error.Message.StartsWith("Project changed while the quantity report was being built", StringComparison.Ordinal))
            {
                Require(project.Elements[0].Properties[ProjectElement.GeneratedSolidStateKey] == "stale", "real semantic edit no longer invalidates generated geometry");
                return;
            }
            finally
            {
                hook.SetValue(null, null);
            }
            throw new InvalidOperationException("Persisted BQ report accepted actual post-capture semantic drift.");
        }

        private static string Describe(ProjectState project)
        {
            var serialize = typeof(QsdbProjectStore).GetMethod("Serialize", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Missing canonical persistence serializer.");
            return ((XDocument)(serialize.Invoke(null, new object[] { project }) ?? throw new InvalidOperationException("Missing serialized project.")))
                .ToString(SaveOptions.DisableFormatting);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Persisted geometry quantity snapshot: " + message + ".");
        }
    }
}
