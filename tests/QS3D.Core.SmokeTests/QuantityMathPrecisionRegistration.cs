using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityMathPrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            QuantityMathPrecisionSmoke.Run();
        }
    }
}
