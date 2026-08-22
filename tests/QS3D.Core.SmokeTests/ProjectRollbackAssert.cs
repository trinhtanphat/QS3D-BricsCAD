using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectRollbackAssert
    {
        internal static ProjectState Capture(ProjectState project) =>
            ProjectStateSnapshot.CreateDetachedCopy(project ?? throw new ArgumentNullException(nameof(project)));

        internal static void Equivalent(ProjectState expected, ProjectState actual, string label = "ProjectState")
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));

            Equal(expected.SchemaVersion, actual.SchemaVersion, label + ".SchemaVersion");
            Equal(expected.ProjectId, actual.ProjectId, label + ".ProjectId");
            Equal(expected.Name, actual.Name, label + ".Name");
            Equal(expected.DrawingPath, actual.DrawingPath, label + ".DrawingPath");
            Equal(expected.DrawingFingerprint, actual.DrawingFingerprint, label + ".DrawingFingerprint");
            Equal(expected.ActiveZoneId, actual.ActiveZoneId, label + ".ActiveZoneId");
            Equal(expected.ActiveFloorId, actual.ActiveFloorId, label + ".ActiveFloorId");
            Equal(expected.UpdatedUtc, actual.UpdatedUtc, label + ".UpdatedUtc");
            Equal(expected.ChangeVersion, actual.ChangeVersion, label + ".ChangeVersion");

            Equal(expected.Zones.Count, actual.Zones.Count, label + ".Zones.Count");
            for (var i = 0; i < expected.Zones.Count; i++)
            {
                var left = expected.Zones[i];
                var right = actual.Zones[i];
                Equal(left.Id, right.Id, label + ".Zones[" + i + "].Id");
                Equal(left.Name, right.Name, label + ".Zones[" + i + "].Name");
            }

            Equal(expected.Floors.Count, actual.Floors.Count, label + ".Floors.Count");
            for (var i = 0; i < expected.Floors.Count; i++)
            {
                var left = expected.Floors[i];
                var right = actual.Floors[i];
                Equal(left.Id, right.Id, label + ".Floors[" + i + "].Id");
                Equal(left.Name, right.Name, label + ".Floors[" + i + "].Name");
                Equal(left.ElevationM, right.ElevationM, label + ".Floors[" + i + "].ElevationM");
            }

            Equal(expected.Families.Count, actual.Families.Count, label + ".Families.Count");
            for (var i = 0; i < expected.Families.Count; i++)
            {
                var left = expected.Families[i];
                var right = actual.Families[i];
                Equal(left.Id, right.Id, label + ".Families[" + i + "].Id");
                Equal(left.Name, right.Name, label + ".Families[" + i + "].Name");
                Equal(left.Category, right.Category, label + ".Families[" + i + "].Category");
                StringDictionary(left.Properties, right.Properties, label + ".Families[" + i + "].Properties");
            }

            Equal(expected.Elements.Count, actual.Elements.Count, label + ".Elements.Count");
            for (var i = 0; i < expected.Elements.Count; i++)
            {
                var left = expected.Elements[i];
                var right = actual.Elements[i];
                Equal(left.Id, right.Id, label + ".Elements[" + i + "].Id");
                Equal(left.Category, right.Category, label + ".Elements[" + i + "].Category");
                Equal(left.FamilyId, right.FamilyId, label + ".Elements[" + i + "].FamilyId");
                Equal(left.FloorId, right.FloorId, label + ".Elements[" + i + "].FloorId");
                Equal(left.ZoneId, right.ZoneId, label + ".Elements[" + i + "].ZoneId");
                Equal(left.DrawingFingerprint, right.DrawingFingerprint, label + ".Elements[" + i + "].DrawingFingerprint");
                Equal(left.Dirty, right.Dirty, label + ".Elements[" + i + "].Dirty");
                Equal(left.UpdatedUtc, right.UpdatedUtc, label + ".Elements[" + i + "].UpdatedUtc");
                StringList(left.SourceHandles, right.SourceHandles, label + ".Elements[" + i + "].SourceHandles");
                StringList(left.DependsOn, right.DependsOn, label + ".Elements[" + i + "].DependsOn");
                StringDictionary(left.Properties, right.Properties, label + ".Elements[" + i + "].Properties");
                DoubleDictionary(left.Quantities, right.Quantities, label + ".Elements[" + i + "].Quantities");
            }

            Equal(expected.QuantityRules.Count, actual.QuantityRules.Count, label + ".QuantityRules.Count");
            for (var i = 0; i < expected.QuantityRules.Count; i++)
            {
                var left = expected.QuantityRules[i];
                var right = actual.QuantityRules[i];
                Equal(left.Id, right.Id, label + ".QuantityRules[" + i + "].Id");
                Equal(left.Category, right.Category, label + ".QuantityRules[" + i + "].Category");
                Equal(left.OutputName, right.OutputName, label + ".QuantityRules[" + i + "].OutputName");
                Equal(left.Expression, right.Expression, label + ".QuantityRules[" + i + "].Expression");
                Equal(left.Version, right.Version, label + ".QuantityRules[" + i + "].Version");
            }

            Equal(expected.AuditEvents.Count, actual.AuditEvents.Count, label + ".AuditEvents.Count");
            for (var i = 0; i < expected.AuditEvents.Count; i++)
            {
                var left = expected.AuditEvents[i];
                var right = actual.AuditEvents[i];
                Equal(left.Utc, right.Utc, label + ".AuditEvents[" + i + "].Utc");
                Equal(left.Action, right.Action, label + ".AuditEvents[" + i + "].Action");
                Equal(left.ElementId, right.ElementId, label + ".AuditEvents[" + i + "].ElementId");
                Equal(left.Detail, right.Detail, label + ".AuditEvents[" + i + "].Detail");
                Equal(left.Actor, right.Actor, label + ".AuditEvents[" + i + "].Actor");
                Equal(left.CorrelationId, right.CorrelationId, label + ".AuditEvents[" + i + "].CorrelationId");
            }

            StringDictionary(expected.Metadata, actual.Metadata, label + ".Metadata");
        }

        private static void StringList(IList<string> expected, IList<string> actual, string label)
        {
            Equal(expected.Count, actual.Count, label + ".Count");
            for (var i = 0; i < expected.Count; i++) Equal(expected[i], actual[i], label + "[" + i + "]");
        }

        private static void StringDictionary(IDictionary<string, string> expected, IDictionary<string, string> actual, string label)
        {
            Equal(expected.Count, actual.Count, label + ".Count");
            var leftKeys = new List<string>(expected.Keys);
            var rightKeys = new List<string>(actual.Keys);
            leftKeys.Sort(StringComparer.Ordinal);
            rightKeys.Sort(StringComparer.Ordinal);
            Equal(leftKeys.Count, rightKeys.Count, label + ".KeyCount");
            for (var i = 0; i < leftKeys.Count; i++)
            {
                Equal(leftKeys[i], rightKeys[i], label + ".Key[" + i + "]");
                Equal(expected[leftKeys[i]], actual[rightKeys[i]], label + "[" + leftKeys[i] + "]");
            }
        }

        private static void DoubleDictionary(IDictionary<string, double> expected, IDictionary<string, double> actual, string label)
        {
            Equal(expected.Count, actual.Count, label + ".Count");
            var leftKeys = new List<string>(expected.Keys);
            var rightKeys = new List<string>(actual.Keys);
            leftKeys.Sort(StringComparer.Ordinal);
            rightKeys.Sort(StringComparer.Ordinal);
            Equal(leftKeys.Count, rightKeys.Count, label + ".KeyCount");
            for (var i = 0; i < leftKeys.Count; i++)
            {
                Equal(leftKeys[i], rightKeys[i], label + ".Key[" + i + "]");
                Equal(expected[leftKeys[i]], actual[rightKeys[i]], label + "[" + leftKeys[i] + "]");
            }
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + " mismatch. Expected '" + expected + "', actual '" + actual + "'.");
        }
    }
}
