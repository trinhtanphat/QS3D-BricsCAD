using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkFamilyRelationDirtySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BulkFamilyRelationDirtySmoke.Run();
    }
}
