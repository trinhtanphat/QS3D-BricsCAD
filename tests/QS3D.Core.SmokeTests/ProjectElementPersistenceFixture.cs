using System;
using System.Collections.Generic;
using System.Reflection;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementPersistenceFixture
    {
        internal static void SetQuantity(ProjectElement element, string name, double value)
        {
            var field = typeof(ProjectElement).GetField("_quantityValues", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement quantity backing dictionary was not found.");
            var quantities = field.GetValue(element) as IDictionary<string, double>
                ?? throw new InvalidOperationException("ProjectElement quantity backing dictionary had an unexpected type.");
            quantities[name] = value;
        }

        internal static bool ContainsQuantity(ProjectElement element, string name)
        {
            var field = typeof(ProjectElement).GetField("_quantityValues", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement quantity backing dictionary was not found.");
            var quantities = field.GetValue(element) as IDictionary<string, double>
                ?? throw new InvalidOperationException("ProjectElement quantity backing dictionary had an unexpected type.");
            return quantities.ContainsKey(name);
        }

        internal static bool ContainsQuantityKey(ProjectElement element, string name)
        {
            var field = typeof(ProjectElement).GetField("_quantityValues", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement quantity backing dictionary was not found.");
            var quantities = field.GetValue(element) as IDictionary<string, double>
                ?? throw new InvalidOperationException("ProjectElement quantity backing dictionary had an unexpected type.");
            return quantities.ContainsKey(name);
        }
    }
}
