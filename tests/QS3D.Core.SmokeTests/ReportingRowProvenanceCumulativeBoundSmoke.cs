using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ReportingRowProvenanceCumulativeBoundSmoke
    {
        private const int MaximumPublishedHandles = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactCumulativeBoundaryRemainsAccepted();
            CumulativeOverrunFailsAtomically();
            AlreadyOversizeTargetFailsBeforeSourceTraversal();
            OrdinaryAppendRemainsAccepted();
        }

        private static void ExactCumulativeBoundaryRemainsAccepted()
        {
            var target = Handles(MaximumPublishedHandles - 1);
            Append(target, new[] { "F000" });
            Equal(MaximumPublishedHandles, target.Count, "exact cumulative boundary");
            Equal("F000", target[target.Count - 1], "exact cumulative boundary appended handle");
        }

        private static void CumulativeOverrunFailsAtomically()
        {
            var target = Handles(MaximumPublishedHandles - 1);
            var before = new List<string>(target);
            ThrowsContaining(() => Append(target, new[] { "F000", "F001" }), "cannot exceed 10000 published entries");
            Equal(before.Count, target.Count, "overrun target count");
            for (var i = 0; i < before.Count; i++)
                Equal(before[i], target[i], "overrun target value " + i.ToString(CultureInfo.InvariantCulture));
        }

        private static void AlreadyOversizeTargetFailsBeforeSourceTraversal()
        {
            var target = Handles(MaximumPublishedHandles + 1);
            var source = new EnumeratorForbiddenSource();
            ThrowsContaining(() => Append(target, source), "cannot exceed 10000 published entries");
            if (source.EnumeratorRequested)
                throw new InvalidOperationException("Oversize provenance target requested source enumeration before admission rejection.");
            Equal(MaximumPublishedHandles + 1, target.Count, "already oversize target remains unchanged");
        }

        private static void OrdinaryAppendRemainsAccepted()
        {
            var target = new List<string> { "AA" };
            Append(target, new[] { "BB", "CC" });
            Equal(3, target.Count, "ordinary target count");
            Equal("AA", target[0], "ordinary seed");
            Equal("BB", target[1], "ordinary first append");
            Equal("CC", target[2], "ordinary second append");
        }

        private static List<string> Handles(int count)
        {
            var values = new List<string>(count);
            for (var i = 0; i < count; i++)
                values.Add((i + 1).ToString("X", CultureInfo.InvariantCulture));
            return values;
        }

        private static void Append(IList<string> target, IEnumerable<string> source)
        {
            var type = typeof(DoorOpeningScheduleBuilder).Assembly.GetType("QS3D.Core.Reporting.ReportingRowProvenance", throwOnError: true)!;
            var method = type.GetMethod("AppendSourceHandles", BindingFlags.Static | BindingFlags.NonPublic)!;
            try
            {
                method.Invoke(null, new object[] { target, source });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void ThrowsContaining(Action action, string expectedText)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException("Expected cumulative provenance failure containing '" + expectedText + "', got '" + ex.Message + "'.", ex);
            }
            throw new InvalidOperationException("Expected cumulative provenance failure containing '" + expectedText + "'.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    "ReportingRowProvenanceCumulativeBoundSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class EnumeratorForbiddenSource : IEnumerable<string>
        {
            internal bool EnumeratorRequested { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new InvalidOperationException("Source enumeration must not begin for an already-oversize provenance target.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
