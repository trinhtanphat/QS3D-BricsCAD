using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticTitleBlockKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            MoveNextInducedCountDriftFailsBeforeCurrent();
            CurrentInducedCountDriftFailsBeforeRetention();
            StableCountedInputRemainsAccepted();
            StreamingInputRemainsAccepted();
        }

        private static void MoveNextInducedCountDriftFailsBeforeCurrent()
        {
            var source = new HostileCountedDefinitions(DriftMode.MoveNext);
            InvalidOperationContains(
                () => SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source),
                "known Count changed during traversal",
                "MoveNext-induced title-block Count drift");
            Equal(1, source.MoveNextCalls, "MoveNext drift call count");
            Equal(0, source.CurrentReads, "MoveNext drift must fail before Current");
        }

        private static void CurrentInducedCountDriftFailsBeforeRetention()
        {
            var source = new HostileCountedDefinitions(DriftMode.Current);
            InvalidOperationContains(
                () => SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source),
                "known Count changed during traversal",
                "Current-induced title-block Count drift");
            Equal(1, source.CurrentReads, "Current drift should read exactly one Current");
        }

        private static void StableCountedInputRemainsAccepted()
        {
            var source = new HostileCountedDefinitions(DriftMode.None);
            var map = SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source);
            Equal(1, map.Values.Count, "stable counted title-block map count");
            Equal("STABLE", map.Values[0].DestinationTag, "stable counted destination tag");
            Equal(7, source.CountReads, "stable one-item Count rebound budget");
        }

        private static void StreamingInputRemainsAccepted()
        {
            var map = SemanticTitleBlockParameterMapBuilder.Build(Sheet(), StreamOne());
            Equal(1, map.Values.Count, "streaming title-block map count");
            Equal("STREAM", map.Values[0].DestinationTag, "streaming destination tag");
        }

        private static IEnumerable<SemanticTitleBlockParameterDefinition> StreamOne()
        {
            yield return Definition("STREAM");
        }

        private static SemanticTitleBlockParameterDefinition Definition(string tag)
        {
            return new SemanticTitleBlockParameterDefinition(tag, SemanticTitleBlockSheetField.SheetNumber);
        }

        private static SemanticSheetPlan Sheet()
        {
            return SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    "sheet-transient-count",
                    "A-001",
                    "Transient Count sheet",
                    841d,
                    594d,
                    Array.Empty<SemanticSheetPlacementDefinition>(),
                    "A1"),
                Array.Empty<SemanticViewPlan>());
        }

        private static void InvalidOperationContains(Action action, string expectedText, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException(label + ": unexpected message: " + ex.Message, ex);
            }

            throw new InvalidOperationException(label + ": expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", got " + actual + ".");
        }

        private enum DriftMode
        {
            None,
            MoveNext,
            Current
        }

        private sealed class HostileCountedDefinitions : IReadOnlyCollection<SemanticTitleBlockParameterDefinition>
        {
            private readonly DriftMode _mode;
            private int _count = 1;

            internal HostileCountedDefinitions(DriftMode mode)
            {
                _mode = mode;
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _count;
                }
            }

            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<SemanticTitleBlockParameterDefinition> GetEnumerator()
            {
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<SemanticTitleBlockParameterDefinition>
            {
                private readonly HostileCountedDefinitions _owner;
                private int _position = -1;

                internal Enumerator(HostileCountedDefinitions owner)
                {
                    _owner = owner;
                }

                public SemanticTitleBlockParameterDefinition Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._mode == DriftMode.Current)
                            _owner._count = 2;
                        return Definition(_owner._mode == DriftMode.None ? "STABLE" : "CURRENT");
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _position++;
                    if (_position == 0)
                    {
                        if (_owner._mode == DriftMode.MoveNext)
                            _owner._count = 2;
                        return true;
                    }
                    return false;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}