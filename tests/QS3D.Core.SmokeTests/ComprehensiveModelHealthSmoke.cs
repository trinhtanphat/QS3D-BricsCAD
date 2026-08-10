using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ComprehensiveModelHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CoversAllGeneratedOutputFamilies();
            CoversDependencyAndStaleDiagnostics();
        }

        private static void CoversAllGeneratedOutputFamilies()
        {
            var project = Project("generated");
            project.Elements.Add(Element("LONG", ElementCategory.Column, "GeneratedRebarHandles", "A1"));
            project.Elements.Add(Element("SHAPE", ElementCategory.Beam, "GeneratedShapeRebarHandles", "A2"));
            project.Elements.Add(Element("TIE", ElementCategory.Column, "GeneratedTieRebarHandles", "A3"));
            project.Elements.Add(Element("STIRRUP", ElementCategory.Beam, "GeneratedBeamStirrupHandles", "A4"));
            project.Elements.Add(Element("SLAB", ElementCategory.Slab, "GeneratedSlabMeshHandles", "A5"));
            project.Elements.Add(Element("WALL", ElementCategory.StructuralWall, "GeneratedWallMeshHandles", "A6"));
            project.Elements.Add(Element("FOUNDATION", ElementCategory.Foundation, "GeneratedFoundationMeshHandles", "A7"));
            project.Elements.Add(Element("CURTAIN", ElementCategory.GlassWall, "GeneratedCurtainFrameHandles", "A8"));

            var live = new HashSet<string>(new[] { "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8" }, StringComparer.OrdinalIgnoreCase);
            var issues = new ComprehensiveModelHealthService().Inspect(project, null, live);

            HasPrefix(issues, "LONG", "GENERATED_REBAR_");
            HasPrefix(issues, "SHAPE", "SHAPE_REBAR_");
            HasPrefix(issues, "TIE", "TIE_REBAR_");
            HasPrefix(issues, "STIRRUP", "BEAM_STIRRUP_");
            HasPrefix(issues, "SLAB", "SLAB_MESH_");
            HasPrefix(issues, "WALL", "WALL_MESH_");
            HasPrefix(issues, "FOUNDATION", "FOUNDATION_MESH_");
            HasPrefix(issues, "CURTAIN", "CURTAIN_FRAME_");

            if (issues.Any(x => x.Code.EndsWith("GENERATED_SOLID_MISSING", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Composite health incorrectly reported a supplied live generated Solid3d handle as missing.");
        }

        private static void CoversDependencyAndStaleDiagnostics()
        {
            var project = Project("dependency");
            var first = new ProjectElement("A", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            var second = new ProjectElement("B", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            first.DependsOn.Add(second.Id);
            second.DependsOn.Add(first.Id);
            first.Properties["GeneratedSolidHandle"] = "B1";
            first.MarkGeneratedGeometryStale("smoke");
            project.Elements.Add(first);
            project.Elements.Add(second);

            var live = new HashSet<string>(new[] { "B1" }, StringComparer.OrdinalIgnoreCase);
            var issues = new ComprehensiveModelHealthService().Inspect(project, null, live);
            HasCode(issues, "DEPENDENCY_CYCLE");
            HasCode(issues, "GENERATED_SOLID_STALE");
        }

        private static ProjectState Project(string suffix) => new ProjectState("health-" + suffix, "Health " + suffix);

        private static ProjectElement Element(string id, ElementCategory category, string key, string handle)
        {
            var element = new ProjectElement(id, category, string.Empty, string.Empty, string.Empty);
            element.Properties[key] = handle;
            return element;
        }

        private static void HasPrefix(IEnumerable<ModelHealthIssue> issues, string elementId, string prefix)
        {
            if (issues.Any(x => string.Equals(x.ElementId, elementId, StringComparison.OrdinalIgnoreCase) && x.Code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) return;
            throw new InvalidOperationException("Composite health did not surface " + prefix + " diagnostics for " + elementId + ".");
        }

        private static void HasCode(IEnumerable<ModelHealthIssue> issues, string code)
        {
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase))) return;
            throw new InvalidOperationException("Composite health did not surface expected code " + code + ".");
        }
    }
}
