using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class WallPropertySetPositiveThicknessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsNonPositiveOrNonFiniteThickness();
            PreservesSignedOffsetSemantics();
        }

        private static void RejectsNonPositiveOrNonFiniteThickness()
        {
            var properties = new WallPropertySet();
            Equal(110d, properties.ThicknessMm);
            Throws<ArgumentOutOfRangeException>(() => properties.ThicknessMm = 0d);
            Throws<ArgumentOutOfRangeException>(() => properties.ThicknessMm = -1d);
            Throws<ArgumentOutOfRangeException>(() => properties.ThicknessMm = double.NaN);
            Throws<ArgumentOutOfRangeException>(() => properties.ThicknessMm = double.PositiveInfinity);

            properties.ThicknessMm = 250d;
            Equal(250d, properties.ThicknessMm);
        }

        private static void PreservesSignedOffsetSemantics()
        {
            var properties = new WallPropertySet
            {
                AxisToLeftMm = -25d,
                AxisToRightMm = -50d,
                BaseOffsetMm = -100d,
                TopOffsetMm = -200d
            };

            Equal(-25d, properties.AxisToLeftMm);
            Equal(-50d, properties.AxisToRightMm);
            Equal(-100d, properties.BaseOffsetMm);
            Equal(-200d, properties.TopOffsetMm);
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

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
