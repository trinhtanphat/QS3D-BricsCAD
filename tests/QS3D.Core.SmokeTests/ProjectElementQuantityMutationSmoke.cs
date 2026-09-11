using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementQuantityMutationSmoke
    {
        internal static void Run()
        {
            SemanticAdmissionMatchesPersistedCardinality();
            DirectAddUsesSemanticLifecycle();
            ReadApisUseCanonicalIdentity();
            DirectMutationUsesSemanticValidation();
            RemoveAndClearUseSemanticLifecycle();
            PairRemovalUsesCanonicalIdentity();
            PairRemovalCanDeletePersistedCorruptValue();
            PairRemovalCanDeletePersistedMalformedKey();
            NoOpMutationsStayStable();
        }

        private static void SemanticAdmissionMatchesPersistedCardinality()
        {
            var element = new ProjectElement("E-QTY-CAP", ElementCategory.Beam);
            for (var i = 0; i < 10000; i++)
                element.Quantities.Add("Q" + i.ToString("D5", CultureInfo.InvariantCulture), i);

            element.MarkClean(ElementDirtyFlags.All);
            var beforeOverflow = element.UpdatedUtc;
            Throws<InvalidOperationException>(() => element.Quantities.Add("Q10000", 1d));
            Equal(10000, element.Quantities.Count);
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(beforeOverflow, element.UpdatedUtc);

            element.Quantities["Q00000"] = 42d;
            Equal(42d, element.Quantities["Q00000"]);
            Has(element.Dirty, ElementDirtyFlags.Quantity);

            element.MarkClean(ElementDirtyFlags.All);
            if (!element.Quantities.Remove("Q00001"))
                throw new Exception("Expected an existing quantity to be removable at capacity.");
            element.MarkClean(ElementDirtyFlags.All);
            element.Quantities.Add("Q10000", 1d);
            Equal(10000, element.Quantities.Count);
            Equal(1d, element.Quantities["Q10000"]);
            Has(element.Dirty, ElementDirtyFlags.Quantity);
        }

        private static void DirectAddUsesSemanticLifecycle()
        {
            var element = new ProjectElement("E-QTY-ADD", ElementCategory.Beam);
            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;

            element.Quantities.Add(" Length ", 2.5d);

            Equal(1, element.Quantities.Count);
            Equal(2.5d, element.Quantities["Length"]);
            Has(element.Dirty, ElementDirtyFlags.Quantity);
            Changed(before, element.UpdatedUtc, "Direct quantity Add must advance element persistence lifecycle.");
        }

        private static void ReadApisUseCanonicalIdentity()
        {
            var element = new ProjectElement("E-QTY-READ", ElementCategory.Beam);
            element.Quantities.Add(" Area ", 12.5d);
            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;
            var quantities = (ICollection<KeyValuePair<string, double>>)element.Quantities;

            Equal(12.5d, element.Quantities[" area "]);
            if (!element.Quantities.ContainsKey(" AREA "))
                throw new Exception("ContainsKey must use canonical quantity identity.");
            if (!element.Quantities.TryGetValue(" area ", out var value))
                throw new Exception("TryGetValue must use canonical quantity identity.");
            Equal(12.5d, value);
            if (!quantities.Contains(new KeyValuePair<string, double>(" AREA ", 12.5d)))
                throw new Exception("Pair Contains must use canonical quantity identity.");

            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);
        }

        private static void DirectMutationUsesSemanticValidation()
        {
            var element = new ProjectElement("E-QTY-VALIDATE", ElementCategory.Column);
            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;

            Throws<ArgumentException>(() => element.Quantities.Add(" ", 1d));
            Throws<ArgumentException>(() => element.Quantities.Add("bad\u0001name", 1d));
            Throws<ArgumentOutOfRangeException>(() => element.Quantities["NEG"] = -1d);
            Throws<ArgumentOutOfRangeException>(() => element.Quantities["NAN"] = double.NaN);
            Throws<ArgumentOutOfRangeException>(() => element.Quantities["INF"] = double.PositiveInfinity);

            element.Quantities.Add("COUNT", 1d);
            element.MarkClean(ElementDirtyFlags.All);
            before = element.UpdatedUtc;
            Throws<ArgumentException>(() => element.Quantities.Add(" count ", 2d));
            Equal(1, element.Quantities.Count);
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);
        }

        private static void RemoveAndClearUseSemanticLifecycle()
        {
            var element = new ProjectElement("E-QTY-REMOVE", ElementCategory.Slab);
            element.SetQuantity("Area", 10d);
            element.SetQuantity("Volume", 3d);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeRemove = element.UpdatedUtc;

            if (!element.Quantities.Remove(" area "))
                throw new Exception("Expected canonical quantity removal to succeed.");
            Equal(1, element.Quantities.Count);
            Has(element.Dirty, ElementDirtyFlags.Quantity);
            Changed(beforeRemove, element.UpdatedUtc, "Direct quantity Remove must advance element persistence lifecycle.");

            element.MarkClean(ElementDirtyFlags.All);
            var beforeClear = element.UpdatedUtc;
            element.Quantities.Clear();
            Equal(0, element.Quantities.Count);
            Has(element.Dirty, ElementDirtyFlags.Quantity);
            Changed(beforeClear, element.UpdatedUtc, "Direct quantity Clear must advance element persistence lifecycle.");
        }

        private static void PairRemovalUsesCanonicalIdentity()
        {
            var element = new ProjectElement("E-QTY-PAIR-REMOVE", ElementCategory.Slab);
            element.SetQuantity("Area", 5d);
            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;
            var quantities = (ICollection<KeyValuePair<string, double>>)element.Quantities;

            if (!quantities.Remove(new KeyValuePair<string, double>(" area ", 5d)))
                throw new Exception("Pair removal must canonicalize quantity identity before matching.");
            Equal(0, element.Quantities.Count);
            Has(element.Dirty, ElementDirtyFlags.Quantity);
            Changed(before, element.UpdatedUtc, "Effective pair removal must advance element persistence lifecycle.");

            element.SetQuantity("Area", 5d);
            element.MarkClean(ElementDirtyFlags.All);
            before = element.UpdatedUtc;
            if (quantities.Remove(new KeyValuePair<string, double>(" area ", 6d)))
                throw new Exception("Pair removal with a mismatched value must report false.");
            Equal(1, element.Quantities.Count);
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);
        }

        private static void PairRemovalCanDeletePersistedCorruptValue()
        {
            var element = new ProjectElement("E-QTY-PAIR-CORRUPT", ElementCategory.Slab);
            SeedPersistedQuantity(element, "Area", -1d);
            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;
            var quantities = (ICollection<KeyValuePair<string, double>>)element.Quantities;

            if (!quantities.Remove(new KeyValuePair<string, double>(" area ", -1d)))
                throw new Exception("Exact pair removal must allow cleanup of persisted-corrupt quantity values.");

            Equal(0, element.Quantities.Count);
            Has(element.Dirty, ElementDirtyFlags.Quantity);
            Changed(before, element.UpdatedUtc, "Removing persisted-corrupt quantity state must advance element persistence lifecycle.");
        }

        private static void PairRemovalCanDeletePersistedMalformedKey()
        {
            var element = new ProjectElement("E-QTY-PAIR-MALFORMED-KEY", ElementCategory.Slab);
            SeedPersistedQuantity(element, " Area ", 5d);
            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;
            var quantities = (ICollection<KeyValuePair<string, double>>)element.Quantities;

            if (!quantities.Remove(new KeyValuePair<string, double>(" area ", 5d)))
                throw new Exception("Exact pair removal must allow cleanup of a persisted non-canonical quantity key through canonical identity.");

            Equal(0, element.Quantities.Count);
            Has(element.Dirty, ElementDirtyFlags.Quantity);
            Changed(before, element.UpdatedUtc, "Removing a persisted malformed quantity key must advance element persistence lifecycle.");
        }

        private static void NoOpMutationsStayStable()
        {
            var element = new ProjectElement("E-QTY-NOOP", ElementCategory.FloorFinish);
            element.SetQuantity("Area", 5d);
            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;

            element.Quantities[" area "] = 5d;
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);

            if (element.Quantities.Remove("Missing"))
                throw new Exception("Removing a missing quantity must report false.");
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);

            element.Quantities.Clear();
            Has(element.Dirty, ElementDirtyFlags.Quantity);
            element.MarkClean(ElementDirtyFlags.All);
            before = element.UpdatedUtc;
            element.Quantities.Clear();
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);
        }

        private static void SeedPersistedQuantity(ProjectElement element, string name, double value)
        {
            var field = typeof(ProjectElement).GetField("_quantityValues", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement quantity backing field changed; update persisted-corruption regression intentionally.");
            var quantities = field.GetValue(element) as IDictionary<string, double>
                ?? throw new InvalidOperationException("Unexpected ProjectElement quantity backing collection.");
            quantities[name] = value;
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

    internal static class ProjectElementQuantityMutationSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectElementQuantityMutationSmoke.Run();
    }
}
