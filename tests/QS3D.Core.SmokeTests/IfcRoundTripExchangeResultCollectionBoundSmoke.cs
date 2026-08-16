using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripExchangeResultCollectionBoundSmoke
    {
        private const int MaxResultsPerCollection = 10_000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OversizedKnownCountFailsBeforeEnumeration();
            DishonestCountStopsAtFirstDisallowedRecord();
            ExactBoundIsAccepted();
            DuplicateCollapseAndOrderingRemainCanonical();
            NullContractsRemainUnchanged();
        }

        private static void OversizedKnownCountFailsBeforeEnumeration()
        {
            var results = new EnumerationFailCollection(MaxResultsPerCollection + 1);

            ThrowsBound(() => IfcRoundTripExchangeResultSet.Create(results));

            Require(!results.EnumerationAttempted,
                "Oversized IFC exchange result collection was enumerated before known-count rejection.");
        }

        private static void DishonestCountStopsAtFirstDisallowedRecord()
        {
            var results = new DishonestCountCollection(MaxResultsPerCollection + 50, reportedCount: 1);

            ThrowsBound(() => IfcRoundTripExchangeResultSet.Create(results));

            Require(results.YieldedCount == MaxResultsPerCollection + 1,
                "Streaming IFC exchange result bound must stop exactly on input record 10,001.");
        }

        private static void ExactBoundIsAccepted()
        {
            var results = new DishonestCountCollection(MaxResultsPerCollection, reportedCount: MaxResultsPerCollection);

            var set = IfcRoundTripExchangeResultSet.Create(results);

            Require(results.YieldedCount == MaxResultsPerCollection,
                "Exact-bound IFC exchange result sequence was not consumed completely.");
            Require(set.Items.Count == MaxResultsPerCollection,
                "Exact-bound IFC exchange result sequence changed result cardinality.");
        }

        private static void DuplicateCollapseAndOrderingRemainCanonical()
        {
            var set = IfcRoundTripExchangeResultSet.Create(new[]
            {
                CreateResult("B", IfcRoundTripResultState.Unmapped),
                CreateResult("A", IfcRoundTripResultState.Unsupported),
                CreateResult("B", IfcRoundTripResultState.Unsupported)
            });

            Require(set.Items.Count == 2, "Duplicate external IFC identity was not collapsed.");
            Require(set.Items[0].ExternalObjectId == "A" && set.Items[0].State == IfcRoundTripResultState.Unsupported,
                "IFC exchange result ordering or unique-item state changed.");
            Require(set.Items[1].ExternalObjectId == "B" &&
                    set.Items[1].State == IfcRoundTripResultState.InvalidOrAmbiguous &&
                    set.Items[1].StateDetail == IfcRoundTripExchangeResultSet.DuplicateExternalIdentityDetail,
                "Duplicate external IFC identity no longer becomes canonical InvalidOrAmbiguous evidence.");
        }

        private static void NullContractsRemainUnchanged()
        {
            Throws<ArgumentNullException>(() => IfcRoundTripExchangeResultSet.Create(null!));
            Throws<ArgumentException>(() => IfcRoundTripExchangeResultSet.Create(new IfcRoundTripExchangeResult[] { null! }));
        }

        private static IfcRoundTripExchangeResult CreateResult(string id, IfcRoundTripResultState state)
        {
            return new IfcRoundTripExchangeResult(id, state, projection: null);
        }

        private static void ThrowsBound(Action action)
        {
            try
            {
                action();
                throw new Exception("Oversized IFC exchange result input must fail closed.");
            }
            catch (InvalidOperationException ex)
            {
                var expected = "IFC exchange result collection cannot exceed " + MaxResultsPerCollection + " input records.";
                if (!string.Equals(ex.Message, expected, StringComparison.Ordinal))
                    throw new Exception("Unexpected IFC exchange result bound diagnostic: " + ex.Message);
            }
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
                throw new Exception("Expected " + typeof(TException).Name + ".");
            }
            catch (TException)
            {
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private sealed class EnumerationFailCollection : IReadOnlyCollection<IfcRoundTripExchangeResult>
        {
            public EnumerationFailCollection(int count) => Count = count;

            public int Count { get; }
            public bool EnumerationAttempted { get; private set; }

            public IEnumerator<IfcRoundTripExchangeResult> GetEnumerator()
            {
                EnumerationAttempted = true;
                throw new InvalidOperationException("Known-oversized input must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class DishonestCountCollection : IReadOnlyCollection<IfcRoundTripExchangeResult>
        {
            private readonly int _actualCount;
            private readonly int _reportedCount;

            public DishonestCountCollection(int actualCount, int reportedCount)
            {
                _actualCount = actualCount;
                _reportedCount = reportedCount;
            }

            public int Count => _reportedCount;
            public int YieldedCount { get; private set; }

            public IEnumerator<IfcRoundTripExchangeResult> GetEnumerator()
            {
                for (var index = 0; index < _actualCount; index++)
                {
                    YieldedCount++;
                    yield return CreateResult("R" + index.ToString("D5"), IfcRoundTripResultState.Unmapped);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
