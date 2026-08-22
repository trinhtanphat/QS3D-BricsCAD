using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class Point2InvariantFormattingRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => Point2InvariantFormattingSmoke.Run();
    }
}
