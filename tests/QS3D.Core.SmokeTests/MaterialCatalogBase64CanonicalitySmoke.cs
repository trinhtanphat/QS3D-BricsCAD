using System;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialCatalogBase64CanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-MATERIAL-B64", "Material Base64 smoke");
            project.Metadata[ProjectMaterialCatalog.MetadataKey] = Record("custom-1", "Custom One", "kg", "desc");

            var canonical = ProjectMaterialCatalog.GetCustom(project);
            Equal(1, canonical.Count, "canonical material count");
            Equal("custom-1", canonical[0].Id, "canonical material id");

            var fields = project.Metadata[ProjectMaterialCatalog.MetadataKey].Split('|');
            fields[0] = fields[0] + " ";
            project.Metadata[ProjectMaterialCatalog.MetadataKey] = string.Join("|", fields);
            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.GetCustom(project), "padded Base64 field");
        }

        private static string Record(string id, string name, string unit, string description)
        {
            return string.Join("|", B64(id), B64(name), B64(unit), B64(description));
        }

        private static string B64(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("MaterialCatalogBase64CanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("MaterialCatalogBase64CanonicalitySmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
