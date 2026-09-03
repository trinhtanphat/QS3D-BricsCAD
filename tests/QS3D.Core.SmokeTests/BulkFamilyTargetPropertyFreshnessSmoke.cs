using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkFamilyTargetPropertyFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            UpdatedTargetPropertyDuringLazyEnumerationFailsClosed();
            RemovedTargetPropertyDuringLazyEnumerationFailsClosed();
        }

        private static void UpdatedTargetPropertyDuringLazyEnumerationFailsClosed()
        {
            var project = CreateProject("P-BULK-FAMILY-TARGET-PROP-1", out var family, out var element);
            family.Properties["WidthM"] = "0.4";
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            ThrowsInvalidOperation(
                () => new BulkEditService().AssignFamily(
                    project,
                    YieldThenSetTargetProperty(family, element.Id, "WidthM", "0.8"),
                    family.Id),
                "updated-property lazy enumeration freshness",
                "changed the project while targets were being enumerated");

            Equal("0.8", family.Properties["WidthM"], "updated canonical Family value");
            False(element.Properties.ContainsKey("WidthM"), "stale bulk assignment must not copy updated Family property");
            Equal(string.Empty, element.FamilyId, "stale bulk assignment must not set FamilyId");
            Equal(beforeVersion + 1L, project.ChangeVersion, "updated-property mutation project revision");
        }

        private static void RemovedTargetPropertyDuringLazyEnumerationFailsClosed()
        {
            var project = CreateProject("P-BULK-FAMILY-TARGET-PROP-2", out var family, out var element);
            family.Properties["WidthM"] = "0.4";
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            ThrowsInvalidOperation(
                () => new BulkEditService().AssignFamily(
                    project,
                    YieldThenRemoveTargetProperty(family, element.Id, "WidthM"),
                    family.Id),
                "removed-property lazy enumeration freshness",
                "changed the project while targets were being enumerated");

            False(family.Properties.ContainsKey("WidthM"), "removed canonical Family property");
            False(element.Properties.ContainsKey("WidthM"), "stale bulk assignment must not leak removed Family property");
            Equal(string.Empty, element.FamilyId, "stale bulk assignment must not set FamilyId after removal");
            Equal(beforeVersion + 1L, project.ChangeVersion, "removed-property mutation project revision");
        }

        private static ProjectState CreateProject(string id, out ProjectFamily family, out ProjectElement element)
        {
            var project = new ProjectState(id, "Bulk Family target property freshness");
            family = new ProjectFamily("F-BULK-TARGET-PROP", "Bulk Target Property Family", ElementCategory.Beam);
            element = new ProjectElement("E-BULK-TARGET-PROP", ElementCategory.Beam);
            project.Families.Add(family);
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<string> YieldThenSetTargetProperty(ProjectFamily family, string elementId, string key, string value)
        {
            yield return elementId;
            family.Properties[key] = value;
        }

        private static IEnumerable<string> YieldThenRemoveTargetProperty(ProjectFamily family, string elementId, string key)
        {
            yield return elementId;
            family.Properties.Remove(key);
        }

        private static void ThrowsInvalidOperation(Action action, string label, string expectedMessagePart)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessagePart, StringComparison.Ordinal) < 0)
                    throw new Exception("BulkFamilyTargetPropertyFreshnessSmoke " + label + " message: expected token='" + expectedMessagePart + "', actual='" + ex.Message + "'.");
                return;
            }

            throw new Exception("BulkFamilyTargetPropertyFreshnessSmoke expected InvalidOperationException: " + label + ".");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("BulkFamilyTargetPropertyFreshnessSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("BulkFamilyTargetPropertyFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
