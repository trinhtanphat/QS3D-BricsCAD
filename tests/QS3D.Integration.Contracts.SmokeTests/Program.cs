using QS3D.Integration.Contracts;

var generation = "gen-001";
var evidence = new EvidenceRef("source-1", "IFC", "#42", "rev-1", "sha256:abc", generation);
var quantity = new QuantityItem(
    "q-1", "component-1", "IfcWall", "Wall volume",
    new QuantityValue(12.5m, "m3"), new[] { evidence });
var publication = new QuantityPublication(
    new ProjectRef("project-1", "Sample", "rev-1"),
    new ModelRef("model-1", "project-1", "Sample.ifc", "IFC4"),
    new SnapshotRef("snapshot-1", "rev-1", "sha256:snapshot", generation),
    new[] { quantity }, Array.Empty<BoqItem>(), Array.Empty<ClassificationRef>(), Array.Empty<QaFinding>());
var envelope = ApiEnvelope<QuantityPublication>.V1(
    "request-1", DateTimeOffset.UnixEpoch,
    new AuthorizationContext("smoke", new[] { "quantity:read" }), publication);

Require(envelope.ContractVersion == IntegrationContractVersions.V1, "v1 contract identifier changed");
Require(envelope.Payload.Snapshot.GenerationId == generation, "snapshot generation was not preserved");
Require(envelope.Payload.Quantities.Single().Evidence.Single().GenerationId == generation, "quantity evidence generation was not preserved");
Require(envelope.Payload.Quantities.Single().Evidence.Single().SourceHandle == "#42", "source handle provenance was not preserved");

var external = new ExternalSystemRef("ERP", "vendor-100", "erp-project-1");
var supplier = new SupplierRef("supplier-1", "Supplier A", "active", external);
var commitment = new SubcontractCommitmentRef("commitment-1", "proc-1", supplier.SupplierId, 800m, "USD", "committed");
var purchaseOrder = new PurchaseOrderRef("po-1", "proc-1", supplier.SupplierId, 1000m, "USD", "issued", DateTimeOffset.UnixEpoch.AddDays(10), external);
var delivery = new DeliveryRef("delivery-1", purchaseOrder.PurchaseOrderId, "received", DateTimeOffset.UnixEpoch.AddDays(8), DateTimeOffset.UnixEpoch.AddDays(9), new[] { evidence });
var lifecycle = new ConstructionLifecyclePublication(
    publication.Project,
    publication.Snapshot,
    new[] { supplier },
    new[] { commitment },
    new[] { purchaseOrder },
    new[] { delivery },
    new[] { new ProcurementProgress("proc-1", 75m, 800m, 600m, "USD", DateTimeOffset.UnixEpoch) },
    new CostControlSnapshot("project-1", "snapshot-1", 1200m, 800m, 600m, 1100m, "USD", DateTimeOffset.UnixEpoch),
    new[] { new FieldProgressRef("progress-1", "project-1", "wp-1", 75m, DateTimeOffset.UnixEpoch, new[] { evidence }, new ExternalSystemRef("ProjectControl", "activity-42")) },
    new[] { new ProjectControlRef("control-1", "project-1", "schedule", "on-track", DateTimeOffset.UnixEpoch, new ExternalSystemRef("ProjectControl", "schedule-1")) });
var lifecycleEnvelope = ApiEnvelope<ConstructionLifecyclePublication>.V1(
    "request-2", DateTimeOffset.UnixEpoch,
    new AuthorizationContext("smoke", new[] { "procurement:read", "project-controls:read" }), lifecycle);

Require(lifecycleEnvelope.ContractVersion == IntegrationContractVersions.V1, "construction lifecycle must remain v1 additive");
Require(lifecycleEnvelope.Payload.Snapshot.GenerationId == generation, "construction lifecycle snapshot generation was not preserved");
Require(lifecycleEnvelope.Payload.Deliveries.Single().Evidence.Single().GenerationId == generation, "delivery evidence generation was not preserved");
Require(lifecycleEnvelope.Payload.PurchaseOrders.Single().SupplierId == lifecycleEnvelope.Payload.Suppliers.Single().SupplierId, "purchase order supplier correlation was not preserved");
Require(lifecycleEnvelope.Payload.CostControl.ActualAmount <= lifecycleEnvelope.Payload.CostControl.CommittedAmount, "smoke fixture actual cost exceeds commitment");
Require(lifecycleEnvelope.Payload.FieldProgress.Single().External?.System == "ProjectControl", "project-control correlation was not preserved");
Require(typeof(ApiEnvelope<>).Assembly.GetReferencedAssemblies().All(x => !x.Name!.Contains("BricsCAD", StringComparison.OrdinalIgnoreCase)), "contracts must remain BricsCAD-independent");

Console.WriteLine("QS3D Integration Contracts smoke PASS");
return;

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
