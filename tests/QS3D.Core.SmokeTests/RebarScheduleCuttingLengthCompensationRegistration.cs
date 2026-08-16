using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarScheduleCuttingLengthCompensationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RebarScheduleCuttingLengthCompensationSmoke.Run();
    }
}
