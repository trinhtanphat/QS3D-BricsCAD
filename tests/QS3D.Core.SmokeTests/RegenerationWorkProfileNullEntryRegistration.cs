using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfileNullEntryRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RegenerationWorkProfileNullEntrySmoke.Run();
    }
}
