using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieLayoutScalingUnderflowRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ColumnTieLayoutScalingUnderflowSmoke.Run();
    }
}
