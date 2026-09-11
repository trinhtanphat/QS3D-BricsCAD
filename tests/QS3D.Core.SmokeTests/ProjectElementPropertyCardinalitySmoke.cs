using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementPropertyCardinalitySmoke
    {
        private const int MaximumPropertyEntries = 10000;

        internal static void Run()
        {
            PropertyCapacityMatchesPersistenceBoundary();
            PairRemovalUsesCanonicalIdentityAndLifecycle();
            ReadLookupsUseCanonicalPropertyIdentity();
        }

        private static void PropertyCapacityMatchesPersistenceBoundary()
        {
            var element = new ProjectElement("E1", ElementCategory.Beam);
            SeedPersistedProperties(element, MaximumPropertyEntries - 1);

            element.Properties.Add("P09999", "V09999");
            Equal(MaximumPropertyEntries, element.Properties.Count);

            element.MarkClean(ElementDirtyFlags.All);
            element.Properties[" p00000 "] = "replacement";
            Equal(MaximumPropertyEntries, element.Properties.Count);
            Equal("replacement", element.Properties["P00000"]);
            Has(element.Dirty, ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);

            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;

            Throws<ArgumentException>(() => element.Properties.Add(" p00000 ", "duplicate"));
            Equal(MaximumPropertyEntries, element.Properties.Count);
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);

            Throws<ArgumentException>(() => element.Properties.Add(" ", "invalid"));
            Equal(MaximumPropertyEntries, element.Properties.Count);
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);

            Throws<ArgumentException>(() => element.Properties["P-INVALID-VALUE"] = "bad\u0001value");
            Equal(MaximumPropertyEntries, element.Properties.Count);
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);

            Throws<InvalidOperationException>(() => element.Properties.Add("P-OVERFLOW-ADD", "overflow"));
            Equal(MaximumPropertyEntries, element.Properties.Count);
            if (element.Properties.ContainsKey("P-OVERFLOW-ADD"))
                throw new Exception("Rejected property Add must not mutate the collection.");
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);

            Throws<InvalidOperationException>(() => element.Properties["P-OVERFLOW-SET"] = "overflow");
            Equal(MaximumPropertyEntries, element.Properties.Count);
            if (element.Properties.ContainsKey("P-OVERFLOW-SET"))
                throw new Exception("Rejected property index assignment must not mutate the collection.");
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);

            if (!element.Properties.Remove("P00001"))
                throw new Exception("Expected an existing property to be removable.");
            Equal(MaximumPropertyEntries - 1, element.Properties.Count);
            element.MarkClean(ElementDirtyFlags.All);
            element.Properties["P-REPLACEMENT"] = "replacement";
            Equal(MaximumPropertyEntries, element.Properties.Count);
            Equal("replacement", element.Properties["P-REPLACEMENT"]);
        }

        private static void PairRemovalUsesCanonicalIdentityAndLifecycle()
        {
            var element = new ProjectElement("E-PAIR-REMOVE", ElementCategory.Beam);
            element.SetProperty("WidthM", "0.4");
            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;
            var properties = (ICollection<KeyValuePair<string, string>>)element.Properties;

            if (!properties.Remove(new KeyValuePair<string, string>(" widthm ", "0.4")))
                throw new Exception("Property pair removal must canonicalize semantic key identity before matching.");
            Equal(0, element.Properties.Count);
            Has(element.Dirty, ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity | ElementDirtyFlags.Geometry);
            Changed(before, element.UpdatedUtc, "Effective property pair removal must advance persistence lifecycle.");

            element.SetProperty("WidthM", "0.4");
            element.MarkClean(ElementDirtyFlags.All);
            before = element.UpdatedUtc;
            if (properties.Remove(new KeyValuePair<string, string>(" widthm ", "0.5")))
                throw new Exception("Property pair removal with a mismatched value must report false.");
            Equal(1, element.Properties.Count);
            Equal("0.4", element.Properties["WidthM"]);
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);
        }

        private static void ReadLookupsUseCanonicalPropertyIdentity()
        {
            var element = new ProjectElement("E-READ-CANONICAL", ElementCategory.Beam);
            element.SetProperty("WidthM", "0.4");
            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;
            var pairs = (ICollection<KeyValuePair<string, string>>)element.Properties;

            if (!element.Properties.ContainsKey(" widthm "))
                throw new Exception("Property ContainsKey must canonicalize semantic key identity.");
            if (!element.Properties.TryGetValue(" WIDTHM ", out var value))
                throw new Exception("Property TryGetValue must canonicalize semantic key identity.");
            Equal("0.4", value);
            Equal("0.4", element.Properties[" widthm "]);
            if (!pairs.Contains(new KeyValuePair<string, string>(" WIDTHM ", "0.4")))
                throw new Exception("Property pair Contains must canonicalize semantic key identity.");
            if (pairs.Contains(new KeyValuePair<string, string>(" widthm ", "0.5")))
                throw new Exception("Property pair Contains must preserve exact value matching.");

            if (element.Properties.ContainsKey(" "))
                throw new Exception("Invalid blank property lookup must remain a miss.");
            if (element.Properties.TryGetValue(" ", out _))
                throw new Exception("Invalid blank property TryGetValue must remain a miss.");
            Throws<KeyNotFoundException>(() => { var ignored = element.Properties[" "]; });
            Throws<ArgumentNullException>(() => element.Properties.ContainsKey(null!));
            Throws<ArgumentNullException>(() => element.Properties.TryGetValue(null!, out _));
            Throws<ArgumentNullException>(() => { var ignored = element.Properties[null!]; });

            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);
        }

        private static void SeedPersistedProperties(ProjectElement element, int count)
        {
            var propertiesField = typeof(ProjectElement).GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to seed persisted ProjectElement property-bound fixture.");
            var properties = propertiesField.GetValue(element) as Dictionary<string, string>
                ?? throw new InvalidOperationException("Unexpected ProjectElement property backing dictionary.");

            for (var index = 0; index < count; index++)
            {
                var suffix = index.ToString("D5");
                properties.Add("P" + suffix, "V" + suffix);
            }
        }

        private static void Has(ElementDirtyFlags actual, ElementDirtyFlags expected)
        {
            if ((actual & expected) != expected)
                throw new Exception("Expected dirty flags " + actual + " to contain " + expected + ".");
        }

        private static void Changed(DateTime before, DateTime after, string message)
        {
            if (after == before) throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectElementPropertyCardinalitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectElementPropertyCardinalitySmoke.Run();
    }
}
