using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallRectAreaOverflowSmoke
    {
        internal static void Run()
        {
            FiniteAreaRemainsStable();
            DerivedAreaOverflowFailsClosed();
        }

        private static void FiniteAreaRemainsStable()
        {
            var rectangle = new CurtainWallRect(1d, 2d, 2d, 3d);
            if (Math.Abs(rectangle.AreaM2 - 6d) > 1e-12d)
                throw new InvalidOperationException("Curtain rectangle finite area changed unexpectedly.");
        }

        private static void DerivedAreaOverflowFailsClosed()
        {
            var rectangle = new CurtainWallRect(0d, 0d, 1e308d, 2d);
            Throws<OverflowException>(() => _ = rectangle.AreaM2);
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

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
