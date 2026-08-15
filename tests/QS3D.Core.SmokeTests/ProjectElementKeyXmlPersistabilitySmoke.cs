using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementKeyXmlPersistabilitySmoke
    {
        internal static void Run()
        {
            RejectsXmlInvalidPropertyKeysBeforeMutation();
            RejectsXmlInvalidQuantityKeysBeforeMutation();
            SupplementaryUnicodeKeysRoundTripThroughQsdb();
        }

        private static void RejectsXmlInvalidPropertyKeysBeforeMutation()
        {
            foreach (var invalid in InvalidXmlTokens())
            {
                var element = new ProjectElement("E-PROP", ElementCategory.Room);
                element.SetProperty("Existing", "value");
                element.MarkClean(ElementDirtyFlags.All);
                var beforeDirty = element.Dirty;
                var beforeUpdatedUtc = element.UpdatedUtc;
                var beforeCount = element.Properties.Count;

                ExpectArgument(() => element.SetProperty("Bad-" + invalid, "value"), "property key");

                Equal(beforeCount, element.Properties.Count, "Rejected property key changed property count.");
                Equal(beforeDirty, element.Dirty, "Rejected property key changed dirty flags.");
                Equal(beforeUpdatedUtc, element.UpdatedUtc, "Rejected property key changed UpdatedUtc.");
                Require(!element.Properties.ContainsKey("Bad-" + invalid), "Rejected XML-invalid property key entered live state.");
                Equal("value", element.Properties["Existing"], "Rejected property key changed existing property state.");
            }
        }

        private static void RejectsXmlInvalidQuantityKeysBeforeMutation()
        {
            foreach (var invalid in InvalidXmlTokens())
            {
                var element = new ProjectElement("E-QTY", ElementCategory.Room);
                element.SetQuantity("ExistingQuantity", 2d);
                element.MarkClean(ElementDirtyFlags.All);
                var beforeDirty = element.Dirty;
                var beforeUpdatedUtc = element.UpdatedUtc;
                var beforeCount = element.Quantities.Count;

                ExpectArgument(() => element.SetQuantity("Bad-" + invalid, 1d), "quantity key");

                Equal(beforeCount, element.Quantities.Count, "Rejected quantity key changed quantity count.");
                Equal(beforeDirty, element.Dirty, "Rejected quantity key changed dirty flags.");
                Equal(beforeUpdatedUtc, element.UpdatedUtc, "Rejected quantity key changed UpdatedUtc.");
                Require(!element.Quantities.ContainsKey("Bad-" + invalid), "Rejected XML-invalid quantity key entered live state.");
                Equal(2d, element.Quantities["ExistingQuantity"], "Rejected quantity key changed existing quantity state.");
            }
        }

        private static void SupplementaryUnicodeKeysRoundTripThroughQsdb()
        {
            const string supplementary = "\U0001F642";
            var propertyKey = "Property-" + supplementary;
            var quantityKey = "Quantity-" + supplementary;
            var path = Path.Combine(Path.GetTempPath(), "qs3d-element-key-xml-" + Guid.NewGuid().ToString("N") + ".qsdb");

            try
            {
                var project = new ProjectState("P-ELEMENT-KEY-XML", "Element key XML");
                var element = new ProjectElement("E1", ElementCategory.Room);
                element.SetProperty("  " + propertyKey + "  ", "value-" + supplementary);
                element.SetQuantity("  " + quantityKey + "  ", 12.5d);
                project.Elements.Add(element);

                Equal("value-" + supplementary, element.Properties[propertyKey], "Property key trimming/supplementary Unicode canonicalization changed.");
                Equal(12.5d, element.Quantities[quantityKey], "Quantity key trimming/supplementary Unicode canonicalization changed.");

                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var restored = loaded.FindElement("E1") ?? throw new InvalidOperationException("Round-trip element was not restored.");

                Require(restored.Properties.ContainsKey(propertyKey), "Supplementary-Unicode property key did not round-trip exactly.");
                Equal("value-" + supplementary, restored.Properties[propertyKey], "Supplementary-Unicode property value did not round-trip exactly.");
                Require(restored.Quantities.ContainsKey(quantityKey), "Supplementary-Unicode quantity key did not round-trip exactly.");
                Equal(12.5d, restored.Quantities[quantityKey], "Supplementary-Unicode quantity value did not round-trip exactly.");
            }
            finally
            {
                DeleteIfExists(path);
                DeleteIfExists(path + ".bak");
                DeleteIfExists(path + ".tmp");
            }
        }

        private static string[] InvalidXmlTokens() => new[]
        {
            new string(new[] { '\uD800' }),
            new string(new[] { '\uDC00' })
        };

        private static void ExpectArgument(Action action, string label)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("Expected ArgumentException for XML-invalid " + label + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
