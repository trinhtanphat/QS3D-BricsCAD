using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieQuantityRegistration
    {
        [ModuleInitializer]
        internal static void InitializeColumnTieQuantity()
        {
            ColumnTieQuantitySmoke.Run();
        }
    }
}
