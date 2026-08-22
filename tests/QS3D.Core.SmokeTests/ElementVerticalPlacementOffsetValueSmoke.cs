using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementVerticalPlacementOffsetValueSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CanonicalConfiguredOffsetsRemainReadable();
            PaddedBottomOffsetFailsClosedWithoutMutation();
            PaddedTopOffsetFailsClosedWithoutMutation();
            MissingOffsetFallbackAndMalformedRejectionRemainStable();
        }

        private static void CanonicalConfiguredOffsetsRemainReadable()
        {
            var project = NewProject();
            var element = NewElement(project, "canonical-offsets");
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
            element.Properties[ProjectFloorService.TopLevelIdKey] = "L2";
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = "0.25";
            element.Properties[ProjectFloorService.TopLevelOffsetKey] = "-0.5";

            var placement = ElementVerticalPlacementService.Resolve(
                project, element, double.NaN, double.NaN, double.NaN);

            Equal(3.25d, placement.BottomElevationM, "canonical bottom offset");
            Equal(6.5d, placement.TopElevationM, "canonical top offset");
            Equal(0.25d,
                ElementVerticalPlacementService.ReadLevelOffset(element, ProjectFloorService.BottomLevelOffsetKey),
                "canonical direct offset read");
        }

        private static void PaddedBottomOffsetFailsClosedWithoutMutation()
        {
            foreach (var raw in new[] { " 0.25", "0.25 ", "\t0.25\r\n" })
            {
                var project = NewProject();
                var element = NewElement(project, "padded-bottom");
                element.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
                element.Properties[ProjectFloorService.BottomLevelOffsetKey] = raw;
                var version = project.ChangeVersion;

                Contains(
                    ThrowsMessage<InvalidOperationException>(() => ElementVerticalPlacementService.Resolve(
                        project, element, double.NaN, 2d, double.NaN)),
                    "canonical finite invariant number");
                Equal(version, project.ChangeVersion, "padded bottom project version");
                Equal(raw, element.Properties[ProjectFloorService.BottomLevelOffsetKey], "padded bottom stored value");
            }
        }

        private static void PaddedTopOffsetFailsClosedWithoutMutation()
        {
            foreach (var raw in new[] { " -0.5", "-0.5 ", "\t-0.5\n" })
            {
                var project = NewProject();
                var element = NewElement(project, "padded-top");
                element.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
                element.Properties[ProjectFloorService.TopLevelIdKey] = "L2";
                element.Properties[ProjectFloorService.TopLevelOffsetKey] = raw;
                var version = project.ChangeVersion;

                Contains(
                    ThrowsMessage<InvalidOperationException>(() => ElementVerticalPlacementService.Resolve(
                        project, element, double.NaN, double.NaN, double.NaN)),
                    "canonical finite invariant number");
                Equal(version, project.ChangeVersion, "padded top project version");
                Equal(raw, element.Properties[ProjectFloorService.TopLevelOffsetKey], "padded top stored value");
            }
        }

        private static void MissingOffsetFallbackAndMalformedRejectionRemainStable()
        {
            var element = new ProjectElement("offset-value-read", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            Equal(0d,
                ElementVerticalPlacementService.ReadLevelOffset(element, ProjectFloorService.BottomLevelOffsetKey),
                "missing offset fallback");

            foreach (var raw in new[] { "", "   ", "not-a-number", "NaN", "Infinity" })
            {
                element.Properties[ProjectFloorService.BottomLevelOffsetKey] = raw;
                Throws<InvalidOperationException>(() =>
                    ElementVerticalPlacementService.ReadLevelOffset(element, ProjectFloorService.BottomLevelOffsetKey));
            }
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("VERTICAL-OFFSET-VALUE", "Vertical offset value smoke");
            project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));
            project.Floors.Add(new FloorDefinition("L1", "Level 1", 3d));
            project.Floors.Add(new FloorDefinition("L2", "Level 2", 7d));
            project.ActiveFloorId = "L0";
            return project;
        }

        private static ProjectElement NewElement(ProjectState project, string id)
        {
            var element = new ProjectElement(id, ElementCategory.Beam, string.Empty, "L0", string.Empty);
            project.Elements.Add(element);
            return element;
        }

        private static string ThrowsMessage<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                return ex.Message;
            }

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Contains(string actual, string expectedFragment)
        {
            if (actual == null || actual.IndexOf(expectedFragment, StringComparison.Ordinal) < 0)
                throw new Exception("Expected message containing '" + expectedFragment + "' but got '" + actual + "'.");
        }

        private static void Equal(double expected, double actual, string scope)
        {
            if (BitConverter.DoubleToInt64Bits(expected) != BitConverter.DoubleToInt64Bits(actual))
                throw new Exception(scope + ": expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(long expected, long actual, string scope)
        {
            if (expected != actual)
                throw new Exception(scope + ": expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(string expected, string actual, string scope)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(scope + ": expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
