using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimatingKnownCountOverrunOrderingSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            PortfolioOverrunPrecedesUnexpectedLineValidation();
            SelectedLineOverrunPrecedesTokenValidation();
            UnitRateOverrunPrecedesUnexpectedAssignmentValidation();
            UnderTraversalStillFailsAfterOtherwiseValidEnumeration();
            HonestCountedInputsRemainAccepted();
        }

        private static void PortfolioOverrunPrecedesUnexpectedLineValidation()
        {
            var source = new MisreportedReadOnlyCollection<EstimatingLine>(
                1,
                Line("PORT-1"),
                null!);

            var error = Capture<InvalidOperationException>(() => new EstimatingPortfolio(source));
            Contains("line count changed during enumeration", error.Message,
                "Estimating portfolio must reject known-Count overrun before validating the unexpected null line.");
        }

        private static void SelectedLineOverrunPrecedesTokenValidation()
        {
            var lineIds = new MisreportedReadOnlyCollection<string>(1, "LINE-1", "");
            var error = Capture<InvalidOperationException>(() =>
                new BulkRateAssignmentRequest(
                    lineIds,
                    "cost-code",
                    "rate-source",
                    "rate-revision",
                    new[] { new UnitRateAssignment("m", 1m) }));

            Contains("selected-line count changed during enumeration", error.Message,
                "Selected-line Count overrun must win before token validation of the unexpected item.");
        }

        private static void UnitRateOverrunPrecedesUnexpectedAssignmentValidation()
        {
            var unitRates = new MisreportedReadOnlyCollection<UnitRateAssignment>(
                1,
                new UnitRateAssignment("m", 1m),
                null!);
            var error = Capture<InvalidOperationException>(() =>
                new BulkRateAssignmentRequest(
                    new[] { "LINE-1" },
                    "cost-code",
                    "rate-source",
                    "rate-revision",
                    unitRates));

            Contains("unit-rate count changed during enumeration", error.Message,
                "Unit-rate Count overrun must win before null validation of the unexpected assignment.");
        }

        private static void UnderTraversalStillFailsAfterOtherwiseValidEnumeration()
        {
            var portfolioError = Capture<InvalidOperationException>(() =>
                new EstimatingPortfolio(
                    new MisreportedReadOnlyCollection<EstimatingLine>(2, Line("UNDER-PORT"))));
            Contains("line count changed during enumeration", portfolioError.Message,
                "Portfolio under-traversal must retain its post-enumeration mismatch rejection.");

            var selectedError = Capture<InvalidOperationException>(() =>
                new BulkRateAssignmentRequest(
                    new MisreportedReadOnlyCollection<string>(2, "LINE-1"),
                    "cost-code",
                    "rate-source",
                    "rate-revision",
                    new[] { new UnitRateAssignment("m", 1m) }));
            Contains("selected-line count changed during enumeration", selectedError.Message,
                "Selected-line under-traversal must retain its post-enumeration mismatch rejection.");

            var rateError = Capture<InvalidOperationException>(() =>
                new BulkRateAssignmentRequest(
                    new[] { "LINE-1" },
                    "cost-code",
                    "rate-source",
                    "rate-revision",
                    new MisreportedReadOnlyCollection<UnitRateAssignment>(
                        2,
                        new UnitRateAssignment("m", 1m))));
            Contains("unit-rate count changed during enumeration", rateError.Message,
                "Unit-rate under-traversal must retain its post-enumeration mismatch rejection.");
        }

        private static void HonestCountedInputsRemainAccepted()
        {
            var portfolio = new EstimatingPortfolio(
                new MisreportedReadOnlyCollection<EstimatingLine>(2, Line("b"), Line("A")));
            Equal("A", portfolio.Lines[0].LineId,
                "Honest counted portfolio input must retain deterministic case-insensitive sorting.");
            Equal("b", portfolio.Lines[1].LineId,
                "Honest counted portfolio input must retain deterministic case-insensitive sorting.");

            var request = new BulkRateAssignmentRequest(
                new MisreportedReadOnlyCollection<string>(2, "LINE-2", "LINE-1"),
                "cost-code",
                "rate-source",
                "rate-revision",
                new MisreportedReadOnlyCollection<UnitRateAssignment>(
                    2,
                    new UnitRateAssignment("m", 1m),
                    new UnitRateAssignment("m2", 2m)));
            Equal(2, request.LineIds.Count, "Honest selected-line Count must remain accepted.");
            Equal(2, request.UnitRates.Count, "Honest unit-rate Count must remain accepted.");
        }

        private static EstimatingLine Line(string id)
        {
            return new EstimatingLine(id, "quantity-source", "quantity-revision", 1m, "m");
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new Exception(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private sealed class MisreportedReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;

            internal MisreportedReadOnlyCollection(int reportedCount, params T[] items)
            {
                Count = reportedCount;
                _items = items;
            }

            public int Count { get; }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                    yield return _items[i];
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
