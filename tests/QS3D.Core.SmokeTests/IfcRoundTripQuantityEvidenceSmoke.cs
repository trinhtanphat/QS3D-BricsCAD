using System;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripQuantityEvidenceSmoke
    {
        internal static void Run()
        {
            CanonicalizesEvidenceAndCollapsesExactDuplicates();
            ReportsConflictingEvidenceAsAmbiguous();
            RejectsMalformedEvidenceAndCanonicalizesZero();
            ProjectionRetainsEvidenceAndComparesDeterministically();
        }

        private static void CanonicalizesEvidenceAndCollapsesExactDuplicates()
        {
            var area = new IfcRoundTripQuantityEvidence(
                "NetArea",
                12.5d,
                "m2",
                "ifc-qto-01",
                "source:base-quantities");
            var exactDuplicate = new IfcRoundTripQuantityEvidence(
                "NetArea",
                12.5d,
                "m2",
                "ifc-qto-01",
                "source:base-quantities");
            var length = new IfcRoundTripQuantityEvidence(
                "Length",
                5d,
                "m",
                "ifc-qto-02",
                "source:base-quantities");

            var set = IfcRoundTripQuantityEvidenceSet.Create(new[] { area, length, exactDuplicate });

            Require(set.Groups.Count == 2, "IFC quantity evidence grouping lost canonical identities.");
            Require(set.CandidateCount == 2, "Exact duplicate IFC quantity evidence was counted twice.");
            Require(!set.HasAmbiguity, "Exact duplicate IFC quantity evidence was incorrectly marked ambiguous.");
            Require(set.Groups[0].QuantityKey == "Length", "IFC quantity evidence group ordering is not deterministic.");
            Require(set.Groups[1].QuantityKey == "NetArea", "IFC quantity evidence group ordering is not deterministic.");
            Require(set.Groups[1].Candidates.Count == 1, "Exact duplicate IFC quantity evidence was not collapsed.");
            Require(set.Groups[1].Candidates[0].ExternalSourceIdentity == "ifc-qto-01", "IFC quantity evidence lost external source identity.");
            Require(set.Groups[1].Candidates[0].ProvenanceIdentity == "source:base-quantities", "IFC quantity evidence lost provenance identity.");
        }

        private static void ReportsConflictingEvidenceAsAmbiguous()
        {
            var first = new IfcRoundTripQuantityEvidence(
                "NetVolume",
                4d,
                "m3",
                "ifc-qto-volume",
                "source:qto-a");
            var conflictingValue = new IfcRoundTripQuantityEvidence(
                "NetVolume",
                4.1d,
                "m3",
                "ifc-qto-volume",
                "source:qto-a");
            var conflictingUnit = new IfcRoundTripQuantityEvidence(
                "NetVolume",
                4000d,
                "L",
                "ifc-qto-volume",
                "source:qto-a");

            var forward = IfcRoundTripQuantityEvidenceSet.Create(new[] { first, conflictingValue, conflictingUnit });
            var reverse = IfcRoundTripQuantityEvidenceSet.Create(new[] { conflictingUnit, conflictingValue, first });

            Require(forward.Groups.Count == 1, "Conflicting IFC quantity evidence split one evidence identity into multiple groups.");
            Require(reverse.Groups.Count == 1, "Reverse IFC quantity evidence order changed grouping.");
            Require(forward.Groups[0].IsAmbiguous, "Conflicting IFC quantity evidence was not marked ambiguous.");
            Require(reverse.Groups[0].IsAmbiguous, "Reverse conflicting IFC quantity evidence was not marked ambiguous.");
            Require(forward.Groups[0].Candidates.Count == 3, "Conflicting IFC quantity evidence silently selected one candidate.");
            Require(reverse.Groups[0].Candidates.Count == 3, "Reverse conflicting IFC quantity evidence silently selected one candidate.");

            for (var index = 0; index < forward.Groups[0].Candidates.Count; index++)
            {
                var left = forward.Groups[0].Candidates[index];
                var right = reverse.Groups[0].Candidates[index];
                Require(left.QuantityKey == right.QuantityKey, "IFC quantity evidence candidate key ordering changed with input order.");
                Require(left.Value.Equals(right.Value), "IFC quantity evidence candidate value ordering changed with input order.");
                Require(left.Unit == right.Unit, "IFC quantity evidence candidate unit ordering changed with input order.");
                Require(left.ProvenanceIdentity == right.ProvenanceIdentity, "IFC quantity evidence candidate provenance ordering changed with input order.");
            }
        }

        private static void RejectsMalformedEvidenceAndCanonicalizesZero()
        {
            Throws<ArgumentException>(() => new IfcRoundTripQuantityEvidence(
                " NetArea ",
                1d,
                "m2",
                "ifc-qto-01",
                "source:qto"));
            Throws<ArgumentException>(() => new IfcRoundTripQuantityEvidence(
                "NetArea",
                1d,
                string.Empty,
                "ifc-qto-01",
                "source:qto"));
            Throws<ArgumentOutOfRangeException>(() => new IfcRoundTripQuantityEvidence(
                "NetArea",
                double.NaN,
                "m2",
                "ifc-qto-01",
                "source:qto"));
            Throws<ArgumentOutOfRangeException>(() => new IfcRoundTripQuantityEvidence(
                "NetArea",
                double.PositiveInfinity,
                "m2",
                "ifc-qto-01",
                "source:qto"));
            Throws<ArgumentException>(() => IfcRoundTripQuantityEvidenceSet.Create(new IfcRoundTripQuantityEvidence[] { null! }));

            var zero = new IfcRoundTripQuantityEvidence(
                "CountDelta",
                -0d,
                "count",
                "ifc-qto-count",
                "source:qto");
            Require(BitConverter.DoubleToInt64Bits(zero.Value) == 0L, "IFC quantity evidence did not canonicalize signed zero.");
        }

        private static void ProjectionRetainsEvidenceAndComparesDeterministically()
        {
            var legacy = new IfcRoundTripProjection(
                "BEAM-LEGACY",
                "ifc-beam-legacy",
                "IfcBeam",
                new[] { new IfcRoundTripNumericProperty("Length", 5d, "m") },
                5d,
                "m",
                new[] { "source:model" });
            Require(legacy.QuantityEvidence.Groups.Count == 0, "Legacy IFC projection constructor invented quantity evidence.");

            var expected = CreateProjection(
                new[]
                {
                    new IfcRoundTripQuantityEvidence("NetArea", 12.5d, "m2", "ifc-qto-area", "source:qto"),
                    new IfcRoundTripQuantityEvidence("Length", 5d, "m", "ifc-qto-length", "source:qto")
                });
            var reconstructed = CreateProjection(
                new[]
                {
                    new IfcRoundTripQuantityEvidence("Length", 5.0000004d, "m", "ifc-qto-length", "source:qto"),
                    new IfcRoundTripQuantityEvidence("NetArea", 12.5000004d, "m2", "ifc-qto-area", "source:qto")
                });

            Require(expected.QuantityEvidence.Groups.Count == 2, "IFC projection lost declared quantity evidence.");
            Require(
                IfcRoundTripProjectionComparer.AreEquivalent(expected, reconstructed, 0.000001d),
                "IFC projection comparison rejected quantity evidence inside tolerance.");
            Require(
                !IfcRoundTripProjectionComparer.AreEquivalent(expected, reconstructed, 0.00000001d),
                "IFC projection comparison accepted quantity evidence outside tolerance.");

            var sourceDrift = CreateProjection(
                new[]
                {
                    new IfcRoundTripQuantityEvidence("Length", 5d, "m", "ifc-qto-length-other", "source:qto"),
                    new IfcRoundTripQuantityEvidence("NetArea", 12.5d, "m2", "ifc-qto-area", "source:qto")
                });
            Require(
                !IfcRoundTripProjectionComparer.AreEquivalent(expected, sourceDrift, 0d),
                "IFC projection comparison ignored quantity evidence source-identity drift.");

            var provenanceDrift = CreateProjection(
                new[]
                {
                    new IfcRoundTripQuantityEvidence("Length", 5d, "m", "ifc-qto-length", "source:qto-other"),
                    new IfcRoundTripQuantityEvidence("NetArea", 12.5d, "m2", "ifc-qto-area", "source:qto")
                });
            Require(
                !IfcRoundTripProjectionComparer.AreEquivalent(expected, provenanceDrift, 0d),
                "IFC projection comparison ignored quantity evidence provenance drift.");
        }

        private static IfcRoundTripProjection CreateProjection(IfcRoundTripQuantityEvidence[] evidence)
        {
            return new IfcRoundTripProjection(
                "BEAM-01",
                "ifc-beam-01",
                "IfcBeam",
                new[] { new IfcRoundTripNumericProperty("Length", 5d, "m") },
                12.5d,
                "m2",
                new[] { "source:model" },
                evidence);
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
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

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
