using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementPropertyValuePersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            const string key = "Note";
            const string validValue = "  line 1\nline\t2  ";
            var element = new ProjectElement("E-PROP", ElementCategory.Beam);
            element.SetProperty(key, validValue);
            element.MarkClean(ElementDirtyFlags.All);

            var dirtyBeforeRejectedValue = element.Dirty;
            var updatedBeforeRejectedValue = element.UpdatedUtc;
            var rejected = false;
            try
            {
                element.SetProperty(key, "bad\u0001value");
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            if (!rejected)
                throw new InvalidOperationException("ProjectElement.SetProperty accepted an XML-illegal property value.");
            Equal(validValue, element.Properties[key], "Rejected property value assignment");
            if (element.Dirty != dirtyBeforeRejectedValue)
                throw new InvalidOperationException("Rejected property value assignment changed Dirty state.");
            if (element.UpdatedUtc != updatedBeforeRejectedValue)
                throw new InvalidOperationException("Rejected property value assignment changed UpdatedUtc.");

            var project = new ProjectState("PROP-VALUE", "ProjectElement property value persistability");
            project.Elements.Add(element);
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-property-value-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);

            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var loadedElement = loaded.FindElement("E-PROP") ?? throw new InvalidOperationException("Saved element did not round-trip.");
                if (!loadedElement.Properties.TryGetValue(key, out var loadedValue))
                    throw new InvalidOperationException("Valid property value did not round-trip through QSDB.");
                Equal(validValue, loadedValue, "Property value QSDB round-trip");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " mismatch.");
        }
    }
}
