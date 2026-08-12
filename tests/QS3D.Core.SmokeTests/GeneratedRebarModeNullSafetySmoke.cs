using System;
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
            try
            {
                new GeneratedRebarModeHealthService().Inspect(project);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("GeneratedRebarModeNullSafetySmoke: malformed null semantic entries must fail visibly.");
        }
    }
}
