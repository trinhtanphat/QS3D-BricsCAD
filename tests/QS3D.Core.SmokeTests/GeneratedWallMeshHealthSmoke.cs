using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedWallMeshHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-wall-health", "Wall mesh ownership health");
            project.Elements.Add(null!);

            var wall = new ProjectElement("W1", ElementCategory.StructuralWall, string.Empty, string.Empty, string.Empty);
            wall.Properties["GeneratedWallMeshHandles"] = "AA";
            wall.Properties["GeneratedWallMeshCount"] = "1";
            wall.Properties["GeneratedWallMeshHorizontalDiameterMm"] = "10";
            wall.Properties["GeneratedWallMeshVerticalDiameterMm"] = "10";
            wall.Properties["GeneratedWallMeshHorizontalActualSpacingM"] = "0.2";
            wall.Properties["GeneratedWallMeshVerticalActualSpacingM"] = "0.2";
            wall.Properties["GeneratedWallMeshCoverM"] = "0.03";
            wall.Properties["GeneratedWallMeshFaces"] = "Both";
            wall.Properties["GeneratedWallMeshMode"] = "StructuralWallMesh";
            project.Elements.Add(wall);

            var later = new ProjectElement("FUTURE", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            later.Properties["GeneratedFutureWallHandles"] = "AA";
            project.Elements.Add(later);

            var issues = new GeneratedWallMeshHealthService().Inspect(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });

            if (!issues.Any(x => x.Code == "WALL_MESH_GENERATED_OWNERSHIP_CONFLICT" && x.ElementId == wall.Id && x.Severity == HealthSeverity.Error))
                throw new InvalidOperationException("GeneratedWallMeshHealthSmoke: later generated owner conflict must be detected regardless of project order.");
        }
    }
}
