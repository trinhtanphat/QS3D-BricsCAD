using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class VerticalPlacementFiniteHeightSmoke
    {
        internal static void Run()
        {
            var project = new ProjectState("vertical-height", "Vertical height integrity");
            var element = new ProjectElement("E1", ElementCategory.Beam);

            var height = ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, 2.5d);
            if (Math.Abs(height - 2.5d) > 1e-12)
                throw new InvalidOperationException("Expected unchanged positive legacy height.");

            Throws<ArgumentOutOfRangeException>(() =>
                ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, double.NaN));
            Throws<ArgumentOutOfRangeException>(() =>
                ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, double.PositiveInfinity));
            Throws<ArgumentOutOfRangeException>(() =>
                ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, 0d));
            Throws<ArgumentOutOfRangeException>(() =>
                ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, -1d));

            Throws<ArgumentOutOfRangeException>(() =>
                new ElementVerticalPlacement(false, false, -double.MaxValue, double.MaxValue));

            var placement = new ElementVerticalPlacement(false, false, -2d, 3d);
            if (Math.Abs(placement.HeightM - 5d) > 1e-12)
                throw new InvalidOperationException("Finite placement height was not preserved.");
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

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
