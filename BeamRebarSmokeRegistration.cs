using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamRebarSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BeamRebarRegressionSmoke.Run();
    }
}