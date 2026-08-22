using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class EntitySnapshotCaptureCategorySmoke
    {
        internal static void Run()
        {
            UndefinedCategoryFailsClosedForNormalEntity();
            UndefinedCategoryFailsClosedForMeasuredProxy();
            DefinedCategoryBehaviorRemainsStable();
        }

        private static void UndefinedCategoryFailsClosedForNormalEntity()
        {
            var snapshot = new EntitySnapshot("A", "Line", "beam");
            var invalid = (ElementCategory)int.MaxValue;
            Throws<ArgumentOutOfRangeException>(() => EntitySnapshotCaptureEligibility.IsReady(snapshot, invalid, out _));
            Throws<ArgumentOutOfRangeException>(() => EntitySnapshotCaptureEligibility.EnsureReady(snapshot, invalid));
        }

        private static void UndefinedCategoryFailsClosedForMeasuredProxy()
        {
            var snapshot = new EntitySnapshot("B", "ProxyEntity", "beam") { LengthDrawingUnits = 10d };
            var invalid = (ElementCategory)(-1);
            Throws<ArgumentOutOfRangeException>(() => EntitySnapshotCaptureEligibility.IsReady(snapshot, invalid, out _));
        }

        private static void DefinedCategoryBehaviorRemainsStable()
        {
            var line = new EntitySnapshot("C", "Line", "beam");
            True(EntitySnapshotCaptureEligibility.IsReady(line, ElementCategory.Beam, out var normalReason));
            Equal(string.Empty, normalReason);

            var metriclessProxy = new EntitySnapshot("D", "ProxyEntity", "beam");
            False(EntitySnapshotCaptureEligibility.IsReady(metriclessProxy, ElementCategory.Beam, out var proxyReason));
            True(proxyReason.Length > 0);

            var measuredProxy = new EntitySnapshot("E", "ProxyEntity", "beam") { LengthDrawingUnits = 10d };
            True(EntitySnapshotCaptureEligibility.IsReady(measuredProxy, ElementCategory.Beam, out var measuredReason));
            Equal(string.Empty, measuredReason);
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected false.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class EntitySnapshotCaptureCategorySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => EntitySnapshotCaptureCategorySmoke.Run();
    }
}
