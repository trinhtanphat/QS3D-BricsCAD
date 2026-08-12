using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeSourceHandleProvenanceDrawingScopeSmoke
    {
        public static void Run()
        {
            PlanRejectsUnscopedSourceHandles();
            StoreRejectsUnscopedSourceHandlesWithoutMutation();
            HandleFreeProvenanceAllowsBlankDrawingFingerprint();
        }

        private static void PlanRejectsUnscopedSourceHandles()
        {
            var target = TargetProject();
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withSourceHandle: true));

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeSourceHandleProvenance.Plan(target, json));
        }

        private static void StoreRejectsUnscopedSourceHandlesWithoutMutation()
        {
            var target = TargetProject();
            target.Metadata["Sentinel"] = "keep";
            var updated = new DateTime(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var metadataCount = target.Metadata.Count;
            var auditCount = target.AuditEvents.Count;
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withSourceHandle: true));

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeSourceHandleProvenance.Store(target, json));

            Equal(metadataCount, target.Metadata.Count);
            Equal("keep", target.Metadata["Sentinel"]);
            Equal(auditCount, target.AuditEvents.Count);
            Equal(updated, target.UpdatedUtc);
            True(!target.Metadata.ContainsKey(ProjectInterchangeSourceHandleProvenance.LastSourceProjectIdKey));
            True(!target.Metadata.ContainsKey(ProjectInterchangeSourceHandleProvenance.LastStoredUtcKey));
        }

        private static void HandleFreeProvenanceAllowsBlankDrawingFingerprint()
        {
            var target = TargetProject();
            var source = SourceProject(withSourceHandle: false);
            var json = ProjectInterchangeJsonExporter.Build(source);

            var plan = ProjectInterchangeSourceHandleProvenance.Plan(target, json);
            Equal(string.Empty, plan.SourceDrawingFingerprint);
            Equal(0, plan.ElementsWithHandles);
            Equal(0, plan.SourceHandleCount);

            var result = ProjectInterchangeSourceHandleProvenance.Store(target, json);
            Equal(source.ProjectId, result.SourceProjectId);
            Equal(0, result.ElementsStored);
            Equal(0, result.SourceHandlesStored);
            Equal(source.ProjectId, target.Metadata[ProjectInterchangeSourceHandleProvenance.LastSourceProjectIdKey]);
            Equal("0", target.Metadata[ProjectInterchangeSourceHandleProvenance.LastElementsStoredKey]);
            Equal("0", target.Metadata[ProjectInterchangeSourceHandleProvenance.LastSourceHandlesStoredKey]);
        }

        private static ProjectState SourceProject(bool withSourceHandle)
        {
            var project = new ProjectState("SOURCE-NO-FP", "Source without drawing fingerprint")
            {
                DrawingFingerprint = string.Empty,
                UpdatedUtc = new DateTime(2026, 8, 12, 11, 0, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("SRC-ZONE", "Source Zone"));
            project.Floors.Add(new FloorDefinition("SRC-FLOOR", "Source Floor", 0d));
            var family = new ProjectFamily("SRC-FAMILY", "Source Beam", ElementCategory.Beam);
            project.Families.Add(family);

            var element = new ProjectElement("SRC-E1", ElementCategory.Beam, family.Id, "SRC-FLOOR", "SRC-ZONE")
            {
                DrawingFingerprint = string.Empty
            };
            if (withSourceHandle)
                element.SourceHandles.Add("ABCD");
            project.Elements.Add(element);
            return project;
        }

        private static ProjectState TargetProject()
        {
            return new ProjectState("TARGET-P", "Target Project")
            {
                DrawingFingerprint = "target-fingerprint"
            };
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Expected condition to be true.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class ProjectInterchangeSourceHandleProvenanceDrawingScopeSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectInterchangeSourceHandleProvenanceDrawingScopeSmoke.Run();
        }
    }
}
