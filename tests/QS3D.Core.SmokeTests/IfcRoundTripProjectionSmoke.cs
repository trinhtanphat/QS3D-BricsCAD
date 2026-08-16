using System;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripProjectionSmoke
    {
        internal static void Run()
        {
            CanonicalizesProjectionAndSetOrdering();
            ComparesNumericPayloadWithinTolerance();
            RequiresExactSemanticPayload();
            RejectsMalformedUtf16Tokens();
            RejectsMalformedAndDuplicateState();
        }

        private static void CanonicalizesProjectionAndSetOrdering()
        {
            var beam = CreateProjection(
                "BEAM-02",
                "ifc-beam-02",
                "IfcBeam",
                new[]
                {
                    new IfcRoundTripNumericProperty("Width", 0.3d, "m"),
                    new IfcRoundTripNumericProperty("Length", 5d, "m")
                },
                5d,
                "m",
                new[] { "source:model-b", "source:drawing-a" });

            Require(beam.Dimensions.Count == 2, "IFC round-trip projection lost dimensions.");
            Require(beam.Dimensions[0].Name == "Length", "IFC dimensions are not ordered canonically.");
            Require(beam.Dimensions[1].Name == "Width", "IFC dimensions are not ordered canonically.");
            Require(beam.Provenance[0] == "source:drawing-a", "IFC provenance is not ordered canonically.");
            Require(beam.Provenance[1] == "source:model-b", "IFC provenance is not ordered canonically.");

            var column = CreateProjection(
                "COLUMN-01",
                "ifc-column-01",
                "IfcColumn",
                new[] { new IfcRoundTripNumericProperty("Height", 3.2d, "m") },
                3.2d,
                "m",
                new[] { "source:drawing-a" });

            var set = IfcRoundTripProjectionSet.Create(new[] { column, beam });
            Require(set.Items.Count == 2, "IFC round-trip set lost projections.");
            Require(set.Items[0].IfcGlobalId == "ifc-beam-02", "IFC round-trip set ordering is not deterministic.");
            Require(set.Items[1].IfcGlobalId == "ifc-column-01", "IFC round-trip set ordering is not deterministic.");
        }

        private static void ComparesNumericPayloadWithinTolerance()
        {
            var expected = CreateProjection(
                "BEAM-01",
                "ifc-beam-01",
                "IfcBeam",
                new[]
                {
                    new IfcRoundTripNumericProperty("Length", 5d, "m"),
                    new IfcRoundTripNumericProperty("Width", 0.3d, "m")
                },
                12.5d,
                "m2",
                new[] { "source:drawing-a", "source:model-a" });

            var reconstructed = CreateProjection(
                "BEAM-01",
                "ifc-beam-01",
                "IfcBeam",
                new[]
                {
                    new IfcRoundTripNumericProperty("Width", 0.3000004d, "m"),
                    new IfcRoundTripNumericProperty("Length", 5.0000004d, "m")
                },
                12.5000004d,
                "m2",
                new[] { "source:model-a", "source:drawing-a" });

            Require(
                IfcRoundTripProjectionComparer.AreEquivalent(expected, reconstructed, 0.000001d),
                "IFC round-trip comparison rejected numeric values inside tolerance.");
            Require(
                !IfcRoundTripProjectionComparer.AreEquivalent(expected, reconstructed, 0.00000001d),
                "IFC round-trip comparison accepted numeric values outside tolerance.");
        }

        private static void RequiresExactSemanticPayload()
        {
            var baseline = CreateProjection(
                "PLATE-01",
                "ifc-plate-01",
                "IfcPlate",
                new[] { new IfcRoundTripNumericProperty("Thickness", 0.02d, "m") },
                4.5d,
                "m2",
                new[] { "source:plate-a" });

            Require(
                !IfcRoundTripProjectionComparer.AreEquivalent(
                    baseline,
                    CreateProjection(
                        "PLATE-01",
                        "ifc-plate-01",
                        "IfcSlab",
                        new[] { new IfcRoundTripNumericProperty("Thickness", 0.02d, "m") },
                        4.5d,
                        "m2",
                        new[] { "source:plate-a" }),
                    0d),
                "IFC round-trip comparison ignored semantic classification drift.");

            Require(
                !IfcRoundTripProjectionComparer.AreEquivalent(
                    baseline,
                    CreateProjection(
                        "PLATE-01",
                        "ifc-plate-01",
                        "IfcPlate",
                        new[] { new IfcRoundTripNumericProperty("Thickness", 0.02d, "mm") },
                        4.5d,
                        "m2",
                        new[] { "source:plate-a" }),
                    0d),
                "IFC round-trip comparison ignored dimension-unit drift.");

            Require(
                !IfcRoundTripProjectionComparer.AreEquivalent(
                    baseline,
                    CreateProjection(
                        "PLATE-01",
                        "ifc-plate-01",
                        "IfcPlate",
                        new[] { new IfcRoundTripNumericProperty("Thickness", 0.02d, "m") },
                        4.5d,
                        "m2",
                        new[] { "source:plate-b" }),
                    0d),
                "IFC round-trip comparison ignored provenance drift.");
        }

        private static void RejectsMalformedUtf16Tokens()
        {
            var unpairedHigh = new string((char)0xD800, 1);
            var unpairedLow = new string((char)0xDC00, 1);

            Throws<ArgumentException>(() => CreateProjection(
                "BEAM-" + unpairedHigh,
                "ifc-beam-utf16-high",
                "IfcBeam",
                new[] { new IfcRoundTripNumericProperty("Length", 5d, "m") },
                5d,
                "m",
                new[] { "source:a" }));

            Throws<ArgumentException>(() => CreateProjection(
                "BEAM-UTF16-LOW",
                "ifc-beam-utf16-low",
                "IfcBeam" + unpairedLow,
                new[] { new IfcRoundTripNumericProperty("Length", 5d, "m") },
                5d,
                "m",
                new[] { "source:a" }));

            Throws<ArgumentException>(() => new IfcRoundTripNumericProperty("Length", 5d, "m" + unpairedHigh));
            Throws<ArgumentException>(() => CreateProjection(
                "BEAM-UTF16-PROVENANCE",
                "ifc-beam-utf16-provenance",
                "IfcBeam",
                new[] { new IfcRoundTripNumericProperty("Length", 5d, "m") },
                5d,
                "m",
                new[] { "source:" + unpairedLow }));

            var validPair = char.ConvertFromUtf32(0x1F642);
            var accepted = CreateProjection(
                "BEAM-" + validPair,
                "ifc-beam-" + validPair,
                "IfcBeam-" + validPair,
                new[] { new IfcRoundTripNumericProperty("Length-" + validPair, 5d, "m-" + validPair) },
                5d,
                "m2-" + validPair,
                new[] { "source:" + validPair });
            Require(accepted.Qs3dElementId == "BEAM-" + validPair, "Well-formed UTF-16 surrogate pairs must remain valid canonical tokens.");
        }

        private static void RejectsMalformedAndDuplicateState()
        {
            Throws<ArgumentException>(() => CreateProjection(
                " BEAM-01",
                "ifc-beam-01",
                "IfcBeam",
                new[] { new IfcRoundTripNumericProperty("Length", 5d, "m") },
                5d,
                "m",
                new[] { "source:a" }));

            Throws<ArgumentException>(() => CreateProjection(
                "BEAM-01",
                "ifc-beam-01",
                "IfcBeam",
                new[]
                {
                    new IfcRoundTripNumericProperty("Width", 0.3d, "m"),
                    new IfcRoundTripNumericProperty("width", 0.31d, "m")
                },
                5d,
                "m",
                new[] { "source:a" }));

            Throws<ArgumentOutOfRangeException>(() => new IfcRoundTripNumericProperty("Width", double.NaN, "m"));
            Throws<ArgumentOutOfRangeException>(() => CreateProjection(
                "BEAM-01",
                "ifc-beam-01",
                "IfcBeam",
                new[] { new IfcRoundTripNumericProperty("Length", 5d, "m") },
                double.PositiveInfinity,
                "m",
                new[] { "source:a" }));

            Throws<ArgumentException>(() => CreateProjection(
                "BEAM-01",
                "ifc-beam-01",
                "IfcBeam",
                new[] { new IfcRoundTripNumericProperty("Length", 5d, "m") },
                5d,
                "m",
                new[] { "source:a", "source:a" }));

            var first = CreateProjection(
                "BEAM-01",
                "ifc-shared",
                "IfcBeam",
                new[] { new IfcRoundTripNumericProperty("Length", 5d, "m") },
                5d,
                "m",
                new[] { "source:a" });
            var duplicateIfcIdentity = CreateProjection(
                "COLUMN-01",
                "ifc-shared",
                "IfcColumn",
                new[] { new IfcRoundTripNumericProperty("Height", 3d, "m") },
                3d,
                "m",
                new[] { "source:b" });
            Throws<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(new[] { first, duplicateIfcIdentity }));

            var duplicateQs3dIdentity = CreateProjection(
                "beam-01",
                "ifc-other",
                "IfcBeam",
                new[] { new IfcRoundTripNumericProperty("Length", 5d, "m") },
                5d,
                "m",
                new[] { "source:c" });
            Throws<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(new[] { first, duplicateQs3dIdentity }));

            Throws<ArgumentOutOfRangeException>(() => IfcRoundTripProjectionComparer.AreEquivalent(first, first, double.NaN));
            Throws<ArgumentOutOfRangeException>(() => IfcRoundTripProjectionComparer.AreEquivalent(first, first, -0.1d));
        }

        private static IfcRoundTripProjection CreateProjection(
            string qs3dElementId,
            string ifcGlobalId,
            string semanticClassification,
            IfcRoundTripNumericProperty[] dimensions,
            double primaryQuantity,
            string primaryQuantityUnit,
            string[] provenance)
        {
            return new IfcRoundTripProjection(
                qs3dElementId,
                ifcGlobalId,
                semanticClassification,
                dimensions,
                primaryQuantity,
                primaryQuantityUnit,
                provenance);
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
