using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementInstanceFiniteMeasurementsSmoke
    {
        internal static void Run()
        {
            var family = new FamilyDefinition("Finite Measurements", ElementCategory.Beam);
            var instance = new ElementInstance("E1", family, "F1");

            Equal(0d, instance.LengthM, "default length");
            Equal(0d, instance.AreaM2, "default area");
            Equal(0d, instance.VolumeM3, "default volume");
            Equal(0d, instance.GrossConcreteM3, "default gross concrete");
            Equal(0d, instance.DeductionM3, "default deduction");
            Equal(0d, instance.FormworkM2, "default formwork");
            Equal(0d, instance.DoorAreaM2, "default door area");
            Equal(0d, instance.OuterPerimeterM, "default outer perimeter");
            Equal(0d, instance.InnerPerimeterM, "default inner perimeter");
            Equal(0d, instance.SideAreaM2, "default side area");
            Equal(0d, instance.BottomAreaM2, "default bottom area");
            Equal(0d, instance.TopAreaM2, "default top area");
            Equal(0d, instance.OtherAreaM2, "default other area");

            instance.LengthM = -1d;
            instance.AreaM2 = 2d;
            instance.VolumeM3 = 3d;
            instance.GrossConcreteM3 = 4d;
            instance.DeductionM3 = 1d;
            instance.FormworkM2 = 6d;
            instance.DoorAreaM2 = 7d;
            instance.OuterPerimeterM = 8d;
            instance.InnerPerimeterM = 9d;
            instance.SideAreaM2 = 10d;
            instance.BottomAreaM2 = 11d;
            instance.TopAreaM2 = 12d;
            instance.OtherAreaM2 = 13d;
            Equal(-1d, instance.LengthM, "finite negative length preserved");
            Equal(3d, instance.NetConcreteM3, "net concrete semantics preserved");

            Reject(value => instance.LengthM = value, () => instance.LengthM, double.NaN, -1d, "length NaN");
            Reject(value => instance.AreaM2 = value, () => instance.AreaM2, double.PositiveInfinity, 2d, "area +Infinity");
            Reject(value => instance.VolumeM3 = value, () => instance.VolumeM3, double.NegativeInfinity, 3d, "volume -Infinity");
            Reject(value => instance.GrossConcreteM3 = value, () => instance.GrossConcreteM3, double.NaN, 4d, "gross concrete NaN");
            Reject(value => instance.DeductionM3 = value, () => instance.DeductionM3, double.PositiveInfinity, 1d, "deduction +Infinity");
            Reject(value => instance.FormworkM2 = value, () => instance.FormworkM2, double.NegativeInfinity, 6d, "formwork -Infinity");
            Reject(value => instance.DoorAreaM2 = value, () => instance.DoorAreaM2, double.NaN, 7d, "door area NaN");
            Reject(value => instance.OuterPerimeterM = value, () => instance.OuterPerimeterM, double.PositiveInfinity, 8d, "outer perimeter +Infinity");
            Reject(value => instance.InnerPerimeterM = value, () => instance.InnerPerimeterM, double.NegativeInfinity, 9d, "inner perimeter -Infinity");
            Reject(value => instance.SideAreaM2 = value, () => instance.SideAreaM2, double.NaN, 10d, "side area NaN");
            Reject(value => instance.BottomAreaM2 = value, () => instance.BottomAreaM2, double.PositiveInfinity, 11d, "bottom area +Infinity");
            Reject(value => instance.TopAreaM2 = value, () => instance.TopAreaM2, double.NegativeInfinity, 12d, "top area -Infinity");
            Reject(value => instance.OtherAreaM2 = value, () => instance.OtherAreaM2, double.NaN, 13d, "other area NaN");
            Equal(3d, instance.NetConcreteM3, "net concrete unchanged after rejected assignments");
        }

        private static void Reject(Action<double> setter, Func<double> getter, double invalid, double expected, string label)
        {
            Throws<ArgumentOutOfRangeException>(() => setter(invalid), label);
            Equal(expected, getter(), label + " preserved value");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (!expected.Equals(actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
