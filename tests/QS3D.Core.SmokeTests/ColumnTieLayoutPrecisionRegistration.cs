using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieLayoutPrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ColumnTieLayoutPrecisionSmoke.Run();
    }
}
