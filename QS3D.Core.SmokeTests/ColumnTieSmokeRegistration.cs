using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieSmokeRegistration
    {
        [ModuleInitializer]
        internal static void InitializeColumnTieSmoke()
        {
            ColumnTieLayoutSmoke.Run();
        }
    }
}
