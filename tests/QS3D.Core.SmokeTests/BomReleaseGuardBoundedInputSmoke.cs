using System;
using System.Collections.Generic;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class BomReleaseGuardBoundedInputSmoke
    {
        internal static void Run()
        {
            ExactBoundIsAccepted();
            OversizedInputFailsBeforeProjectTraversal();
        }

        private static void ExactBoundIsAccepted()
        {
            var project = new ProjectState("bom-live-bound", "BOM live bound");
            var handles = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < BomReleaseGuardService.MaxLiveGeneratedHandleInputs; i++)
                handles.Add("H" + i.ToString("X"));

            BomReleaseGuardService.Inspect(project, handles);
        }

        private static void OversizedInputFailsBeforeProjectTraversal()
        {
            var project = new ProjectState("bom-live-overflow", "BOM live overflow");
            project.Elements.Add(null!);
            var version = project.Version;
            var handles = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i <= BomReleaseGuardService.MaxLiveGeneratedHandleInputs; i++)
                handles.Add("H" + i.ToString("X"));

            try
            {
                BomReleaseGuardService.Inspect(project, handles);
                throw new Exception("Oversized live generated Handle input must fail closed.");
            }
            catch (InvalidOperationException ex)
            {
                var expected = "BOM live generated Handle input exceeds the supported bound of " + BomReleaseGuardService.MaxLiveGeneratedHandleInputs + ".";
                if (!string.Equals(ex.Message, expected, StringComparison.Ordinal))
                    throw new Exception("Unexpected BOM live-handle bound diagnostic: " + ex.Message);
            }

            if (project.Version != version || project.Elements.Count != 1 || project.Elements[0] != null)
                throw new Exception("Rejected BOM live-handle input must not mutate or traverse-repair project state.");
        }
    }
}
