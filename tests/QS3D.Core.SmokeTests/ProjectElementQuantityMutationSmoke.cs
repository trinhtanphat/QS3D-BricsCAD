using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementQuantityMutationSmoke
    {
        internal static void Run()
        {
            DirectAddUsesSemanticLifecycle();
            DirectMutationUsesSemanticValidation();
            RemoveAndClearUseSemanticLifecycle();
            NoOpMutationsStayStable();
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
