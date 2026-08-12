using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxXmlCharacterIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => XlsxXmlCharacterIntegritySmoke.Run();
    }
}
