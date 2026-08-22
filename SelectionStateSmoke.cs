using System;
using System.Linq;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SelectionStateSmoke
    {
        public static void Run()
        {
            ReplaceTrimsDeduplicatesAndIgnoresBlankIds();
            CanonicallyEquivalentReplaceDoesNotRaiseChanged();
            ClearRaisesOnlyWhenStateChanges();
        }

        private static void ReplaceTrimsDeduplicatesAndIgnoresBlankIds()
        {
            var state = new SelectionState();
            var changed = 0;
            state.Changed += (_, __) => changed++;
            state.Replace(new[] { " A ", "a", " B", "   " });

            if (changed != 1) throw new Exception("Canonical selection replace must raise exactly one change event.");
            var ids = state.ElementIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            if (!ids.SequenceEqual(new[] { "A", "B" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Selection state must trim, de-duplicate case-insensitively and ignore blank semantic IDs.");
            if (ids.Any(id => id != id.Trim())) throw new Exception("Selection state must never expose padded semantic IDs.");
        }

        private static void CanonicallyEquivalentReplaceDoesNotRaiseChanged()
        {
            var state = new SelectionState();
            state.Replace(new[] { "A", "B" });
            var changed = 0;
            state.Changed += (_, __) => changed++;
            state.Replace(new[] { " b ", " A ", "a" });
            if (changed != 0) throw new Exception("Canonical-equivalent selection replace must not raise Changed.");
        }

        private static void ClearRaisesOnlyWhenStateChanges()
        {
            var state = new SelectionState();
            var changed = 0;
            state.Changed += (_, __) => changed++;
            state.Clear();
            if (changed != 0) throw new Exception("Clearing an empty selection must not raise Changed.");
            state.Replace(new[] { "A" });
            state.Clear();
            state.Clear();
            if (changed != 2) throw new Exception("Selection replace + first clear must raise two total changes; repeated empty clear must be silent.");
        }
    }
}
