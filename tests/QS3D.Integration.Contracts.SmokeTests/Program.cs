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
Require(typeof(ApiEnvelope<>).Assembly.GetReferencedAssemblies().All(x => !x.Name!.Contains("BricsCAD", StringComparison.OrdinalIgnoreCase)), "contracts must remain BricsCAD-independent");

Console.WriteLine("QS3D Integration Contracts smoke PASS");
return;

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
