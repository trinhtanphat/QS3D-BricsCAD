using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Scheduling;

namespace QS3D.Core.SmokeTests
{
    internal static class ScheduleSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Utf16IdentityValidationFailsClosed();
        }

        private static void Utf16IdentityValidationFailsClosed()
        {
            var start = new DateTime(2026, 8, 27, 8, 0, 0, DateTimeKind.Unspecified);
            var finish = start.AddHours(8);

            Throws<ArgumentException>(() => new ScheduleActivity(
                "ACT-\uD800", "Activity", start, finish, "CAL", "1"));
            Throws<ArgumentException>(() => new ScheduleActivity(
                "ACT-\uDC00", "Activity", start, finish, "CAL", "1"));
            Throws<ArgumentException>(() => new ScheduleActivity(
                "ACT", "Activity-\uD800", start, finish, "CAL", "1"));
            Throws<ArgumentException>(() => new ScheduleSnapshot(
                "SCH", "1", "ALLOC-1", "TZ-\uDC00", start.Date,
                new[] { new ScheduleActivity("ACT", "Activity", start, finish, "CAL", "1") }));

            const string validId = "ACT-😀";
            const string validName = "Activity-🚀";
            const string validCalendar = "CAL-😀";
            const string validCalendarVersion = "V-🚀";
            const string validScheduleId = "SCH-😀";
            const string validTimeZoneId = "TZ-🚀";

            var activity = new ScheduleActivity(
                validId,
                validName,
                start,
                finish,
                validCalendar,
                validCalendarVersion);
            var snapshot = new ScheduleSnapshot(
                validScheduleId,
                "VER-🚀",
                "ALLOC-😀",
                validTimeZoneId,
                start.Date,
                new[] { activity });

            Equal(validId, activity.Id, "valid supplementary activity ID was not preserved");
            Equal(validName, activity.Name, "valid supplementary activity name was not preserved");
            Equal(validCalendar, activity.CalendarId, "valid supplementary calendar ID was not preserved");
            Equal(validCalendarVersion, activity.CalendarVersion, "valid supplementary calendar version was not preserved");
            Equal(validScheduleId, snapshot.ScheduleId, "valid supplementary schedule ID was not preserved");
            Equal(validTimeZoneId, snapshot.ProjectTimeZoneId, "valid supplementary timezone ID was not preserved");

            var canonical = snapshot.ToCanonicalString();
            if (!canonical.Contains(validId, StringComparison.Ordinal) ||
                !canonical.Contains(validName, StringComparison.Ordinal) ||
                !canonical.Contains(validScheduleId, StringComparison.Ordinal) ||
                !canonical.Contains(validTimeZoneId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "ScheduleSnapshotSmoke: valid supplementary Unicode was not preserved in canonical scheduling text.");
            }
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "ScheduleSnapshotSmoke: " + message + ". Expected '" + expected + "', actual '" + actual + "'.");
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
                throw new InvalidOperationException(
                    "ScheduleSnapshotSmoke: expected " + typeof(TException).Name + ", got " + ex.GetType().Name + ".",
                    ex);
            }

            throw new InvalidOperationException(
                "ScheduleSnapshotSmoke: expected " + typeof(TException).Name + " but no exception was thrown.");
        }
    }
}
