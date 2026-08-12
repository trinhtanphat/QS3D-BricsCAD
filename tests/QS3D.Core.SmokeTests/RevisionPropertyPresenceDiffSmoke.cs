using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionPropertyPresenceDiffSmoke
    {
        private const string ElementId = "E-REV-PRESENCE";
        private const string PropertyName = "Note";

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AssertSinglePropertyDelta(
                Snapshot(false, string.Empty),
                Snapshot(true, string.Empty),
                string.Empty,
                string.Empty,
                "absent-to-empty");

            AssertSinglePropertyDelta(
                Snapshot(true, string.Empty),
                Snapshot(false, string.Empty),
                string.Empty,
                string.Empty,
                "empty-to-absent");

            var unchanged = new RevisionService().Compare(
                Snapshot(true, string.Empty),
                Snapshot(true, string.Empty));
            if (unchanged.Count != 0)
                throw new InvalidOperationException("Explicit empty property on both revision sides must remain a no-op.");

            AssertSinglePropertyDelta(
                Snapshot(true, "before"),
                Snapshot(true, "after"),
                "before",
                "after",
                "ordinary property change");
        }

        private static RevisionSnapshot Snapshot(bool includeProperty, string value)
        {
            var snapshot = new RevisionSnapshot();
            var element = new RevisionElementSnapshot
            {
                ElementId = ElementId,
                Category = ElementCategory.Beam.ToString()
            };
            if (includeProperty) element.Properties[PropertyName] = value;
            snapshot.Elements.Add(element);
            return snapshot;
        }

        private static void AssertSinglePropertyDelta(
            RevisionSnapshot before,
            RevisionSnapshot after,
            string expectedBefore,
            string expectedAfter,
            string label)
        {
            var deltas = new RevisionService().Compare(before, after);
            if (deltas.Count != 1)
                throw new InvalidOperationException(label + ": expected one revision delta, actual " + deltas.Count + ".");

            var delta = deltas[0];
            if (!string.Equals(delta.ElementId, ElementId, StringComparison.Ordinal) ||
                !string.Equals(delta.Change, "Changed", StringComparison.Ordinal) ||
                delta.Fields.Count != 1)
                throw new InvalidOperationException(label + ": revision delta shape is invalid.");

            var field = delta.Fields[0];
            if (!string.Equals(field.Field, "Property:" + PropertyName, StringComparison.Ordinal) ||
                !string.Equals(field.Before, expectedBefore, StringComparison.Ordinal) ||
                !string.Equals(field.After, expectedAfter, StringComparison.Ordinal))
                throw new InvalidOperationException(label + ": revision property delta values are invalid.");
        }
    }
}
