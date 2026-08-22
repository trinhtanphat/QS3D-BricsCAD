using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarScheduleBuilderResourceBoundSmoke
    {
        private const int MaxRowCount = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactRowBoundRemainsAccepted();
            RowBeyondBoundFailsClosed();
            InfiniteEnumerationStopsAtFirstOverBoundRow();
        }

        private static void ExactRowBoundRemainsAccepted()
        {
            var rows = RebarScheduleBuilder.Build(Enumerable.Repeat(CanonicalInput(), MaxRowCount));

            Equal(MaxRowCount, rows.Count);
            Equal("1D16", rows[0].Notation);
            Equal("1D16", rows[rows.Count - 1].Notation);
        }

        private static void RowBeyondBoundFailsClosed()
        {
            Throws<ArgumentOutOfRangeException>(() =>
                RebarScheduleBuilder.Build(Enumerable.Repeat(CanonicalInput(), MaxRowCount + 1)));
        }

        private static void InfiniteEnumerationStopsAtFirstOverBoundRow()
        {
            var source = new CountingEnumerable(CanonicalInput());

            Throws<ArgumentOutOfRangeException>(() => RebarScheduleBuilder.Build(source));
            Equal(MaxRowCount + 1, source.MoveNextCount);
        }

        private static RebarScheduleInput CanonicalInput()
        {
            return new RebarScheduleInput
            {
                ElementId = "E1",
                BarMark = "B1",
                ShapeCode = "00",
                Notation = "1D16",
                CuttingLengthM = 2d
            };
        }

        private sealed class CountingEnumerable : IEnumerable<RebarScheduleInput>
        {
            private readonly RebarScheduleInput _input;

            public CountingEnumerable(RebarScheduleInput input)
            {
                _input = input ?? throw new ArgumentNullException(nameof(input));
            }

            public int MoveNextCount { get; private set; }

            public IEnumerator<RebarScheduleInput> GetEnumerator()
            {
                while (true)
                {
                    MoveNextCount++;
                    yield return _input;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
