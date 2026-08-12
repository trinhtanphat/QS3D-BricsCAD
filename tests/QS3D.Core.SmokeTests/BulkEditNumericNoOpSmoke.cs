using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditNumericNoOpSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            GeometryNumericNoOpPreservesLexicalAndFreshnessState();
            NonGeometryNumericNoOpPreservesLexicalState();
            RealGeometryNumericChangeStillMutates();
            ParseUnderflowFailsWithoutMutation();
            MultiplicationUnderflowFailsWithoutMutation();
            LegitimateZeroAndRepresentableSubnormalRemainValid();
        }

        private static void GeometryNumericNoOpPreservesLexicalAndFreshnessState()
        {
            var setup = NewWall("noop-geometry");
            setup.Element.Properties["WidthM"] = "1.0";
            setup.Element.Properties["GeneratedSolidHandle"] = "A1";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeProjectVersion = setup.Project.ChangeVersion;
            var beforeProjectUpdatedUtc = setup.Project.UpdatedUtc;
            var beforeElementUpdatedUtc = setup.Element.UpdatedUtc;

            var changed = new BulkEditService().MultiplyNumericProperty(
                setup.Project,
                new[] { setup.Element },
                "WidthM",
                1d);

            if (changed.Count != 0)
                throw new InvalidOperationException("Bulk numeric x1 on an exact numeric value must report no changed elements.");
            if (!setup.Element.Properties.TryGetValue("WidthM", out var raw) || !string.Equals(raw, "1.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Bulk numeric x1 rewrote the geometry property's lexical representation.");
            if (setup.Project.ChangeVersion != beforeProjectVersion || setup.Project.UpdatedUtc != beforeProjectUpdatedUtc)
                throw new InvalidOperationException("Bulk numeric x1 advanced project freshness for an exact numeric no-op.");
            if (setup.Element.Dirty != ElementDirtyFlags.None || setup.Element.UpdatedUtc != beforeElementUpdatedUtc)
                throw new InvalidOperationException("Bulk numeric x1 dirtied the element for an exact numeric no-op.");
            if (setup.Element.IsGeneratedSolidStale())
                throw new InvalidOperationException("Bulk numeric x1 marked generated solid output stale for an exact numeric no-op.");
        }

        private static void NonGeometryNumericNoOpPreservesLexicalState()
        {
            var setup = NewWall("noop-property");
            setup.Element.Properties["Scale"] = "1.0";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeProjectVersion = setup.Project.ChangeVersion;
            var beforeProjectUpdatedUtc = setup.Project.UpdatedUtc;
            var beforeElementUpdatedUtc = setup.Element.UpdatedUtc;

            var changed = new BulkEditService().MultiplyNumericProperty(
                setup.Project,
                new[] { setup.Element },
                "Scale",
                1d);

            if (changed.Count != 0)
                throw new InvalidOperationException("Bulk numeric x1 on a non-geometry property must report no changed elements.");
            if (!setup.Element.Properties.TryGetValue("Scale", out var raw) || !string.Equals(raw, "1.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Bulk numeric x1 rewrote a non-geometry property's lexical representation.");
            if (setup.Project.ChangeVersion != beforeProjectVersion || setup.Project.UpdatedUtc != beforeProjectUpdatedUtc)
                throw new InvalidOperationException("Bulk numeric x1 advanced project freshness for a non-geometry no-op.");
            if (setup.Element.Dirty != ElementDirtyFlags.None || setup.Element.UpdatedUtc != beforeElementUpdatedUtc)
                throw new InvalidOperationException("Bulk numeric x1 dirtied the element for a non-geometry no-op.");
        }

        private static void RealGeometryNumericChangeStillMutates()
        {
            var setup = NewWall("real-change");
            setup.Element.Properties["WidthM"] = "1.0";
            setup.Element.Properties["GeneratedSolidHandle"] = "A1";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeProjectVersion = setup.Project.ChangeVersion;

            var changed = new BulkEditService().MultiplyNumericProperty(
                setup.Project,
                new[] { setup.Element },
                "WidthM",
                2d);

            if (changed.Count != 1 || !string.Equals(changed[0], setup.Element.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("A real bulk numeric multiplication did not report the changed element.");
            if (!setup.Element.Properties.TryGetValue("WidthM", out var raw) || !string.Equals(raw, "2", StringComparison.Ordinal))
                throw new InvalidOperationException("A real bulk numeric multiplication did not persist the expected round-trip value.");
            if (setup.Project.ChangeVersion != checked(beforeProjectVersion + 1L))
                throw new InvalidOperationException("A real bulk numeric multiplication must advance project revision exactly once.");
            var requiredDirty = ElementDirtyFlags.Geometry | ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if ((setup.Element.Dirty & requiredDirty) != requiredDirty)
                throw new InvalidOperationException("A real geometry numeric multiplication did not preserve expected dirty flags.");
            if (!setup.Element.IsGeneratedSolidStale())
                throw new InvalidOperationException("A real geometry numeric multiplication did not mark generated solid output stale.");
        }

        private static void ParseUnderflowFailsWithoutMutation()
        {
            var setup = NewWall("parse-underflow");
            setup.Element.Properties["Scale"] = "1e-4000";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeProjectVersion = setup.Project.ChangeVersion;
            var beforeProjectUpdatedUtc = setup.Project.UpdatedUtc;
            var beforeElementUpdatedUtc = setup.Element.UpdatedUtc;

            ExpectInvalidOperation(() => new BulkEditService().MultiplyNumericProperty(
                setup.Project,
                new[] { setup.Element },
                "Scale",
                2d));

            if (!setup.Element.Properties.TryGetValue("Scale", out var raw) || !string.Equals(raw, "1e-4000", StringComparison.Ordinal))
                throw new InvalidOperationException("Bulk numeric parse underflow mutated the original property token.");
            if (setup.Project.ChangeVersion != beforeProjectVersion || setup.Project.UpdatedUtc != beforeProjectUpdatedUtc)
                throw new InvalidOperationException("Bulk numeric parse underflow changed project freshness state.");
            if (setup.Element.Dirty != ElementDirtyFlags.None || setup.Element.UpdatedUtc != beforeElementUpdatedUtc)
                throw new InvalidOperationException("Bulk numeric parse underflow partially mutated the target element.");
        }

        private static void MultiplicationUnderflowFailsWithoutMutation()
        {
            var setup = NewWall("multiply-underflow");
            var epsilonText = double.Epsilon.ToString("R", CultureInfo.InvariantCulture);
            setup.Element.Properties["Scale"] = epsilonText;
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeProjectVersion = setup.Project.ChangeVersion;
            var beforeProjectUpdatedUtc = setup.Project.UpdatedUtc;
            var beforeElementUpdatedUtc = setup.Element.UpdatedUtc;

            ExpectInvalidOperation(() => new BulkEditService().MultiplyNumericProperty(
                setup.Project,
                new[] { setup.Element },
                "Scale",
                0.5d));

            if (!setup.Element.Properties.TryGetValue("Scale", out var raw) || !string.Equals(raw, epsilonText, StringComparison.Ordinal))
                throw new InvalidOperationException("Bulk numeric multiplication underflow replaced a representable subnormal with zero.");
            if (setup.Project.ChangeVersion != beforeProjectVersion || setup.Project.UpdatedUtc != beforeProjectUpdatedUtc)
                throw new InvalidOperationException("Bulk numeric multiplication underflow changed project freshness state.");
            if (setup.Element.Dirty != ElementDirtyFlags.None || setup.Element.UpdatedUtc != beforeElementUpdatedUtc)
                throw new InvalidOperationException("Bulk numeric multiplication underflow partially mutated the target element.");
        }

        private static void LegitimateZeroAndRepresentableSubnormalRemainValid()
        {
            var zeroSetup = NewWall("true-zero");
            zeroSetup.Element.Properties["Scale"] = "0e-4000";
            zeroSetup.Element.MarkClean(ElementDirtyFlags.All);
            var zeroVersion = zeroSetup.Project.ChangeVersion;

            var zeroChanged = new BulkEditService().MultiplyNumericProperty(
                zeroSetup.Project,
                new[] { zeroSetup.Element },
                "Scale",
                2d);

            if (zeroChanged.Count != 0 || zeroSetup.Project.ChangeVersion != zeroVersion)
                throw new InvalidOperationException("A legitimate exact-zero scientific token must remain a no-op when multiplied by a finite factor.");
            if (!zeroSetup.Element.Properties.TryGetValue("Scale", out var zeroRaw) || !string.Equals(zeroRaw, "0e-4000", StringComparison.Ordinal))
                throw new InvalidOperationException("A legitimate exact-zero scientific token lost its lexical representation.");

            var subnormalSetup = NewWall("representable-subnormal");
            var epsilonText = double.Epsilon.ToString("R", CultureInfo.InvariantCulture);
            var expected = (double.Epsilon * 2d).ToString("R", CultureInfo.InvariantCulture);
            subnormalSetup.Element.Properties["Scale"] = epsilonText;
            subnormalSetup.Element.MarkClean(ElementDirtyFlags.All);

            var changed = new BulkEditService().MultiplyNumericProperty(
                subnormalSetup.Project,
                new[] { subnormalSetup.Element },
                "Scale",
                2d);

            if (changed.Count != 1)
                throw new InvalidOperationException("A representable subnormal multiplication was rejected as underflow.");
            if (!subnormalSetup.Element.Properties.TryGetValue("Scale", out var subnormalRaw) || !string.Equals(subnormalRaw, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("A representable subnormal multiplication did not persist its exact round-trip result.");

            var zeroFactorSetup = NewWall("zero-factor");
            zeroFactorSetup.Element.Properties["Scale"] = "2";
            zeroFactorSetup.Element.MarkClean(ElementDirtyFlags.All);
            var zeroFactorChanged = new BulkEditService().MultiplyNumericProperty(
                zeroFactorSetup.Project,
                new[] { zeroFactorSetup.Element },
                "Scale",
                0d);
            if (zeroFactorChanged.Count != 1 || !zeroFactorSetup.Element.Properties.TryGetValue("Scale", out var zeroFactorRaw) || !string.Equals(zeroFactorRaw, "0", StringComparison.Ordinal))
                throw new InvalidOperationException("Multiplication by an explicit zero factor must remain a legitimate zero-producing edit.");
        }

        private static void ExpectInvalidOperation(Action action)
        {
            try
            {
                action();
                throw new InvalidOperationException("Expected BulkEdit numeric underflow to fail closed.");
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, "Expected BulkEdit numeric underflow to fail closed.", StringComparison.Ordinal)) throw;
            }
        }

        private static Setup NewWall(string suffix)
        {
            var project = new ProjectState("P-BULK-NUMERIC-" + suffix, "Bulk numeric no-op");
            var element = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private sealed class Setup
        {
            public Setup(ProjectState project, ProjectElement element)
            {
                Project = project;
                Element = element;
            }

            public ProjectState Project { get; }
            public ProjectElement Element { get; }
        }
    }
}
