using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedRebarModeNullSafetySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-null-mode", "Null-safe rebar mode health");
            project.Elements.Add(null!);

            var slab = new ProjectElement("S1", ElementCategory.Slab, string.Empty, string.Empty, string.Empty);
            slab.Properties["GeneratedSlabMeshHandles"] = "AA";
            slab.Properties["GeneratedSlabMeshMode"] = "SlabMeshXY";
            slab.Properties["GeneratedSlabMeshXDiameterMm"] = "10";
            slab.Properties["GeneratedSlabMeshYDiameterMm"] = "10";
            slab.Properties["GeneratedSlabMeshCoverM"] = "0.03";
            slab.Properties["GeneratedSlabMeshXActualSpacingM"] = "0.2";
            slab.Properties["GeneratedSlabMeshYActualSpacingM"] = "0.2";
            slab.Properties["GeneratedSlabMeshFaces"] = "Bottom";
            project.Elements.Add(slab);

            var issues = new GeneratedRebarModeHealthService().Inspect(project);
            if (issues.Any(x => x.Severity == HealthSeverity.Error || x.Code == "GENERATED_REBAR_MODE_METADATA_INVALID"))
                throw new InvalidOperationException("GeneratedRebarModeNullSafetySmoke: valid slab mode metadata should remain diagnosable when the project contains a null semantic entry.");
        }
    }
}
