using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementInstanceNonNegativeMeasurementsSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsNegativeOrNonFiniteMeasurements();
            AcceptsZeroAndPositiveMeasurements();
            CanonicalizesNegativeZeroMeasurements();
        }

        private static void RejectsNegativeOrNonFiniteMeasurements()
        {
            var element = CreateElement();
            foreach (var setter in MeasurementSetters(element))
            {
                Throws<ArgumentOutOfRangeException>(() => setter(-0.01d));
                Throws<ArgumentOutOfRangeException>(() => setter(double.NaN));
                Throws<ArgumentOutOfRangeException>(() => setter(double.PositiveInfinity));
                Throws<ArgumentOutOfRangeException>(() => setter(double.NegativeInfinity));
            }
        }

        private static void AcceptsZeroAndPositiveMeasurements()
        {
            var element = CreateElement();
            foreach (var setter in MeasurementSetters(element))
            {
                setter(0d);
                setter(1.25d);
            }
        }

        private static void CanonicalizesNegativeZeroMeasurements()
        {
            var element = CreateElement();
            var setters = MeasurementSetters(element);
            var getters = MeasurementGetters(element);
            if (setters.Length != getters.Length)
                throw new InvalidOperationException("ElementInstance measurement setter/getter coverage is out of sync.");

            for (var i = 0; i < setters.Length; i++)
            {
                setters[i](-0d);
                CanonicalPositiveZero(getters[i]());
            }

            element.GrossConcreteM3 = -0d;
            element.DeductionM3 = 0d;
            CanonicalPositiveZero(element.NetConcreteM3);
        }

        private static Action<double>[] MeasurementSetters(ElementInstance element) => new Action<double>[]
        {
            value => element.LengthM = value,
            value => element.AreaM2 = value,
            value => element.VolumeM3 = value,
            value => element.GrossConcreteM3 = value,
            value => element.DeductionM3 = value,
            value => element.FormworkM2 = value,
            value => element.DoorAreaM2 = value,
            value => element.OuterPerimeterM = value,
            value => element.InnerPerimeterM = value,
            value => element.SideAreaM2 = value,
            value => element.BottomAreaM2 = value,
            value => element.TopAreaM2 = value,
            value => element.OtherAreaM2 = value
        };

        private static Func<double>[] MeasurementGetters(ElementInstance element) => new Func<double>[]
        {
            () => element.LengthM,
            () => element.AreaM2,
            () => element.VolumeM3,
            () => element.GrossConcreteM3,
            () => element.DeductionM3,
            () => element.FormworkM2,
            () => element.DoorAreaM2,
            () => element.OuterPerimeterM,
            () => element.InnerPerimeterM,
            () => element.SideAreaM2,
            () => element.BottomAreaM2,
            () => element.TopAreaM2,
            () => element.OtherAreaM2
        };

        private static ElementInstance CreateElement() =>
            new ElementInstance("E-1", new FamilyDefinition("Test", ElementCategory.CustomQuantity), "L1");

        private static void CanonicalPositiveZero(double value)
        {
            if (value != 0d)
                throw new InvalidOperationException("Expected zero but got " + value + ".");
            if (BitConverter.DoubleToInt64Bits(value) != BitConverter.DoubleToInt64Bits(0d))
                throw new InvalidOperationException("Expected canonical positive zero.");
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
            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }
    }
}
