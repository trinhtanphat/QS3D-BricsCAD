using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Review;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class PreviewReviewNullOrderingSmoke
    {
        internal static void Run()
        {
            RejectsNullElementBeforeOrdering();
            RejectsNullChangeBeforeOrdering();
            PreservesCanonicalOrdering();
        }

        private static void RejectsNullElementBeforeOrdering()
        {
            var preview = NewProjectPreview(new QuantityRuleElementPreview[] { null! });
            ThrowsInvalid(
                () => new PreviewReviewSnapshotService().Create("null-element", preview),
                "Quantity-rule preview contains a null element preview at index 0.");
        }

        private static void RejectsNullChangeBeforeOrdering()
        {
            var element = NewElementPreview("E1", new QuantityRulePreviewChange[] { null! });
            var preview = NewProjectPreview(new[] { element });
            ThrowsInvalid(
                () => new PreviewReviewSnapshotService().Create("null-change", preview),
                "Quantity-rule preview contains a null change for element E1 at index 0.");
        }

        private static void PreservesCanonicalOrdering()
        {
            var second = NewElementPreview("B", new[] { NewChange("Z", QuantityRulePreviewChangeKind.Changed, 1d, 2d) });
            var first = NewElementPreview("A", new[]
            {
                NewChange("Y", QuantityRulePreviewChangeKind.Changed, 2d, 3d),
                NewChange("X", QuantityRulePreviewChangeKind.Added, null, 1d)
            });
            var snapshot = new PreviewReviewSnapshotService().Create("ordering", NewProjectPreview(new[] { second, first }));
            Equal(3, snapshot.Entries.Count);
            Equal("A", snapshot.Entries[0].ElementId);
            Equal("Quantity:X", snapshot.Entries[0].Field);
            Equal("A", snapshot.Entries[1].ElementId);
            Equal("Quantity:Y", snapshot.Entries[1].Field);
            Equal("B", snapshot.Entries[2].ElementId);
            Equal("Quantity:Z", snapshot.Entries[2].Field);
        }

        private static QuantityRuleProjectPreview NewProjectPreview(IEnumerable<QuantityRuleElementPreview> elements)
        {
            return Construct<QuantityRuleProjectPreview>("P1", 0L, elements);
        }

        private static QuantityRuleElementPreview NewElementPreview(string elementId, IEnumerable<QuantityRulePreviewChange> changes)
        {
            return Construct<QuantityRuleElementPreview>("P1", 0L, elementId, ElementCategory.Beam, changes);
        }

        private static QuantityRulePreviewChange NewChange(string output, QuantityRulePreviewChangeKind kind, double? before, double? after)
        {
            return Construct<QuantityRulePreviewChange>(output, kind, before, after, "before", "after");
        }

        private static T Construct<T>(params object?[] arguments)
        {
            var constructors = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length != arguments.Length) continue;
                try
                {
                    return (T)constructor.Invoke(arguments);
                }
                catch (TargetParameterCountException)
                {
                }
                catch (ArgumentException)
                {
                }
            }
            throw new InvalidOperationException("No compatible internal constructor found for " + typeof(T).Name + ".");
        }

        private static void ThrowsInvalid(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                Equal(expectedMessage, ex.Message);
                return;
            }
            throw new InvalidOperationException("Expected InvalidOperationException: " + expectedMessage);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but received " + actual + ".");
        }
    }
}
