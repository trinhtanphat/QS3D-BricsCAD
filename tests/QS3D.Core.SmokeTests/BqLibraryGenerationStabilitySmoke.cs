using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class BqLibraryGenerationStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ConstructorSameCountReplacementIsRejected();
            ImportSameCountReorderIsRejected();
            StableCountedSourcesRemainAccepted();
            StreamingSourcesRemainSinglePassCompatible();
            Console.WriteLine("PASS BQ library generation stability");
        }

        private static void ConstructorSameCountReplacementIsRejected()
        {
            var a = Entry("BQ-A", "Concrete", "m3", "Structure/Concrete", 100m);
            var b = Entry("BQ-B", "Rebar", "kg", "Structure/Rebar", 2m);
            var replacement = Entry("BQ-C", "Formwork", "m2", "Structure/Formwork", 30m);
            var source = new SameCountGenerationCollection<BqLibraryEntry>(
                new[] { a, b },
                new[] { a, replacement });

            BqLibraryCatalog? published = null;
            ExpectGenerationDrift(
                () => published = new BqLibraryCatalog("LIB-A", source),
                "BQ library entry collection",
                "constructor same-count replacement");
            Require(published == null, "constructor replacement drift published a partial catalog");
        }

        private static void ImportSameCountReorderIsRejected()
        {
            var baseline = new BqLibraryCatalog("LIB-B", new[]
            {
                Entry("BQ-D", "Existing", "nr", "Existing")
            });
            var a = Entry("BQ-E", "Earthwork", "m3", "Civil/Earthwork");
            var b = Entry("BQ-F", "Drainage", "m", "Civil/Drainage");
            var source = new SameCountGenerationCollection<BqLibraryEntry>(
                new[] { a, b },
                new[] { b, a });

            ExpectGenerationDrift(
                () => baseline.ImportFromProject(source, replaceExisting: true),
                "BQ project import collection",
                "import same-count reorder");
        }

        private static void StableCountedSourcesRemainAccepted()
        {
            var a = Entry("BQ-G", "Blockwork", "m2", "Architecture/Walls");
            var b = Entry("BQ-H", "Paint", "m2", "Architecture/Finishes");
            var catalogSource = new SameCountGenerationCollection<BqLibraryEntry>(
                new[] { b, a },
                new[] { b, a });
            var catalog = new BqLibraryCatalog("LIB-C", catalogSource);
            Require(catalogSource.GetEnumeratorCalls == 2, "stable counted catalog source must be admitted then replayed exactly once");
            Require(catalog.Entries.Count == 2 && catalog.Entries[0].ItemCode == "BQ-H" && catalog.Entries[1].ItemCode == "BQ-G",
                "stable counted catalog lost canonical category ordering");

            var importA = Entry("BQ-I", "Tiles", "m2", "Architecture/Finishes");
            var importB = Entry("BQ-J", "Doors", "nr", "Architecture/Doors");
            var importSource = new SameCountGenerationCollection<BqLibraryEntry>(
                new[] { importA, importB },
                new[] { importA, importB });
            var imported = catalog.ImportFromProject(importSource, replaceExisting: true);
            Require(importSource.GetEnumeratorCalls == 2, "stable counted import source must be admitted then replayed exactly once");
            Require(imported.Entries.Count == 4, "stable counted import changed cardinality");
        }

        private static void StreamingSourcesRemainSinglePassCompatible()
        {
            var catalogSource = new SinglePassEnumerable<BqLibraryEntry>(
                Entry("BQ-K", "Ceiling", "m2", "Architecture/Ceilings"),
                Entry("BQ-L", "Skirting", "m", "Architecture/Finishes"));
            var catalog = new BqLibraryCatalog("LIB-D", catalogSource);
            Require(catalogSource.GetEnumeratorCalls == 1, "streaming catalog source was replayed unexpectedly");

            var importSource = new SinglePassEnumerable<BqLibraryEntry>(
                Entry("BQ-M", "Kerb", "m", "Civil/Kerbs"));
            var imported = catalog.ImportFromProject(importSource, replaceExisting: true);
            Require(importSource.GetEnumeratorCalls == 1, "streaming import source was replayed unexpectedly");
            Require(imported.Entries.Count == 3, "streaming import lost entries");
        }

        private static BqLibraryEntry Entry(
            string itemCode,
            string description,
            string unit,
            string categoryPath,
            decimal? rate = null) =>
            new BqLibraryEntry(itemCode, description, unit, categoryPath, rate);

        private static void ExpectGenerationDrift(Action action, string collectionLabel, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException error)
            {
                var expected = collectionLabel + " content changed during traversal.";
                if (string.Equals(error.Message, expected, StringComparison.Ordinal))
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
