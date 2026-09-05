using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomSourceSignatureBoundSmoke
    {
        private const int MaxHandles = 5000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ExactBoundaryRemainsAccepted();
            PersistedSignatureOverBoundaryFailsClosed();
            PersistedHandleFallbackOverBoundaryFailsClosed();
            MarkActiveOverBoundaryFailsClosedBeforeMutation();
            RemoveEmptyEntriesSemanticsRemainStable();
            WhitespaceOnlyTokensStillConsumeTheInputEnvelope();
            OpaqueCaseEquivalentPermutationsCanonicalizeDeterministically();
        }

        private static void ExactBoundaryRemainsAccepted()
        {
            var room = NewRoom("BOUNDARY");
            room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey] = CanonicalHandles(MaxHandles);

            var normalized = AutoRoomLifecycle.SourceSignature(room);
            AssertEqual(MaxHandles, CountHandles(normalized), "Exact Auto Room source-signature boundary must remain accepted.");
        }

        private static void PersistedSignatureOverBoundaryFailsClosed()
        {
            var room = NewRoom("SIGNATURE-OVER");
            room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey] = CanonicalHandles(MaxHandles + 1);

            AssertThrows<InvalidOperationException>(
                () => AutoRoomLifecycle.SourceSignature(room),
                "Persisted Auto Room source signature above the 5,000-input envelope must fail closed.",
                "cannot exceed 5000 input entries");
        }

        private static void PersistedHandleFallbackOverBoundaryFailsClosed()
        {
            var room = NewRoom("HANDLES-OVER");
            room.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = CanonicalHandles(MaxHandles + 1);

            AssertThrows<InvalidOperationException>(
                () => AutoRoomLifecycle.SourceSignature(room),
                "Persisted Auto Room source-handle fallback above the 5,000-input envelope must fail closed.",
                "cannot exceed 5000 input entries");
        }

        private static void MarkActiveOverBoundaryFailsClosedBeforeMutation()
        {
            var room = NewRoom("MARK-ACTIVE-OVER");
            room.Properties[AutoRoomLifecycle.BoundaryStateKey] = AutoRoomLifecycle.BoundaryStateStale;
            room.Properties["BoundaryStaleUtc"] = "sentinel";
            room.Properties["BoundaryStaleReason"] = "sentinel";

            AssertThrows<InvalidOperationException>(
                () => AutoRoomLifecycle.MarkActive(room, CanonicalHandles(MaxHandles + 1)),
                "Oversized Auto Room source signature must fail before MarkActive mutates lifecycle state.",
                "cannot exceed 5000 input entries");

            AssertEqual(AutoRoomLifecycle.BoundaryStateStale, room.Properties[AutoRoomLifecycle.BoundaryStateKey], "MarkActive mutated state before validating signature envelope.");
            AssertEqual("sentinel", room.Properties["BoundaryStaleUtc"], "MarkActive removed stale UTC before validating signature envelope.");
            AssertEqual("sentinel", room.Properties["BoundaryStaleReason"], "MarkActive removed stale reason before validating signature envelope.");
        }

        private static void RemoveEmptyEntriesSemanticsRemainStable()
        {
            var room = NewRoom("EMPTY-TOKENS");
            room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey] = ";A;;b;";
            AssertEqual("A;B", AutoRoomLifecycle.SourceSignature(room), "Bounded parsing changed historical RemoveEmptyEntries semantics.");
        }

        private static void WhitespaceOnlyTokensStillConsumeTheInputEnvelope()
        {
            var prefix = CanonicalHandles(MaxHandles - 1);
            var raw = prefix + "; ;ABCDEF";
            var room = NewRoom("WHITESPACE-COUNT");
            room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey] = raw;

            AssertThrows<InvalidOperationException>(
                () => AutoRoomLifecycle.SourceSignature(room),
                "Whitespace-only non-empty tokens historically survive RemoveEmptyEntries and must still consume the 5,000-input envelope.",
                "cannot exceed 5000 input entries");
        }

        private static void OpaqueCaseEquivalentPermutationsCanonicalizeDeterministically()
        {
            var first = AutoRoomLifecycle.NormalizeSourceHandles(new[] { "room-x", "ZONE-y", "ROOM-X" });
            var reversed = AutoRoomLifecycle.NormalizeSourceHandles(new[] { "ROOM-X", "zone-Y", "room-x" });

            AssertEqual("ROOM-X;ZONE-Y", first, "Opaque Auto Room source handles must have one exact canonical casing.");
            AssertEqual(first, reversed, "Case-equivalent Auto Room source-handle permutations must produce the same persisted signature.");
        }

        private static ProjectElement NewRoom(string id) =>
            new ProjectElement(id, ElementCategory.Room, "room", "f", "z");

        private static string CanonicalHandles(int count) =>
            string.Join(";", Enumerable.Range(1, count).Select(x => x.ToString("X")));

        private static int CountHandles(string signature) =>
            signature.Length == 0 ? 0 : signature.Count(x => x == ';') + 1;

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
