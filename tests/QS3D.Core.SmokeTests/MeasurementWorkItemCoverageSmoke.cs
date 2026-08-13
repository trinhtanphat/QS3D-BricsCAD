using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemCoverageSmoke
    {
        internal static void Run()
        {
            CoverageStatesAreExplicitAndDetached();
            OrderingIsDeterministicAndCultureIndependent();
            CorruptProjectStateFailsClosed();
        }

        private static void CoverageStatesAreExplicitAndDetached()
        {
            var catalog = Catalog();
            var project = BuildCoverageProject(reverse: false);
            var findings = MeasurementWorkItemCoverageEvaluator.Evaluate(project, catalog);

            Equal(5, findings.Count, "Coverage must emit one finding for each single-quantity element plus the missing-quantity element.");

            var ready = findings.Single(x => x.ElementId == "A-ready");
            True(ready.IsReady, "Mapped fresh quantity must be ready.");
            Equal("NetVolumeM3", ready.QuantityKey, "Ready quantity key mismatch.");
            Equal(2d, ready.QuantityValue, "Ready quantity value mismatch.");
            Equal(0, ready.Issues.Count, "Ready quantity must not carry coverage issues.");
            var readyMapping = ready.Mapping ?? throw new InvalidOperationException("Ready quantity must expose its canonical mapping.");
            Equal("map-slab-volume", readyMapping.MappingId, "Ready mapping identity mismatch.");
            Equal("class-slab", readyMapping.ClassificationId, "Ready classification identity mismatch.");
            Equal("work-slab-volume", readyMapping.WorkItemId, "Ready work-item identity mismatch.");

            var unmapped = findings.Single(x => x.ElementId == "b-unmapped");
            True(!unmapped.IsReady, "Fresh but unmapped quantity must not be ready.");
            SequenceEqual(
                new[] { MeasurementWorkItemCoverageIssue.UnmappedWorkItem },
                unmapped.Issues,
                "Unmapped quantity issue mismatch.");
            True(unmapped.Mapping == null, "Unmapped quantity must not invent mapping identity.");

            var staleMapped = findings.Single(x => x.ElementId == "C-stale-mapped");
            True(!staleMapped.IsReady, "Stale mapped quantity must not be ready.");
            SequenceEqual(
                new[] { MeasurementWorkItemCoverageIssue.StaleQuantity },
                staleMapped.Issues,
                "Stale mapped quantity issue mismatch.");
            True(staleMapped.Mapping != null, "Stale mapped quantity should preserve known mapping identity for diagnosis.");

            var staleUnmapped = findings.Single(x => x.ElementId == "D-stale-unmapped");
            True(!staleUnmapped.IsReady, "Stale unmapped quantity must not be ready.");
            SequenceEqual(
                new[]
                {
                    MeasurementWorkItemCoverageIssue.StaleQuantity,
                    MeasurementWorkItemCoverageIssue.UnmappedWorkItem
                },
                staleUnmapped.Issues,
                "Stale + unmapped reasons must both remain visible.");

            var missing = findings.Single(x => x.ElementId == "E-missing");
            True(!missing.IsReady, "Element without quantities must not be ready.");
            True(missing.QuantityKey == null && !missing.QuantityValue.HasValue && missing.Mapping == null,
                "Missing-quantity finding must not invent quantity or mapping data.");
            SequenceEqual(
                new[] { MeasurementWorkItemCoverageIssue.MissingQuantity },
                missing.Issues,
                "Missing-quantity issue mismatch.");

            var readyElement = project.Elements.Single(x => x.Id == "A-ready");
            readyElement.Quantities["NetVolumeM3"] = 99d;
            Equal(2d, ready.QuantityValue, "Coverage findings must remain detached from later source dictionary mutation.");
        }

        private static void OrderingIsDeterministicAndCultureIndependent()
        {
            var catalog = Catalog();
            var first = MeasurementWorkItemCoverageEvaluator.Evaluate(BuildCoverageProject(reverse: false), catalog);
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                var second = MeasurementWorkItemCoverageEvaluator.Evaluate(BuildCoverageProject(reverse: true), catalog);
                SequenceEqual(
                    first.Select(Signature),
                    second.Select(Signature),
                    "Coverage ordering/content must not depend on project insertion order or current culture.");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        private static void CorruptProjectStateFailsClosed()
        {
            var catalog = Catalog();

            var duplicate = new ProjectState("duplicate", "Duplicate");
            duplicate.Elements.Add(CleanQuantityElement("Dup", ElementCategory.Slab, "NetVolumeM3", 1d));
            duplicate.Elements.Add(CleanQuantityElement("dup", ElementCategory.Slab, "NetVolumeM3", 2d));
            ExpectThrows<InvalidOperationException>(() => MeasurementWorkItemCoverageEvaluator.Evaluate(duplicate, catalog));

            var nullElement = new ProjectState("null", "Null");
            nullElement.Elements.Add(null!);
            ExpectThrows<InvalidOperationException>(() => MeasurementWorkItemCoverageEvaluator.Evaluate(nullElement, catalog));

            var nonFinite = new ProjectState("nan", "NaN");
            var nan = CleanQuantityElement("NaN", ElementCategory.Slab, "NetVolumeM3", 1d);
            nan.Quantities["NetVolumeM3"] = double.NaN;
            nonFinite.Elements.Add(nan);
            ExpectThrows<InvalidOperationException>(() => MeasurementWorkItemCoverageEvaluator.Evaluate(nonFinite, catalog));

            var paddedQuantity = new ProjectState("padded", "Padded");
            var padded = new ProjectElement("Padded", ElementCategory.Slab);
            padded.Quantities[" NetVolumeM3"] = 1d;
            padded.MarkClean(ElementDirtyFlags.All);
            paddedQuantity.Elements.Add(padded);
            ExpectThrows<InvalidOperationException>(() => MeasurementWorkItemCoverageEvaluator.Evaluate(paddedQuantity, catalog));

            var undefinedCategory = new ProjectState("category", "Category");
            var corrupted = CleanQuantityElement("CorruptCategory", ElementCategory.Slab, "NetVolumeM3", 1d);
            var categoryField = typeof(ProjectElement).GetField("_category", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement category backing field changed; update corruption regression intentionally.");
            categoryField.SetValue(corrupted, (ElementCategory)int.MaxValue);
            undefinedCategory.Elements.Add(corrupted);
            ExpectThrows<InvalidOperationException>(() => MeasurementWorkItemCoverageEvaluator.Evaluate(undefinedCategory, catalog));
        }

        private static ProjectState BuildCoverageProject(bool reverse)
        {
            var ready = CleanQuantityElement("A-ready", ElementCategory.Slab, "NetVolumeM3", 2d);
            var unmapped = CleanQuantityElement("b-unmapped", ElementCategory.Slab, "OtherVolumeM3", 4d);

            var staleMapped = CleanQuantityElement("C-stale-mapped", ElementCategory.Slab, "NetVolumeM3", 3d);
            staleMapped.MarkDirty(ElementDirtyFlags.Quantity);

            var staleUnmapped = CleanQuantityElement("D-stale-unmapped", ElementCategory.Beam, "CustomLengthM", 5d);
            staleUnmapped.MarkDirty(ElementDirtyFlags.Quantity);

            var missing = new ProjectElement("E-missing", ElementCategory.Column);
            missing.MarkClean(ElementDirtyFlags.All);

            var elements = new[] { ready, unmapped, staleMapped, staleUnmapped, missing };
            var project = new ProjectState("coverage", "Coverage");
            foreach (var element in reverse ? elements.Reverse() : elements)
                project.Elements.Add(element);
            return project;
        }

        private static ProjectElement CleanQuantityElement(string id, ElementCategory category, string quantityKey, double value)
        {
            var element = new ProjectElement(id, category);
            element.SetQuantity(quantityKey, value);
            element.MarkClean(ElementDirtyFlags.All);
            return element;
        }

        private static MeasurementWorkItemMappingCatalog Catalog() =>
            new MeasurementWorkItemMappingCatalog(new[]
            {
                new MeasurementWorkItemMapping(
                    "map-slab-volume",
                    ElementCategory.Slab,
                    "NetVolumeM3",
                    "class-slab",
                    "work-slab-volume")
            });

        private static string Signature(MeasurementWorkItemCoverageFinding finding) =>
            finding.ElementId + "\u001f" +
            (finding.QuantityKey ?? "<missing>") + "\u001f" +
            (finding.QuantityValue.HasValue ? finding.QuantityValue.Value.ToString("R", CultureInfo.InvariantCulture) : "<missing>") + "\u001f" +
            (finding.Mapping?.MappingId ?? "<unmapped>") + "\u001f" +
            string.Join(",", finding.Issues.Select(x => x.ToString()));

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
        {
            if (!expected.SequenceEqual(actual))
                throw new InvalidOperationException(message);
        }

        private static void ExpectThrows<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
