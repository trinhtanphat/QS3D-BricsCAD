using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateProfileXmlNodeCanonicalityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => TemplateProfileXmlNodeCanonicalitySmoke.Run();
    }
}
