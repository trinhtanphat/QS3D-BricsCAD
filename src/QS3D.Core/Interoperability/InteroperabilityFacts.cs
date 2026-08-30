using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QS3D.Core.Interoperability
{
    public enum InteroperabilitySourceSystem
    {
        BricsCad = 0,
        Dwg = 1,
        Dxf = 2,
        Ifc = 3,
        RevitBridge = 4,
        NeutralSnapshot = 5
    }

    public enum InteroperabilityTransport
    {
        NativeHost = 0,
        Dwg = 1,
        Dxf = 2,
        Ifc = 3,
        NeutralSnapshot = 4
    }

    public enum InteroperabilityPropertyValueKind
    {
        Text = 0,
        Number = 1,
        Boolean = 2
    }

    public enum InteroperabilityQuantityOrigin
    {
        DeclaredSource = 0,
        DerivedQs3d = 1
    }

    public enum InteroperabilityDiagnosticSeverity
    {
        Info = 0,
        Warning = 1,
        Blocking = 2
    }

    public sealed class InteroperabilitySourceProvenance
    {
        public InteroperabilitySourceProvenance(
            InteroperabilitySourceSystem sourceSystem,
            InteroperabilityTransport transport,
            string sourceDocumentId,
            string? sourceFingerprint,
            string? sourceSchemaVersion,
            string importBatchId)
        {
            SourceSystem = InteroperabilityContract.RequireDefined(sourceSystem, nameof(sourceSystem));
            Transport = InteroperabilityContract.RequireDefined(transport, nameof(transport));
            SourceDocumentId = InteroperabilityContract.RequireToken(sourceDocumentId, nameof(sourceDocumentId));
            SourceFingerprint = InteroperabilityContract.OptionalToken(sourceFingerprint, nameof(sourceFingerprint));
            SourceSchemaVersion = InteroperabilityContract.OptionalToken(sourceSchemaVersion, nameof(sourceSchemaVersion));
            ImportBatchId = InteroperabilityContract.RequireToken(importBatchId, nameof(importBatchId));
            ScopeKey = InteroperabilityContract.BuildScopeKey(
                SourceSystem.ToString(),
                Transport.ToString(),
                SourceDocumentId,
                SourceFingerprint);
        }

        public InteroperabilitySourceSystem SourceSystem { get; }
        public InteroperabilityTransport Transport { get; }
        public string SourceDocumentId { get; }
        public string? SourceFingerprint { get; }
        public string? SourceSchemaVersion { get; }
        public string ImportBatchId { get; }
        public string ScopeKey { get; }
        public bool HasSourceFingerprint => SourceFingerprint != null;

        internal bool MatchesFactSetProvenance(InteroperabilitySourceProvenance other)
        {
            if (other == null) return false;
            return string.Equals(ScopeKey, other.ScopeKey, StringComparison.Ordinal) &&
                string.Equals(SourceSchemaVersion, other.SourceSchemaVersion, StringComparison.Ordinal) &&
                string.Equals(ImportBatchId, other.ImportBatchId, StringComparison.Ordinal);
        }

        internal void EnsureDrawingSourceCanBeScoped()
        {
            if (Transport != InteroperabilityTransport.Dwg && Transport != InteroperabilityTransport.Dxf)
                throw new InvalidOperationException("Drawing-source identity requires DWG or DXF transport.");
            if (SourceFingerprint == null)
                throw new InvalidOperationException(
                    "DWG/DXF source-local identity requires a source drawing fingerprint. " +
                    "External drawing handles remain provenance only and cannot claim target-native ownership.");
        }
    }

    public sealed class InteroperabilityElementIdentity
    {
        private InteroperabilityElementIdentity(
            InteroperabilitySourceProvenance provenance,
            string sourceElementId,
            string? qs3dElementId,
            string? dwgHandle,
            string? ifcGlobalId,
            string? externalAuthoringId)
        {
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            SourceElementId = InteroperabilityContract.RequireToken(sourceElementId, nameof(sourceElementId));
            Qs3dElementId = InteroperabilityContract.OptionalToken(qs3dElementId, nameof(qs3dElementId));
            DwgHandle = InteroperabilityContract.OptionalToken(dwgHandle, nameof(dwgHandle));
            IfcGlobalId = InteroperabilityContract.OptionalToken(ifcGlobalId, nameof(ifcGlobalId));
            ExternalAuthoringId = InteroperabilityContract.OptionalToken(externalAuthoringId, nameof(externalAuthoringId));

            if (DwgHandle != null)
                Provenance.EnsureDrawingSourceCanBeScoped();

            SourceIdentityKey = InteroperabilityContract.BuildScopeKey(
                Provenance.ScopeKey,
                SourceElementId);
        }

        public InteroperabilitySourceProvenance Provenance { get; }
        public string SourceElementId { get; }
        public string? Qs3dElementId { get; }
        public string? DwgHandle { get; }
        public string? IfcGlobalId { get; }
        public string? ExternalAuthoringId { get; }
        public string SourceIdentityKey { get; }

        // Exchange identities are always source provenance. They never establish ownership
        // of an ObjectId/handle in the active target DWG.
        public bool CanClaimTargetNativeOwnership => false;

        public static InteroperabilityElementIdentity ForDrawingSource(
            InteroperabilitySourceProvenance provenance,
            string sourceHandle,
            string? qs3dElementId = null)
        {
            if (provenance == null) throw new ArgumentNullException(nameof(provenance));
            provenance.EnsureDrawingSourceCanBeScoped();
            var handle = InteroperabilityContract.RequireToken(sourceHandle, nameof(sourceHandle));
            return new InteroperabilityElementIdentity(
                provenance,
                handle,
                qs3dElementId,
                handle,
                null,
                null);
        }

        public static InteroperabilityElementIdentity ForIfc(
            InteroperabilitySourceProvenance provenance,
            string ifcGlobalId,
            string? qs3dElementId)
        {
            if (provenance == null) throw new ArgumentNullException(nameof(provenance));
            if (provenance.Transport != InteroperabilityTransport.Ifc &&
                provenance.Transport != InteroperabilityTransport.NeutralSnapshot)
                throw new InvalidOperationException("IFC identity requires IFC or neutral-snapshot transport.");
            var globalId = InteroperabilityContract.RequireToken(ifcGlobalId, nameof(ifcGlobalId));
            return new InteroperabilityElementIdentity(
                provenance,
                globalId,
                qs3dElementId,
                null,
                globalId,
                null);
        }

        public static InteroperabilityElementIdentity ForExternalAuthoring(
            InteroperabilitySourceProvenance provenance,
            string externalAuthoringId,
            string? qs3dElementId = null)
        {
            if (provenance == null) throw new ArgumentNullException(nameof(provenance));
            var externalId = InteroperabilityContract.RequireToken(externalAuthoringId, nameof(externalAuthoringId));
            return new InteroperabilityElementIdentity(
                provenance,
                externalId,
                qs3dElementId,
                null,
                null,
                externalId);
        }
    }

    public sealed class InteroperabilityPropertyFact
    {
        public InteroperabilityPropertyFact(
            string propertyNamespace,
            string setName,
            string name,
            string value,
            InteroperabilityPropertyValueKind valueKind,
            string? unit = null,
            bool isMeasured = false)
        {
            PropertyNamespace = InteroperabilityContract.RequireToken(propertyNamespace, nameof(propertyNamespace));
            SetName = InteroperabilityContract.RequireToken(setName, nameof(setName));
            Name = InteroperabilityContract.RequireToken(name, nameof(name));
            var canonicalValue = InteroperabilityContract.RequireToken(value, nameof(value));
            ValueKind = InteroperabilityContract.RequireDefined(valueKind, nameof(valueKind));
            Unit = InteroperabilityContract.OptionalToken(unit, nameof(unit));
            IsMeasured = isMeasured;

            if (IsMeasured && ValueKind != InteroperabilityPropertyValueKind.Number)
                throw new ArgumentException(
                    "Measured interoperability property must use Number value kind.",
                    nameof(isMeasured));

            if (ValueKind == InteroperabilityPropertyValueKind.Number)
            {
                if (!double.TryParse(canonicalValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric) ||
                    double.IsNaN(numeric) ||
                    double.IsInfinity(numeric))
                    throw new ArgumentException("Numeric interoperability property must contain a finite invariant number.", nameof(value));

                var roundTrip = numeric.ToString("R", CultureInfo.InvariantCulture);
                if (!string.Equals(canonicalValue, roundTrip, StringComparison.Ordinal))
                    throw new ArgumentException(
                        "Numeric interoperability property must use the canonical finite invariant round-trip form.",
                        nameof(value));
            }
            else if (ValueKind == InteroperabilityPropertyValueKind.Boolean)
            {
                if (!bool.TryParse(canonicalValue, out var booleanValue))
                    throw new ArgumentException("Boolean interoperability property must contain true or false.", nameof(value));
                canonicalValue = booleanValue ? "true" : "false";
            }

            Value = canonicalValue;
        }

        public string PropertyNamespace { get; }
        public string SetName { get; }
        public string Name { get; }
        public string Value { get; }
        public InteroperabilityPropertyValueKind ValueKind { get; }
        public string? Unit { get; }
        public bool IsMeasured { get; }

        public static InteroperabilityPropertyFact Number(
            string propertyNamespace,
            string setName,
            string name,
            double value,
            string? unit,
            bool isMeasured = true)
        {
            InteroperabilityContract.RequireFinite(value, nameof(value));
            return new InteroperabilityPropertyFact(
                propertyNamespace,
                setName,
                name,
                value.ToString("R", CultureInfo.InvariantCulture),
                InteroperabilityPropertyValueKind.Number,
                unit,
                isMeasured);
        }
    }

    public sealed class InteroperabilityClassificationReference
    {
        public InteroperabilityClassificationReference(
            string system,
            string code,
            string? name = null,
            string? edition = null)
        {
            System = InteroperabilityContract.RequireToken(system, nameof(system));
            Code = InteroperabilityContract.RequireToken(code, nameof(code));
            Name = InteroperabilityContract.OptionalToken(name, nameof(name));
            Edition = InteroperabilityContract.OptionalToken(edition, nameof(edition));
        }

        public string System { get; }
        public string Code { get; }
        public string? Name { get; }
        public string? Edition { get; }
    }

    public sealed class InteroperabilityQuantityFact
    {
        public InteroperabilityQuantityFact(
            string name,
            double value,
            string? unit,
            InteroperabilityQuantityOrigin origin,
            string? sourceIdentity = null,
            string? provenanceIdentity = null,
            string? methodOfMeasurement = null,
            string? calculationRuleId = null)
        {
            Name = InteroperabilityContract.RequireToken(name, nameof(name));
            Value = InteroperabilityContract.RequireFinite(value, nameof(value));
            Unit = InteroperabilityContract.OptionalToken(unit, nameof(unit));
            Origin = InteroperabilityContract.RequireDefined(origin, nameof(origin));
            SourceIdentity = InteroperabilityContract.OptionalToken(sourceIdentity, nameof(sourceIdentity));
            ProvenanceIdentity = InteroperabilityContract.OptionalToken(provenanceIdentity, nameof(provenanceIdentity));
            MethodOfMeasurement = InteroperabilityContract.OptionalToken(methodOfMeasurement, nameof(methodOfMeasurement));
            CalculationRuleId = InteroperabilityContract.OptionalToken(calculationRuleId, nameof(calculationRuleId));

            if (Origin == InteroperabilityQuantityOrigin.DeclaredSource &&
                (SourceIdentity == null || ProvenanceIdentity == null))
                throw new ArgumentException(
                    "Source-declared quantities require source and provenance identities.");

            if (Origin == InteroperabilityQuantityOrigin.DerivedQs3d && CalculationRuleId == null)
                throw new ArgumentException(
                    "QS3D-derived quantities require a calculation rule identity.");
        }

        public string Name { get; }
        public double Value { get; }
        public string? Unit { get; }
        public InteroperabilityQuantityOrigin Origin { get; }
        public string? SourceIdentity { get; }
        public string? ProvenanceIdentity { get; }
        public string? MethodOfMeasurement { get; }
        public string? CalculationRuleId { get; }
    }

    public sealed class InteroperabilityLossDiagnostic
    {
        public InteroperabilityLossDiagnostic(
            string code,
            InteroperabilityDiagnosticSeverity severity,
            string message,
            string? sourceElementId = null,
            string? sourcePath = null)
        {
            Code = InteroperabilityContract.RequireToken(code, nameof(code));
            Severity = InteroperabilityContract.RequireDefined(severity, nameof(severity));
            Message = InteroperabilityContract.RequireToken(message, nameof(message));
            SourceElementId = InteroperabilityContract.OptionalToken(sourceElementId, nameof(sourceElementId));
            SourcePath = InteroperabilityContract.OptionalToken(sourcePath, nameof(sourcePath));
        }

        public string Code { get; }
        public InteroperabilityDiagnosticSeverity Severity { get; }
        public string Message { get; }
        public string? SourceElementId { get; }
        public string? SourcePath { get; }
    }

    public sealed class InteroperabilityElementRecord
    {
        public const int MaxNestedItems = 10000;

        public InteroperabilityElementRecord(
            InteroperabilityElementIdentity identity,
            IEnumerable<InteroperabilityPropertyFact>? properties,
            IEnumerable<InteroperabilityClassificationReference>? classifications,
            IEnumerable<InteroperabilityQuantityFact>? quantities,
            IEnumerable<string>? provenanceTokens,
            IEnumerable<InteroperabilityLossDiagnostic>? diagnostics = null)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Properties = CanonicalizeProperties(SnapshotBounded(properties, nameof(properties)));
            Classifications = CanonicalizeClassifications(SnapshotBounded(classifications, nameof(classifications)));
            Quantities = CanonicalizeQuantities(SnapshotBounded(quantities, nameof(quantities)));
            ProvenanceTokens = CanonicalizeTokens(SnapshotBounded(provenanceTokens, nameof(provenanceTokens)), nameof(provenanceTokens));
            Diagnostics = InteroperabilityDiagnosticOrder.Canonicalize(SnapshotBounded(diagnostics, nameof(diagnostics)));
        }

        public InteroperabilityElementIdentity Identity { get; }
        public IReadOnlyList<InteroperabilityPropertyFact> Properties { get; }
        public IReadOnlyList<InteroperabilityClassificationReference> Classifications { get; }
        public IReadOnlyList<InteroperabilityQuantityFact> Quantities { get; }
        public IReadOnlyList<string> ProvenanceTokens { get; }
        public IReadOnlyList<InteroperabilityLossDiagnostic> Diagnostics { get; }

        private static IReadOnlyList<InteroperabilityPropertyFact> CanonicalizeProperties(
            IEnumerable<InteroperabilityPropertyFact>? values)
        {
            var items = Materialize(values);
            items.Sort((left, right) =>
            {
                var byNamespace = StringComparer.Ordinal.Compare(left.PropertyNamespace, right.PropertyNamespace);
                if (byNamespace != 0) return byNamespace;
                var bySet = StringComparer.Ordinal.Compare(left.SetName, right.SetName);
                if (bySet != 0) return bySet;
                var byName = StringComparer.Ordinal.Compare(left.Name, right.Name);
                if (byName != 0) return byName;
                var byUnit = StringComparer.Ordinal.Compare(left.Unit ?? string.Empty, right.Unit ?? string.Empty);
                if (byUnit != 0) return byUnit;
                return StringComparer.Ordinal.Compare(left.Value, right.Value);
            });
            return Array.AsReadOnly(items.ToArray());
        }

        private static IReadOnlyList<InteroperabilityClassificationReference> CanonicalizeClassifications(
            IEnumerable<InteroperabilityClassificationReference>? values)
        {
            var items = Materialize(values);
            items.Sort((left, right) =>
            {
                var bySystem = StringComparer.Ordinal.Compare(left.System, right.System);
                if (bySystem != 0) return bySystem;
                return StringComparer.Ordinal.Compare(left.Code, right.Code);
            });
            return Array.AsReadOnly(items.ToArray());
        }

        private static IReadOnlyList<InteroperabilityQuantityFact> CanonicalizeQuantities(
            IEnumerable<InteroperabilityQuantityFact>? values)
        {
            var items = Materialize(values);
            items.Sort((left, right) =>
            {
                var byName = StringComparer.Ordinal.Compare(left.Name, right.Name);
                if (byName != 0) return byName;
                var byOrigin = left.Origin.CompareTo(right.Origin);
                if (byOrigin != 0) return byOrigin;
                var bySource = StringComparer.Ordinal.Compare(left.SourceIdentity ?? string.Empty, right.SourceIdentity ?? string.Empty);
                if (bySource != 0) return bySource;
                var byUnit = StringComparer.Ordinal.Compare(left.Unit ?? string.Empty, right.Unit ?? string.Empty);
                if (byUnit != 0) return byUnit;
                return left.Value.CompareTo(right.Value);
            });
            return Array.AsReadOnly(items.ToArray());
        }

        private static List<T> Materialize<T>(IEnumerable<T>? source) where T : class
        {
            var items = new List<T>();
            if (source == null) return items;
            foreach (var item in source)
            {
                if (item == null)
                    throw new ArgumentException("Interoperability fact collection cannot contain null entries.", nameof(source));
                items.Add(item);
            }
            return items;
        }

        private static IReadOnlyList<string> CanonicalizeTokens(IEnumerable<string>? values, string parameterName)
        {
            if (values == null) return Array.Empty<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var items = new List<string>();
            foreach (var raw in values)
            {
                var token = InteroperabilityContract.RequireToken(raw, parameterName);
                if (seen.Add(token)) items.Add(token);
            }
            items.Sort(StringComparer.Ordinal);
            return Array.AsReadOnly(items.ToArray());
        }

        private static IReadOnlyList<T> SnapshotBounded<T>(IEnumerable<T>? source, string parameterName)
        {
            if (source == null) return Array.Empty<T>();

            var label = "Interoperability element " + parameterName;
            var knownCount = InteroperabilityCollectionContract.ValidateKnownCount(source, MaxNestedItems, label);
            var items = knownCount.HasValue ? new List<T>(knownCount.Value) : new List<T>();
            var observedCount = 0;
            foreach (var item in source)
            {
                observedCount++;
                if (observedCount > MaxNestedItems)
                    throw new InvalidOperationException(label + " cannot exceed " + MaxNestedItems + " items.");
                items.Add(item);
            }

            InteroperabilityCollectionContract.ValidateCompletedTraversal(
                source,
                MaxNestedItems,
                label,
                knownCount,
                observedCount);
            return Array.AsReadOnly(items.ToArray());
        }
    }

    public sealed class InteroperabilityFactSet
    {
        public const int MaxRecords = 10000;

        private InteroperabilityFactSet(
            InteroperabilitySourceProvenance provenance,
            IReadOnlyList<InteroperabilityElementRecord> records)
        {
            Provenance = provenance;
            Records = records;
        }

        public InteroperabilitySourceProvenance Provenance { get; }
        public IReadOnlyList<InteroperabilityElementRecord> Records { get; }

        public static InteroperabilityFactSet Create(
            InteroperabilitySourceProvenance provenance,
            IEnumerable<InteroperabilityElementRecord> records)
        {
            if (provenance == null) throw new ArgumentNullException(nameof(provenance));
            if (records == null) throw new ArgumentNullException(nameof(records));

            const string label = "Interoperability fact set records";
            var knownCount = InteroperabilityCollectionContract.ValidateKnownCount(records, MaxRecords, label);
            var items = knownCount.HasValue
                ? new List<InteroperabilityElementRecord>(knownCount.Value)
                : new List<InteroperabilityElementRecord>();
            foreach (var record in records)
            {
                if (items.Count == MaxRecords)
                    throw new InvalidOperationException("Interoperability fact set cannot exceed " + MaxRecords + " records.");
                if (record == null)
                    throw new ArgumentException("Interoperability fact set cannot contain null records.", nameof(records));
                if (!record.Identity.Provenance.MatchesFactSetProvenance(provenance))
                    throw new InvalidOperationException("Interoperability record provenance does not match the fact-set source revision.");
                items.Add(record);
            }

            InteroperabilityCollectionContract.ValidateCompletedTraversal(
                records,
                MaxRecords,
                label,
                knownCount,
                items.Count);
            items.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.Identity.SourceIdentityKey, right.Identity.SourceIdentityKey));
            return new InteroperabilityFactSet(provenance, Array.AsReadOnly(items.ToArray()));
        }
    }

    public sealed class InteroperabilityAdmissionResult
    {
        internal InteroperabilityAdmissionResult(
            InteroperabilityFactSet factSet,
            IReadOnlyList<InteroperabilityLossDiagnostic> diagnostics)
        {
            FactSet = factSet;
            Diagnostics = diagnostics;
        }

        public InteroperabilityFactSet FactSet { get; }
        public IReadOnlyList<InteroperabilityLossDiagnostic> Diagnostics { get; }
        public bool IsAdmissible => !Diagnostics.Any(x => x.Severity == InteroperabilityDiagnosticSeverity.Blocking);

        public void ThrowIfBlocked()
        {
            var blockers = Diagnostics
                .Where(x => x.Severity == InteroperabilityDiagnosticSeverity.Blocking)
                .ToArray();
            if (blockers.Length == 0) return;

            throw new InvalidOperationException(
                "Interoperability admission blocked: " +
                string.Join("; ", blockers.Select(x => x.Code + ": " + x.Message)));
        }
    }

    public static class InteroperabilityAdmission
    {
        public const int MaxAdditionalDiagnostics = 10000;

        public static InteroperabilityAdmissionResult Evaluate(
            InteroperabilityFactSet factSet,
            IEnumerable<InteroperabilityLossDiagnostic>? additionalDiagnostics = null)
        {
            if (factSet == null) throw new ArgumentNullException(nameof(factSet));
            var diagnostics = new List<InteroperabilityLossDiagnostic>();
            if (additionalDiagnostics != null)
            {
                const string label = "Interoperability additional diagnostics";
                var knownCount = InteroperabilityCollectionContract.ValidateKnownCount(
                    additionalDiagnostics,
                    MaxAdditionalDiagnostics,
                    label);
                var additionalCount = 0;
                foreach (var diagnostic in additionalDiagnostics)
                {
                    additionalCount++;
                    if (additionalCount > MaxAdditionalDiagnostics)
                        throw new InvalidOperationException(
                            "Interoperability admission cannot exceed " + MaxAdditionalDiagnostics + " additional diagnostics.");
                    if (diagnostic == null)
                        throw new ArgumentException("Diagnostic collection cannot contain null entries.", nameof(additionalDiagnostics));
                    diagnostics.Add(diagnostic);
                }
                InteroperabilityCollectionContract.ValidateCompletedTraversal(
                    additionalDiagnostics,
                    MaxAdditionalDiagnostics,
                    label,
                    knownCount,
                    additionalCount);
            }

            var seenSourceIdentities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var record in factSet.Records)
            {
                diagnostics.AddRange(record.Diagnostics);

                if (!seenSourceIdentities.Add(record.Identity.SourceIdentityKey))
                {
                    diagnostics.Add(new InteroperabilityLossDiagnostic(
                        "DUPLICATE_SOURCE_IDENTITY",
                        InteroperabilityDiagnosticSeverity.Blocking,
                        "Multiple normalized records use the same source-scoped element identity.",
                        record.Identity.SourceElementId));
                }

                if (record.Identity.DwgHandle != null && !record.Identity.Provenance.HasSourceFingerprint)
                {
                    diagnostics.Add(new InteroperabilityLossDiagnostic(
                        "DRAWING_FINGERPRINT_REQUIRED",
                        InteroperabilityDiagnosticSeverity.Blocking,
                        "DWG/DXF source-local handles require a source drawing fingerprint.",
                        record.Identity.SourceElementId));
                }

                foreach (var property in record.Properties)
                {
                    if (property.IsMeasured && property.Unit == null)
                    {
                        diagnostics.Add(new InteroperabilityLossDiagnostic(
                            "MEASURED_PROPERTY_UNIT_UNRESOLVED",
                            InteroperabilityDiagnosticSeverity.Blocking,
                            "Measured property " + property.SetName + "." + property.Name + " has no resolvable unit.",
                            record.Identity.SourceElementId,
                            property.PropertyNamespace + "/" + property.SetName + "/" + property.Name));
                    }
                }

                foreach (var quantity in record.Quantities)
                {
                    if (quantity.Unit == null)
                    {
                        diagnostics.Add(new InteroperabilityLossDiagnostic(
                            "QUANTITY_UNIT_UNRESOLVED",
                            InteroperabilityDiagnosticSeverity.Blocking,
                            "Quantity " + quantity.Name + " has no resolvable unit.",
                            record.Identity.SourceElementId,
                            quantity.Name));
                    }
                }
            }

            return new InteroperabilityAdmissionResult(
                factSet,
                InteroperabilityDiagnosticOrder.Canonicalize(diagnostics));
        }
    }

    internal static class InteroperabilityDiagnosticOrder
    {
        internal static IReadOnlyList<InteroperabilityLossDiagnostic> Canonicalize(
            IEnumerable<InteroperabilityLossDiagnostic>? source)
        {
            var items = new List<InteroperabilityLossDiagnostic>();
            if (source != null)
            {
                foreach (var item in source)
                {
                    if (item == null)
                        throw new ArgumentException("Diagnostic collection cannot contain null entries.", nameof(source));
                    items.Add(item);
                }
            }

            items.Sort((left, right) =>
            {
                var bySeverity = right.Severity.CompareTo(left.Severity);
                if (bySeverity != 0) return bySeverity;
                var byCode = StringComparer.Ordinal.Compare(left.Code, right.Code);
                if (byCode != 0) return byCode;
                var bySource = StringComparer.Ordinal.Compare(left.SourceElementId ?? string.Empty, right.SourceElementId ?? string.Empty);
                if (bySource != 0) return bySource;
                return StringComparer.Ordinal.Compare(left.Message, right.Message);
            });
            return Array.AsReadOnly(items.ToArray());
        }
    }

    internal static class InteroperabilityCollectionContract
    {
        internal static int? ValidateKnownCount<T>(IEnumerable<T> source, int maximum, string label)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maximum < 0) throw new ArgumentOutOfRangeException(nameof(maximum));

            int? knownCount = null;
            Observe(source is ICollection<T> collection ? collection.Count : (int?)null, maximum, label, ref knownCount);
            Observe(source is IReadOnlyCollection<T> readOnlyCollection ? readOnlyCollection.Count : (int?)null, maximum, label, ref knownCount);
            Observe(source is System.Collections.ICollection nonGenericCollection ? nonGenericCollection.Count : (int?)null, maximum, label, ref knownCount);
            return knownCount;
        }

        internal static void ValidateCompletedTraversal<T>(
            IEnumerable<T> source,
            int maximum,
            string label,
            int? initialKnownCount,
            int observedCount)
        {
            var finalKnownCount = ValidateKnownCount(source, maximum, label);
            if (initialKnownCount.HasValue != finalKnownCount.HasValue ||
                (initialKnownCount.HasValue && finalKnownCount!.Value != initialKnownCount.Value) ||
                (initialKnownCount.HasValue && observedCount != initialKnownCount.Value))
            {
                throw new InvalidOperationException(
                    label + " Count contract changed or did not match completed traversal cardinality.");
            }
        }

        private static void Observe(int? count, int maximum, string label, ref int? knownCount)
        {
            if (!count.HasValue) return;
            if (count.Value < 0)
                throw new InvalidOperationException(label + " exposes an invalid negative Count.");
            if (count.Value > maximum)
                throw new InvalidOperationException(label + " cannot exceed " + maximum + " items.");
            if (knownCount.HasValue && knownCount.Value != count.Value)
                throw new InvalidOperationException(label + " exposes conflicting Count contracts.");
            knownCount = count.Value;
        }
    }

    internal static class InteroperabilityContract
    {
        internal static T RequireDefined<T>(T value, string parameterName) where T : struct
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new ArgumentOutOfRangeException(parameterName, value, "Interoperability enum value must be defined.");
            return value;
        }

        internal static string RequireToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A non-empty canonical token is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Token must not contain surrounding whitespace.", parameterName);

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsControl(character))
                    throw new ArgumentException("Token must not contain control characters.", parameterName);

                if (char.IsHighSurrogate(character))
                {
                    if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                        throw new ArgumentException("Token must contain well-formed UTF-16.", parameterName);
                    index++;
                    continue;
                }

                if (char.IsLowSurrogate(character))
                    throw new ArgumentException("Token must contain well-formed UTF-16.", parameterName);
            }

            return value;
        }

        internal static string? OptionalToken(string? value, string parameterName)
        {
            return value == null ? null : RequireToken(value, parameterName);
        }

        internal static double RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, "Numeric interoperability value must be finite.");
            return value;
        }

        internal static string BuildScopeKey(params string?[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            return string.Concat(values.Select(value =>
            {
                var token = value ?? string.Empty;
                return token.Length.ToString(CultureInfo.InvariantCulture) + ":" + token;
            }));
        }
    }
}