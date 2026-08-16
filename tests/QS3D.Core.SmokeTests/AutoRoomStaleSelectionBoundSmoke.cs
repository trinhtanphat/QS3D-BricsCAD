using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomStaleSelectionBoundSmoke
    {
        private const int MaximumSourceHandles = 5000;

        internal static void Run()
        {
            OversizeInputFailsBeforeMutation();
            ExactBoundaryRemainsAccepted();
        }

        private static void OversizeInputFailsBeforeMutation()
        {
            var project = NewProject();
            var handles = Handles(MaximumSourceHandles + 1);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            var error = Capture<InvalidOperationException>(() => AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                handles,
                "f",
                "z",
                new DateTime(2026, 8, 16, 15, 0, 0, DateTimeKind.Utc)));

            Contains("cannot exceed 5000", error.Message, "Oversize stale-selection input must report the existing Auto Room source-handle bound.");
            Equal(beforeVersion, project.ChangeVersion, "Oversize stale-selection input must fail before project mutation.");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "Oversize stale-selection input must preserve project timestamps.");
        }

        private static void ExactBoundaryRemainsAccepted()
        {
            var project = NewProject();
            var handles = Handles(MaximumSourceHandles);
            var beforeVersion = project.ChangeVersion;

            var stale = AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                handles,
                "f",
                "z",
                new DateTime(2026, 8, 16, 15, 0, 0, DateTimeKind.Utc));

            Equal(0, stale.Count, "Exactly 5000 stale-selection source handles must remain accepted.");
            Equal(beforeVersion, project.ChangeVersion, "A boundary-sized selection with no matching rooms must not mutate the project.");
        }

        private static HashSet<string> Handles(int count)
        {
            var handles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < count; index++)
                handles.Add("H" + index.ToString("D5", System.Globalization.CultureInfo.InvariantCulture));
            return handles;
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("p-stale-bound", "AutoRoom stale selection bound");
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.ActiveFloorId = "f";
            project.ActiveZoneId = "z";
            project.Families.Add(new ProjectFamily("room", "Room", ElementCategory.Room));
            return project;
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception(message + " Actual: " + (actual ?? "<null>"));
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
