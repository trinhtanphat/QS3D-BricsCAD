using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamStirrupMetadataHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            LegacySnapshotRemainsCompatible();
            AdvancedSnapshotIsAccepted();
            LengthMismatchIsReported();
            HookModeMismatchIsReported();
            MissingAdvancedModeIsReported();
        }

        private static void LegacySnapshotRemainsCompatible()
        {
            var project = Project("LEGACY");
            var element = Beam("B1");
            element.Properties["GeneratedBeamStirrupHandles"] = "AA;AB";
            element.Properties["GeneratedBeamStirrupCount"] = "2";
            element.Properties["GeneratedBeamStirrupDiameterMm"] = "8";
            element.Properties["GeneratedBeamStirrupMode"] = "Beam.Line.RectangularClosedLoop";
            project.Elements.Add(element);
            Equal(0, new GeneratedBeamStirrupHealthService().Inspect(project).Count);
        }

        private static void AdvancedSnapshotIsAccepted()
        {
            var project = Project("ADVANCED");
            var element = Beam("B2");
            SeedAdvanced(element, "AA;AB;AC", 3, 1.25d, .02d, .08d, 45d, "Beam.Line.RectangularHookedPath");
            project.Elements.Add(element);
            Equal(0, new GeneratedBeamStirrupHealthService().Inspect(project).Count);
        }

        private static void LengthMismatchIsReported()
        {
            var project = Project("LENGTH");
            var element = Beam("B3");
            SeedAdvanced(element, "AA;AB", 2, 1.1d, .02d, 0d, 0d, "Beam.Line.RectangularRoundedLoop");
            element.Properties["GeneratedBeamStirrupTotalCenterlineLengthM"] = "9";
            project.Elements.Add(element);
            True(new GeneratedBeamStirrupHealthService().Inspect(project).Any(x => x.Code == "BEAM_STIRRUP_GENERATED_LENGTH_MISMATCH"));
        }

        private static void HookModeMismatchIsReported()
        {
            var project = Project("HOOK");
            var element = Beam("B4");
            SeedAdvanced(element, "AA", 1, 1d, .02d, 0d, 0d, "Beam.Line.RectangularHookedPath");
            project.Elements.Add(element);
            True(new GeneratedBeamStirrupHealthService().Inspect(project).Any(x => x.Code == "BEAM_STIRRUP_GENERATED_MODE_MISMATCH"));
        }

        private static void MissingAdvancedModeIsReported()
        {
            var project = Project("MODE");
            var element = Beam("B5");
            SeedAdvanced(element, "AA", 1, 1d, .02d, 0d, 0d, "Beam.Line.RectangularRoundedLoop");
            element.Properties.Remove("GeneratedBeamStirrupMode");
            project.Elements.Add(element);
            True(new GeneratedBeamStirrupHealthService().Inspect(project).Any(x => x.Code == "BEAM_STIRRUP_GENERATED_MODE_INVALID"));
        }

        private static void SeedAdvanced(ProjectElement element, string handles, int count, double centerline, double bend, double hook, double angle, string mode)
        {
            element.Properties["GeneratedBeamStirrupHandles"] = handles;
            element.Properties["GeneratedBeamStirrupCount"] = count.ToString(CultureInfo.InvariantCulture);
            element.Properties["GeneratedBeamStirrupDiameterMm"] = "8";
            element.Properties["GeneratedBeamStirrupActualSpacingM"] = "0.15";
            element.Properties["GeneratedBeamStirrupCenterlineLengthM"] = centerline.ToString("R", CultureInfo.InvariantCulture);
            element.Properties["GeneratedBeamStirrupTotalCenterlineLengthM"] = (centerline * count).ToString("R", CultureInfo.InvariantCulture);
            element.Properties["GeneratedBeamStirrupPolylineLengthM"] = (centerline - .001d).ToString("R", CultureInfo.InvariantCulture);
            element.Properties["GeneratedBeamStirrupBendRadiusM"] = bend.ToString("R", CultureInfo.InvariantCulture);
            element.Properties["GeneratedBeamStirrupHookLengthM"] = hook.ToString("R", CultureInfo.InvariantCulture);
            element.Properties["GeneratedBeamStirrupHookTailAngleDeg"] = angle.ToString("R", CultureInfo.InvariantCulture);
            element.Properties["GeneratedBeamStirrupMode"] = mode;
        }

        private static ProjectState Project(string id) => new ProjectState(id, id);
        private static ProjectElement Beam(string id) => new ProjectElement(id, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }
    }
}
