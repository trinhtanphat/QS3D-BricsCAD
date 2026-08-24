using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionMarkerPlannerSmoke
    {
        public static void Run()
        {
            PairIdentityIsCanonicalAndOccurrenceScoped();
            InputOrderDoesNotChangePairOwnerIdentity();
            IdentityTokensStayCompactForMaximumIds();
            UnsupportedOccurrenceFailsClosed();
            PairRecordCodecRoundTripsCanonicalOwnership();
            PairRecordCodecRejectsTampering();
            NullIntersectionFailsClosed();
        }

        private static void PairIdentityIsCanonicalAndOccurrenceScoped()
        {
            var planned = GridIntersectionMarkerPlanner.Plan(new[]
            {
                new GridIntersection("GRID-B", "GRID-A", new Point2(1, 2)),
                new GridIntersection("GRID-A", "GRID-B", new Point2(3, 4))
            });

            Equal(2, planned.Count);
            Equal("GRID-A", planned[0].FirstElementId);
            Equal("GRID-B", planned[0].SecondElementId);
            Equal(planned[0].PairToken, planned[1].PairToken);
            Equal(0, planned[0].Occurrence);
            Equal(1, planned[1].Occurrence);
            Equal(GridIntersectionIdentityPlanner.BuildIntersectionOwner("GRID-A", "GRID-B", 0), planned[0].OwnerToken);
            Equal(GridIntersectionIdentityPlanner.BuildIntersectionOwner("GRID-A", "GRID-B", 1), planned[1].OwnerToken);
            True(!string.Equals(planned[0].OwnerToken, planned[1].OwnerToken, StringComparison.Ordinal));
        }

        private static void InputOrderDoesNotChangePairOwnerIdentity()
        {
            var first = GridIntersectionMarkerPlanner.Plan(new[]
            {
                new GridIntersection("G-2", "G-1", new Point2(0, 0))
            })[0];
            var second = GridIntersectionMarkerPlanner.Plan(new[]
            {
                new GridIntersection("G-1", "G-2", new Point2(0, 0))
            })[0];

            Equal(first.PairToken, second.PairToken);
            Equal(first.OwnerToken, second.OwnerToken);
        }

        private static void IdentityTokensStayCompactForMaximumIds()
        {
            var firstId = new string('A', 128);
            var secondId = new string('B', 128);
            var pair = GridIntersectionIdentityPlanner.BuildPairToken(firstId, secondId);
            var owner = GridIntersectionIdentityPlanner.BuildIntersectionOwner(firstId, secondId, 1);

            Equal(69, pair.Length);
            Equal(71, owner.Length);
            True(pair.StartsWith("GIP1:", StringComparison.Ordinal));
            True(owner.StartsWith("GIX1:", StringComparison.Ordinal));
        }

        private static void UnsupportedOccurrenceFailsClosed()
        {
            Throws<ArgumentOutOfRangeException>(() =>
                GridIntersectionIdentityPlanner.BuildIntersectionOwner("GRID-A", "GRID-B", 2));
        }

        private static void PairRecordCodecRoundTripsCanonicalOwnership()
        {
            const string firstId = "GRID-A";
            const string secondId = "LƯỚI-B";
            var pair = GridIntersectionIdentityPlanner.BuildPairToken(firstId, secondId);
            var record = new GridIntersectionPairRecord(
                firstId,
                secondId,
                pair,
                new[]
                {
                    new GridIntersectionMarkerRecordEntry(
                        0,
                        GridIntersectionIdentityPlanner.BuildIntersectionOwner(firstId, secondId, 0),
                        "A1",
                        new Point2(-1.25, 2.5),
                        -0.0d),
                    new GridIntersectionMarkerRecordEntry(
                        1,
                        GridIntersectionIdentityPlanner.BuildIntersectionOwner(firstId, secondId, 1),
                        "B2",
                        new Point2(3.75, -4.5),
                        6.25)
                });

            var key = GridIntersectionMarkerRecordCodec.MetadataKey(pair);
            var encoded = GridIntersectionMarkerRecordCodec.Encode(record);
            var decoded = GridIntersectionMarkerRecordCodec.Decode(key, encoded);

            Equal(firstId, decoded.FirstElementId);
            Equal(secondId, decoded.SecondElementId);
            Equal(pair, decoded.PairToken);
            Equal(2, decoded.Entries.Count);
            Equal("A1", decoded.Entries[0].Handle);
            Equal(0d, decoded.Entries[0].Elevation);
            Equal("B2", decoded.Entries[1].Handle);
            Equal(6.25d, decoded.Entries[1].Elevation);
        }

        private static void PairRecordCodecRejectsTampering()
        {
            const string firstId = "GRID-A";
            const string secondId = "GRID-B";
            var pair = GridIntersectionIdentityPlanner.BuildPairToken(firstId, secondId);
            var owner = GridIntersectionIdentityPlanner.BuildIntersectionOwner(firstId, secondId, 0);
            var record = new GridIntersectionPairRecord(
                firstId,
                secondId,
                pair,
                new[] { new GridIntersectionMarkerRecordEntry(0, owner, "A1", new Point2(1, 2), 3) });
            var key = GridIntersectionMarkerRecordCodec.MetadataKey(pair);
            var encoded = GridIntersectionMarkerRecordCodec.Encode(record);
            var tamperedOwner = "GIX1:" + new string('0', 64) + ":0";

            Throws<FormatException>(() => GridIntersectionMarkerRecordCodec.Decode(key, encoded.Replace(owner, tamperedOwner)));
            Throws<ArgumentException>(() =>
                new GridIntersectionMarkerRecordEntry(0, owner, "0A", new Point2(1, 2), 3));
        }

        private static void NullIntersectionFailsClosed()
        {
            Throws<InvalidOperationException>(() => GridIntersectionMarkerPlanner.Plan(new GridIntersection[] { null! }));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
