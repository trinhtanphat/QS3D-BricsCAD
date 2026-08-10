using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarMatHealthSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RebarMatHealthSmoke.Run();
        }
    }
}
