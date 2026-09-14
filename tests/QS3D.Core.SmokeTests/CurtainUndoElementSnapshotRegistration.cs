using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainUndoElementSnapshotRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CurtainUndoElementSnapshotSmoke.Run();
    }
}
