using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallRemovedAreaIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            FramePlanPreservesFiniteSemantics();
            PanelPlanPreservesFiniteSemantics();
            FramePlanRejectsNonRepresentableResults();
            PanelPlanRejectsNonRepresentableResults();
        }

        private static void FramePlanPreservesFiniteSemantics()
        {
            var plan = new CurtainWallOpeningFramePlan
            {
                OriginalFrameAreaM2 = 10d,
                RemainingFrameAreaM2 = 4d
            };
            Equal(6d, plan.RemovedFrameAreaM2, "ordinary frame removed area changed");

            plan.RemainingFrameAreaM2 = 12d;
            RequirePositiveZero(plan.RemovedFrameAreaM2, "frame removed-area clamp did not return canonical zero");

            plan.OriginalFrameAreaM2 = -0d;
            plan.RemainingFrameAreaM2 = 0d;
            RequirePositiveZero(plan.RemovedFrameAreaM2, "frame signed-zero result was not canonicalized");
        }

        private static void PanelPlanPreservesFiniteSemantics()
        {
            var plan = new CurtainWallOpeningPanelPlan
            {
                OriginalPanelAreaM2 = 9d,
                RemainingPanelAreaM2 = 2.5d
            };
            Equal(6.5d, plan.RemovedPanelAreaM2, "ordinary panel removed area changed");

            plan.RemainingPanelAreaM2 = 10d;
            RequirePositiveZero(plan.RemovedPanelAreaM2, "panel removed-area clamp did not return canonical zero");

            plan.OriginalPanelAreaM2 = -0d;
            plan.RemainingPanelAreaM2 = 0d;
            RequirePositiveZero(plan.RemovedPanelAreaM2, "panel signed-zero result was not canonicalized");
        }

        private static void FramePlanRejectsNonRepresentableResults()
        {
            var plan = new CurtainWallOpeningFramePlan
            {
                OriginalFrameAreaM2 = 1d,
                RemainingFrameAreaM2 = 0d
            };

            plan.OriginalFrameAreaM2 = double.NaN;
            RequireThrows<OverflowException>(() => ReadFrame(plan), "frame NaN removed area leaked through public mutation");

            plan.OriginalFrameAreaM2 = 1d;
            plan.RemainingFrameAreaM2 = double.PositiveInfinity;
            RequireThrows<OverflowException>(() => ReadFrame(plan), "frame infinite removed area was silently clamped");

            plan.OriginalFrameAreaM2 = double.MaxValue;
            plan.RemainingFrameAreaM2 = -double.MaxValue;
            RequireThrows<OverflowException>(() => ReadFrame(plan), "frame finite subtraction overflow leaked infinity");
        }

        private static void PanelPlanRejectsNonRepresentableResults()
        {
            var plan = new CurtainWallOpeningPanelPlan
            {
                OriginalPanelAreaM2 = 1d,
                RemainingPanelAreaM2 = 0d
            };

            plan.RemainingPanelAreaM2 = double.NaN;
            RequireThrows<OverflowException>(() => ReadPanel(plan), "panel NaN removed area leaked through public mutation");

            plan.OriginalPanelAreaM2 = double.PositiveInfinity;
            plan.RemainingPanelAreaM2 = 1d;
            RequireThrows<OverflowException>(() => ReadPanel(plan), "panel infinite removed area leaked through public mutation");

            plan.OriginalPanelAreaM2 = double.MaxValue;
            plan.RemainingPanelAreaM2 = -double.MaxValue;
            RequireThrows<OverflowException>(() => ReadPanel(plan), "panel finite subtraction overflow leaked infinity");
        }

        private static void ReadFrame(CurtainWallOpeningFramePlan plan)
        {
            _ = plan.RemovedFrameAreaM2;
        }

        private static void ReadPanel(CurtainWallOpeningPanelPlan plan)
        {
            _ = plan.RemovedPanelAreaM2;
        }

        private static void RequirePositiveZero(double value, string message)
        {
            if (value != 0d || BitConverter.DoubleToInt64Bits(value) != 0L)
                throw new InvalidOperationException("CurtainWallRemovedAreaIntegritySmoke: " + message + ".");
        }

        private static void Equal(double expected, double actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    "CurtainWallRemovedAreaIntegritySmoke: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void RequireThrows<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("CurtainWallRemovedAreaIntegritySmoke: " + message + ".");
        }
    }
}
