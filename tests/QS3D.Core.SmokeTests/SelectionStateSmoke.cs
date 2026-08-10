using System;
using System.Collections.Generic;
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
            ElementIdsAreDeterministicAndDoNotLeakMutableState();
            ClearRaisesOnlyWhenStateChanges();
        }

        private static void ReplaceTrimsDeduplicatesAndIgnoresBlankIds()
        {
            var state = new SelectionState();
            var changed = 0;
            state.Changed += (_, __) => changed++;
            state.Replace(new[] { " A ", "a", " B", "   " });

            if (changed != 1) throw new Exception("Canonical selection replace must raise exactly one change event.");
            var ids = state.ElementIds.ToArray();
            if (!ids.SequenceEqual(new[] { "A", "B" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Selection state must trim, de-duplicate case-insensitively, ignore blanks and expose deterministic ordering.");
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

        private static void ElementIdsAreDeterministicAndDoNotLeakMutableState()
        {
            var state = new SelectionState();
            state.Replace(new[] { "Z", "a", "M" });
            var exposed = state.ElementIds;
            if (exposed is HashSet<string>) throw new Exception("Selection state must not expose its mutable HashSet implementation.");
            var ids = exposed.ToArray();
            if (!ids.SequenceEqual(new[] { "a", "M", "Z" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Selection state enumeration must be deterministic and case-insensitively ordered.");

            if (exposed is string[] snapshot && snapshot.Length > 0) snapshot[0] = "MUTATED";
            var after = state.ElementIds.ToArray();
            if (!after.SequenceEqual(new[] { "a", "M", "Z" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Mutating an exposed selection snapshot must not mutate internal selection state.");
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
