using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityFormworkBoundarySweepSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            ConstructorBoundariesFailClosed();
            ExactMinimumThresholdsRemainInclusive();
            ZeroAreaDeductionIsDeterministicNoOp();
            DirectedBasisRoutingUsesExactPersistedFlags();
            EmptyInputAndCallerOrderingRemainStable();
            AggregateAreaOverflowFailsClosed();
        }

        private static void ConstructorBoundariesFailClosed()
        {
            Throws<ArgumentOutOfRangeException>(() => new QuantityFormworkDeductionCandidate(
                -1, QuantityFormworkDeductionBasis.Concrete, 0d), "negative deduction target");
            Throws<ArgumentOutOfRangeException>(() => new QuantityFormworkDeductionCandidate(
                1, (QuantityFormworkDeductionBasis)0, 0d), "invalid deduction basis");
            Throws<ArgumentOutOfRangeException>(() => new QuantityFormworkDeductionCandidate(
                1, QuantityFormworkDeductionBasis.Concrete, double.NaN), "NaN deduction area");
            Throws<ArgumentOutOfRangeException>(() => new QuantityFormworkDeductionCandidate(
                1, QuantityFormworkDeductionBasis.Concrete, double.PositiveInfinity), "infinite deduction area");
            Throws<ArgumentOutOfRangeException>(() => new QuantityFormworkDeductionCandidate(
                1, QuantityFormworkDeductionBasis.Concrete, -double.Epsilon), "negative deduction area");

            Throws<ArgumentException>(() => new QuantityFormworkFaceCandidate(
                "  ", 1, QuantityFormworkFaceKind.Side, 0d), "blank face id");
            Throws<ArgumentOutOfRangeException>(() => new QuantityFormworkFaceCandidate(
                "F1", -1, QuantityFormworkFaceKind.Side, 0d), "negative source category");
            Throws<ArgumentOutOfRangeException>(() => new QuantityFormworkFaceCandidate(
                "F1", 1, (QuantityFormworkFaceKind)0, 0d), "invalid face kind");
            Throws<ArgumentOutOfRangeException>(() => new QuantityFormworkFaceCandidate(
                "F1", 1, QuantityFormworkFaceKind.Side, double.NaN), "NaN gross area");
            Throws<ArgumentOutOfRangeException>(() => new QuantityFormworkFaceCandidate(
                "F1", 1, QuantityFormworkFaceKind.Side, double.NegativeInfinity), "infinite gross area");
            Throws<ArgumentOutOfRangeException>(() => new QuantityFormworkFaceCandidate(
                "F1", 1, QuantityFormworkFaceKind.Side, -double.Epsilon), "negative gross area");
        }

        private static void ExactMinimumThresholdsRemainInclusive()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.MinFormworkAreaMm2 = 1000d;
            settings.MinSubtractAreaMm2 = 100d;
            var rule = settings.FindIntersectionRule((int)ElementCategory.Beam, (int)ElementCategory.Column)
                ?? throw new Exception("Missing Beam->Column rule fixture.");
            rule.SubtractSideFormworkByConcrete = true;
            settings.NormalizeAndValidate();
            var engine = new QuantityFormworkCalculationEngine(new QuantityCalculationRuleSet(settings));

            var exact = engine.Calculate(
                "THRESHOLD",
                "Threshold fixture",
                new[]
                {
                    new QuantityFormworkFaceCandidate(
                        "F-EXACT",
                        (int)ElementCategory.Beam,
                        QuantityFormworkFaceKind.Side,
                        1000d,
                        new[]
                        {
                            new QuantityFormworkDeductionCandidate(
                                (int)ElementCategory.Column,
                                QuantityFormworkDeductionBasis.Concrete,
                                100d,
                                "exact")
                        })
                });
            True(exact.Faces.Single().Included, "face exactly at MinFormworkAreaMm2 must be included");
            Near(100d, exact.Faces.Single().DeductionAreaMm2, "deduction exactly at MinSubtractAreaMm2 must apply");
            Near(900d, exact.Faces.Single().NetAreaMm2, "exact-threshold net area");

            var below = engine.Calculate(
                "BELOW",
                "Below fixture",
                new[]
                {
                    new QuantityFormworkFaceCandidate(
                        "F-BELOW-FACE",
                        (int)ElementCategory.Beam,
                        QuantityFormworkFaceKind.Side,
                        999d),
                    new QuantityFormworkFaceCandidate(
                        "F-BELOW-DEDUCTION",
                        (int)ElementCategory.Beam,
                        QuantityFormworkFaceKind.Side,
                        1000d,
                        new[]
                        {
                            new QuantityFormworkDeductionCandidate(
                                (int)ElementCategory.Column,
                                QuantityFormworkDeductionBasis.Concrete,
                                99d,
                                "below")
                        })
                });
            True(!below.Faces[0].Included, "face below MinFormworkAreaMm2 must be excluded");
            Near(0d, below.Faces[1].DeductionAreaMm2, "deduction below MinSubtractAreaMm2 must not apply");
        }

        private static void ZeroAreaDeductionIsDeterministicNoOp()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.IntersectionRules.RemoveAll(x =>
                x.Source == (int)ElementCategory.Beam && x.Target == (int)ElementCategory.Column);
            settings.NormalizeAndValidate();
            var engine = new QuantityFormworkCalculationEngine(new QuantityCalculationRuleSet(settings));

            var result = engine.Calculate(
                "ZERO",
                "Zero deduction fixture",
                new[]
                {
                    new QuantityFormworkFaceCandidate(
                        "ZERO:S1",
                        (int)ElementCategory.Beam,
                        QuantityFormworkFaceKind.Side,
                        2000d,
                        new[]
                        {
                            new QuantityFormworkDeductionCandidate(
                                (int)ElementCategory.Column,
                                QuantityFormworkDeductionBasis.Concrete,
                                0d,
                                "zero")
                        })
                });

            Near(0d, result.Faces.Single().DeductionAreaMm2,
                "zero-area deduction must remain a no-op even when no directed rule exists");
            True(result.Faces.Single().Trace.Any(x => x.Code == "FW-DEDUCTION-ZERO" && !x.Applied),
                "zero-area deduction must retain explicit no-op trace");
        }

        private static void DirectedBasisRoutingUsesExactPersistedFlags()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.MinFormworkAreaMm2 = 1d;
            settings.MinSubtractAreaMm2 = 1d;
            var rule = settings.FindIntersectionRule((int)ElementCategory.Beam, (int)ElementCategory.Column)
                ?? throw new Exception("Missing Beam->Column rule fixture.");
            rule.SubtractSideFormworkByConcrete = true;
            rule.SubtractSideFormworkBySideFormwork = true;
            rule.SubtractBottomFormworkByConcrete = true;
            rule.SubtractBottomFormworkByBottomFormwork = true;
            settings.NormalizeAndValidate();
            var engine = new QuantityFormworkCalculationEngine(new QuantityCalculationRuleSet(settings));

            var result = engine.Calculate(
                "ROUTING",
                "Routing fixture",
                new[]
                {
                    Face("S-C", QuantityFormworkFaceKind.Side, QuantityFormworkDeductionBasis.Concrete),
                    Face("S-F", QuantityFormworkFaceKind.Side, QuantityFormworkDeductionBasis.Formwork),
                    Face("B-C", QuantityFormworkFaceKind.Bottom, QuantityFormworkDeductionBasis.Concrete),
                    Face("B-F", QuantityFormworkFaceKind.Bottom, QuantityFormworkDeductionBasis.Formwork)
                });

            Equal(4, result.Faces.Count, "directed routing face count");
            foreach (var face in result.Faces)
            {
                Near(100d, face.DeductionAreaMm2, face.FaceId + " directed deduction");
                True(face.Trace.Any(x => x.Code == "FW-DEDUCTION-APPLIED" && x.Applied),
                    face.FaceId + " applied trace");
            }
        }

        private static QuantityFormworkFaceCandidate Face(
            string id,
            QuantityFormworkFaceKind kind,
            QuantityFormworkDeductionBasis basis)
        {
            return new QuantityFormworkFaceCandidate(
                id,
                (int)ElementCategory.Beam,
                kind,
                1000d,
                new[]
                {
                    new QuantityFormworkDeductionCandidate(
                        (int)ElementCategory.Column,
                        basis,
                        100d,
                        id + ":region")
                });
        }

        private static void EmptyInputAndCallerOrderingRemainStable()
        {
            var engine = new QuantityFormworkCalculationEngine(
                new QuantityCalculationRuleSet(QuantityCalculationSettings.CreateDefault()));
            var empty = engine.Calculate(
                "EMPTY",
                "Empty fixture",
                Array.Empty<QuantityFormworkFaceCandidate>());
            Equal(0, empty.Faces.Count, "empty face result count");
            Near(0d, empty.FormworkM2, "empty formwork total");
            Equal(0, empty.GeometryExplanation.FormworkFaces.Count, "empty explanation face count");

            var result = engine.Calculate(
                "ORDER",
                "Ordering fixture",
                new[]
                {
                    new QuantityFormworkFaceCandidate(
                        "Z-FACE", (int)ElementCategory.Beam, QuantityFormworkFaceKind.Side, 2000d),
                    new QuantityFormworkFaceCandidate(
                        "A-FACE", (int)ElementCategory.Beam, QuantityFormworkFaceKind.Side, 2000d)
                });
            Equal("Z-FACE", result.Faces[0].FaceId, "caller order first face");
            Equal("A-FACE", result.Faces[1].FaceId, "caller order second face");
            Equal("Z-FACE", result.GeometryExplanation.FormworkFaces[0].FaceId,
                "explanation must preserve result ordering");
        }

        private static void AggregateAreaOverflowFailsClosed()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.MinFormworkAreaMm2 = 0d;
            settings.NormalizeAndValidate();
            var engine = new QuantityFormworkCalculationEngine(new QuantityCalculationRuleSet(settings));

            Throws<InvalidOperationException>(() => engine.Calculate(
                "OVERFLOW",
                "Overflow fixture",
                new[]
                {
                    new QuantityFormworkFaceCandidate(
                        "OVERFLOW:1", (int)ElementCategory.Beam, QuantityFormworkFaceKind.Side, double.MaxValue),
                    new QuantityFormworkFaceCandidate(
                        "OVERFLOW:2", (int)ElementCategory.Beam, QuantityFormworkFaceKind.Side, double.MaxValue)
                }), "aggregate formwork area overflow");
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
                throw new Exception("Formwork boundary regression: expected " + typeof(T).Name + " for " + message + ".");
            }
            catch (T)
            {
            }
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception("Formwork boundary regression: " + message + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new Exception("Formwork boundary regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Formwork boundary regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new Exception("Formwork boundary regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
