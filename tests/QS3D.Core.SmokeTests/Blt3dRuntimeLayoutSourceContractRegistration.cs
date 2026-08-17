using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class Blt3dRuntimeLayoutSourceContractRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => Blt3dRuntimeLayoutSourceContractSmoke.Run();
    }
}
