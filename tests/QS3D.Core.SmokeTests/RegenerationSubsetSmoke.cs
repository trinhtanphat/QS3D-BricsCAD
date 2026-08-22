using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationSubsetSmoke
    {
        public static void Run()
        {
            RegeneratesOnlyRequestedElements();
            RejectsMalformedRequestedIds();
            RejectsUnknownTarget();
            RejectsDuplicateProjectIds();
        }

        private static void RegeneratesOnlyRequestedElements()
        {
            var project = new ProjectState("regen-subset", "Targeted regeneration");
            var selected = new ProjectElement("selected", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            selected.SetProperty("LengthM", "12.5");
            selected.SetProperty("AreaM2", "3.25");
            project.Elements.Add(selected);

            var unrelated = new ProjectElement("unrelated", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            unrelated.SetProperty("LengthM", "99");
            unrelated.SetProperty("AreaM2", "8");
            project.Elements.Add(unrelated);

            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            var count = engine.RegenerateDirtySubset(project, new[] { selected.Id });

            Equal(1, count);
            Equal(ElementDirtyFlags.None, selected.Dirty);
            True(unrelated.Dirty != ElementDirtyFlags.None);
            True(!unrelated.Quantities.ContainsKey("Count"));
            Near(12.5d, selected.Quantities["LengthM"]);
            Near(3.25d, selected.Quantities["AreaM2"]);
            Near(1d, selected.Quantities["Count"]);
        }

        private static void RejectsMalformedRequestedIds()
        {
            var project = new ProjectState("regen-target-guard", "Canonical subset targets");
            var selected = new ProjectElement("Selected", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            selected.SetProperty("LengthM", "4");
            project.Elements.Add(selected);
            var dirtyBefore = selected.Dirty;

            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { " Selected " }));
            Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { "Selected", "selected" }));
            Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { string.Empty }));

            Equal(dirtyBefore, selected.Dirty);
            True(!selected.Quantities.ContainsKey("Count"));
        }

        private static void RejectsUnknownTarget()
        {
            var project = new ProjectState("regen-unknown", "Unknown target");
            project.Elements.Add(new ProjectElement("present", ElementCategory.CustomQuantity));
            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            Throws<KeyNotFoundException>(() => engine.RegenerateDirtySubset(project, new[] { "missing" }));
        }

        private static void RejectsDuplicateProjectIds()
        {
            var project = new ProjectState("regen-duplicate", "Duplicate target ownership");
            var first = new ProjectElement("DUP", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            first.SetProperty("LengthM", "1");
            var second = new ProjectElement("dup", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            second.SetProperty("LengthM", "2");
            project.Elements.Add(first);
            project.Elements.Add(second);

            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            Throws<InvalidOperationException>(() => engine.RegenerateDirtySubset(project, new[] { "dup" }));
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(ElementDirtyFlags expected, ElementDirtyFlags actual)
        {
            if (expected != actual) throw new Exception("Expected dirty " + expected + ", got " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
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
