using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Coordination;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Stateful changed-only coordination recheck over live semantic/native snapshots.
    /// Stable semantic ElementId is the spatial identity. Handles/ObjectIds are current-runtime
    /// evidence only and are never used as the durable item or pair identity.
    /// </summary>
    public sealed class CoordinationIncrementalCommands
    {
        private const int MaxLiveSolidComponents = 5000;
        private const int MaxAllowedNativeHandlePairs = 100000;
        private static readonly Dictionary<string, RuntimeState> States =
            new Dictionary<string, RuntimeState>(StringComparer.OrdinalIgnoreCase);

        [CommandMethod("QS3DCOORDINATIONRECHECKCHANGED", CommandFlags.Modal)]
        public void RecheckChanged()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Coordination changed-only recheck cần một QS3D project hiện hữu.");
                var fingerprint = (project.DrawingFingerprint ?? string.Empty).Trim();
                if (fingerprint.Length == 0)
                    throw new InvalidOperationException("QS3D project chưa có Drawing Fingerprint để cô lập incremental cache theo DWG.");

                var snapshot = BuildLiveSnapshot(document, project);
                if (!States.TryGetValue(fingerprint, out var state))
                {
                    state = new RuntimeState();
                    States.Add(fingerprint, state);
                }

                var result = state.Controller.ApplySnapshot(snapshot.CellSize, snapshot.Items);
                foreach (var pairKey in result.InvalidatedPairKeys)
                    state.ExactPairKeys.Remove(pairKey);

                var exactRechecked = 0;
                var narrowPhasePairs = 0;
                var skipped = 0;
                if (result.CandidatePairs.Count > 0)
                {
                    var candidateKeys = new HashSet<string>(
                        result.CandidatePairs.Select(pair => pair.PairKey),
                        StringComparer.Ordinal);
                    state.ExactPairKeys.ExceptWith(candidateKeys);

                    var handlesByElement = snapshot.BindingByHandle
                        .GroupBy(pair => pair.Value.ElementId, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(pair => pair.Key).OrderBy(handle => handle, StringComparer.Ordinal).ToArray(),
                            StringComparer.OrdinalIgnoreCase);
                    var involvedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var allowedHandlePairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var pair in result.CandidatePairs)
                    {
                        if (!handlesByElement.TryGetValue(pair.LeftId, out var leftHandles) ||
                            !handlesByElement.TryGetValue(pair.RightId, out var rightHandles))
                            throw new InvalidOperationException(
                                "Changed-only candidate không còn đủ live semantic handle evidence: " + pair.PairKey + ".");

                        foreach (var leftHandle in leftHandles)
                        {
                            involvedHandles.Add(leftHandle);
                            foreach (var rightHandle in rightHandles)
                            {
                                involvedHandles.Add(rightHandle);
                                var handlePairKey = MepExactClashCommands.BuildHandlePairKey(leftHandle, rightHandle);
                                if (allowedHandlePairs.Add(handlePairKey) &&
                                    allowedHandlePairs.Count > MaxAllowedNativeHandlePairs)
                                    throw new InvalidOperationException(
                                        "Coordination changed-only vượt " + MaxAllowedNativeHandlePairs +
                                        " allowed native pair; hãy partition coordination scope trước khi scan.");
                            }
                        }
                    }

                    var handles = involvedHandles.OrderBy(handle => handle, StringComparer.Ordinal).ToArray();
                    var ids = CadHandleService.Resolve(document, handles);
                    var clashes = MepExactClashCommands.DetectExact(
                        document,
                        ids,
                        snapshot.SnapshotByHandle,
                        out _,
                        out skipped,
                        out narrowPhasePairs,
                        allowedHandlePairs);

                    var exactThisPass = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var clash in clashes)
                    {
                        var leftHandle = NormalizeHandle(clash.LeftHandle);
                        var rightHandle = NormalizeHandle(clash.RightHandle);
                        if (!snapshot.BindingByHandle.TryGetValue(leftHandle, out var left) ||
                            !snapshot.BindingByHandle.TryGetValue(rightHandle, out var right) ||
                            string.Equals(left.ElementId, right.ElementId, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var pairKey = PairKey(left.ElementId, right.ElementId);
                        if (candidateKeys.Contains(pairKey)) exactThisPass.Add(pairKey);
                    }

                    exactRechecked = exactThisPass.Count;
                    state.ExactPairKeys.UnionWith(exactThisPass);
                }

                var status = "Coordination changed-only: items=" + result.SnapshotItemCount +
                             " • dirty=" + result.Delta.AllDirtyIds.Count +
                             " • invalidated=" + result.InvalidatedPairKeys.Count +
                             " • candidates=" + result.CandidatePairs.Count +
                             " • native-pairs=" + narrowPhasePairs +
                             " • exact-rechecked=" + exactRechecked +
                             " • exact-cache=" + state.ExactPairKeys.Count +
                             " • skipped=" + skipped +
                             (result.IsNoOp ? " • no-op" : result.RequiresFullRescan ? " • full" : " • incremental");
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + ".");
            }
            catch (Exception error)
            {
                Report(document, "QS3DCOORDINATIONRECHECKCHANGED", error);
            }
        }

        [CommandMethod("QS3DCOORDINATIONRESETCACHE", CommandFlags.Modal)]
        public void ResetCurrentDrawingCache()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Coordination cache reset cần một QS3D project hiện hữu.");
                var fingerprint = (project.DrawingFingerprint ?? string.Empty).Trim();
                if (fingerprint.Length == 0)
                    throw new InvalidOperationException("QS3D project chưa có Drawing Fingerprint.");
                States.Remove(fingerprint);
                var status = "Coordination incremental cache đã reset cho active drawing";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + ".");
            }
            catch (Exception error)
            {
                Report(document, "QS3DCOORDINATIONRESETCACHE", error);
            }
        }

        private static HostSnapshot BuildLiveSnapshot(Document document, ProjectState project)
        {
            var bindingByHandle = BuildBindings(project);
            var snapshots = EntitySnapshotReader.ReadHandles(document, bindingByHandle.Keys);
            var snapshotByHandle = MepExactClashCommands.BuildSnapshotIndex(snapshots);
            var ids = CadHandleService.Resolve(document, snapshotByHandle.Keys);
            if (ids.Count > MaxLiveSolidComponents)
                throw new InvalidOperationException(
                    "Coordination incremental recheck giới hạn " + MaxLiveSolidComponents +
                    " live Solid3d components; hãy partition coordination scope trước khi scan.");

            var aggregateByElement = new Dictionary<string, ElementAggregate>(StringComparer.OrdinalIgnoreCase);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                    if (solid == null || solid.IsErased) continue;
                    var handle = NormalizeHandle(solid.Handle.ToString());
                    if (handle.Length == 0 ||
                        !bindingByHandle.TryGetValue(handle, out var binding) ||
                        !snapshotByHandle.TryGetValue(handle, out var entitySnapshot))
                        continue;

                    Extents3d extents;
                    try { extents = solid.GeometricExtents; }
                    catch { continue; }
                    if (!HasFiniteExtents(extents)) continue;
                    if (!entitySnapshot.SurfaceAreaDrawingUnitsSquared.HasValue ||
                        !entitySnapshot.VolumeDrawingUnitsCubed.HasValue)
                        throw new InvalidOperationException(
                            "Coordination changed-only không thể fingerprint đầy đủ Solid3d " + handle +
                            "; surface-area/volume metric không khả dụng nên cache exact-clash không được tái sử dụng.");

                    if (!aggregateByElement.TryGetValue(binding.ElementId, out var aggregate))
                    {
                        aggregate = new ElementAggregate(binding);
                        aggregateByElement.Add(binding.ElementId, aggregate);
                    }
                    aggregate.Add(
                        handle,
                        entitySnapshot.Layer,
                        entitySnapshot.SurfaceAreaDrawingUnitsSquared.Value,
                        entitySnapshot.VolumeDrawingUnitsCubed.Value,
                        extents);
                }
                transaction.Commit();
            }

            var items = aggregateByElement.Values
                .OrderBy(aggregate => aggregate.Binding.ElementId, StringComparer.Ordinal)
                .Select(aggregate => aggregate.ToSpatialItem())
                .ToArray();
            var cellSize = ChooseCellSize(items);
            return new HostSnapshot(
                items,
                cellSize,
                bindingByHandle,
                snapshotByHandle);
        }

        private static Dictionary<string, SemanticBinding> BuildBindings(ProjectState project)
        {
            var result = new Dictionary<string, SemanticBinding>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                var floorId = (element.FloorId ?? string.Empty).Trim();
                var floor = floorId.Length == 0 ? null : project.FindFloor(floorId);
                var binding = new SemanticBinding(
                    element.Id,
                    element.Category.ToString(),
                    floor == null ? floorId : floor.Name);

                foreach (var rawHandle in SemanticReferenceHandles.GetSelectionAliases(element))
                {
                    var handle = NormalizeHandle(rawHandle);
                    if (handle.Length == 0) continue;
                    if (result.TryGetValue(handle, out var existing) &&
                        !string.Equals(existing.ElementId, binding.ElementId, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            "Semantic Handle " + handle + " map tới nhiều element; incremental coordination fail-closed.");
                    result[handle] = binding;
                }
            }
            return result;
        }

        private static double ChooseCellSize(IReadOnlyList<CoordinationSpatialItem> items)
        {
            if (items.Count == 0) return 1d;
            var spans = items.Select(item => Math.Max(
                    item.Bounds.MaxX - item.Bounds.MinX,
                    Math.Max(item.Bounds.MaxY - item.Bounds.MinY, item.Bounds.MaxZ - item.Bounds.MinZ)))
                .Where(span => span > 0d && IsFinite(span))
                .OrderBy(span => span)
                .ToArray();
            if (spans.Length == 0) return 1d;

            var median = spans[spans.Length / 2];
            var exponent = Math.Ceiling(Math.Log(median, 2d));
            var cell = Math.Pow(2d, exponent);
            return IsFinite(cell) && cell > 0d ? cell : 1d;
        }

        private static string PairKey(string first, string second)
        {
            return StringComparer.Ordinal.Compare(first, second) <= 0
                ? first + "\u001f" + second
                : second + "\u001f" + first;
        }

        private static string NormalizeHandle(string value)
        {
            return (CadHandleService.NormalizeHexHandle(value) ?? string.Empty).Trim();
        }

        private static bool HasFiniteExtents(Extents3d extents)
        {
            return IsFinite(extents.MinPoint.X) && IsFinite(extents.MinPoint.Y) && IsFinite(extents.MinPoint.Z) &&
                   IsFinite(extents.MaxPoint.X) && IsFinite(extents.MaxPoint.Y) && IsFinite(extents.MaxPoint.Z) &&
                   extents.MaxPoint.X >= extents.MinPoint.X &&
                   extents.MaxPoint.Y >= extents.MinPoint.Y &&
                   extents.MaxPoint.Z >= extents.MinPoint.Z;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static void Report(Document document, string operation, Exception error)
        {
            var message = error is AggregateException aggregate ? aggregate.GetBaseException().Message : error.Message;
            try { PaletteCoordinator.SetStatus(operation + ": " + message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + operation + ": " + message); } catch { }
        }

        private sealed class RuntimeState
        {
            internal CoordinationIncrementalScanController Controller { get; } = new CoordinationIncrementalScanController();
            internal HashSet<string> ExactPairKeys { get; } = new HashSet<string>(StringComparer.Ordinal);
        }

        private sealed class HostSnapshot
        {
            internal HostSnapshot(
                IReadOnlyList<CoordinationSpatialItem> items,
                double cellSize,
                IReadOnlyDictionary<string, SemanticBinding> bindingByHandle,
                IReadOnlyDictionary<string, EntitySnapshot> snapshotByHandle)
            {
                Items = items;
                CellSize = cellSize;
                BindingByHandle = bindingByHandle;
                SnapshotByHandle = snapshotByHandle;
            }

            internal IReadOnlyList<CoordinationSpatialItem> Items { get; }
            internal double CellSize { get; }
            internal IReadOnlyDictionary<string, SemanticBinding> BindingByHandle { get; }
            internal IReadOnlyDictionary<string, EntitySnapshot> SnapshotByHandle { get; }
        }

        private sealed class SemanticBinding
        {
            internal SemanticBinding(string elementId, string category, string floor)
            {
                ElementId = (elementId ?? string.Empty).Trim();
                Category = (category ?? string.Empty).Trim();
                Floor = (floor ?? string.Empty).Trim();
                if (ElementId.Length == 0) throw new InvalidOperationException("Coordination semantic ElementId is required.");
            }

            internal string ElementId { get; }
            internal string Category { get; }
            internal string Floor { get; }
        }

        private sealed class ElementAggregate
        {
            private readonly List<ComponentEvidence> _components = new List<ComponentEvidence>();
            private bool _hasBounds;
            private double _minX;
            private double _minY;
            private double _minZ;
            private double _maxX;
            private double _maxY;
            private double _maxZ;

            internal ElementAggregate(SemanticBinding binding)
            {
                Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            }

            internal SemanticBinding Binding { get; }

            internal void Add(
                string handle,
                string layer,
                double surfaceAreaDrawingUnitsSquared,
                double volumeDrawingUnitsCubed,
                Extents3d extents)
            {
                var evidence = new ComponentEvidence(
                    handle,
                    layer,
                    surfaceAreaDrawingUnitsSquared,
                    volumeDrawingUnitsCubed,
                    extents);
                _components.Add(evidence);
                if (!_hasBounds)
                {
                    _minX = extents.MinPoint.X; _minY = extents.MinPoint.Y; _minZ = extents.MinPoint.Z;
                    _maxX = extents.MaxPoint.X; _maxY = extents.MaxPoint.Y; _maxZ = extents.MaxPoint.Z;
                    _hasBounds = true;
                    return;
                }
                _minX = Math.Min(_minX, extents.MinPoint.X);
                _minY = Math.Min(_minY, extents.MinPoint.Y);
                _minZ = Math.Min(_minZ, extents.MinPoint.Z);
                _maxX = Math.Max(_maxX, extents.MaxPoint.X);
                _maxY = Math.Max(_maxY, extents.MaxPoint.Y);
                _maxZ = Math.Max(_maxZ, extents.MaxPoint.Z);
            }

            internal CoordinationSpatialItem ToSpatialItem()
            {
                if (!_hasBounds || _components.Count == 0)
                    throw new InvalidOperationException("Coordination aggregate has no live Solid3d evidence.");
                var bounds = new CoordinationBounds(_minX, _minY, _minZ, _maxX, _maxY, _maxZ);
                return new CoordinationSpatialItem(Binding.ElementId, BuildRevision(), bounds);
            }

            private string BuildRevision()
            {
                var text = new StringBuilder();
                text.Append("QS3D_COORD_LIVE_V2\n")
                    .Append(Binding.ElementId).Append('\n')
                    .Append(Binding.Category).Append('\n')
                    .Append(Binding.Floor).Append('\n');
                foreach (var component in _components.OrderBy(item => item.Handle, StringComparer.Ordinal))
                    component.AppendTo(text);

                byte[] hash;
                using (var sha = SHA256.Create())
                    hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
                var hex = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) hex.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return "LIVE:" + hex;
            }
        }

        private sealed class ComponentEvidence
        {
            internal ComponentEvidence(
                string handle,
                string layer,
                double surfaceAreaDrawingUnitsSquared,
                double volumeDrawingUnitsCubed,
                Extents3d extents)
            {
                Handle = handle;
                Layer = (layer ?? string.Empty).Trim();
                SurfaceAreaDrawingUnitsSquared = surfaceAreaDrawingUnitsSquared;
                VolumeDrawingUnitsCubed = volumeDrawingUnitsCubed;
                Extents = extents;
            }

            internal string Handle { get; }
            private string Layer { get; }
            private double SurfaceAreaDrawingUnitsSquared { get; }
            private double VolumeDrawingUnitsCubed { get; }
            private Extents3d Extents { get; }

            internal void AppendTo(StringBuilder text)
            {
                text.Append(Handle).Append('|').Append(Layer).Append('|')
                    .Append(R(SurfaceAreaDrawingUnitsSquared)).Append('|')
                    .Append(R(VolumeDrawingUnitsCubed)).Append('|')
                    .Append(R(Extents.MinPoint.X)).Append('|').Append(R(Extents.MinPoint.Y)).Append('|').Append(R(Extents.MinPoint.Z)).Append('|')
                    .Append(R(Extents.MaxPoint.X)).Append('|').Append(R(Extents.MaxPoint.Y)).Append('|').Append(R(Extents.MaxPoint.Z)).Append('\n');
            }

            private static string R(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
