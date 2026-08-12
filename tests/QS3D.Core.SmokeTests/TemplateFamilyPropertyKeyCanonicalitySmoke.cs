using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateFamilyPropertyKeyCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsMalformedKey(string.Empty, "blank");
            RejectsMalformedKey(" WidthM ", "padded");
            AcceptsCanonicalKey();
        }

        private static void RejectsMalformedKey(string key, string label)
        {
            var project = new ProjectState("P-TEMPLATE-PROP-" + label, "Template family property key");
            var profile = ProfileWithProperty(key, "0.2");
            var beforeUpdated = project.UpdatedUtc;
            var beforeVersion = project.ChangeVersion;
            var beforeAudits = project.AuditEvents.Count;

            Throws<InvalidDataException>(() => new TemplateProfileStore().Apply(project, profile), label);

            Equal(0, project.Families.Count, label + " family count");
            Equal(beforeAudits, project.AuditEvents.Count, label + " audit count");
            Equal(beforeVersion, project.ChangeVersion, label + " change version");
            Equal(beforeUpdated, project.UpdatedUtc, label + " UpdatedUtc");
        }

        private static void AcceptsCanonicalKey()
        {
            var project = new ProjectState("P-TEMPLATE-PROP-OK", "Template family property key");
            var profile = ProfileWithProperty("WidthM", "0.2");

            var result = new TemplateProfileStore().Apply(project, profile);

            Equal(1, result.FamiliesAdded, "canonical FamiliesAdded");
            var family = project.FindFamily("F-WALL") ?? throw new Exception("Canonical template family was not applied.");
            Equal(1, family.Properties.Count, "canonical property count");
            Equal("0.2", family.Properties["WidthM"], "canonical WidthM value");
        }

        private static TemplateProfile ProfileWithProperty(string key, string value)
        {
            var profile = new TemplateProfile("T-PROP", "Template property key");
            var family = new ProjectFamily("F-WALL", "Wall", ElementCategory.ArchitecturalWall);
            family.Properties[key] = value;
            profile.Families.Add(family);
            return profile;
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("TemplateFamilyPropertyKeyCanonicalitySmoke expected " + typeof(TException).Name + " for " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("TemplateFamilyPropertyKeyCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
