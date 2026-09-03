using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateBookGenerationStabilitySmoke
    {
        private static readonly DateTime T0 = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Initialize()
        {
            SameCountReplacementIsRejected();
            SameCountReorderIsRejected();
            SameIdentityContentChangeIsRejected();
            StableCountedGenerationRemainsAccepted();
            StreamingInputRemainsSinglePassCompatible();
            Console.WriteLine("PASS rate book generation stability");
        }

        private static void SameCountReplacementIsRejected()
        {
            var a = Item("RATE-A", "COST-A", 10m, "v1", 0);
            var b = Item("RATE-B", "COST-B", 20m, "v1", 1);
            var c = Item("RATE-C", "COST-C", 30m, "v1", 2);
            var source = new SameCountGenerationCollection<RateItem>(
                new[] { a, b },
                new[] { a, c });

            RateBook? published = null;
            ExpectContentDrift(() => published = new RateBook("book-replacement", source), "same-count rate replacement");
            Require(published == null, "replacement drift published a partial rate book");
        }

        private static void SameCountReorderIsRejected()
        {
            var a = Item("RATE-D", "COST-D", 40m, "v1", 3);
            var b = Item("RATE-E", "COST-E", 50m, "v1", 4);
            var source = new SameCountGenerationCollection<RateItem>(
                new[] { a, b },
                new[] { b, a });

            ExpectContentDrift(() => new RateBook("book-reorder", source), "same-count rate reorder");
        }

        private static void SameIdentityContentChangeIsRejected()
        {
            var admitted = Item("RATE-F", "COST-F", 60m, "v1", 5);
            var changed = Item("RATE-F", "COST-F", 61m, "v2", 5);
            var source = new SameCountGenerationCollection<RateItem>(
                new[] { admitted },
                new[] { changed });

            ExpectContentDrift(() => new RateBook("book-content", source), "same-identity rate content change");
        }

        private static void StableCountedGenerationRemainsAccepted()
        {
            var older = Item("RATE-G", "COST-G", 70m, "v1", 6);
            var newer = Item("RATE-H", "COST-G", 80m, "v2", 7);
            var source = new SameCountGenerationCollection<RateItem>(
                new[] { newer, older },
                new[] { newer, older });

            var book = new RateBook("book-stable", source);
            Require(source.GetEnumeratorCalls == 2, "stable counted rate source must be admitted then replayed exactly once");
            Require(book.Items.Count == 2, "stable counted rate source changed cardinality");
            Require(book.Items[0].RateItemId == "RATE-G" && book.Items[1].RateItemId == "RATE-H",
                "stable counted rate book lost canonical ordering");
            var resolved = book.Resolve(new CostCode("COST-G"), "m3", "USD", T0.AddTicks(8));
            Require(resolved.IsMatched && resolved.Item != null && resolved.Item.RateItemId == "RATE-H",
                "stable counted rate replay changed Resolve semantics");
        }

        private static void StreamingInputRemainsSinglePassCompatible()
        {
            var source = new SinglePassEnumerable<RateItem>(
                Item("RATE-I", "COST-I", 90m, "v1", 8),
                Item("RATE-J", "COST-J", 100m, "v1", 9));

            var book = new RateBook("book-stream", source);
            Require(source.GetEnumeratorCalls == 1, "raw streaming rate source was replayed unexpectedly");
            Require(book.Items.Count == 2, "raw streaming rate source lost items");
        }

        private static RateItem Item(string id, string code, decimal rate, string version, int ticks) =>
            new RateItem(id, new CostCode(code), "m3", "USD", rate, T0.AddTicks(ticks), version);

        private static void ExpectContentDrift(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException error)
            {
                if (string.Equals(error.Message, "Rate book item source content changed during traversal.", StringComparison.Ordinal))
                    return;
                throw new InvalidOperationException(label + " failed for the wrong reason: " + error.Message, error);
            }

            throw new InvalidOperationException(label + " was accepted unexpectedly.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class SameCountGenerationCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[][] _generations;
            private int _enumerationIndex;

            internal SameCountGenerationCollection(params T[][] generations)
            {
                if (generations == null || generations.Length == 0)
                    throw new ArgumentException("At least one generation is required.", nameof(generations));
                _generations = generations;
                Count = generations[0].Length;
                for (var i = 1; i < generations.Length; i++)
                {
                    if (generations[i].Length != Count)
                        throw new ArgumentException("All generations must preserve Count.", nameof(generations));
                }
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                var index = _enumerationIndex < _generations.Length ? _enumerationIndex++ : _generations.Length - 1;
                return ((IEnumerable<T>)_generations[index]).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class SinglePassEnumerable<T> : IEnumerable<T>
        {
            private readonly T[] _items;
            internal SinglePassEnumerable(params T[] items) { _items = items; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                if (GetEnumeratorCalls > 1)
                    throw new InvalidOperationException("Streaming source was enumerated more than once.");
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
