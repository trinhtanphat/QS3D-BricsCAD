using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedGeometryStaleSmoke
    {
        public static void Run()
        {
            GeneratedOutputsBecomeStaleAfterSemanticEdit();
            ReplacedHandleAutoResolvesOnlyItsOwnStaleKind();
            ExplicitClearPreservesOtherStaleKinds();
            ElementsWithoutGeneratedOutputsRemainFresh();
        }

        private static void GeneratedOutputsBecomeStaleAfterSemanticEdit()
        {
            var element = Element();
            element.Properties["GeneratedSolidHandle"] = "AA";
            element.Properties["GeneratedRebarHandles"] = "BB;BC";
            element.Properties["GeneratedShapeRebarHandles"] = "CC";
            element.MarkGeneratedGeometryStale("Thickness changed");

            True(element.IsGeneratedSolidStale());
            True(element.IsGeneratedRebarStale());
            True(element.IsGeneratedShapeRebarStale());
            True(element.IsGeneratedGeometryStale());
            Equal("Thickness changed", element.Properties[ProjectElement.GeneratedGeometryStaleReasonKey]);
        }

        private static void ReplacedHandleAutoResolvesOnlyItsOwnStaleKind()
        {
            var element = Element();
            element.Properties["GeneratedSolidHandle"] = "AA";
            element.Properties["GeneratedRebarHandles"] = "BB";
            element.Properties["GeneratedShapeRebarHandles"] = "CC";
            element.MarkGeneratedGeometryStale("Family changed");

            element.Properties["GeneratedRebarHandles"] = "BD";
            False(element.IsGeneratedRebarStale());
            True(element.IsGeneratedSolidStale());
            True(element.IsGeneratedShapeRebarStale());
            True(element.IsGeneratedGeometryStale());

            element.Properties["GeneratedSolidHandle"] = "AD";
            False(element.IsGeneratedSolidStale());
            True(element.IsGeneratedShapeRebarStale());

            element.Properties["GeneratedShapeRebarHandles"] = "CD";
            False(element.IsGeneratedShapeRebarStale());
            False(element.IsGeneratedGeometryStale());
        }

        private static void ExplicitClearPreservesOtherStaleKinds()
        {
            var element = Element();
            element.Properties["GeneratedSolidHandle"] = "10";
            element.Properties["GeneratedRebarHandles"] = "20";
            element.Properties["GeneratedShapeRebarHandles"] = "30";
            element.MarkGeneratedGeometryStale("Instance changed");

            element.ClearGeneratedRebarStale();
            False(element.IsGeneratedRebarStale());
            True(element.IsGeneratedSolidStale());
            True(element.IsGeneratedShapeRebarStale());

            element.ClearGeneratedSolidStale();
            False(element.IsGeneratedSolidStale());
            True(element.IsGeneratedShapeRebarStale());

            element.ClearGeneratedShapeRebarStale();
            False(element.IsGeneratedGeometryStale());
        }

        private static void ElementsWithoutGeneratedOutputsRemainFresh()
        {
            var element = Element();
            element.MarkGeneratedGeometryStale("No generated geometry");
            False(element.IsGeneratedGeometryStale());
            False(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStateKey));
        }

        private static ProjectElement Element() => new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void False(bool value) { if (value) throw new Exception("Expected false."); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
    }
}
