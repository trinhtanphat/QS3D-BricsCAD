using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampCountTraversalSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsUnderYield();
            RejectsOverYield();
            AcceptsHonestCount();
        }

        private static void RejectsUnderYield()
        {
            var error = InvokeSnapshot(new[] { "A" }, 2);
            Require(error is InvalidOperationException, "Persistence stamp accepted Count=2 with one enumerated entry.");
            Require(error!.Message.Contains("known count does not match enumerated entry count", StringComparison.Ordinal),
                "Persistence stamp under-yield did not use the deterministic Count mismatch diagnostic.");
        }

        private static void RejectsOverYield()
        {
            var error = InvokeSnapshot(new[] { "A", "B" }, 1);
            Require(error is InvalidOperationException, "Persistence stamp accepted Count=1 with two enumerated entries.");
            Require(error!.Message.Contains("known count does not match enumerated entry count", StringComparison.Ordinal),
                "Persistence stamp over-yield did not use the deterministic Count mismatch diagnostic.");
        }

        private static void AcceptsHonestCount()
        {
            var error = InvokeSnapshot(new[] { "A", "B" }, 2);
            Require(error == null, "Persistence stamp rejected an honest known Count.");
        }

        private static Exception? InvokeSnapshot(IEnumerable<string> values, int knownCount)
        {
            var method = typeof(ProjectPersistenceStamp).GetMethod("SnapshotBounded", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new Exception("ProjectPersistenceStamp.SnapshotBounded was not found.");
            try
            {
                method.MakeGenericMethod(typeof(string)).Invoke(
                    null,
                    new object[] { values, knownCount, "regression entries", 10_000 });
                return null;
            }
            catch (TargetInvocationException ex)
            {
                return ex.InnerException ?? ex;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
