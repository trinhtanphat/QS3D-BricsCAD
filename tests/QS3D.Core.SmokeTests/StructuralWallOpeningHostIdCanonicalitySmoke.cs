using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class StructuralWallOpeningHostIdCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CanonicalHostIsDeducted();
            PaddedHostFailsBeforeQuantityMutation();
            WhitespaceOnlyHostFailsBeforeQuantityMutation();
            MissingAndEmptyHostsRemainUnhosted();
        }

        private static void CanonicalHostIsDeducted()
        {
            var setup = NewSetup("canonical", "W1", true);
            new StructuralRegenerator().Regenerate(setup.Project, setup.Wall);

            RequireQuantity(setup.Wall, "GrossWallAreaM2", 30d, "canonical host gross area");
            RequireQuantity(setup.Wall, "OpeningAreaM2", 2d, "canonical linked opening area");
            RequireQuantity(setup.Wall, "NetWallAreaM2", 28d, "canonical host net area");
            RequireQuantity(setup.Wall, "DeductionM3", 0.4d, "canonical linked opening deduction");
        }

        private static void PaddedHostFailsBeforeQuantityMutation()
        {
            var setup = NewSetup("padded", " W1 ", true);
            setup.Wall.Quantities["Sentinel"] = 7d;
            var beforeCount = setup.Wall.Quantities.Count;

            Throws<InvalidOperationException>(() => new StructuralRegenerator().Regenerate(setup.Project, setup.Wall));
            if (setup.Wall.Quantities.Count != beforeCount ||
                !setup.Wall.Quantities.TryGetValue("Sentinel", out var sentinel) || sentinel != 7d)
                throw new InvalidOperationException("Padded HostWallId partially mutated structural-wall quantities before rejection.");
        }

        private static void WhitespaceOnlyHostFailsBeforeQuantityMutation()
        {
            var setup = NewSetup("whitespace", "   ", true);
            setup.Wall.Quantities["Sentinel"] = 9d;
            var beforeCount = setup.Wall.Quantities.Count;

            Throws<InvalidOperationException>(() => new StructuralRegenerator().Regenerate(setup.Project, setup.Wall));
            if (setup.Wall.Quantities.Count != beforeCount ||
                !setup.Wall.Quantities.TryGetValue("Sentinel", out var sentinel) || sentinel != 9d)
                throw new InvalidOperationException("Whitespace-only HostWallId partially mutated structural-wall quantities before rejection.");
        }

        private static void MissingAndEmptyHostsRemainUnhosted()
        {
            var missing = NewSetup("missing", string.Empty, false);
            var empty = NewSetup("empty", string.Empty, true);

            new StructuralRegenerator().Regenerate(missing.Project, missing.Wall);
            new StructuralRegenerator().Regenerate(empty.Project, empty.Wall);

            RequireQuantity(missing.Wall, "OpeningAreaM2", 0d, "missing HostWallId");
            RequireQuantity(empty.Wall, "OpeningAreaM2", 0d, "empty HostWallId");
        }

        private static Setup NewSetup(string suffix, string hostId, bool includeHostProperty)
        {
            var project = new ProjectState("P-STRUCT-HOST-" + suffix, "Structural wall host canonicality");
            var wall = new ProjectElement("W1", ElementCategory.StructuralWall);
            wall.Properties["LengthM"] = "10";
            wall.Properties["HeightM"] = "3";
            wall.Properties["ThicknessM"] = "0.2";

            var opening = new ProjectElement("O1", ElementCategory.WallOpening);
            if (includeHostProperty) opening.Properties["HostWallId"] = hostId;
            opening.Quantities["OpeningAreaM2"] = 2d;

            project.Elements.Add(wall);
            project.Elements.Add(opening);
            return new Setup(project, wall);
        }

        private static void RequireQuantity(ProjectElement element, string key, double expected, string label)
        {
            if (!element.Quantities.TryGetValue(key, out var actual) || Math.Abs(actual - expected) > 1e-12)
                throw new InvalidOperationException(label + " produced unexpected quantity " + key + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
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

        private sealed class Setup
        {
            internal Setup(ProjectState project, ProjectElement wall)
            {
                Project = project;
                Wall = wall;
            }

            internal ProjectState Project { get; }
            internal ProjectElement Wall { get; }
        }
    }
}
