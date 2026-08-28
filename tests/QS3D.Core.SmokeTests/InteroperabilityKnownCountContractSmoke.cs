using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Interoperability;

namespace QS3D.Core.SmokeTests
{
    internal static class InteroperabilityKnownCountContractSmoke
    {
        internal static void Run()
        {
            NestedCollectionConflictingCountFailsBeforeEnumeration();
            FactSetOversizedCountFailsBeforeEnumeration();
            AdmissionNegativeCountFailsBeforeEnumeration();
            KnownCountTraversalMismatchFailsClosed();
            HonestCountedInputsRemainSupported();
            PureStreamingFactSetRemainsBounded();
        }

        private static void NestedCollectionConflictingCountFailsBeforeEnumeration()
        {
            var provenance = IfcProvenance("batch-nested-conflict");
            var identity = InteroperabilityElementIdentity.ForIfc(provenance, "IFC-NESTED", "QS-NESTED");
            var property = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "CountContract",
                "Length",
                "1",
                InteroperabilityPropertyValueKind.Number,
                "m",
                isMeasured: true);
            var values = new CountContractEnumerable<InteroperabilityPropertyFact>(1, 2, 1, new[] { property });

            Throws<InvalidOperationException>(() => new InteroperabilityElementRecord(
                identity,
                values,
                Array.Empty<InteroperabilityClassificationReference>(),
                Array.Empty<InteroperabilityQuantityFact>(),
                Array.Empty<string>()));
            True(!values.Enumerated);
        }

        private static void FactSetOversizedCountFailsBeforeEnumeration()
        {
            var provenance = IfcProvenance("batch-factset-oversized");
            var record = EmptyRecord(provenance, "IFC-OVER", "over");
            var records = new CountContractEnumerable<InteroperabilityElementRecord>(
                InteroperabilityFactSet.MaxRecords + 1,
                InteroperabilityFactSet.MaxRecords + 1,
                InteroperabilityFactSet.MaxRecords + 1,
                new[] { record });

            Throws<InvalidOperationException>(() => InteroperabilityFactSet.Create(provenance, records));
            True(!records.Enumerated);
        }

        private static void AdmissionNegativeCountFailsBeforeEnumeration()
        {
            var provenance = IfcProvenance("batch-admission-negative");
            var factSet = InteroperabilityFactSet.Create(provenance, Array.Empty<InteroperabilityElementRecord>());
            var diagnostic = Diagnostic("NEGATIVE_COUNT");
            var diagnostics = new CountContractEnumerable<InteroperabilityLossDiagnostic>(
                -1,
                -1,
                -1,
                new[] { diagnostic });

            Throws<InvalidOperationException>(() => InteroperabilityAdmission.Evaluate(factSet, diagnostics));
            True(!diagnostics.Enumerated);
        }

        private static void KnownCountTraversalMismatchFailsClosed()
        {
            var provenance = IfcProvenance("batch-count-mismatch");
            var record = EmptyRecord(provenance, "IFC-MISMATCH", "mismatch");
            var shortRecords = new CountContractEnumerable<InteroperabilityElementRecord>(
                2,
                2,
                2,
                new[] { record });

            Throws<InvalidOperationException>(() => InteroperabilityFactSet.Create(provenance, shortRecords));
            True(shortRecords.Enumerated);

            var factSet = InteroperabilityFactSet.Create(provenance, Array.Empty<InteroperabilityElementRecord>());
            var longDiagnostics = new CountContractEnumerable<InteroperabilityLossDiagnostic>(
                1,
                1,
                1,
                new[] { Diagnostic("MISMATCH-A"), Diagnostic("MISMATCH-B") });

            Throws<InvalidOperationException>(() => InteroperabilityAdmission.Evaluate(factSet, longDiagnostics));
            True(longDiagnostics.Enumerated);
        }

        private static void HonestCountedInputsRemainSupported()
        {
            var provenance = IfcProvenance("batch-honest-count");
            var b = EmptyRecord(provenance, "B-source", "b");
            var a = EmptyRecord(provenance, "A-source", "a");
            var records = new CountContractEnumerable<InteroperabilityElementRecord>(2, 2, 2, new[] { b, a });

            var factSet = InteroperabilityFactSet.Create(provenance, records);
            True(records.Enumerated);
            Equal(2, factSet.Records.Count);
            Equal("A-source", factSet.Records[0].Identity.SourceElementId);
            Equal("B-source", factSet.Records[1].Identity.SourceElementId);

            var diagnostics = new CountContractEnumerable<InteroperabilityLossDiagnostic>(
                1,
                1,
                1,
                new[] { Diagnostic("HONEST_COUNT") });
            var admission = InteroperabilityAdmission.Evaluate(factSet, diagnostics);
            True(diagnostics.Enumerated);
            True(admission.Diagnostics.Any(x => x.Code == "HONEST_COUNT"));
        }

        private static void PureStreamingFactSetRemainsBounded()
        {
            var provenance = IfcProvenance("batch-streaming-bound");
            var record = EmptyRecord(provenance, "IFC-STREAM", "stream");
            Throws<InvalidOperationException>(() => InteroperabilityFactSet.Create(
                provenance,
                StreamRepeated(record, InteroperabilityFactSet.MaxRecords + 1)));
        }

        private static IEnumerable<T> StreamRepeated<T>(T value, int count)
        {
            for (var i = 0; i < count; i++) yield return value;
        }

        private static InteroperabilityElementRecord EmptyRecord(
            InteroperabilitySourceProvenance provenance,
            string sourceId,
            string provenanceToken)
        {
            return new InteroperabilityElementRecord(
                InteroperabilityElementIdentity.ForIfc(provenance, sourceId, "QS-" + sourceId),
                Array.Empty<InteroperabilityPropertyFact>(),
                Array.Empty<InteroperabilityClassificationReference>(),
                Array.Empty<InteroperabilityQuantityFact>(),
                new[] { provenanceToken });
        }

        private static InteroperabilityLossDiagnostic Diagnostic(string code)
        {
            return new InteroperabilityLossDiagnostic(
                code,
                InteroperabilityDiagnosticSeverity.Info,
                "Synthetic Count-contract diagnostic.");
        }

        private static InteroperabilitySourceProvenance IfcProvenance(string batch)
        {
            return new InteroperabilitySourceProvenance(
                InteroperabilitySourceSystem.Ifc,
                InteroperabilityTransport.Ifc,
                "model.ifc",
                "sha256:count-contract",
                "IFC4",
                batch);
        }

        private sealed class CountContractEnumerable<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly IReadOnlyList<T> _values;

            internal CountContractEnumerable(
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                IEnumerable<T> values)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _values = values.ToList().AsReadOnly();
            }

            internal bool Enumerated { get; private set; }

            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                Enumerated = true;
                return _values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<T>)this).GetEnumerator();
            }

            bool ICollection<T>.Contains(T item) => _values.Contains(item);

            void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            {
                for (var i = 0; i < _values.Count; i++) array[arrayIndex + i] = _values[i];
            }

            void ICollection.CopyTo(Array array, int index)
            {
                for (var i = 0; i < _values.Count; i++) array.SetValue(_values[i], index + i);
            }

            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "InteroperabilityKnownCountContractSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool value)
        {
            if (!value)
                throw new InvalidOperationException("InteroperabilityKnownCountContractSmoke assertion failed.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(
                "InteroperabilityKnownCountContractSmoke expected exception " + typeof(TException).Name + ".");
        }
    }
}
