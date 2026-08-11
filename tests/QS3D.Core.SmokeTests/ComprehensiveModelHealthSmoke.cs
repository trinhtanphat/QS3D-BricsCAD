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
            CoversSemanticIntegrityFamilies();
            CoversGeneratedLocateTargetClassification();
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

            var live = new HashSet<string>(new[] { " a1 ", " a2 ", " a3 ", " a4 ", " a5 ", " a6 ", " a7 ", " a8 " }, StringComparer.Ordinal);
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

        private static void CoversSemanticIntegrityFamilies()
        {
            var project = Project("semantic");
            project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));

            var level = new ProjectElement("LEVEL-BAD", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            level.Properties[ProjectFloorService.TopLevelIdKey] = "L0";
            project.Elements.Add(level);

            var finish = new ProjectElement("FINISH-BAD", ElementCategory.WallFinish, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(finish);

            project.Metadata[RebarFabricationQualificationHealthService.RequireQualificationMetadataKey] = "true";

            var issues = new ComprehensiveModelHealthService().Inspect(project);
            HasCode(issues, "TOP_LEVEL_REQUIRES_BOTTOM_LEVEL");
            HasCode(issues, "UNLINKED_ROOM_FINISH");
            HasCode(issues, "REBAR_FAB_OUTPUT_MISSING");
        }

        private static void CoversGeneratedLocateTargetClassification()
        {
            foreach (var code in new[]
            {
                "GENERATED_SOLID_STALE",
                "INVALID_REBAR_GENERATED_HANDLE",
                "SHAPE_REBAR_CATEGORY_MISMATCH",
                "TIE_REBAR_CATEGORY_MISMATCH",
                "BEAM_STIRRUP_CATEGORY_MISMATCH",
                "SLAB_MESH_COUNT_MISMATCH",
                "WALL_MESH_CATEGORY_MISMATCH",
                "FOUNDATION_MESH_COUNT_MISMATCH",
                "CURTAIN_FRAME_COUNT_INVALID",
                "INVALID_CURTAIN_FRAME_GENERATED_HANDLE"
            })
            {
                var issue = new ModelHealthIssue(code, HealthSeverity.Warning, "smoke", "E");
                if (!ComprehensiveModelHealthService.TargetsGeneratedOutput(issue))
                    throw new InvalidOperationException("Generated-output health code was not classified for generated CAD locate: " + code + ".");
            }

            foreach (var code in new[] { "MISSING_FAMILY", "DEPENDENCY_CYCLE", "TOP_LEVEL_REQUIRES_BOTTOM_LEVEL", "REBAR_FAB_OUTPUT_MISSING" })
            {
                var issue = new ModelHealthIssue(code, HealthSeverity.Warning, "smoke", "E");
                if (ComprehensiveModelHealthService.TargetsGeneratedOutput(issue))
                    throw new InvalidOperationException("Semantic/non-CAD health code was incorrectly classified as a generated CAD locate target: " + code + ".");
            }
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
