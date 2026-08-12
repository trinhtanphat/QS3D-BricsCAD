using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class WallPierChamferUnitClassifierSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RequireLinear("WallPierChamferM");
            RequireLinear("ChamferM");
            RequireNotLinear("BIM");
            RequireNotLinear("WallPierChamferMm");
            RequireNotLinear("WallPierChamferM2");
            RequireNotLinear("WallPierChamferM3");
        }

        private static void RequireLinear(string key)
        {
            if (!SemanticPropertyUnitClassifier.IsLinearMeterProperty(key))
                throw new InvalidOperationException(key + " must be classified as a linear-meter semantic property.");
        }

        private static void RequireNotLinear(string key)
        {
            if (SemanticPropertyUnitClassifier.IsLinearMeterProperty(key))
                throw new InvalidOperationException(key + " must not be classified as a linear-meter semantic property.");
        }
    }
}
