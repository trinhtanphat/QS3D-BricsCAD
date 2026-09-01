using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ReportingRowProvenanceTargetStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MoveNextAppendMutationFailsBeforeCurrentAndPublishesNothing();
            MoveNextRemoveMutationFailsBeforeCurrentAndPublishesNothing();
            CurrentReplaceMutationFailsClosedAndPublishesNothing();
            CurrentReorderMutationFailsClosedAndPublishesNothing();
            StableTargetStillPublishesAtomically();
            PureStreamingStableTargetRemainsAccepted();
        }

        private static void MoveNextAppendMutationFailsBeforeCurrentAndPublishesNothing()
        {
            var target = SeedTwo();
            var source = new CallbackSource(new[] { "BB" }, onMoveNext: call =>
            {
                if (call == 1) target.Add("EE");
            });

            ThrowsTargetMutation(() => Append(target, source), "MoveNext append mutation");
            Equal(1, source.MoveNextCalls, "MoveNext append calls");
            Equal(0, source.CurrentReads, "MoveNext append Current reads");
            SequenceEqual(new[] { "AA", "DD", "EE" }, target, "MoveNext append target");
        }

        private static void MoveNextRemoveMutationFailsBeforeCurrentAndPublishesNothing()
        {
            var target = SeedTwo();
            var source = new CallbackSource(new[] { "BB" }, onMoveNext: call =>
            {
                if (call == 1) target.RemoveAt(1);
            });

            ThrowsTargetMutation(() => Append(target, source), "MoveNext remove mutation");
            Equal(1, source.MoveNextCalls, "MoveNext remove calls");
            Equal(0, source.CurrentReads, "MoveNext remove Current reads");
            SequenceEqual(new[] { "AA" }, target, "MoveNext remove target");
        }

        private static void CurrentReplaceMutationFailsClosedAndPublishesNothing()
        {
            var target = SeedTwo();
            var source = new CallbackSource(new[] { "BB" }, onCurrent: read =>
            {
                if (read == 1) target[1] = "EE";
            });

            ThrowsTargetMutation(() => Append(target, source), "Current replace mutation");
            Equal(1, source.MoveNextCalls, "Current replace MoveNext calls");
            Equal(1, source.CurrentReads, "Current replace reads");
            SequenceEqual(new[] { "AA", "EE" }, target, "Current replace target");
        }

        private static void CurrentReorderMutationFailsClosedAndPublishesNothing()
        {
            var target = SeedTwo();
            var source = new CallbackSource(new[] { "BB" }, onCurrent: read =>
            {
                if (read != 1) return;
                var first = target[0];
                target[0] = target[1];
                target[1] = first;
            });

            ThrowsTargetMutation(() => Append(target, source), "Current reorder mutation");
            Equal(1, source.MoveNextCalls, "Current reorder MoveNext calls");
            Equal(1, source.CurrentReads, "Current reorder reads");
            SequenceEqual(new[] { "DD", "AA" }, target, "Current reorder target");
        }

        private static void StableTargetStillPublishesAtomically()
        {
            var target = SeedTwo();
            var source = new CallbackSource(new[] { "BB", "CC" });

            Append(target, source);

            SequenceEqual(new[] { "AA", "DD", "BB", "CC" }, target, "stable counted target");
            Equal(3, source.MoveNextCalls, "stable counted MoveNext calls");
            Equal(2, source.CurrentReads, "stable counted Current reads");
        }

        private static void PureStreamingStableTargetRemainsAccepted()
        {
            var target = SeedTwo();
            Append(target, Stream("BB", "CC"));
            SequenceEqual(new[] { "AA", "DD", "BB", "CC" }, target, "stable streaming target");
        }

        private static List<string> SeedTwo() => new List<string> { "AA", "DD" };

        private static IEnumerable<string> Stream(params string[] values)
        {
            foreach (var value in values) yield return value;
        }

        private static void Append(IList<string> target, IEnumerable<string> source)
        {
            var type = typeof(DoorOpeningScheduleBuilder).Assembly.GetType("QS3D.Core.Reporting.ReportingRowProvenance", throwOnError: true)!;
            var method = type.GetMethod("AppendSourceHandles", BindingFlags.Static | BindingFlags.NonPublic)!;
            try
            {
                method.Invoke(null, new object[] { target, source });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void ThrowsTargetMutation(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("target SourceHandles changed", StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException(label + " expected target-stability failure, got '" + ex.Message + "'.", ex);
            }

            throw new InvalidOperationException(label + " expected target-stability failure.");
        }

        private static void SequenceEqual(IList<string> expected, IList<string> actual, string label)
        {
            Equal(expected.Count, actual.Count, label + " count");
            for (var i = 0; i < expected.Count; i++)
                Equal(expected[i], actual[i], label + " item " + i);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    "ReportingRowProvenanceTargetStabilitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CallbackSource : IReadOnlyCollection<string>
        {
            private readonly string[] _items;
            private readonly Action<int>? _onMoveNext;
            private readonly Action<int>? _onCurrent;

            internal CallbackSource(string[] items, Action<int>? onMoveNext = null, Action<int>? onCurrent = null)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
                _onMoveNext = onMoveNext;
                _onCurrent = onCurrent;
            }

            public int Count => _items.Length;
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly CallbackSource _owner;
                private int _index = -1;

                internal Enumerator(CallbackSource owner) { _owner = owner; }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._onCurrent?.Invoke(_owner.CurrentReads);
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    _owner._onMoveNext?.Invoke(_owner.MoveNextCalls);
                    return _index < _owner._items.Length;
                }

                public void Reset() { throw new NotSupportedException(); }
                public void Dispose() { }
            }
        }
    }
}
