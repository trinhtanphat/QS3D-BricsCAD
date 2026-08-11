using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SelectionStateInputBoundSmoke
    {
        internal static void Run()
        {
            TenThousandEntriesRemainSupported();
            KnownOversizeInputFailsWithoutMutationOrEvent();
            LazyOversizeInputStopsAtMaxPlusOneWithoutMutationOrEvent();
        }

        private static void TenThousandEntriesRemainSupported()
        {
            var state = new SelectionState();
            var changed = 0;
            state.Changed += (_, __) => changed++;

            state.Replace(Enumerable.Range(0, 10000).Select(index => "E-" + index));

            Equal(10000, state.ElementIds.Count);
            Equal(1, changed);
        }

        private static void KnownOversizeInputFailsWithoutMutationOrEvent()
        {
            var state = new SelectionState();
            state.Replace(new[] { "KEEP" });
            var changed = 0;
            state.Changed += (_, __) => changed++;
            var oversized = Enumerable.Range(0, 10001).Select(index => "E-" + index).ToArray();

            Throws<InvalidOperationException>(() => state.Replace(oversized));

            Equal(0, changed);
            Equal(1, state.ElementIds.Count);
            Equal("KEEP", state.ElementIds.Single());
        }

        private static void LazyOversizeInputStopsAtMaxPlusOneWithoutMutationOrEvent()
        {
            var state = new SelectionState();
            state.Replace(new[] { "KEEP" });
            var changed = 0;
            state.Changed += (_, __) => changed++;
            var observed = 0;

            Throws<InvalidOperationException>(() => state.Replace(CountedIds(20000, () => observed++)));

            Equal(10001, observed);
            Equal(0, changed);
            Equal(1, state.ElementIds.Count);
            Equal("KEEP", state.ElementIds.Single());
        }

        private static IEnumerable<string> CountedIds(int count, Action onYield)
        {
            for (var index = 0; index < count; index++)
            {
                onYield();
                yield return "E-" + index;
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + " but got " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class SelectionStateInputBoundSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SelectionStateInputBoundSmoke.Run();
    }
}
