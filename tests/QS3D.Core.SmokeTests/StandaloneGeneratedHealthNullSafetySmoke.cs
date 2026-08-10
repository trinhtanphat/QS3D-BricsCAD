using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class StandaloneGeneratedHealthNullSafetySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-null-health", "Null-safe standalone health");
            project.Elements.Add(null!);

            RequireNoThrow(() => new GeneratedFoundationMeshHealthService().Inspect(project), "foundation mesh health");
            RequireNoThrow(() => new GeneratedCurtainFrameHealthService().Inspect(project), "curtain frame health");
            RequireNoThrow(() => new GeneratedSemanticTagHealthService().Inspect(project), "semantic tag health");
            RequireNoThrow(() => new GeneratedGridAnnotationHealthService().Inspect(project), "grid annotation health");
            RequireNoThrow(() => new GeneratedRebarOwnershipHealthService().Inspect(project), "rebar ownership health");
        }

        private static void RequireNoThrow(Action action, string provider)
        {
            try { action(); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("StandaloneGeneratedHealthNullSafetySmoke: " + provider + " crashed on a null semantic entry.", ex);
            }
        }
    }
}
