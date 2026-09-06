using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateSaveSizePreflightSmoke
    {
        internal static void Run()
        {
            OversizedTemplateFailsBeforeFilesystemMutation();
        }

        private static void OversizedTemplateFailsBeforeFilesystemMutation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-template-size-preflight-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "nested", "oversized.qs3d-template.xml");
            var profile = new TemplateProfile("T-OVERSIZED", "Oversized template");
            var family = new ProjectFamily("F-OVERSIZED", "Oversized family", ElementCategory.ArchitecturalWall);
            InjectLegacyFamilyProperty(family, "Payload", new string('\u0800', 3 * 1024 * 1024));
            profile.Families.Add(family);

            try
            {
                try
                {
                    new TemplateProfileStore().Save(profile, path);
                }
                catch (InvalidDataException ex)
                {
                    if (!string.Equals(ex.Message, "QS3D template exceeds 8 MiB.", StringComparison.Ordinal))
                        throw new Exception("Unexpected oversized template error: " + ex.Message, ex);
                    if (Directory.Exists(root))
                        throw new Exception("Oversized template save created the destination directory before size rejection.");
                    if (File.Exists(path))
                        throw new Exception("Oversized template save created the destination file before size rejection.");
                    return;
                }

                throw new Exception("Oversized template save must fail closed before filesystem mutation.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void InjectLegacyFamilyProperty(ProjectFamily family, string key, string value)
        {
            var innerField = family.Properties.GetType().GetField("_inner", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Template-size legacy fixture could not locate the Family property backing dictionary.");
            var inner = innerField.GetValue(family.Properties) as Dictionary<string, string>
                ?? throw new InvalidOperationException("Template-size legacy fixture Family property backing dictionary had an unexpected type.");
            inner[key] = value;
        }
    }
}
