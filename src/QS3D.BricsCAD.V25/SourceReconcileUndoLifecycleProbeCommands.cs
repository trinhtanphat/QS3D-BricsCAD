using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-004 diagnostic. It isolates database/object Undo
    /// enrollment for an existing XData carrier and either an appended or an
    /// erased topology sentinel. It does not call Source Reconcile production
    /// code and cannot qualify LOCAL-004.
    /// </summary>
    public sealed class SourceReconcileUndoLifecycleProbeCommands
    {
        private const string ResultVariable = "QS3D_SOURCE_UNDO_MATRIX_RESULT";
        private const string NonceVariable = "QS3D_SOURCE_UNDO_MATRIX_NONCE";
        private const string VariantVariable = "QS3D_SOURCE_UNDO_MATRIX_VARIANT";
        private const string DrawingVariable = "QS3D_SOURCE_UNDO_MATRIX_DWG";
        private const string ResultFileName = "source-undo-lifecycle-result.txt";
        private const string Schema = "QS3D_SOURCE_UNDO_LIFECYCLE_V1";
        private const string Boundary = "LOCAL_004_DIAGNOSTIC_ONLY";
        private const string RegAppName = "QS3D_SR_UNDO_MATRIX";
        private const string MarkerVersion = "SRUL1";
        private const string BeforeToken = "BEFORE";
        private const string AfterToken = "AFTER";
        private static readonly object Sync = new object();
        private static MatrixState? _state;

        [CommandMethod("QS3DSRULPREPARE", CommandFlags.Modal)]
        public void Prepare()
        {
            Execute("PREPARE", () =>
            {
                var context = RequireContext();
                var sentinelId = ObjectId.Null;
                using (var transaction = context.Document.Database.TransactionManager.StartTransaction())
                {
                    EnsureRegApp(context.Document.Database, transaction);
                    var carrier = OpenCarrier(context.Document.Database, transaction, OpenMode.ForRead);
                    carrier.DisableUndoRecording(false);
                    carrier.UpgradeOpen();
                    WriteMarker(carrier, BeforeToken);

                    if (UsesEraseTopology(context.Variant))
                    {
                        var modelSpace = OpenModelSpace(context.Document.Database, transaction, OpenMode.ForWrite);
                        var sentinel = new DBPoint(Point3d.Origin);
                        sentinelId = modelSpace.AppendEntity(sentinel);
                        transaction.AddNewlyCreatedDBObject(sentinel, true);
                    }
                    transaction.Commit();
                }

                if (!string.Equals(ReadMarker(context.Document), BeforeToken, StringComparison.Ordinal))
                    throw new InvalidOperationException("LOCAL-004 Undo lifecycle baseline marker was not committed.");
                if (UsesEraseTopology(context.Variant) &&
                    !string.Equals(ClassifyTopology(context.Document, sentinelId), "PRESENT", StringComparison.Ordinal))
                    throw new InvalidOperationException("LOCAL-004 Undo lifecycle erase sentinel was not committed.");

                lock (Sync)
                {
                    _state = new MatrixState(context.Document, context.Nonce, context.Variant)
                    {
                        SentinelId = sentinelId,
                    };
                }
            });
        }

        [CommandMethod("QS3DSRULMUTATE", CommandFlags.Modal)]
        public void Mutate()
        {
            Execute("MUTATE", () =>
            {
                var context = RequireContext();
                var state = RequireState(context);
                var database = context.Document.Database;
                state.DatabaseRecordingAtEntry = BooleanClass(database.UndoRecording);

                if (context.Variant == MatrixVariant.DbEnableObject ||
                    context.Variant == MatrixVariant.DbEnableDbStartObject)
                {
                    database.DisableUndoRecording(false);
                    state.DatabaseRecordingAfterEnable = BooleanClass(database.UndoRecording);
                }

                if (context.Variant == MatrixVariant.DbStartObject ||
                    context.Variant == MatrixVariant.DbEnableDbStartObject)
                {
                    database.StartUndoRecord();
                    state.DatabaseRecordingAfterStart = BooleanClass(database.UndoRecording);
                }

                using (var transaction = database.TransactionManager.StartTransaction())
                {
                    EnsureRegApp(database, transaction);
                    var carrier = OpenCarrier(database, transaction, OpenMode.ForRead);
                    if (!string.Equals(ReadMarker(carrier), BeforeToken, StringComparison.Ordinal))
                        throw new InvalidOperationException("LOCAL-004 Undo lifecycle baseline marker drifted.");

                    carrier.DisableUndoRecording(false);
                    carrier.UpgradeOpen();
                    WriteMarker(carrier, AfterToken);

                    if (UsesEraseTopology(context.Variant))
                    {
                        var sentinel = transaction.GetObject(state.SentinelId, OpenMode.ForWrite, false) as Entity;
                        if (sentinel == null || sentinel.IsErased)
                            throw new InvalidOperationException("LOCAL-004 Undo lifecycle erase sentinel is unavailable.");
                        sentinel.Erase();
                    }
                    else
                    {
                        var modelSpace = OpenModelSpace(database, transaction, OpenMode.ForWrite);
                        var sentinel = new DBPoint(Point3d.Origin);
                        state.SentinelId = modelSpace.AppendEntity(sentinel);
                        transaction.AddNewlyCreatedDBObject(sentinel, true);
                    }
                    transaction.Commit();
                }

                var expectedTopology = UsesEraseTopology(context.Variant) ? "UNDONE" : "PRESENT";
                if (!string.Equals(ReadMarker(context.Document), AfterToken, StringComparison.Ordinal) ||
                    !string.Equals(ClassifyTopology(context.Document, state.SentinelId), expectedTopology, StringComparison.Ordinal))
                    throw new InvalidOperationException("LOCAL-004 Undo lifecycle mutation did not commit.");

                state.MutationCommitted = true;
            });
        }

        [CommandMethod("QS3DSRULINSPECT", CommandFlags.Modal)]
        public void InspectCommittedMutation()
        {
            Execute("INSPECT", () =>
            {
                var context = RequireContext();
                if (context.Variant != MatrixVariant.ObjectInspected)
                    throw new InvalidOperationException("LOCAL-004 Undo lifecycle inspection variant rejected.");
                var state = RequireState(context);
                if (!state.MutationCommitted)
                    throw new InvalidOperationException("LOCAL-004 Undo lifecycle mutation is missing before inspection.");
                if (!string.Equals(ReadMarker(context.Document), AfterToken, StringComparison.Ordinal) ||
                    !string.Equals(ClassifyTopology(context.Document, state.SentinelId), "UNDONE", StringComparison.Ordinal))
                    throw new InvalidOperationException("LOCAL-004 Undo lifecycle inspected mutation drifted.");
                state.InspectionCount = checked(state.InspectionCount + 1);
                if (state.InspectionCount > 2)
                    throw new InvalidOperationException("LOCAL-004 Undo lifecycle inspection count exceeded its exact bound.");
            });
        }

        [CommandMethod("QS3DSRULCHECKUNDO", CommandFlags.Modal)]
        public void CheckUndo()
        {
            Execute("CHECK_UNDO", () =>
            {
                var context = RequireContext();
                var state = RequireState(context);
                if (!state.MutationCommitted)
                    throw new InvalidOperationException("LOCAL-004 Undo lifecycle mutation is missing.");
                if (context.Variant == MatrixVariant.ObjectInspected && state.InspectionCount != 2)
                    throw new InvalidOperationException("LOCAL-004 Undo lifecycle inspection sequence is incomplete.");

                var existingClass = ClassifyMarker(ReadMarker(context.Document));
                var topologyClass = ClassifyTopology(context.Document, state.SentinelId);
                WriteNew(RequiredResultPath(), new[]
                {
                    "status=PASS",
                    "schema=" + Schema,
                    "qualification_boundary=" + Boundary,
                    "production_local004_qualified=false",
                    "nonce=" + context.Nonce,
                    "variant=" + VariantText(context.Variant),
                    "db_recording_entry=" + state.DatabaseRecordingAtEntry,
                    "db_recording_after_enable=" + state.DatabaseRecordingAfterEnable,
                    "db_recording_after_start=" + state.DatabaseRecordingAfterStart,
                    "existing_after_undo=" + existingClass,
                    "topology_after_undo=" + topologyClass,
                });
            });
        }

        private static void Execute(string phase, Action action)
        {
            try
            {
                action();
            }
            catch
            {
                TryWriteFailure(phase);
                throw;
            }
        }

        private static ProbeContext RequireContext()
        {
            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException("LOCAL-004 Undo lifecycle probe requires an active document.");
            var expectedDrawing = Path.GetFullPath(Environment.GetEnvironmentVariable(DrawingVariable) ?? string.Empty);
            var actualDrawing = Path.GetFullPath(document.Name ?? string.Empty);
            if (expectedDrawing.Length == 0 ||
                !string.Equals(expectedDrawing, actualDrawing, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("LOCAL-004 Undo lifecycle document rejected.");

            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            if (!Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("LOCAL-004 Undo lifecycle automation context rejected.");

            var variant = ParseVariant(Environment.GetEnvironmentVariable(VariantVariable));
            return new ProbeContext(document, nonce, variant);
        }

        private static MatrixState RequireState(ProbeContext context)
        {
            MatrixState? state;
            lock (Sync) state = _state;
            if (state == null ||
                !ReferenceEquals(state.Document, context.Document) ||
                !string.Equals(state.Nonce, context.Nonce, StringComparison.Ordinal) ||
                state.Variant != context.Variant)
                throw new InvalidOperationException("LOCAL-004 Undo lifecycle state rejected.");
            return state;
        }

        private static MatrixVariant ParseVariant(string? raw)
        {
            switch ((raw ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "OBJECT_ONLY": return MatrixVariant.ObjectOnly;
                case "OBJECT_ERASE": return MatrixVariant.ObjectErase;
                case "OBJECT_INSPECTED": return MatrixVariant.ObjectInspected;
                case "DB_ENABLE_OBJECT": return MatrixVariant.DbEnableObject;
                case "DB_START_OBJECT": return MatrixVariant.DbStartObject;
                case "DB_ENABLE_DB_START_OBJECT": return MatrixVariant.DbEnableDbStartObject;
                default: throw new InvalidOperationException("LOCAL-004 Undo lifecycle variant rejected.");
            }
        }

        private static string VariantText(MatrixVariant variant)
        {
            switch (variant)
            {
                case MatrixVariant.ObjectOnly: return "OBJECT_ONLY";
                case MatrixVariant.ObjectErase: return "OBJECT_ERASE";
                case MatrixVariant.ObjectInspected: return "OBJECT_INSPECTED";
                case MatrixVariant.DbEnableObject: return "DB_ENABLE_OBJECT";
                case MatrixVariant.DbStartObject: return "DB_START_OBJECT";
                case MatrixVariant.DbEnableDbStartObject: return "DB_ENABLE_DB_START_OBJECT";
                default: throw new InvalidOperationException("LOCAL-004 Undo lifecycle variant is invalid.");
            }
        }

        private static string BooleanClass(bool value) => value ? "ON" : "OFF";

        private static bool UsesEraseTopology(MatrixVariant variant) =>
            variant == MatrixVariant.ObjectErase || variant == MatrixVariant.ObjectInspected;

        private static string ClassifyMarker(string marker)
        {
            if (string.Equals(marker, BeforeToken, StringComparison.Ordinal)) return "BEFORE";
            if (string.Equals(marker, AfterToken, StringComparison.Ordinal)) return "AFTER";
            return "OTHER_OR_INVALID";
        }

        private static string ClassifyTopology(Document document, ObjectId sentinelId)
        {
            try
            {
                if (sentinelId.IsNull || sentinelId.IsErased) return "UNDONE";
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var entity = transaction.GetObject(sentinelId, OpenMode.ForRead, true) as Entity;
                    if (entity == null || entity.IsErased) return "UNDONE";
                    transaction.Commit();
                    return "PRESENT";
                }
            }
            catch
            {
                return sentinelId.IsNull || sentinelId.IsErased ? "UNDONE" : "OTHER_OR_INVALID";
            }
        }

        private static string ReadMarker(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var marker = ReadMarker(OpenCarrier(document.Database, transaction, OpenMode.ForRead));
                transaction.Commit();
                return marker;
            }
        }

        private static string ReadMarker(BlockBegin carrier)
        {
            using (var data = carrier.GetXDataForApplication(RegAppName))
            {
                if (data == null) return string.Empty;
                var values = data.AsArray();
                if (values.Length != 3 ||
                    !string.Equals(Convert.ToString(values[0].Value, CultureInfo.InvariantCulture), RegAppName, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(Convert.ToString(values[1].Value, CultureInfo.InvariantCulture), MarkerVersion, StringComparison.Ordinal))
                    return string.Empty;
                return Convert.ToString(values[2].Value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
        }

        private static void WriteMarker(BlockBegin carrier, string token)
        {
            using (var data = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, MarkerVersion),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, token)))
            {
                carrier.XData = data;
            }
        }

        private static BlockBegin OpenCarrier(Database database, Transaction transaction, OpenMode mode)
        {
            var modelSpace = OpenModelSpace(database, transaction, OpenMode.ForRead);
            return (BlockBegin)transaction.GetObject(modelSpace.BlockBeginId, mode);
        }

        private static BlockTableRecord OpenModelSpace(Database database, Transaction transaction, OpenMode mode)
        {
            var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], mode);
        }

        private static void EnsureRegApp(Database database, Transaction transaction)
        {
            var table = (RegAppTable)transaction.GetObject(database.RegAppTableId, OpenMode.ForRead);
            if (table.Has(RegAppName)) return;
            table.UpgradeOpen();
            var record = new RegAppTableRecord { Name = RegAppName };
            table.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
        }

        private static string RequiredResultPath()
        {
            var raw = (Environment.GetEnvironmentVariable(ResultVariable) ?? string.Empty).Trim();
            if (raw.Length == 0) throw new InvalidOperationException("LOCAL-004 Undo lifecycle result path rejected.");
            var path = Path.GetFullPath(raw);
            if (!string.Equals(Path.GetFileName(path), ResultFileName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(Path.GetDirectoryName(path)) ||
                !Directory.Exists(Path.GetDirectoryName(path)))
                throw new InvalidOperationException("LOCAL-004 Undo lifecycle result path rejected.");
            return path;
        }

        private static void TryWriteFailure(string phase)
        {
            try
            {
                var path = RequiredResultPath();
                if (File.Exists(path)) return;
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                var variant = ParseVariant(Environment.GetEnvironmentVariable(VariantVariable));
                WriteNew(path, new[]
                {
                    "status=FAIL",
                    "schema=" + Schema,
                    "qualification_boundary=" + Boundary,
                    "production_local004_qualified=false",
                    "nonce=" + nonce,
                    "variant=" + VariantText(variant),
                    "failure_phase=" + phase,
                    "failure_code=UNDO_LIFECYCLE_PROBE_FAILED",
                });
            }
            catch
            {
                // The original command failure remains authoritative.
            }
        }

        private static void WriteNew(string path, IReadOnlyCollection<string> lines)
        {
            if (File.Exists(path)) throw new IOException("LOCAL-004 Undo lifecycle result already exists.");
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(line);
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(temp, path);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private enum MatrixVariant
        {
            ObjectOnly,
            ObjectErase,
            ObjectInspected,
            DbEnableObject,
            DbStartObject,
            DbEnableDbStartObject,
        }

        private sealed class ProbeContext
        {
            public ProbeContext(Document document, string nonce, MatrixVariant variant)
            {
                Document = document;
                Nonce = nonce;
                Variant = variant;
            }

            public Document Document { get; }
            public string Nonce { get; }
            public MatrixVariant Variant { get; }
        }

        private sealed class MatrixState
        {
            public MatrixState(Document document, string nonce, MatrixVariant variant)
            {
                Document = document;
                Nonce = nonce;
                Variant = variant;
            }

            public Document Document { get; }
            public string Nonce { get; }
            public MatrixVariant Variant { get; }
            public ObjectId SentinelId { get; set; } = ObjectId.Null;
            public string DatabaseRecordingAtEntry { get; set; } = "OFF";
            public string DatabaseRecordingAfterEnable { get; set; } = "NOT_RUN";
            public string DatabaseRecordingAfterStart { get; set; } = "NOT_RUN";
            public bool MutationCommitted { get; set; }
            public int InspectionCount { get; set; }
        }
    }
}
