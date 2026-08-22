using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateProfileXmlTextPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            InvalidTemplateIdFailsBeforeFilesystemMutation();
            InvalidPropertyValueFailsBeforeFilesystemMutation();
            LoneSurrogateFailsBeforeFilesystemMutation();
            SupplementaryUnicodeRoundTrips();
        }

        private static void InvalidTemplateIdFailsBeforeFilesystemMutation()
        {
            var root = TempRoot("invalid-id");
            try
            {
                Throws<ArgumentException>(() => Profile("TPL-\u0001"));
                if (Directory.Exists(root))
                    throw new InvalidOperationException("Invalid template identity text mutated the filesystem before constructor rejection.");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static void InvalidPropertyValueFailsBeforeFilesystemMutation()
        {
            var profile = Profile("TPL-PROPERTY-CONTROL");
            Family(profile).Properties["Note"] = "bad\u0001value";
            AssertPreflightFailure(profile, "invalid-property-control");
        }

        private static void LoneSurrogateFailsBeforeFilesystemMutation()
        {
            var profile = Profile("TPL-LONE-SURROGATE");
            Family(profile).Properties["Note"] = new string(new[] { '\uD800' });
            AssertPreflightFailure(profile, "invalid-property-surrogate");
        }

        private static void SupplementaryUnicodeRoundTrips()
        {
            var root = TempRoot("valid-supplementary");
            var path = Path.Combine(root, "template.xml");
            const string expected = "Valid supplementary \U0001F642 text";
            var profile = Profile("TPL-SUPPLEMENTARY");
            var family = Family(profile);
            family.Properties["Note"] = expected;
            family.Properties["NullValue"] = null!;

            try
            {
                var store = new TemplateProfileStore();
                store.Save(profile, path);
                var loaded = store.Load(path);
                var loadedFamily = loaded.Families[0];
                if (!loadedFamily.Properties.TryGetValue("Note", out var actual) ||
                    !string.Equals(actual, expected, StringComparison.Ordinal))
                    throw new InvalidOperationException("Valid supplementary Unicode template property did not round-trip exactly.");
                if (!loadedFamily.Properties.TryGetValue("NullValue", out var nullValue) ||
                    !string.Equals(nullValue, string.Empty, StringComparison.Ordinal))
                    throw new InvalidOperationException("Null template property value no longer preserves empty-string serialization semantics.");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static TemplateProfile Profile(string id) => new TemplateProfile(id, "XML Text Template");

        private static ProjectFamily Family(TemplateProfile profile)
        {
            if (profile.Families.Count > 0) return profile.Families[0];
            var family = new ProjectFamily("F-XML-TEXT", "XML Text Family", ElementCategory.Beam);
            profile.Families.Add(family);
            return family;
        }

        private static void AssertPreflightFailure(TemplateProfile profile, string suffix)
        {
            var root = TempRoot(suffix);
            var path = Path.Combine(root, "template.xml");
            try
            {
                Throws<InvalidDataException>(() => new TemplateProfileStore().Save(profile, path));
                if (Directory.Exists(root))
                    throw new InvalidOperationException("Invalid template XML text mutated the filesystem before failing preflight: " + suffix + ".");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static string TempRoot(string suffix) =>
            Path.Combine(Path.GetTempPath(), "QS3D-TemplateXmlText-" + suffix + "-" + Guid.NewGuid().ToString("N"));

        private static void DeleteTree(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
