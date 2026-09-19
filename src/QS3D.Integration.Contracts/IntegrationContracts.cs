namespace QS3D.Integration.Contracts;

public static class IntegrationContractVersions
{
    public const string V1 = "qs3d.integration.v1";
}

public sealed record ApiEnvelope<T>(
    string ContractVersion,
    string RequestId,
    DateTimeOffset CapturedAtUtc,
    AuthorizationContext Authorization,
    T Payload)
{
    public static ApiEnvelope<T> V1(string requestId, DateTimeOffset capturedAtUtc, AuthorizationContext authorization, T payload) =>
        new(IntegrationContractVersions.V1, requestId, capturedAtUtc, authorization, payload);
}

public sealed record AuthorizationContext(
    string Subject,
    IReadOnlyList<string> Scopes,
    string? TenantId = null);

public sealed record EvidenceRef(
    string SourceId,
    string SourceKind,
    string? SourceHandle,
    string? RevisionId,
    string? Fingerprint,
    string? GenerationId);

public sealed record ProjectRef(string ProjectId, string Name, string RevisionId);
public sealed record ModelRef(string ModelId, string ProjectId, string Name, string? IfcSchema);
public sealed record SourceRef(string SourceId, string ModelId, string Kind, string Uri, string Fingerprint);

public sealed record QuantityValue(decimal Value, string Unit);

public sealed record QuantityItem(
    string QuantityId,
    string ComponentId,
    string ClassificationCode,
    string Description,
    QuantityValue Quantity,
    IReadOnlyList<EvidenceRef> Evidence);

public sealed record BoqItem(
    string BoqItemId,
    string Code,
    string Description,
    QuantityValue Quantity,
    decimal? UnitRate,
    string? Currency,
    IReadOnlyList<string> QuantityIds);

public sealed record EstimateRef(string EstimateId, string BoqItemId, decimal Quantity, decimal UnitRate, decimal Amount, string Currency);
public sealed record ClassificationRef(string System, string Code, string? Title);
public sealed record QaFinding(string FindingId, string Severity, string Message, IReadOnlyList<EvidenceRef> Evidence);
public sealed record RevisionRef(string RevisionId, string ParentRevisionId, DateTimeOffset CapturedAtUtc);
public sealed record SnapshotRef(string SnapshotId, string RevisionId, string Fingerprint, string GenerationId);
public sealed record DiffEntry(string EntityId, string ChangeKind, string? BeforeFingerprint, string? AfterFingerprint);
public sealed record TenderRef(string TenderId, string RevisionId, string Currency);
public sealed record ProcurementRef(string ProcurementId, string TenderId, string Status);

public sealed record ExternalSystemRef(string System, string ExternalId, string? ExternalProjectId = null);
public sealed record SupplierRef(string SupplierId, string Name, string Status, ExternalSystemRef? External = null);
public sealed record SubcontractCommitmentRef(
    string CommitmentId,
    string ProcurementId,
    string SupplierId,
    decimal CommittedAmount,
    string Currency,
    string Status,
    ExternalSystemRef? External = null);
public sealed record PurchaseOrderRef(
    string PurchaseOrderId,
    string ProcurementId,
    string SupplierId,
    decimal OrderedAmount,
    string Currency,
    string Status,
    DateTimeOffset? RequiredAtUtc,
    ExternalSystemRef? External = null);
public sealed record DeliveryRef(
    string DeliveryId,
    string PurchaseOrderId,
    string Status,
    DateTimeOffset? PlannedAtUtc,
    DateTimeOffset? ReceivedAtUtc,
    IReadOnlyList<EvidenceRef> Evidence);
public sealed record ProcurementProgress(
    string ProcurementId,
    decimal PercentComplete,
    decimal CommittedAmount,
    decimal ActualAmount,
    string Currency,
    DateTimeOffset CapturedAtUtc);
public sealed record CostControlSnapshot(
    string ProjectId,
    string SnapshotId,
    decimal BudgetAmount,
    decimal CommittedAmount,
    decimal ActualAmount,
    decimal ForecastAmount,
    string Currency,
    DateTimeOffset CapturedAtUtc);
public sealed record FieldProgressRef(
    string ProgressId,
    string ProjectId,
    string WorkPackageId,
    decimal PercentComplete,
    DateTimeOffset CapturedAtUtc,
    IReadOnlyList<EvidenceRef> Evidence,
    ExternalSystemRef? External = null);
public sealed record ProjectControlRef(
    string ControlId,
    string ProjectId,
    string ControlType,
    string Status,
    DateTimeOffset CapturedAtUtc,
    ExternalSystemRef? External = null);
public sealed record ConstructionLifecyclePublication(
    ProjectRef Project,
    SnapshotRef Snapshot,
    IReadOnlyList<SupplierRef> Suppliers,
    IReadOnlyList<SubcontractCommitmentRef> SubcontractCommitments,
    IReadOnlyList<PurchaseOrderRef> PurchaseOrders,
    IReadOnlyList<DeliveryRef> Deliveries,
    IReadOnlyList<ProcurementProgress> ProcurementProgress,
    CostControlSnapshot CostControl,
    IReadOnlyList<FieldProgressRef> FieldProgress,
    IReadOnlyList<ProjectControlRef> ProjectControls);

public sealed record QuantityPublication(
    ProjectRef Project,
    ModelRef Model,
    SnapshotRef Snapshot,
    IReadOnlyList<QuantityItem> Quantities,
    IReadOnlyList<BoqItem> Boq,
    IReadOnlyList<ClassificationRef> Classifications,
    IReadOnlyList<QaFinding> QaFindings);
