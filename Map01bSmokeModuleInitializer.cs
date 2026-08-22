using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class Map01bSmokeModuleInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Map01bMappingPersistenceSmoke.Run();
        }
    }
}
