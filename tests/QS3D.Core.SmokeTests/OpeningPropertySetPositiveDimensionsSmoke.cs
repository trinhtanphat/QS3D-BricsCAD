using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class OpeningPropertySetPositiveDimensionsSmoke
    {
        internal static void Run()
        {
            DefaultsRemainPhysical();
            RejectsNonPositivePhysicalDimensionsBeforeMutation();
            PreservesFiniteSillOffsetSemantics();
            PreservesNonFiniteRejectionBeforeMutation();
        }

        private static void DefaultsRemainPhysical()
        {
            var properties = new OpeningPropertySet();
            Require(properties.WidthMm > 0d, "Default opening width is not positive.");
            Require(properties.HeightMm > 0d, "Default opening height is not positive.");
            Require(properties.ThicknessMm > 0d, "Default opening thickness is not positive.");
        }

        private static void RejectsNonPositivePhysicalDimensionsBeforeMutation()
        {
            var properties = new OpeningPropertySet
            {
                WidthMm = 1200d,
                HeightMm = 2100d,
                ThicknessMm = 150d
            };

            Throws<ArgumentOutOfRangeException>(() => properties.WidthMm = 0d);
            Require(properties.WidthMm == 1200d, "Zero width replaced the previous valid width.");
            Throws<ArgumentOutOfRangeException>(() => properties.WidthMm = -1d);
            Require(properties.WidthMm == 1200d, "Negative width replaced the previous valid width.");

            Throws<ArgumentOutOfRangeException>(() => properties.HeightMm = 0d);
            Require(properties.HeightMm == 2100d, "Zero height replaced the previous valid height.");
            Throws<ArgumentOutOfRangeException>(() => properties.HeightMm = -1d);
            Require(properties.HeightMm == 2100d, "Negative height replaced the previous valid height.");

            Throws<ArgumentOutOfRangeException>(() => properties.ThicknessMm = 0d);
            Require(properties.ThicknessMm == 150d, "Zero thickness replaced the previous valid thickness.");
            Throws<ArgumentOutOfRangeException>(() => properties.ThicknessMm = -1d);
            Require(properties.ThicknessMm == 150d, "Negative thickness replaced the previous valid thickness.");
        }

        private static void PreservesFiniteSillOffsetSemantics()
        {
            var properties = new OpeningPropertySet();
            properties.SillOffsetMm = -250d;
            Require(properties.SillOffsetMm == -250d, "Negative finite sill offset was rejected or changed.");
            properties.SillOffsetMm = 0d;
            Require(properties.SillOffsetMm == 0d, "Zero sill offset was rejected or changed.");
        }

        private static void PreservesNonFiniteRejectionBeforeMutation()
        {
            var properties = new OpeningPropertySet
            {
                WidthMm = 1000d,
                HeightMm = 2000d,
                ThicknessMm = 120d,
                SillOffsetMm = 50d
            };

            Throws<ArgumentOutOfRangeException>(() => properties.WidthMm = double.NaN);
            Require(properties.WidthMm == 1000d, "NaN width replaced the previous valid width.");
            Throws<ArgumentOutOfRangeException>(() => properties.HeightMm = double.PositiveInfinity);
            Require(properties.HeightMm == 2000d, "Infinite height replaced the previous valid height.");
            Throws<ArgumentOutOfRangeException>(() => properties.ThicknessMm = double.NegativeInfinity);
            Require(properties.ThicknessMm == 120d, "Infinite thickness replaced the previous valid thickness.");
            Throws<ArgumentOutOfRangeException>(() => properties.SillOffsetMm = double.NaN);
            Require(properties.SillOffsetMm == 50d, "NaN sill offset replaced the previous valid offset.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
