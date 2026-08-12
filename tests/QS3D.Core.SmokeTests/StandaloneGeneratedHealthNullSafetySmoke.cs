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
            var project = new ProjectState("P-null-health", "Fail-visible standalone health");
            project.Elements.Add(null!);

            RequireFailVisible(() => new GeneratedFoundationMeshHealthService().Inspect(project), "foundation mesh health");
            RequireFailVisible(() => new GeneratedCurtainFrameHealthService().Inspect(project), "curtain frame health");
            RequireFailVisible(() => new GeneratedSemanticTagHealthService().Inspect(project), "semantic tag health");
            RequireFailVisible(() => new GeneratedGridAnnotationHealthService().Inspect(project), "grid annotation health");
            RequireFailVisible(() => new GeneratedRebarOwnershipHealthService().Inspect(project), "rebar ownership health");
        }

        private static void RequireFailVisible(Action action, string provider)
        {
            try { action(); }
            catch (InvalidOperationException) { return; }
            throw new InvalidOperationException("StandaloneGeneratedHealthNullSafetySmoke: " + provider + " must reject a null semantic entry.");
        }
    }
}
