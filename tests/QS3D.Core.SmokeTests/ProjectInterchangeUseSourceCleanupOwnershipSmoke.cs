using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeUseSourceCleanupOwnershipSmoke
    {
        internal static void Run()
        {
            AmbiguousGeneratedOwnershipFailsBeforePlan();
            DestructiveCleanupRequiresTargetDrawingFingerprint();
            UniqueOwnedCleanupRemainsPlannable();
        }

        private static void AmbiguousGeneratedOwnershipFailsBeforePlan()
        {
            var target = TargetProject("target-fingerprint", ambiguousOwnership: true);
            var owner = target.FindElement("E1") ?? throw new Exception("Expected owner element.");
            var conflicting = target.FindElement("E2") ?? throw new Exception("Expected conflicting element.");
            var updated = new DateTime(2026, 8, 11, 10, 45, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var metadata = target.Metadata.Count;
            var audits = target.AuditEvents.Count;

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeUseSourceSemanticImporter.Plan(
                    target,
                    ProjectInterchangeJsonExporter.Build(SourceProject())));

            Equal("TARGET", owner.Properties["Mark"]);
            Equal("AA11", owner.Properties["GeneratedSolidHandle"]);
            Equal("AA11", conflicting.Properties["GeneratedSolidHandle"]);
            Equal(metadata, target.Metadata.Count);
            Equal(audits, target.AuditEvents.Count);
            Equal(updated, target.UpdatedUtc);
        }

        private static void DestructiveCleanupRequiresTargetDrawingFingerprint()
        {
            var target = TargetProject(string.Empty, ambiguousOwnership: false);
            var owner = target.FindElement("E1") ?? throw new Exception("Expected owner element.");
            var updated = new DateTime(2026, 8, 11, 10, 46, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var metadata = target.Metadata.Count;
            var audits = target.AuditEvents.Count;

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeUseSourceSemanticImporter.Plan(
                    target,
                    ProjectInterchangeJsonExporter.Build(SourceProject())));

            Equal("TARGET", owner.Properties["Mark"]);
            Equal("AA11", owner.Properties["GeneratedSolidHandle"]);
            Equal(metadata, target.Metadata.Count);
            Equal(audits, target.AuditEvents.Count);
            Equal(updated, target.UpdatedUtc);
        }

        private static void UniqueOwnedCleanupRemainsPlannable()
        {
            var target = TargetProject("target-fingerprint", ambiguousOwnership: false);
            var plan = ProjectInterchangeUseSourceSemanticImporter.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(SourceProject()));

            True(plan.RequiresNativeCleanup);
            Equal(1, plan.NativeCleanupRequirements.Count);
            Equal("E1", plan.NativeCleanupRequirements.Single().ElementId);
            Equal("AA11", plan.NativeCleanupRequirements.Single().OwnerHandles.Single());
            Equal("target-fingerprint", plan.TargetDrawingFingerprint);
        }

        private static ProjectState TargetProject(string drawingFingerprint, bool ambiguousOwnership)
        {
            var target = new ProjectState("TARGET", "Target")
            {
                DrawingFingerprint = drawingFingerprint ?? string.Empty
            };
            var owner = new ProjectElement("E1", ElementCategory.Beam)
            {
                DrawingFingerprint = target.DrawingFingerprint
            };
            owner.Properties["Mark"] = "TARGET";
            owner.Properties["GeneratedSolidHandle"] = "AA11";
            owner.Properties[ProjectElement.GeneratedSolidStateKey] = "current";
            target.Elements.Add(owner);

            if (ambiguousOwnership)
            {
                var conflicting = new ProjectElement("E2", ElementCategory.Beam)
                {
                    DrawingFingerprint = target.DrawingFingerprint
                };
                conflicting.Properties["GeneratedSolidHandle"] = "AA11";
                conflicting.Properties[ProjectElement.GeneratedSolidStateKey] = "current";
                target.Elements.Add(conflicting);
            }

            return target;
        }

        private static ProjectState SourceProject()
        {
            var source = new ProjectState("SOURCE", "Source")
            {
                DrawingFingerprint = "source-fingerprint",
                UpdatedUtc = new DateTime(2026, 8, 11, 10, 40, 0, DateTimeKind.Utc)
            };
            var element = new ProjectElement("E1", ElementCategory.Beam)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            element.Properties["Mark"] = "SOURCE";
            source.Elements.Add(element);
            return source;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectInterchangeUseSourceCleanupOwnershipSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeUseSourceCleanupOwnershipSmoke.Run();
    }
}
