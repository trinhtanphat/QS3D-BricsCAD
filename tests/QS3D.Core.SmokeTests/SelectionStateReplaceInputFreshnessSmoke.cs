using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SelectionStateReplaceInputFreshnessSmoke
    {
        public static void Run()
        {
            StableLazyReplacementPreservesSelectionSemantics();
            ReentrantReplacementFailsWithoutOverwritingNewerSelection();
            ReentrantClearWithEmptyOuterInputFailsBeforeNoOp();
            ReentrantNoOpDoesNotInvalidateOuterReplacement();
        }

        private static void StableLazyReplacementPreservesSelectionSemantics()
        {
            var state = new SelectionState();
            var changed = 0;
            state.Changed += (_, _) => changed++;

            state.Replace(StableIds());

            Equal(1, changed);
            SequenceEqual(new[] { "A", "B" }, state.ElementIds);

            state.Replace(new[] { "a", "B" });
            Equal(1, changed);
        }

        private static void ReentrantReplacementFailsWithoutOverwritingNewerSelection()
        {
            var state = new SelectionState();
            state.Replace(new[] { "BASE" });
            var changed = 0;
            state.Changed += (_, _) => changed++;

            ThrowsContaining<InvalidOperationException>(
                () => state.Replace(ReplaceThenYield(state, "INNER", "OUTER")),
                "Selection changed while replacement element ids were being enumerated");

            Equal(1, changed);
            SequenceEqual(new[] { "INNER" }, state.ElementIds);
        }

        private static void ReentrantClearWithEmptyOuterInputFailsBeforeNoOp()
        {
            var state = new SelectionState();
            state.Replace(new[] { "BASE" });
            var changed = 0;
            state.Changed += (_, _) => changed++;

            ThrowsContaining<InvalidOperationException>(
                () => state.Replace(ClearThenStop(state)),
                "Selection changed while replacement element ids were being enumerated");

            Equal(1, changed);
            Equal(0, state.ElementIds.Count);
        }

        private static void ReentrantNoOpDoesNotInvalidateOuterReplacement()
        {
            var state = new SelectionState();
            state.Replace(new[] { "BASE" });
            var changed = 0;
            state.Changed += (_, _) => changed++;

            state.Replace(NoOpThenYield(state, "OUTER"));

            Equal(1, changed);
            SequenceEqual(new[] { "OUTER" }, state.ElementIds);
        }

        private static IEnumerable<string> StableIds()
        {
            yield return " B ";
            yield return "A";
            yield return "a";
            yield return "   ";
        }

        private static IEnumerable<string> ReplaceThenYield(SelectionState state, string innerId, string outerId)
        {
            state.Replace(new[] { innerId });
            yield return outerId;
        }

        private static IEnumerable<string> ClearThenStop(SelectionState state)
        {
            state.Clear();
            yield break;
        }

        private static IEnumerable<string> NoOpThenYield(SelectionState state, string outerId)
        {
            state.Replace(new[] { "base" });
            yield return outerId;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual)
        {
            if (!expected.SequenceEqual(actual, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Expected selection [" + string.Join(", ", expected) + "] but got [" + string.Join(", ", actual) + "].");
        }

        private static void ThrowsContaining<T>(Action action, string expectedText) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("Expected exception message containing '" + expectedText + "', got '" + ex.Message + "'.");
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
