using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionMarkerRecordCodecBoundSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            EncodedGridIdExactBoundaryRoundTrips();
            RawRecordMaxPlusOneFailsBeforeSplit();
            EncodedGridIdMaxPlusOneFailsBeforeBase64();
            MalformedPairTokenFailsBeforePayloadDecode();
            OversizedMetadataKeyFailsWithoutPayloadDecode();
            MarkerOwnerShapeFailsAtEntryBoundary();
            DecodedMarkerValidationFailuresAreFormatErrors();
        }

        private static void EncodedGridIdExactBoundaryRoundTrips()
        {
            const string firstId = "A";
            var secondId = new string('\u0800', 128);
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
                        new Point2(1.25, -2.5),
                        3.75)
                });

            var encoded = GridIntersectionMarkerRecordCodec.Encode(record);
            var fields = encoded.Split('|');
            Equal(GridIntersectionMarkerRecordCodec.MaxEncodedGridIdCharacters, fields[2].Length, "Exact encoded Grid-id boundary changed.");
            var decoded = GridIntersectionMarkerRecordCodec.Decode(GridIntersectionMarkerRecordCodec.MetadataKey(pair), encoded);
            Equal(secondId, decoded.SecondElementId, "Exact encoded Grid-id boundary did not round-trip.");
        }

        private static void RawRecordMaxPlusOneFailsBeforeSplit()
        {
            var pair = GridIntersectionIdentityPlanner.BuildPairToken("A", "B");
            var key = GridIntersectionMarkerRecordCodec.MetadataKey(pair);
            RejectFormat(
                () => GridIntersectionMarkerRecordCodec.Decode(key, new string('X', GridIntersectionMarkerRecordCodec.MaxRecordCharacters + 1)),
                "maximum record length");
        }

        private static void EncodedGridIdMaxPlusOneFailsBeforeBase64()
        {
            var pair = GridIntersectionIdentityPlanner.BuildPairToken("A", "B");
            var owner = GridIntersectionIdentityPlanner.BuildIntersectionOwner("A", "B", 0);
            var oversized = new string('A', GridIntersectionMarkerRecordCodec.MaxEncodedGridIdCharacters + 1);
            var value = "1|" + oversized + "|Qg==|0," + owner + ",A1,1,2,3";
            RejectFormat(
                () => GridIntersectionMarkerRecordCodec.Decode(GridIntersectionMarkerRecordCodec.MetadataKey(pair), value),
                "maximum encoded length");
        }

        private static void MalformedPairTokenFailsBeforePayloadDecode()
        {
            var invalidPair = "GIP1:" + new string('G', 64);
            RejectArgument(
                () => GridIntersectionMarkerRecordCodec.Decode(GridIntersectionMarkerRecordCodec.MetadataPrefix + invalidPair, "BAD"),
                "lowercase hexadecimal");
        }

        private static void OversizedMetadataKeyFailsWithoutPayloadDecode()
        {
            var oversizedKey = GridIntersectionMarkerRecordCodec.MetadataPrefix + "GIP1:" + new string('0', 256);
            RejectArgument(
                () => GridIntersectionMarkerRecordCodec.Decode(oversizedKey, "BAD"),
                "pair-token length");
        }

        private static void MarkerOwnerShapeFailsAtEntryBoundary()
        {
            RejectArgument(
                () => new GridIntersectionMarkerRecordEntry(0, new string('A', 1024), "A1", new Point2(1, 2), 3),
                "canonical GIX1");
            RejectArgument(
                () => new GridIntersectionMarkerRecordEntry(0, "GIX1:" + new string('A', 64) + ":0", "A1", new Point2(1, 2), 3),
                "lowercase hexadecimal");
            RejectArgument(
                () => new GridIntersectionMarkerRecordEntry(0, "GIX1:" + new string('0', 64) + ":1", "A1", new Point2(1, 2), 3),
                "canonical GIX1");
        }

        private static void DecodedMarkerValidationFailuresAreFormatErrors()
        {
            const string firstId = "A";
            const string secondId = "B";
            var pair = GridIntersectionIdentityPlanner.BuildPairToken(firstId, secondId);
            var key = GridIntersectionMarkerRecordCodec.MetadataKey(pair);
            var owner = GridIntersectionIdentityPlanner.BuildIntersectionOwner(firstId, secondId, 0);
            const string firstEncoded = "QQ==";
            const string secondEncoded = "Qg==";

            RejectFormat(
                () => GridIntersectionMarkerRecordCodec.Decode(key, "1|" + firstEncoded + "|" + secondEncoded + "|2," + owner + ",A1,1,2,3"),
                "invalid marker data");
            RejectFormat(
                () => GridIntersectionMarkerRecordCodec.Decode(key, "1|" + firstEncoded + "|" + secondEncoded + "|0,GIX1:" + new string('0', 64) + ":1,A1,1,2,3"),
                "invalid marker data");
            RejectFormat(
                () => GridIntersectionMarkerRecordCodec.Decode(key, "1|" + firstEncoded + "|" + secondEncoded + "|0," + owner + ",0,1,2,3"),
                "invalid marker data");
            RejectFormat(
                () => GridIntersectionMarkerRecordCodec.Decode(key, "1|" + firstEncoded + "|" + secondEncoded + "|0," + owner + ",A1,NaN,2,3"),
                "invalid marker data");
            RejectFormat(
                () => GridIntersectionMarkerRecordCodec.Decode(key, "1|" + firstEncoded + "|" + secondEncoded + "|0," + owner + ",A1,1,Infinity,3"),
                "invalid marker data");

            var valid = GridIntersectionMarkerRecordCodec.Decode(
                key,
                "1|" + firstEncoded + "|" + secondEncoded + "|0," + owner + ",A1,1.25,-2.5,3.75");
            Equal(1, valid.Entries.Count, "Valid finite decoded marker count changed.");
            Equal("A1", valid.Entries[0].Handle, "Valid finite decoded marker handle changed.");
            Equal(1.25d, valid.Entries[0].Point.X, "Valid finite decoded marker X changed.");
            Equal(-2.5d, valid.Entries[0].Point.Y, "Valid finite decoded marker Y changed.");
            Equal(3.75d, valid.Entries[0].Elevation, "Valid finite decoded marker elevation changed.");
        }

        private static void RejectFormat(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (FormatException ex)
            {
                Contains(ex.Message, expectedMessage, "wrong FormatException");
                return;
            }
            throw new InvalidOperationException("GridIntersectionMarkerRecordCodecBoundSmoke: expected FormatException.");
        }

        private static void RejectArgument(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                Contains(ex.Message, expectedMessage, "wrong ArgumentException");
                return;
            }
            throw new InvalidOperationException("GridIntersectionMarkerRecordCodecBoundSmoke: expected ArgumentException.");
        }

        private static void Contains(string actual, string expected, string label)
        {
            if (actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("GridIntersectionMarkerRecordCodecBoundSmoke: " + label + ": " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("GridIntersectionMarkerRecordCodecBoundSmoke: " + message + " Expected " + expected + ", got " + actual + ".");
        }
    }
}
