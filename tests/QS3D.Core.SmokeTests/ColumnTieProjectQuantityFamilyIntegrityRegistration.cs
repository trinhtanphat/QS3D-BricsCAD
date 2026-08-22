using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieProjectQuantityFamilyIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ColumnTieProjectQuantityFamilyIntegritySmoke.Run();
    }
}
