using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class TopologyScaleRegistration
    {
        [ModuleInitializer]
        internal static void InitializeTopologyScale()
        {
            TopologyScaleSmoke.Run();
        }
    }
}
