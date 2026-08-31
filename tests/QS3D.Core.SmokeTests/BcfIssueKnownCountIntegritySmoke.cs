using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfIssueKnownCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            TopicOverrunRejectsBeforeCurrent();
            TopicPostTraversalCountDriftRejects();
            ComponentOverrunRejectsBeforeCurrent();
            StreamingTopicSourceRemainsAccepted();
        }

        private static void TopicOverrunRejectsBeforeCurrent()
        {
            var source = new CurrentTrackingCollection<BcfTopic>(
                _ => 1,
                NewTopic("00000000-0000-0000-0000-000000000101"),
                NewTopic("00000000-0000-0000-0000-000000000102"));

            ThrowsCountIntegrity(() => BcfIssueExchange.Create(source), "topic Count overrun");
            Require(source.MoveNextCalls == 2,
                "BCF topic Count overrun must observe the first unexpected successful MoveNext.");
            Require(source.CurrentReads == 1,
                "BCF topic Count overrun exposed IEnumerator.Current beyond the admitted Count.");
        }

        private static void TopicPostTraversalCountDriftRejects()
        {
            var source = new CurrentTrackingCollection<BcfTopic>(
                read => read <= 5 ? 1 : 2,
                NewTopic("00000000-0000-0000-0000-000000000103"));

            ThrowsCountIntegrity(() => BcfIssueExchange.Create(source), "topic post-traversal Count drift");
            Require(source.CountReads >= 6,
                "BCF topic Count evidence was not rebound after terminal MoveNext.");
            Require(source.MoveNextCalls == 2,
                "BCF topic post-traversal Count drift must observe one admitted item and terminal MoveNext.");
            Require(source.CurrentReads == 1,
                "BCF topic Count drift test must expose exactly one admitted topic before post-terminal drift.");
        }

        private static void ComponentOverrunRejectsBeforeCurrent()
        {
            var source = new CurrentTrackingCollection<BcfComponentReference>(
                _ => 1,
                new BcfComponentReference("E-1", "0000000000000000000001"),
                new BcfComponentReference("E-2", "0000000000000000000002"));

            ThrowsCountIntegrity(
                () => new BcfViewpoint(
                    "00000000-0000-0000-0000-000000000201",
                    NewCamera(),
                    source),
                "component Count overrun");
            Require(source.MoveNextCalls == 2,
                "BCF component Count overrun must observe the first unexpected successful MoveNext.");
            Require(source.CurrentReads == 1,
                "BCF component Count overrun exposed IEnumerator.Current beyond the admitted Count.");
        }

        private static void StreamingTopicSourceRemainsAccepted()
        {
            var exchange = BcfIssueExchange.Create(StreamOneTopic());
            Require(exchange.Topics.Count == 1,
                "BCF pure streaming topic source must remain supported.");
        }

        private static IEnumerable<BcfTopic> StreamOneTopic()
        {
            yield return NewTopic("00000000-0000-0000-0000-000000000104");
        }

        private static BcfTopic NewTopic(string id) =>
            new BcfTopic(
                id,
                "Count integrity",
                "Open",
                "Issue",
                "BCF Count integrity smoke",
                "qs3d@example.invalid",
                new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc),
                Array.Empty<BcfComment>(),
                Array.Empty<BcfViewpoint>());

        private static BcfOrthogonalCamera NewCamera() =>
            new BcfOrthogonalCamera(
                new BcfPoint3(0d, 0d, 0d),
                new BcfPoint3(0d, 0d, -1d),
                new BcfPoint3(0d, 1d, 0d),
                1d,
                1d);

        private static void ThrowsCountIntegrity(Action action, string label)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException(
                    "BcfIssueKnownCountIntegritySmoke " + label + " returned the wrong diagnostic: " + ex.Message,
                    ex);
            }

            throw new InvalidOperationException(
                "BcfIssueKnownCountIntegritySmoke did not reject " + label + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class CurrentTrackingCollection<T> : ICollection<T>
        {
            private readonly Func<int, int> _countForRead;
            private readonly T[] _items;

            internal CurrentTrackingCollection(Func<int, int> countForRead, params T[] items)
            {
                _countForRead = countForRead ?? throw new ArgumentNullException(nameof(countForRead));
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _countForRead(CountReads);
                }
            }

            public int CountReads { get; private set; }
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator() => new TrackingEnumerator(this, _items);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class TrackingEnumerator : IEnumerator<T>
            {
                private readonly CurrentTrackingCollection<T> _owner;
                private readonly T[] _items;
                private int _index = -1;

                internal TrackingEnumerator(CurrentTrackingCollection<T> owner, T[] items)
                {
                    _owner = owner;
                    _items = items;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _items.Length)
                            throw new InvalidOperationException("Current requested outside the active BCF traversal item.");
                        return _items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_index + 1 >= _items.Length)
                    {
                        _index = _items.Length;
                        return false;
                    }
                    _index++;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}