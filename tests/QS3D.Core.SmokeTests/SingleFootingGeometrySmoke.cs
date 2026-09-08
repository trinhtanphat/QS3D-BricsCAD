using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Reporting;
using QS3D.Core.Rules;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SingleFootingGeometrySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var box = new SingleFootingDimensions(1.6d, 1.6d, 1d, 1d, 1d, 0d);
            AssertClose(box.VolumeM3, 2.56d, "H2=0 box volume");
            if (box.HasTaper) throw new InvalidOperationException("H2=0 must not report a taper.");

            var tapered = new SingleFootingDimensions(1.6d, 1.6d, 1d, 1d, 1d, .3d);
            // Integral of (1.6 - .6t)^2 from t=0..1, multiplied by H2, plus lower prism.
            AssertClose(tapered.VolumeM3, 2.56d + .3d * (2.56d - .96d + .12d), "square tapered volume");
            if (!tapered.HasTaper) throw new InvalidOperationException("Reduced top with H2>0 must report a taper.");

            var oneAxis = new SingleFootingDimensions(2d, 1.5d, 2d, 1d, .5d, .4d);
            AssertClose(oneAxis.VolumeM3, 1.5d + .4d * 2d * 1.25d, "one-axis tapered volume");

            ExpectInvalid(() => new SingleFootingDimensions(0d, 1d, 1d, 1d, 1d, 0d));
            ExpectInvalid(() => new SingleFootingDimensions(1d, 1d, 1.1d, 1d, 1d, 0d));
            ExpectInvalid(() => new SingleFootingDimensions(1d, 1d, 1d, 1.1d, 1d, 0d));
            ExpectInvalid(() => new SingleFootingDimensions(1d, 1d, 1d, 1d, 1d, -.01d));
            ExpectInvalid(() => new SingleFootingDimensions(double.NaN, 1d, 1d, 1d, 1d, 0d));
            RegenerationUsesSingleFootingDimensions();
            DirectProjectionRefreshesCleanReporting();
            LegacyAndInvalidDimensions();
            OrdinaryFoundationIsUnchanged();
            IncompatibleEvidenceAndOverflowAreAtomic();
            CustomRulesRemainReportingAuthority();
            Console.WriteLine("PASS single footing canonical creation/edit/regeneration quantities");
        }

        private static void RegenerationUsesSingleFootingDimensions()
        {
            var project = new ProjectState("single-footing-quantity", "Single footing quantity");
            project.Families.Add(new ProjectFamily("footing", "Móng đơn", ElementCategory.Foundation));
            for (var index = 0; index < 2; index++)
            {
                var element = new ProjectElement("footing-" + index, ElementCategory.Foundation) { FamilyId = "footing" };
                element.Properties["CategoryCode"] = "Foundation.SingleFooting";
                WriteDimensions(element, 4d, 2d, 2d, 1d, 1d, 1d);
                // Real native capture retained the original box/taper prism quantity inputs.
                element.Properties["AreaM2"] = index == 0 ? "4" : "6";
                element.Properties["ThicknessM"] = "2";
                element.Properties["VolumeM3"] = "999";
                element.SetQuantity("GrossVolumeM3", index == 0 ? 4d : 12d);
                element.SetQuantity("NetVolumeM3", index == 0 ? 4d : 12d);
                element.SetQuantity("GrossConcreteM3", 999d);
                element.SetQuantity("NetConcreteM3", 998d);
                element.SetQuantity("DeductionM3", 1d);
                element.SetQuantity("FormworkM2", 999d);
                project.Elements.Add(element);
            }

            var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
            if (regenerated != 2) throw new InvalidOperationException("Both single footings must regenerate.");
            foreach (var element in project.Elements)
            {
                AssertClose(element.Quantities["GrossVolumeM3"], 38d / 3d, "edited single footing gross");
                AssertClose(element.Quantities["NetVolumeM3"], 38d / 3d, "edited single footing net");
            }
            var rows = ProjectQuantityReportBuilder.Group(project);
            if (rows.Count != 1 || rows[0].Count != 2)
                throw new InvalidOperationException("Single footing BQ must preserve grouped instance identity.");
            AssertClose(rows[0].GrossConcreteM3, 76d / 3d, "two edited single footings BQ gross");
            AssertClose(rows[0].NetConcreteM3, 76d / 3d, "two edited single footings BQ net");
            AssertClose(rows[0].DeductionM3, 0d, "standalone footing deduction");
            if (rows[0].HasFormworkM2Evidence)
                throw new InvalidOperationException("Old prism formwork must not qualify as loft evidence.");
        }

        private static void DirectProjectionRefreshesCleanReporting()
        {
            var project = new ProjectState("footing-direct", "Footing direct");
            var element = new ProjectElement("footing", ElementCategory.Foundation);
            project.Elements.Add(element);
            SingleFootingQuantityPolicy.Apply(element, new SingleFootingDimensions(2d, 2d, 1d, 1d, 1d, 0d));
            element.MarkClean(ElementDirtyFlags.All);
            AssertClose(ProjectQuantityReportBuilder.Group(project)[0].GrossConcreteM3, 4d, "new box BQ");
            SingleFootingQuantityPolicy.Apply(element, new SingleFootingDimensions(4d, 2d, 2d, 1d, 1d, 1d));
            element.MarkClean(ElementDirtyFlags.All);
            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            if (engine.RegenerateDirty(project) != 0)
                throw new InvalidOperationException("Already-clean native footing must not need a reporting regeneration.");
            AssertClose(ProjectQuantityReportBuilder.Group(project)[0].NetConcreteM3, 38d / 3d, "clean edited taper BQ");
            SingleFootingQuantityPolicy.Apply(element, new SingleFootingDimensions(2d, 1.5d, 2d, 1d, .5d, .4d));
            AssertClose(element.Quantities["GrossVolumeM3"], 2.5d, "one-axis taper quantity");
        }

        private static void LegacyAndInvalidDimensions()
        {
            var element = new ProjectElement("legacy", ElementCategory.Foundation);
            element.Properties["SingleFootingSubtype"] = "MongDon";
            var keys = new[] { "L1", "W1", "L2", "W2", "H1", "H2" };
            var values = new[] { "4", "2", "2", "1", "1", "1" };
            for (var i = 0; i < keys.Length; i++) element.Properties["SingleFooting" + keys[i] + "M"] = values[i];
            if (!SingleFootingQuantityPolicy.TryApply(element)) throw new InvalidOperationException("Legacy footing not recognized.");
            AssertClose(element.Quantities["GrossVolumeM3"], 38d / 3d, "legacy taper quantity");
            foreach (var bad in new[] { "NaN", "Infinity", "", "not-a-number", "1e-400", "-0" })
            {
                element.Properties["SINGLE_FOOTING_H2"] = bad;
                var before = element.Quantities["GrossVolumeM3"];
                var rejected = false;
                try { SingleFootingQuantityPolicy.TryApply(element); }
                catch (InvalidOperationException) { rejected = true; }
                if (!rejected) throw new InvalidOperationException("Malformed canonical dimension fell back to legacy.");
                AssertClose(element.Quantities["GrossVolumeM3"], before, "invalid projection remains atomic");
            }
        }

        private static void IncompatibleEvidenceAndOverflowAreAtomic()
        {
            foreach (var measuredKey in new[] { "MeasuredSolidVolumeM3", "MeasuredSolidSurfaceAreaM2" })
            {
                var project = new ProjectState("conflict", "Conflict");
                var element = new ProjectElement("footing", ElementCategory.Foundation);
                element.Properties["CategoryCode"] = "Foundation.SingleFooting";
                WriteDimensions(element, 4d, 2d, 2d, 1d, 1d, 1d);
                element.Properties[measuredKey] = "999";
                element.SetQuantity("GrossVolumeM3", 4d);
                project.Elements.Add(element);
                var rejected = false;
                try { new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project); }
                catch (InvalidOperationException) { rejected = true; }
                if (!rejected) throw new InvalidOperationException("Stale measured evidence overwrote footing dimensions.");
                AssertClose(project.Elements[0].Quantities["GrossVolumeM3"], 4d, "conflicting evidence rollback");
            }
            var overflow = new ProjectElement("overflow", ElementCategory.Foundation);
            overflow.SetQuantity("GrossVolumeM3", 4d);
            overflow.SetQuantity("FormworkM2", 7d);
            var overflowRejected = false;
            try { SingleFootingQuantityPolicy.Apply(overflow, new SingleFootingDimensions(1e200d, 1e200d, 1d, 1d, 1d, 1d)); }
            catch (OverflowException) { overflowRejected = true; }
            if (!overflowRejected) throw new InvalidOperationException("Unrepresentable footing quantity accepted.");
            AssertClose(overflow.Quantities["GrossVolumeM3"], 4d, "overflow volume unchanged");
            AssertClose(overflow.Quantities["FormworkM2"], 7d, "overflow preserves old state atomically");
        }

        private static void OrdinaryFoundationIsUnchanged()
        {
            var element = new ProjectElement("ordinary", ElementCategory.Foundation);
            element.Properties["AreaM2"] = "6";
            element.Properties["ThicknessM"] = "2";
            element.Properties["PerimeterM"] = "10";
            if (SingleFootingQuantityPolicy.TryApply(element)) throw new InvalidOperationException("Ordinary foundation misclassified.");
            new StructuralRegenerator().Regenerate(new ProjectState("ordinary", "Ordinary"), element);
            AssertClose(element.Quantities["GrossVolumeM3"], 12d, "ordinary foundation volume");
            AssertClose(element.Quantities["FormworkM2"], 20d, "ordinary foundation formwork");
        }

        private static void WriteDimensions(ProjectElement element, params double[] dimensions)
        {
            var keys = new[] { "L1", "W1", "L2", "W2", "H1", "H2" };
            for (var index = 0; index < keys.Length; index++)
                element.Properties["SINGLE_FOOTING_" + keys[index]] = dimensions[index].ToString("R", CultureInfo.InvariantCulture);
        }

        private static void CustomRulesRemainReportingAuthority()
        {
            var project = new ProjectState("footing-rule", "Footing rule");
            var element = new ProjectElement("footing", ElementCategory.Foundation);
            element.Properties["CategoryCode"] = "Foundation.SingleFooting";
            WriteDimensions(element, 4d, 2d, 2d, 1d, 1d, 1d);
            element.SetQuantity("GrossConcreteM3", 999d);
            element.SetQuantity("NetConcreteM3", 998d);
            element.SetQuantity("DeductionM3", 1d);
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("footing-net", ElementCategory.Foundation, "NetVolumeM3", "5", "v1"));
            new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
            var row = ProjectQuantityReportBuilder.Group(project)[0];
            AssertClose(row.GrossConcreteM3, 38d / 3d, "rule-independent gross");
            AssertClose(row.NetConcreteM3, 5d, "custom rule net remains authoritative");
            AssertClose(row.DeductionM3, 23d / 3d, "custom rule implied deduction");
        }

        private static void AssertClose(double actual, double expected, string label)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(actual - expected) > 1e-12d)
                throw new InvalidOperationException(label + " mismatch. Expected " + expected + ", actual " + actual + ".");
        }

        private static void ExpectInvalid(Action action)
        {
            try { action(); }
            catch (ArgumentOutOfRangeException) { return; }
            throw new InvalidOperationException("Invalid single footing dimensions must fail closed.");
        }
    }
}
