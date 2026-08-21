using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityFormworkCalculationEngineSmoke
    {
        internal static void Run()
        {
            AppliesDirectedSideDeductionAndConvertsToM2();
            CategoryAndMinimumAreaGatesExcludeFaces();
            DisabledDirectedRuleDoesNotDeduct();
            MissingDirectedRuleFailsClosed();
            OverDeductionFailsClosed();
            DuplicateFaceAndRegionIdentityFailsClosed();
            BltCompatibilityCodesRemainExact();
        }

        private static void AppliesDirectedSideDeductionAndConvertsToM2()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            var directed = settings.FindIntersectionRule((int)ElementCategory.Beam, (int)ElementCategory.Column)
                ?? throw new Exception("Missing native Beam->Column quantity rule fixture.");
            directed.SubtractSideFormworkByConcrete = true;
            settings.NormalizeAndValidate();

            var engine = new QuantityFormworkCalculationEngine(new QuantityCalculationRuleSet(settings));
            var result = engine.Calculate(
                "B1",
                "Beam 1",
                new[]
                {
                    new QuantityFormworkFaceCandidate(
                        "B1:S1",
                        (int)ElementCategory.Beam,
                        QuantityFormworkFaceKind.Side,
                        2000000d,
                        new[]
                        {
                            new QuantityFormworkDeductionCandidate(
                                (int)ElementCategory.Column,
                                QuantityFormworkDeductionBasis.Concrete,
                                200000d,
                                "contact-1")
                        })
                },
                new[] { "AB12" },
                "geometry-v1");

            Near(2d, result.GrossAreaM2, "gross m2");
            Near(0.2d, result.DeductionAreaM2, "deduction m2");
            Near(1.8d, result.NetAreaM2, "net m2");
            Near(1.8d, result.FormworkM2, "FormworkM2 alias");
            Equal(1, result.Faces.Count, "face result count");
            True(result.Faces[0].Included, "eligible side face must be included");
            True(result.Faces[0].Trace.Any(x => x.Code == "FW-DEDUCTION-APPLIED" && x.Applied),
                "applied deduction trace missing");
            Equal("B1", result.GeometryExplanation.ElementId, "explanation element id");
            Equal("AB12", result.GeometryExplanation.SourceHandles.Single(), "explanation source handle");
            Near(1.8d, result.GeometryExplanation.NetFormworkArea, "explanation net formwork m2");
            Equal("contact-1", result.GeometryExplanation.FormworkFaces[0].Deductions.Single().RegionKey,
                "deduction region provenance");
        }

        private static void CategoryAndMinimumAreaGatesExcludeFaces()
        {
            var disabledSettings = QuantityCalculationSettings.CreateDefault();
            var beam = disabledSettings.FindCategoryRule((int)ElementCategory.Beam)
                ?? throw new Exception("Missing Beam category quantity rule fixture.");
            beam.ExtractSide = false;
            disabledSettings.NormalizeAndValidate();

            var disabledResult = new QuantityFormworkCalculationEngine(new QuantityCalculationRuleSet(disabledSettings))
                .Calculate(
                    "B2",
                    "Beam 2",
                    new[]
                    {
                        new QuantityFormworkFaceCandidate(
                            "B2:S1",
                            (int)ElementCategory.Beam,
                            QuantityFormworkFaceKind.Side,
                            2000000d)
                    });
            True(!disabledResult.Faces[0].Included, "disabled category side must be excluded");
            Near(0d, disabledResult.FormworkM2, "disabled category formwork");
            True(disabledResult.Faces[0].Trace.Any(x => x.Code == "FW-FACE-DISABLED"),
                "disabled category trace missing");

            var minimumSettings = QuantityCalculationSettings.CreateDefault();
            minimumSettings.MinFormworkAreaMm2 = 5000d;
            minimumSettings.NormalizeAndValidate();
            var minimumResult = new QuantityFormworkCalculationEngine(new QuantityCalculationRuleSet(minimumSettings))
                .Calculate(
                    "B3",
                    "Beam 3",
                    new[]
                    {
                        new QuantityFormworkFaceCandidate(
                            "B3:B1",
                            (int)ElementCategory.Beam,
                            QuantityFormworkFaceKind.Bottom,
                            4999d)
                    });
            True(!minimumResult.Faces[0].Included, "below-minimum bottom must be excluded");
            Near(0d, minimumResult.FormworkM2, "below-minimum formwork");
            True(minimumResult.Faces[0].Trace.Any(x => x.Code == "FW-BELOW-MIN-AREA"),
                "minimum-area trace missing");
        }

        private static void DisabledDirectedRuleDoesNotDeduct()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.NormalizeAndValidate();
            var engine = new QuantityFormworkCalculationEngine(new QuantityCalculationRuleSet(settings));
            var result = engine.Calculate(
                "B4",
                "Beam 4",
                new[]
                {
                    new QuantityFormworkFaceCandidate(
                        "B4:S1",
                        (int)ElementCategory.Beam,
                        QuantityFormworkFaceKind.Side,
                        1000000d,
                        new[]
                        {
                            new QuantityFormworkDeductionCandidate(
                                (int)ElementCategory.Column,
                                QuantityFormworkDeductionBasis.Concrete,
                                100000d,
                                "disabled-rule-contact")
                        })
                });

            Near(1d, result.FormworkM2, "disabled directed rule must not subtract");
            True(result.Faces[0].Trace.Any(x => x.Code == "FW-DEDUCTION-SKIPPED" && !x.Applied),
                "disabled deduction trace missing");
        }

        private static void MissingDirectedRuleFailsClosed()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.IntersectionRules.RemoveAll(x =>
                x.Source == (int)ElementCategory.Beam && x.Target == (int)ElementCategory.Column);
            settings.NormalizeAndValidate();
            var engine = new QuantityFormworkCalculationEngine(new QuantityCalculationRuleSet(settings));

            Throws<InvalidOperationException>(() => engine.Calculate(
                "B5",
                "Beam 5",
                new[]
                {
                    new QuantityFormworkFaceCandidate(
                        "B5:S1",
                        (int)ElementCategory.Beam,
                        QuantityFormworkFaceKind.Side,
                        1000000d,
                        new[]
                        {
                            new QuantityFormworkDeductionCandidate(
                                (int)ElementCategory.Column,
                                QuantityFormworkDeductionBasis.Concrete,
                                100000d,
                                "missing-rule-contact")
                        })
                }), "missing directed rule");
        }

        private static void OverDeductionFailsClosed()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            var directed = settings.FindIntersectionRule((int)ElementCategory.Beam, (int)ElementCategory.Column)
                ?? throw new Exception("Missing native Beam->Column quantity rule fixture.");
            directed.SubtractSideFormworkByConcrete = true;
            settings.MinFormworkAreaMm2 = 1000d;
            settings.MinSubtractAreaMm2 = 10d;
            settings.NormalizeAndValidate();
            var engine = new QuantityFormworkCalculationEngine(new QuantityCalculationRuleSet(settings));

            Throws<InvalidOperationException>(() => engine.Calculate(
                "B6",
                "Beam 6",
                new[]
                {
                    new QuantityFormworkFaceCandidate(
                        "B6:S1",
                        (int)ElementCategory.Beam,
                        QuantityFormworkFaceKind.Side,
                        1000d,
                        new[]
                        {
                            new QuantityFormworkDeductionCandidate(
                                (int)ElementCategory.Column,
                                QuantityFormworkDeductionBasis.Concrete,
                                1200d,
                                "overlap-a")
                        })
                }), "over-deduction");
        }

        private static void DuplicateFaceAndRegionIdentityFailsClosed()
        {
            var engine = new QuantityFormworkCalculationEngine(
                new QuantityCalculationRuleSet(QuantityCalculationSettings.CreateDefault()));
            var face = new QuantityFormworkFaceCandidate(
                "B7:S1",
                (int)ElementCategory.Beam,
                QuantityFormworkFaceKind.Side,
                1000d);
            Throws<ArgumentException>(() => engine.Calculate("B7", "Beam 7", new[] { face, face }),
                "duplicate face id");

            Throws<ArgumentException>(() => new QuantityFormworkFaceCandidate(
                "B7:S2",
                (int)ElementCategory.Beam,
                QuantityFormworkFaceKind.Side,
                1000d,
                new[]
                {
                    new QuantityFormworkDeductionCandidate((int)ElementCategory.Column, QuantityFormworkDeductionBasis.Concrete, 100d, "R1"),
                    new QuantityFormworkDeductionCandidate((int)ElementCategory.Column, QuantityFormworkDeductionBasis.Formwork, 100d, "r1")
                }), "duplicate region key");
        }

        private static void BltCompatibilityCodesRemainExact()
        {
            var preset = QuantityCalculationBltCompatibilityPreset.Create();
            var engine = new QuantityFormworkCalculationEngine(new QuantityCalculationRuleSet(preset));
            var result = engine.Calculate(
                "BLT-201",
                "BLT compatibility fixture",
                new[]
                {
                    new QuantityFormworkFaceCandidate(
                        "BLT-201:S1",
                        201,
                        QuantityFormworkFaceKind.Side,
                        2000000d,
                        new[]
                        {
                            new QuantityFormworkDeductionCandidate(
                                207,
                                QuantityFormworkDeductionBasis.Concrete,
                                100000d,
                                "blt-201-207")
                        })
                });

            Near(1.9d, result.FormworkM2, "BLT 201->207 side-by-concrete exact rule");
            True(result.Faces[0].Trace.Any(x => x.TargetCode == 207 && x.Applied),
                "BLT directed rule trace missing");
            Near(30d, result.Faces[0].FaceAngleThresholdDeg, "BLT face-angle provenance");
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
                throw new Exception("Formwork calculation regression: expected " + typeof(T).Name + " for " + message + ".");
            }
            catch (T)
            {
            }
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception("Formwork calculation regression: " + message + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new Exception("Formwork calculation regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Formwork calculation regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new Exception("Formwork calculation regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
