using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementVerticalPlacementAdditivePrecisionSmoke
    {
        internal static void Run()
        {
            LegacyBottomOffsetRejectsSwallowedPositiveAndNegativeTerms();
            LegacyHeightRejectsSwallowedPositiveTerm();
            BottomLevelOffsetRejectsSwallowedPositiveAndNegativeTerms();
            TopLevelOffsetRejectsSwallowedPositiveAndNegativeTerms();
            HostedOpeningRejectsSwallowedHostSubtraction();
            ZeroCancellationAndRepresentableLargeTermsRemainValid();
        }

        private static void LegacyBottomOffsetRejectsSwallowedPositiveAndNegativeTerms()
        {
            foreach (var offset in new[] { 1d, -1d })
            {
                var project = NewProject();
                var element = NewElement(project, "legacy-bottom-" + offset);
                var version = project.ChangeVersion;
                Contains(
                    ThrowsMessage<InvalidOperationException>(() =>
                        ElementVerticalPlacementService.Resolve(project, element, 1e16d, 4d, offset)),
                    "legacy bottom elevation cannot preserve its non-zero additive term");
                Equal(version, project.ChangeVersion);
            }
        }

        private static void LegacyHeightRejectsSwallowedPositiveTerm()
        {
            var project = NewProject();
            var element = NewElement(project, "legacy-height");
            var version = project.ChangeVersion;
            Contains(
                ThrowsMessage<InvalidOperationException>(() =>
                    ElementVerticalPlacementService.Resolve(project, element, 1e16d, 1d, 0d)),
                "legacy top elevation cannot preserve its non-zero additive term");
            Equal(version, project.ChangeVersion);
        }

        private static void BottomLevelOffsetRejectsSwallowedPositiveAndNegativeTerms()
        {
            foreach (var offset in new[] { "1", "-1" })
            {
                var project = NewProject();
                project.Floors.Add(new FloorDefinition("HUGE", "Huge", 1e16d));
                var element = NewElement(project, "bottom-level-" + offset.Replace("-", "negative"));
                element.Properties[ProjectFloorService.BottomLevelIdKey] = "HUGE";
                element.Properties[ProjectFloorService.BottomLevelOffsetKey] = offset;
                var version = project.ChangeVersion;
                Contains(
                    ThrowsMessage<InvalidOperationException>(() =>
                        ElementVerticalPlacementService.Resolve(project, element, double.NaN, 4d, double.NaN)),
                    "bottom level elevation cannot preserve its non-zero additive term");
                Equal(version, project.ChangeVersion);
            }
        }

        private static void TopLevelOffsetRejectsSwallowedPositiveAndNegativeTerms()
        {
            foreach (var offset in new[] { "1", "-1" })
            {
                var project = NewProject();
                project.Floors.Add(new FloorDefinition("HUGE", "Huge", 1e16d));
                var element = NewElement(project, "top-level-" + offset.Replace("-", "negative"));
                element.Properties[ProjectFloorService.BottomLevelIdKey] = "L0";
                element.Properties[ProjectFloorService.TopLevelIdKey] = "HUGE";
                element.Properties[ProjectFloorService.TopLevelOffsetKey] = offset;
                var version = project.ChangeVersion;
                Contains(
                    ThrowsMessage<InvalidOperationException>(() =>
                        ElementVerticalPlacementService.Resolve(project, element, double.NaN, double.NaN, double.NaN)),
                    "top level elevation cannot preserve its non-zero additive term");
                Equal(version, project.ChangeVersion);
            }
        }

        private static void HostedOpeningRejectsSwallowedHostSubtraction()
        {
            var project = NewProject();
            var opening = NewElement(project, "hosted-opening", ElementCategory.WallOpening);
            var host = new ElementVerticalPlacement(false, false, 1d, 2d);
            var version = project.ChangeVersion;
            Contains(
                ThrowsMessage<InvalidOperationException>(() =>
                    ElementVerticalPlacementService.ResolveHostedOpening(project, host, opening, 4d, 1e16d)),
                "relative sill elevation cannot preserve its non-zero additive term");
            Equal(version, project.ChangeVersion);
        }

        private static void ZeroCancellationAndRepresentableLargeTermsRemainValid()
        {
            var project = NewProject();
            var zero = NewElement(project, "zero-offset");
            var zeroPlacement = ElementVerticalPlacementService.Resolve(project, zero, 10d, 3d, 0d);
            Equal(10d, zeroPlacement.BottomElevationM);
            Equal(13d, zeroPlacement.TopElevationM);

            var cancellation = NewElement(project, "cancellation");
            var cancellationPlacement = ElementVerticalPlacementService.Resolve(project, cancellation, 1d, 2d, -1d);
            Equal(0d, cancellationPlacement.BottomElevationM);
            Equal(2d, cancellationPlacement.TopElevationM);

            var representable = NewElement(project, "representable-large");
            var representablePlacement = ElementVerticalPlacementService.Resolve(project, representable, 1e16d, 4d, 2d);
            Equal(10000000000000002d, representablePlacement.BottomElevationM);
            Equal(10000000000000006d, representablePlacement.TopElevationM);
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("VERTICAL-PRECISION", "Vertical precision");
            project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));
            project.ActiveFloorId = "L0";
            return project;
        }

        private static ProjectElement NewElement(ProjectState project, string id, ElementCategory category = ElementCategory.Beam)
        {
            var element = new ProjectElement(id, category, string.Empty, "L0", string.Empty);
            project.Elements.Add(element);
            return element;
        }

        private static string ThrowsMessage<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T ex) { return ex.Message; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Contains(string actual, string expectedFragment)
        {
            if (actual == null || actual.IndexOf(expectedFragment, StringComparison.Ordinal) < 0)
                throw new Exception("Expected message containing '" + expectedFragment + "' but got '" + actual + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}
