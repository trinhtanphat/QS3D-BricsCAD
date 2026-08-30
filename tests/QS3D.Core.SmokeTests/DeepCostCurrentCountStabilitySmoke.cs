using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class DeepCostCurrentCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RateReferenceRejectsCurrentInducedDriftBeforeNullAcceptance();
            BqLibraryRejectsCurrentInducedDriftBeforeNullAcceptance();
            StableControlsRemainAccepted();
            Console.WriteLine("PASS deep cost Current-induced Count stability");
        }

        private static void RateReferenceRejectsCurrentInducedDriftBeforeNullAcceptance()
        {
            var source = new CurrentDriftCollection<RateReferenceEdge>(null!);
            ExpectCountDrift(
                () => new RateReferenceGraph(source),
                "rate-reference Current-induced Count drift");
            Require(source.CurrentReads == 1, "rate-reference regression must read Current exactly once");
        }

        private static void BqLibraryRejectsCurrentInducedDriftBeforeNullAcceptance()
        {
            var source = new CurrentDriftCollection<BqLibraryEntry>(null!);
            ExpectCountDrift(
                () => new BqLibraryCatalog("LIB-CURRENT-DRIFT", source),
                "BQ library Current-induced Count drift");
            Require(source.CurrentReads == 1, "BQ-library regression must read Current exactly once");
        }

        private static void StableControlsRemainAccepted()
        {
            var graph = new RateReferenceGraph(new[]
            {
                new RateReferenceEdge("RATE-STABLE", RateReferenceTargetKind.BillItem, "ITEM-STABLE")
            });
            Require(graph.Edges.Count == 1, "stable rate-reference control changed");

            var catalog = new BqLibraryCatalog("LIB-STABLE", new[]
            {
                new BqLibraryEntry("ITEM-STABLE", "Description", "m", "Category", 1m)
            });
            Require(catalog.Entries.Count == 1, "stable BQ-library control changed");
        }

        private static void ExpectCountDrift(Action action, string label)
        {
            try
            {
                action();
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                if (ex.Message.IndexOf("known count changed during traversal", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException(label + " was rejected after item acceptance instead of at the Count boundary: " + ex.Message, ex);
            }

            throw new InvalidOperationException(label + " was accepted unexpectedly.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class CurrentDriftCollection<T> : ICollection<T>
        {
            private readonly T _item;
            private bool _emitDrift;

            internal CurrentDriftCollection(T item) => _item = item;
            internal int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    if (_emitDrift)
                    {
                        _emitDrift = false;
                        return 2;
                    }
                    return 1;
                }
            }

            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
            public void CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CurrentDriftCollection<T> _owner;
                private int _state;

                internal Enumerator(CurrentDriftCollection<T> owner) => _owner = owner;

                public bool MoveNext()
                {
                    if (_state != 0)
                    {
                        _state = 2;
                        return false;
                    }
                    _state = 1;
                    return true;
                }

                public T Current
                {
                    get
                    {
                        if (_state != 1) throw new InvalidOperationException();
                        _owner.CurrentReads++;
                        _owner._emitDrift = true;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
