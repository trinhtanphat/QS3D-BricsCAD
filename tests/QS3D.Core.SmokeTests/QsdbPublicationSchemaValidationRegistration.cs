using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbPublicationSchemaValidationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsdbPublicationSchemaValidationSmoke.Run();
    }
}
