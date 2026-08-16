using System;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateProfileIdentityCanonicalitySmoke
    {
        internal static void Run()
        {
            var canonical = new TemplateProfile("  qs3d-vn-mẫu  ", "  Mẫu Việt Nam  ");
            Equal("qs3d-vn-mẫu", canonical.Id, "Template id should retain existing outer-whitespace normalization.");
            Equal("Mẫu Việt Nam", canonical.Name, "Template name should retain existing outer-whitespace normalization.");

            canonical.Name = "  Hồ sơ kết cấu  ";
            Equal("Hồ sơ kết cấu", canonical.Name, "Template name setter should retain canonical normalization.");

            ThrowsArgument(() => new TemplateProfile("bad\nid", "Valid"), "Template id control character must fail closed.");
            ThrowsArgument(() => new TemplateProfile("valid", "bad\tname"), "Template name control character must fail closed.");
            ThrowsArgument(() => canonical.Name = "bad\0name", "Template name setter control character must fail closed.");
            ThrowsArgument(() => new TemplateProfile("bad\uFFFEid", "Valid"), "Template id XML-invalid text must fail closed.");
            ThrowsArgument(() => new TemplateProfile("valid", "bad\uFFFEname"), "Template name XML-invalid text must fail closed.");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(message + " Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void ThrowsArgument(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}
