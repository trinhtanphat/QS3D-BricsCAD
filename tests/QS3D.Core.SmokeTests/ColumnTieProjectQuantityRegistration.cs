using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieProjectQuantityRegistration
    {
        [ModuleInitializer]
        internal static void InitializeColumnTieProjectQuantity()
        {
            ColumnTieProjectQuantitySmoke.Run();
        }
    }
}
