using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Automation-only clean-room probe for determining whether proxy/BRC entities
    /// expose measurement-capable public BricsCAD APIs. The report intentionally
    /// excludes drawing paths, CAD handles, layer/text/property values and BLT APIs.
    /// </summary>
    public sealed class BrcPublicProbeCommands
    {
        private const string ResultVariable = "QS3D_BRC_PROBE_RESULT";
        private const string NonceVariable = "QS3D_BRC_PROBE_NONCE";
        private const string ResultFileName = "brc-public-probe-result.txt";

        [CommandMethod("QS3DBRCPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (string.IsNullOrWhiteSpace(resultPath))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D BRC public probe skipped: " + ResultVariable + " is not set.");
                return;
            }

            try
            {
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _))
                    throw new InvalidOperationException("The BRC public probe nonce is invalid.");
                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available for the BRC public probe.");
                var lines = new List<string>(BrcPublicEntityProbe.Build(document));
                lines.Insert(0, "nonce=" + nonce);
                lines.Insert(0, "process=" + OneLine(Process.GetCurrentProcess().ProcessName));
                lines.Insert(0, "command=QS3DBRCPROBE");
                lines.Insert(0, "status=PASS");
                WriteMarkerAtomic(resultPath, lines);
                document.Editor.WriteMessage("\nQS3D BRC public probe PASS.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(resultPath);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D BRC public probe FAIL. See the local qualification result.");
                throw;
            }
        }

        private static void TryWriteFailure(string resultPath)
        {
            try
            {
                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=FAIL",
                    "command=QS3DBRCPROBE",
                    "error_code=PROBE_FAILED"
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            if (string.IsNullOrWhiteSpace(resultPath)) throw new ArgumentException("Probe result path is required.", nameof(resultPath));
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            var fullPath = Path.GetFullPath(resultPath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The BRC public probe result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("The BRC public probe result directory must already exist.");
            if (File.Exists(fullPath))
                throw new IOException("The BRC public probe result already exists.");
            var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
                }

                File.Move(tempPath, fullPath);
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        private static string OneLine(string value) =>
            (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    internal static class BrcPublicEntityProbe
    {
        private const int MaxCurrentSpaceEntities = 250000;
        private const int MaxProxyExplosions = 10000;
        private const int MaxExplodedParts = 250000;
        private const int MaxNestedExplodeDepth = 1;

        public static IReadOnlyList<string> Build(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var stats = new ProbeStats();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var space = transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForRead, false) as BlockTableRecord;
                if (space == null) throw new InvalidOperationException("The active BricsCAD Current Space is unavailable.");
                foreach (ObjectId id in space)
                {
                    if (stats.EntityAttemptedCount >= MaxCurrentSpaceEntities)
                        throw new ProbeLimitExceededException("BRC public probe exceeded its guarded Current Space limit.");
                    stats.EntityAttemptedCount++;
                    try
                    {
                        var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                        if (entity == null) continue;
                        stats.EntityOpenedCount++;
                        AccumulatePublicMetrics(entity, stats);
                        if (!(entity is ProxyEntity)) continue;
                        ProbeProxy(entity, stats);
                    }
                    catch (ProbeLimitExceededException)
                    {
                        throw;
                    }
                    catch
                    {
                        stats.EntityReadFailureCount++;
                    }
                }
                transaction.Commit();
            }
            if (stats.EntityReadFailureCount != 0)
                throw new InvalidOperationException("The BRC public probe could not complete its Current Space scan.");

            var lines = new List<string>
            {
                "schema=QS3D_BRC_PUBLIC_PROBE_V1",
                "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                "scan_complete=true",
                "drawing_unit_code=" + ((int)document.Database.Insunits).ToString(CultureInfo.InvariantCulture),
                "tile_mode=" + (document.Database.TileMode ? "true" : "false"),
                "entity_attempted_count=" + stats.EntityAttemptedCount.ToString(CultureInfo.InvariantCulture),
                "entity_opened_count=" + stats.EntityOpenedCount.ToString(CultureInfo.InvariantCulture),
                "entity_read_failure_count=" + stats.EntityReadFailureCount.ToString(CultureInfo.InvariantCulture),
                "public_length_entity_count=" + stats.PublicLengthEntityCount.ToString(CultureInfo.InvariantCulture),
                "public_plan_area_entity_count=" + stats.PublicPlanAreaEntityCount.ToString(CultureInfo.InvariantCulture),
                "public_surface_area_entity_count=" + stats.PublicSurfaceAreaEntityCount.ToString(CultureInfo.InvariantCulture),
                "public_volume_entity_count=" + stats.PublicVolumeEntityCount.ToString(CultureInfo.InvariantCulture),
                "proxy_entity_count=" + stats.ProxyEntityCount.ToString(CultureInfo.InvariantCulture),
                "proxy_direct_metric_ready_count=" + stats.ProxyDirectMetricReadyCount.ToString(CultureInfo.InvariantCulture),
                "proxy_direct_length_count=" + stats.ProxyDirectLengthCount.ToString(CultureInfo.InvariantCulture),
                "proxy_direct_plan_area_count=" + stats.ProxyDirectPlanAreaCount.ToString(CultureInfo.InvariantCulture),
                "proxy_direct_surface_area_count=" + stats.ProxyDirectSurfaceAreaCount.ToString(CultureInfo.InvariantCulture),
                "proxy_direct_volume_count=" + stats.ProxyDirectVolumeCount.ToString(CultureInfo.InvariantCulture),
                "proxy_extents_available_count=" + stats.ProxyExtentsAvailableCount.ToString(CultureInfo.InvariantCulture),
                "proxy_explode_success_count=" + stats.ProxyExplodeSuccessCount.ToString(CultureInfo.InvariantCulture),
                "proxy_explode_nonempty_count=" + stats.ProxyExplodeNonEmptyCount.ToString(CultureInfo.InvariantCulture),
                "proxy_explode_failure_count=" + stats.ProxyExplodeFailureCount.ToString(CultureInfo.InvariantCulture),
                "proxy_exploded_part_count=" + stats.ProxyExplodedPartCount.ToString(CultureInfo.InvariantCulture),
                "proxy_exploded_length_ready_count=" + stats.ProxyExplodedLengthReadyCount.ToString(CultureInfo.InvariantCulture),
                "proxy_exploded_plan_area_ready_count=" + stats.ProxyExplodedPlanAreaReadyCount.ToString(CultureInfo.InvariantCulture),
                "proxy_exploded_surface_area_ready_count=" + stats.ProxyExplodedSurfaceAreaReadyCount.ToString(CultureInfo.InvariantCulture),
                "proxy_exploded_volume_ready_count=" + stats.ProxyExplodedVolumeReadyCount.ToString(CultureInfo.InvariantCulture),
                "proxy_nested_explode_success_count=" + stats.ProxyNestedExplodeSuccessCount.ToString(CultureInfo.InvariantCulture),
                "proxy_nested_explode_nonempty_count=" + stats.ProxyNestedExplodeNonEmptyCount.ToString(CultureInfo.InvariantCulture),
                "proxy_nested_explode_failure_count=" + stats.ProxyNestedExplodeFailureCount.ToString(CultureInfo.InvariantCulture),
                "exploded_part_positive_length_count=" + stats.ExplodedPartPositiveLengthCount.ToString(CultureInfo.InvariantCulture),
                "exploded_part_positive_plan_area_count=" + stats.ExplodedPartPositivePlanAreaCount.ToString(CultureInfo.InvariantCulture),
                "exploded_part_positive_surface_area_count=" + stats.ExplodedPartPositiveSurfaceAreaCount.ToString(CultureInfo.InvariantCulture),
                "exploded_part_positive_volume_count=" + stats.ExplodedPartPositiveVolumeCount.ToString(CultureInfo.InvariantCulture)
            };
            return lines.AsReadOnly();
        }

        private static void ProbeProxy(Entity entity, ProbeStats stats)
        {
            stats.ProxyEntityCount++;
            try
            {
                var extents = entity.GeometricExtents;
                if (Finite(extents.MinPoint.X) && Finite(extents.MinPoint.Y) && Finite(extents.MinPoint.Z) &&
                    Finite(extents.MaxPoint.X) && Finite(extents.MaxPoint.Y) && Finite(extents.MaxPoint.Z))
                    stats.ProxyExtentsAvailableCount++;
            }
            catch { }

            AccumulateDirectMetrics(entity, stats);
            if (stats.ProxyEntityCount > MaxProxyExplosions)
                throw new ProbeLimitExceededException("BRC public probe exceeded its guarded proxy explosion limit.");

            var exploded = new DBObjectCollection();
            var proxyMetric = new ProbeMetric();
            try
            {
                entity.Explode(exploded);
                stats.ProxyExplodeSuccessCount++;
                if (exploded.Count > 0) stats.ProxyExplodeNonEmptyCount++;
                foreach (DBObject item in exploded)
                {
                    if (!(item is Entity part)) continue;
                    ProbeExplodedPart(part, stats, proxyMetric, 0);
                }
            }
            catch (ProbeLimitExceededException) { throw; }
            catch { stats.ProxyExplodeFailureCount++; }
            finally
            {
                foreach (DBObject item in exploded) item.Dispose();
            }
            if (proxyMetric.HasLength) stats.ProxyExplodedLengthReadyCount++;
            if (proxyMetric.HasPlanArea) stats.ProxyExplodedPlanAreaReadyCount++;
            if (proxyMetric.HasSurfaceArea) stats.ProxyExplodedSurfaceAreaReadyCount++;
            if (proxyMetric.HasVolume) stats.ProxyExplodedVolumeReadyCount++;
        }

        private static void ProbeExplodedPart(Entity entity, ProbeStats stats, ProbeMetric proxyMetric, int depth)
        {
            if (stats.ProxyExplodedPartCount >= MaxExplodedParts)
                throw new ProbeLimitExceededException("BRC public probe exceeded its guarded exploded-part limit.");
            stats.ProxyExplodedPartCount++;
            AccumulateExplodedMetrics(entity, stats, proxyMetric);
            if (depth >= MaxNestedExplodeDepth) return;

            var nested = new DBObjectCollection();
            try
            {
                entity.Explode(nested);
                stats.ProxyNestedExplodeSuccessCount++;
                if (nested.Count > 0) stats.ProxyNestedExplodeNonEmptyCount++;
                foreach (DBObject item in nested)
                    if (item is Entity part) ProbeExplodedPart(part, stats, proxyMetric, depth + 1);
            }
            catch (ProbeLimitExceededException) { throw; }
            catch { stats.ProxyNestedExplodeFailureCount++; }
            finally
            {
                foreach (DBObject item in nested) item.Dispose();
            }
        }

        private static void AccumulateDirectMetrics(Entity entity, ProbeStats stats)
        {
            var metric = ReadMetrics(entity);
            if (metric.HasLength) stats.ProxyDirectLengthCount++;
            if (metric.HasPlanArea) stats.ProxyDirectPlanAreaCount++;
            if (metric.HasSurfaceArea) stats.ProxyDirectSurfaceAreaCount++;
            if (metric.HasVolume) stats.ProxyDirectVolumeCount++;
            if (metric.HasLength || metric.HasPlanArea || metric.HasVolume) stats.ProxyDirectMetricReadyCount++;
        }

        private static void AccumulatePublicMetrics(Entity entity, ProbeStats stats)
        {
            var metric = ReadMetrics(entity);
            if (metric.HasLength) stats.PublicLengthEntityCount++;
            if (metric.HasPlanArea) stats.PublicPlanAreaEntityCount++;
            if (metric.HasSurfaceArea) stats.PublicSurfaceAreaEntityCount++;
            if (metric.HasVolume) stats.PublicVolumeEntityCount++;
        }

        private static void AccumulateExplodedMetrics(Entity entity, ProbeStats stats, ProbeMetric proxyMetric)
        {
            var metric = ReadMetrics(entity);
            if (metric.HasLength) stats.ExplodedPartPositiveLengthCount++;
            if (metric.HasPlanArea) stats.ExplodedPartPositivePlanAreaCount++;
            if (metric.HasSurfaceArea) stats.ExplodedPartPositiveSurfaceAreaCount++;
            if (metric.HasVolume) stats.ExplodedPartPositiveVolumeCount++;
            proxyMetric.HasLength |= metric.HasLength;
            proxyMetric.HasPlanArea |= metric.HasPlanArea;
            proxyMetric.HasSurfaceArea |= metric.HasSurfaceArea;
            proxyMetric.HasVolume |= metric.HasVolume;
        }

        private static ProbeMetric ReadMetrics(Entity entity)
        {
            var metric = new ProbeMetric();
            if (entity is Curve curve)
            {
                try
                {
                    var length = curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);
                    metric.HasLength = Finite(length) && length > 0d;
                }
                catch { }
            }
            if (entity is Polyline polyline && polyline.Closed)
            {
                try { metric.HasPlanArea = Finite(polyline.Area) && Math.Abs(polyline.Area) > 0d; } catch { }
            }
            else if (entity is Region region)
            {
                try { metric.HasPlanArea = Finite(region.Area) && Math.Abs(region.Area) > 0d; } catch { }
            }
            else if (entity is Hatch hatch)
            {
                try { metric.HasPlanArea = Finite(hatch.Area) && Math.Abs(hatch.Area) > 0d; } catch { }
            }
            else if (entity is Face face)
            {
                try
                {
                    var p0 = face.GetVertexAt(0);
                    var p1 = face.GetVertexAt(1);
                    var p2 = face.GetVertexAt(2);
                    var p3 = face.GetVertexAt(3);
                    var area = TriangleArea(p0, p1, p2);
                    if (p3.DistanceTo(p2) > 1e-9) area += TriangleArea(p0, p2, p3);
                    metric.HasSurfaceArea = Finite(area) && area > 0d;
                }
                catch { }
            }
            if (entity is Solid3d solid)
            {
                try
                {
                    var surfaceArea = solid.Area;
                    metric.HasSurfaceArea = Finite(surfaceArea) && Math.Abs(surfaceArea) > 0d;
                }
                catch { }
                try
                {
                    var volume = solid.MassProperties.Volume;
                    metric.HasVolume = Finite(volume) && Math.Abs(volume) > 0d;
                }
                catch { }
            }
            return metric;
        }

        private static double TriangleArea(Point3d p0, Point3d p1, Point3d p2) =>
            0.5d * (p1 - p0).CrossProduct(p2 - p0).Length;

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private sealed class ProbeStats
        {
            public int EntityAttemptedCount;
            public int EntityOpenedCount;
            public int EntityReadFailureCount;
            public int PublicLengthEntityCount;
            public int PublicPlanAreaEntityCount;
            public int PublicSurfaceAreaEntityCount;
            public int PublicVolumeEntityCount;
            public int ProxyEntityCount;
            public int ProxyDirectMetricReadyCount;
            public int ProxyDirectLengthCount;
            public int ProxyDirectPlanAreaCount;
            public int ProxyDirectSurfaceAreaCount;
            public int ProxyDirectVolumeCount;
            public int ProxyExtentsAvailableCount;
            public int ProxyExplodeSuccessCount;
            public int ProxyExplodeNonEmptyCount;
            public int ProxyExplodeFailureCount;
            public int ProxyExplodedPartCount;
            public int ProxyExplodedLengthReadyCount;
            public int ProxyExplodedPlanAreaReadyCount;
            public int ProxyExplodedSurfaceAreaReadyCount;
            public int ProxyExplodedVolumeReadyCount;
            public int ProxyNestedExplodeSuccessCount;
            public int ProxyNestedExplodeNonEmptyCount;
            public int ProxyNestedExplodeFailureCount;
            public int ExplodedPartPositiveLengthCount;
            public int ExplodedPartPositivePlanAreaCount;
            public int ExplodedPartPositiveSurfaceAreaCount;
            public int ExplodedPartPositiveVolumeCount;
        }

        private sealed class ProbeMetric
        {
            public bool HasLength;
            public bool HasPlanArea;
            public bool HasSurfaceArea;
            public bool HasVolume;
        }

        private sealed class ProbeLimitExceededException : InvalidOperationException
        {
            public ProbeLimitExceededException(string message) : base(message) { }
        }
    }
}
