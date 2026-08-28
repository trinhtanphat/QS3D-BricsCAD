using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityEvidenceKnownCountOverrunSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            OperandOverrunPrecedesUnexpectedNullValidation();
            ExplanationOverrunPrecedesUnexpectedNullValidation();
            UnderTraversalStillFailsAfterValidEnumeration();
            HonestCountedInputsRemainAcceptedAndOrdered();
            PureStreamingInputKeepsIndependentCapacityBound();
            Console.WriteLine("PASS quantity evidence known-Count overrun ordering");
        }

        private static void OperandOverrunPrecedesUnexpectedNullValidation()
        {
            var valid = new QuantityEvidenceOperand("L", 5m, "m");
            var source = new MisreportedCollection<QuantityEvidenceOperand>(
                1,
                valid,
                null!);

            ThrowsMessage<InvalidOperationException>(
                () => QuantityContribution.Create(
                    "wall.length",
                    "Wall length",
                    QuantityEvidenceOperation.Add,
                    "L",
                    5m,
                    QuantityEvidenceSelector.ForEntity("W-COUNT"),
                    source),
                "Quantity contribution operands count changed during snapshot.");
        }

        private static void ExplanationOverrunPrecedesUnexpectedNullValidation()
        {
            var valid = QuantityContribution.Create(
                "wall.length",
                "Wall length",
                QuantityEvidenceOperation.Add,
                "L",
                5m,
                QuantityEvidenceSelector.ForEntity("W-COUNT"));
            var source = new MisreportedCollection<QuantityContribution>(
                1,
                valid,
                null!);

            ThrowsMessage<InvalidOperationException>(
                () => QuantityExplanation.Create(
                    "W-COUNT",
                    "Wall",
                    "Length",
                    "m",
                    5m,
                    5m,
                    source),
                "Quantity explanation contributions count changed during snapshot.");
        }

        private static void UnderTraversalStillFailsAfterValidEnumeration()
        {
            var valid = new QuantityEvidenceOperand("L", 5m, "m");
            var source = new MisreportedCollection<QuantityEvidenceOperand>(2, valid);

            ThrowsMessage<InvalidOperationException>(
                () => QuantityContribution.Create(
                    "wall.length",
                    "Wall length",
                    QuantityEvidenceOperation.Add,
                    "L",
                    5m,
                    QuantityEvidenceSelector.ForEntity("W-UNDER"),
                    source),
                "Quantity contribution operands count changed during snapshot.");
        }

        private static void HonestCountedInputsRemainAcceptedAndOrdered()
        {
            var contribution = QuantityContribution.Create(
                "wall.area",
                "Wall area",
                QuantityEvidenceOperation.Add,
                "L x H",
                15m,
                QuantityEvidenceSelector.ForEntity("W-HONEST"),
                new List<QuantityEvidenceOperand>
                {
                    new QuantityEvidenceOperand("L", 5m, "m"),
                    new QuantityEvidenceOperand("H", 3m, "m")
                });

            Equal(2, contribution.Operands.Count, "honest counted operand count");
            Equal("H", contribution.Operands[0].Key, "deterministic operand order first");
            Equal("L", contribution.Operands[1].Key, "deterministic operand order second");
        }

        private static void PureStreamingInputKeepsIndependentCapacityBound()
        {
            var operand = new QuantityEvidenceOperand("L", 1m, "m");
            ThrowsMessage<InvalidOperationException>(
                () => QuantityContribution.Create(
                    "stream.length",
                    "Stream length",
                    QuantityEvidenceOperation.Add,
                    "L",
                    1m,
                    QuantityEvidenceSelector.ForEntity("W-STREAM"),
                    Stream(operand, 10001)),
                "Quantity contribution operands supports at most 10000 items.");
        }

        private static IEnumerable<QuantityEvidenceOperand> Stream(QuantityEvidenceOperand operand, int count)
        {
            for (var index = 0; index < count; index++)
            {
                yield return operand;
            }
        }

        private static void ThrowsMessage<TException>(Action action, string expectedMessage)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                Equal(expectedMessage, ex.Message, typeof(TException).Name + " message");
                return;
            }

            throw new Exception("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception(label + ": expected " + expected + ", got " + actual + ".");
            }
        }

        private sealed class MisreportedCollection<T> : ICollection<T>
        {
            private readonly T[] _items;

            public MisreportedCollection(int reportedCount, params T[] items)
            {
                Count = reportedCount;
                _items = items;
            }

            public int Count { get; }
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool Contains(T item)
            {
                return ((ICollection<T>)_items).Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                _items.CopyTo(array, arrayIndex);
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}
