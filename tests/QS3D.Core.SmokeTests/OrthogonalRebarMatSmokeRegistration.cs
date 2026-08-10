using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class OrthogonalRebarMatSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OrthogonalRebarMatSmoke.Run();
        }
    }
}
