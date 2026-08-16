using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateProfileIdentityTextRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => TemplateProfileIdentityTextSmoke.Run();
    }
}
