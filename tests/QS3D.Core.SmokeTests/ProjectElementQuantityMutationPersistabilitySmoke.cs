using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementQuantityMutationPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalAndPaddedNamesRemainSupported();
            ControlCharactersFailBeforeMutation();
        }

        private static void CanonicalAndPaddedNamesRemainSupported()
        {
            var element = new ProjectElement("E-QTY-1", ElementCategory.CustomQuantity);
            element.MarkClean(ElementDirtyFlags.All);

            element.SetQuantity("  AreaM2  ", 12.5d);

            Equal(1, element.Quantities.Count);
            True(element.Quantities.ContainsKey("AreaM2"));
            Equal(12.5d, element.Quantities["AreaM2"]);
            True((element.Dirty & ElementDirtyFlags.Quantity) != 0);
        }

        private static void ControlCharactersFailBeforeMutation()
        {
            var element = new ProjectElement("E-QTY-2", ElementCategory.CustomQuantity);
            element.SetQuantity("AreaM2", 1d);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeCount = element.Quantities.Count;
            var beforeValue = element.Quantities["AreaM2"];

            Throws<ArgumentException>(() => element.SetQuantity("Area\u0001M2", 2d));

            Equal(beforeCount, element.Quantities.Count);
            Equal(beforeValue, element.Quantities["AreaM2"]);
            False(element.Quantities.ContainsKey("Area\u0001M2"));
            Equal(ElementDirtyFlags.None, element.Dirty);
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

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Expected true.");
        }

        private static void False(bool value)
        {
            if (value) throw new InvalidOperationException("Expected false.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
