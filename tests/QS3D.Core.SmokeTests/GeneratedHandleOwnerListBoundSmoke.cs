using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedHandleOwnerListBoundSmoke
    {
        private const int MaxHandles = 10000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ExactBoundaryRemainsAccepted();
            PersistedOwnerListOverBoundaryFailsClosed();
            ExistingStrictTokenIntegrityRemainsEnforced();
        }

        private static void ExactBoundaryRemainsAccepted()
        {
            var element = new ProjectElement("BOUNDARY", ElementCategory.Beam);
            element.Properties["GeneratedRebarHandles"] = CanonicalHandles(MaxHandles);

            var handles = GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element).ToList();
            AssertEqual(MaxHandles, handles.Count, "Persisted generated-owner handle boundary must remain accepted.");
            AssertEqual("1", handles[0].Key, "Boundary traversal changed first canonical handle.");
            AssertEqual(MaxHandles.ToString("X"), handles[MaxHandles - 1].Key, "Boundary traversal changed last canonical handle.");
        }

        private static void PersistedOwnerListOverBoundaryFailsClosed()
        {
            var element = new ProjectElement("OVERFLOW", ElementCategory.Beam);
            element.Properties["GeneratedRebarHandles"] = CanonicalHandles(MaxHandles + 1);

            AssertThrows<InvalidOperationException>(
                () => GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element).ToList(),
                "Persisted generated-owner metadata above the destructive 10,000-handle safety envelope must fail closed.",
                "cannot exceed 10000 handle tokens");
        }

        private static void ExistingStrictTokenIntegrityRemainsEnforced()
        {
            var element = new ProjectElement("STRICT", ElementCategory.Beam);

            element.Properties["GeneratedRebarHandles"] = "A;;B";
            AssertThrows<InvalidOperationException>(
                () => GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element).ToList(),
                "Empty persisted owner tokens must remain rejected.",
                "empty handle token");

            element.Properties["GeneratedRebarHandles"] = "0xA;B";
            AssertThrows<InvalidOperationException>(
                () => GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element).ToList(),
                "Non-canonical persisted owner tokens must remain rejected.",
                "non-canonical handle token");

            element.Properties["GeneratedRebarHandles"] = "A;A";
            AssertThrows<InvalidOperationException>(
                () => GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element).ToList(),
                "Duplicate persisted owner tokens must remain rejected.",
                "duplicate handle token");
        }

        private static string CanonicalHandles(int count) =>
            string.Join(";", Enumerable.Range(1, count).Select(x => x.ToString("X")));

        private static void AssertThrows<T>(Action action, string message, string expectedMessageFragment) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException(message + " Wrong exception message: " + ex.Message, ex);
            }
            throw new InvalidOperationException(message + " Expected " + typeof(T).Name + ".");
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }
    }
}
