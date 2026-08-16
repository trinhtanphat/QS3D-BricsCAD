using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateProfileIdentityCanonicalityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => TemplateProfileIdentityCanonicalitySmoke.Run();
    }
}
