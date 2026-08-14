using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementNegativeQuantitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var element = new ProjectElement("Q-NEG", ElementCategory.Room, "", "floor-0", "zone-1");
            element.SetQuantity("NetAreaM2", 12.5d);
            element.MarkClean(ElementDirtyFlags.All);

            var beforeDirty = element.Dirty;
            var beforeUpdatedUtc = element.UpdatedUtc;
            var beforeCount = element.Quantities.Count;
            var beforeValue = element.Quantities["NetAreaM2"];

            Throws<ArgumentOutOfRangeException>(() => element.SetQuantity("NetAreaM2", -0.25d));

            Equal(beforeCount, element.Quantities.Count, "quantity count after rejection");
            Equal(beforeValue, element.Quantities["NetAreaM2"], "existing quantity after rejection");
            Equal(beforeDirty, element.Dirty, "dirty flags after rejection");
            Equal(beforeUpdatedUtc, element.UpdatedUtc, "updated timestamp after rejection");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectElementNegativeQuantitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
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

            throw new InvalidOperationException("ProjectElementNegativeQuantitySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
