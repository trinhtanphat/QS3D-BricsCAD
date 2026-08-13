using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Services;
using QS3D.Core.Takeoff;
using QS3D.Core.Units;

namespace QS3D.Core.PerfHarness;

internal sealed class Program
{
    private const ulong TraceChecksumOffset = 14695981039346656037UL;
    private const ulong TraceChecksumPrime = 1099511628211UL;

    private static int Main(string[] args)
    {
        try
        {
            var options = Options.Parse(args);
            var results = new List<BenchmarkResult>();

            if (options.Scenario is "all" or "dependency-rebuild")
                results.Add(RunDependencyRebuild(options));
            if (options.Scenario is "all" or "dependency-closure")
                results.Add(RunDependencyClosure(options));
            if (options.Scenario is "all" or "mark-changed")
                results.Add(RunMarkChanged(options));
            if (options.Scenario is "all" or "targeted-regeneration")
                results.Add(RunTargetedRegeneration(options));
            if (options.Scenario is "all" or "quantity-trace-generation")
                results.Add(RunQuantityTraceGeneration(options));

            if (results.Count == 0)
                throw new ArgumentException("Unknown --scenario. Use all, dependency-rebuild, dependency-closure, mark-changed, targeted-regeneration, or quantity-trace-generation.");

            var report = new BenchmarkReport
            {
                SchemaVersion = 1,
                RecordedUtc = DateTime.UtcNow,
                SourceRevision = options.Revision,
                Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                ProcessorCount = Environment.ProcessorCount,
                Elements = options.Elements,
                Targets = options.Targets,
                Iterations = options.Iterations,
                WarmupIterations = options.Warmups,
                Results = results
            };

            foreach (var result in results)
            {
                Console.WriteLine(
                    $"{result.Name}: median={result.MedianMilliseconds:F3} ms, p95={result.P95Milliseconds:F3} ms, " +
                    $"median alloc={result.MedianAllocatedBytes:N0} B, peak working set={result.PeakWorkingSetBytes:N0} B");
            }

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            if (!string.IsNullOrWhiteSpace(options.JsonPath))
            {
                if (options.JsonPath == "-") Console.WriteLine(json);
                else
                {
                    var fullPath = Path.GetFullPath(options.JsonPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                    File.WriteAllText(fullPath, json);
                    Console.WriteLine("JSON: " + fullPath);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("QS3D.Core.PerfHarness: " + ex.Message);
            return 2;
        }
    }

    private static BenchmarkResult RunDependencyRebuild(Options options)
    {
        var elements = BuildChain(options.Elements).Elements;
        var graph = new DependencyGraph();
        for (var i = 0; i < options.Warmups; i++) graph.Rebuild(elements);
        return Measure("dependency-rebuild", options.Iterations, () => graph.Rebuild(elements));
    }

    private static BenchmarkResult RunDependencyClosure(Options options)
    {
        var elements = BuildChain(options.Elements).Elements;
        var graph = new DependencyGraph();
        graph.Rebuild(elements);
        var expected = Math.Max(0, options.Elements - 1);
        for (var i = 0; i < options.Warmups; i++)
            RequireCount(graph.GetDependentsTransitive("E0").Count, expected, "dependency closure warmup");

        return Measure("dependency-closure", options.Iterations, () =>
            RequireCount(graph.GetDependentsTransitive("E0").Count, expected, "dependency closure"));
    }

    private static BenchmarkResult RunMarkChanged(Options options)
    {
        for (var i = 0; i < options.Warmups; i++) PrepareMarkChanged(options.Elements)();
        return MeasurePrepared("mark-changed", options.Iterations, () => PrepareMarkChanged(options.Elements));
    }

    private static Action PrepareMarkChanged(int elements)
    {
        var project = BuildChain(elements);
        var engine = new RegenerationEngine(new DependencyGraph(), new[] { new NoOpRegenerator() });
        return () =>
        {
            engine.MarkChanged(project, "E0", ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
            if (project.Elements.Count > 1 && project.Elements[project.Elements.Count - 1].Dirty == ElementDirtyFlags.None)
                throw new InvalidOperationException("MarkChanged benchmark fixture did not dirty the transitive tail.");
        };
    }

    private static BenchmarkResult RunTargetedRegeneration(Options options)
    {
        for (var i = 0; i < options.Warmups; i++) PrepareTargetedRegeneration(options.Elements, options.Targets)();
        return MeasurePrepared("targeted-regeneration", options.Iterations, () => PrepareTargetedRegeneration(options.Elements, options.Targets));
    }

    private static Action PrepareTargetedRegeneration(int elements, int targets)
    {
        var project = BuildChain(elements);
        var actualTargets = Math.Min(targets, elements);
        var ids = new List<string>(actualTargets);
        var first = elements - actualTargets;
        for (var i = first; i < elements; i++)
        {
            project.Elements[i].MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
            ids.Add(project.Elements[i].Id);
        }

        var engine = new RegenerationEngine(new DependencyGraph(), new[] { new NoOpRegenerator() });
        return () =>
        {
            var regenerated = engine.RegenerateDirtySubset(project, ids);
            RequireCount(regenerated, actualTargets, "targeted regeneration");
        };
    }

    private static BenchmarkResult RunQuantityTraceGeneration(Options options)
    {
        var fixtures = BuildQuantityTraceFixtures(options.Elements);
        var expected = EvaluateQuantityTraces(fixtures, validateProjection: true);
        var repeated = EvaluateQuantityTraces(fixtures, validateProjection: true);
        RequireTraceAggregate(repeated, expected, "quantity trace deterministic preflight");

        for (var i = 0; i < options.Warmups; i++)
        {
            var warmup = EvaluateQuantityTraces(fixtures, validateProjection: false);
            RequireTraceAggregate(warmup, expected, "quantity trace warmup");
        }

        return Measure("quantity-trace-generation", options.Iterations, () =>
        {
            var measured = EvaluateQuantityTraces(fixtures, validateProjection: false);
            RequireTraceAggregate(measured, expected, "quantity trace measured pass");
        });
    }

    private static EntitySnapshot[] BuildQuantityTraceFixtures(int count)
    {
        var fixtures = new EntitySnapshot[count];
        for (var i = 0; i < count; i++)
        {
            var snapshot = new EntitySnapshot(
                "Q" + i.ToString(CultureInfo.InvariantCulture),
                "PerfEntity",
                "PERF")
            {
                LengthDrawingUnits = 1_000d + (i % 997),
                AreaDrawingUnitsSquared = 1_000_000d + ((i % 991) * 1_000d),
                VolumeDrawingUnitsCubed = 1_000_000_000d + ((i % 983) * 1_000_000d)
            };
            fixtures[i] = snapshot;
        }
        return fixtures;
    }

    private static TraceAggregate EvaluateQuantityTraces(IReadOnlyList<EntitySnapshot> fixtures, bool validateProjection)
    {
        var checksum = TraceChecksumOffset;
        var factCount = 0;
        for (var i = 0; i < fixtures.Count; i++)
        {
            var snapshot = fixtures[i];
            var kind = QuantityTraceKind(i);
            var projected = QuantityEngine.CalculateWithTrace(snapshot, kind, DrawingUnit.Millimeter);
            if (validateProjection) ValidateTraceProjection(snapshot, kind, projected);

            factCount += projected.Trace.InputFacts.Count;
            checksum = MixTraceChecksum(checksum, (ulong)kind);
            checksum = MixTraceChecksum(checksum, unchecked((ulong)BitConverter.DoubleToInt64Bits(projected.Result.Value)));
            checksum = MixTraceChecksum(checksum, unchecked((ulong)BitConverter.DoubleToInt64Bits(projected.Trace.NetValue)));
            checksum = MixTraceChecksum(checksum, (ulong)projected.Trace.InputFacts.Count);
            checksum = MixTraceChecksum(checksum, (ulong)projected.Trace.Assumptions.Count);
            checksum = MixTraceChecksum(checksum, (ulong)projected.Trace.Adjustments.Count);
            checksum = MixTraceChecksum(checksum, projected.Result.Handle);
            checksum = MixTraceChecksum(checksum, projected.Result.Unit);
            checksum = MixTraceChecksum(checksum, projected.Trace.SourceIdentity);
        }

        return new TraceAggregate(fixtures.Count, factCount, checksum);
    }

    private static TakeoffKind QuantityTraceKind(int index)
    {
        return (index % 4) switch
        {
            0 => TakeoffKind.Count,
            1 => TakeoffKind.Length,
            2 => TakeoffKind.Area,
            _ => TakeoffKind.Volume
        };
    }

    private static void ValidateTraceProjection(EntitySnapshot snapshot, TakeoffKind kind, TakeoffResultWithTrace projected)
    {
        var result = projected.Result;
        var trace = projected.Trace;
        if (result.Kind != kind)
            throw new InvalidOperationException("Quantity trace benchmark kind mismatch for " + snapshot.Handle + ".");
        if (!string.Equals(result.Handle, snapshot.Handle, StringComparison.Ordinal) ||
            !string.Equals(trace.SemanticIdentity, result.Handle, StringComparison.Ordinal) ||
            !string.Equals(trace.SourceIdentity, result.Handle, StringComparison.Ordinal))
            throw new InvalidOperationException("Quantity trace benchmark source identity mismatch for " + snapshot.Handle + ".");
        if (trace.GrossValue != result.Value || trace.NetValue != result.Value ||
            !string.Equals(trace.Unit, result.Unit, StringComparison.Ordinal))
            throw new InvalidOperationException("Quantity trace benchmark canonical result parity failed for " + snapshot.Handle + ".");
        if (!string.Equals(trace.QuantityKey, kind.ToString(), StringComparison.Ordinal) ||
            !string.Equals(trace.RoundingPolicy, "none", StringComparison.Ordinal) ||
            trace.Adjustments.Count != 0)
            throw new InvalidOperationException("Quantity trace benchmark metadata parity failed for " + snapshot.Handle + ".");

        var expectedFacts = kind == TakeoffKind.Count ? 0 : 1;
        var expectedAssumptions = kind == TakeoffKind.Count ? 0 : 1;
        if (trace.InputFacts.Count != expectedFacts || trace.Assumptions.Count != expectedAssumptions)
            throw new InvalidOperationException("Quantity trace benchmark explanation cardinality failed for " + snapshot.Handle + ".");
        if (kind == TakeoffKind.Count) return;

        var fact = trace.InputFacts[0];
        if (fact.Value != RawMetric(snapshot, kind) ||
            !string.Equals(fact.SourceIdentity, snapshot.Handle, StringComparison.Ordinal))
            throw new InvalidOperationException("Quantity trace benchmark raw fact parity failed for " + snapshot.Handle + ".");
    }

    private static double RawMetric(EntitySnapshot snapshot, TakeoffKind kind)
    {
        return kind switch
        {
            TakeoffKind.Length => snapshot.LengthDrawingUnits.GetValueOrDefault(),
            TakeoffKind.Area => snapshot.AreaDrawingUnitsSquared.GetValueOrDefault(),
            TakeoffKind.Volume => snapshot.VolumeDrawingUnitsCubed.GetValueOrDefault(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Count does not have a raw metric.")
        };
    }

    private static ulong MixTraceChecksum(ulong checksum, ulong value)
    {
        unchecked
        {
            checksum ^= value;
            return checksum * TraceChecksumPrime;
        }
    }

    private static ulong MixTraceChecksum(ulong checksum, string value)
    {
        unchecked
        {
            checksum = MixTraceChecksum(checksum, (ulong)value.Length);
            for (var i = 0; i < value.Length; i++)
                checksum = MixTraceChecksum(checksum, value[i]);
            return checksum;
        }
    }

    private static void RequireTraceAggregate(TraceAggregate actual, TraceAggregate expected, string operation)
    {
        if (actual.Processed != expected.Processed ||
            actual.FactCount != expected.FactCount ||
            actual.Checksum != expected.Checksum)
        {
            throw new InvalidOperationException(
                $"{operation} expected processed={expected.Processed}, facts={expected.FactCount}, checksum={expected.Checksum:X16} " +
                $"but got processed={actual.Processed}, facts={actual.FactCount}, checksum={actual.Checksum:X16}.");
        }
    }

    private static ProjectState BuildChain(int count)
    {
        var project = new ProjectState("perf", "Core Performance Fixture");
        for (var i = 0; i < count; i++)
        {
            var element = new ProjectElement("E" + i.ToString(CultureInfo.InvariantCulture), ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            if (i > 0) element.DependsOn.Add("E" + (i - 1).ToString(CultureInfo.InvariantCulture));
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
        }
        return project;
    }

    private static BenchmarkResult Measure(string name, int iterations, Action action) =>
        MeasurePrepared(name, iterations, () => action);

    private static BenchmarkResult MeasurePrepared(string name, int iterations, Func<Action> prepare)
    {
        var timings = new double[iterations];
        var allocations = new long[iterations];
        long peakWorkingSet = 0;

        for (var i = 0; i < iterations; i++)
        {
            var action = prepare();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            var afterAllocated = GC.GetAllocatedBytesForCurrentThread();

            timings[i] = stopwatch.Elapsed.TotalMilliseconds;
            allocations[i] = Math.Max(0, afterAllocated - beforeAllocated);
            using var process = Process.GetCurrentProcess();
            peakWorkingSet = Math.Max(peakWorkingSet, process.WorkingSet64);
        }

        Array.Sort(timings);
        Array.Sort(allocations);
        var allocationSamples = allocations.Select(x => (double)x).ToArray();
        return new BenchmarkResult
        {
            Name = name,
            MedianMilliseconds = Percentile(timings, 0.50),
            P95Milliseconds = Percentile(timings, 0.95),
            MinMilliseconds = timings[0],
            MaxMilliseconds = timings[timings.Length - 1],
            MedianAllocatedBytes = (long)Percentile(allocationSamples, 0.50),
            P95AllocatedBytes = (long)Percentile(allocationSamples, 0.95),
            PeakWorkingSetBytes = peakWorkingSet
        };
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        if (sorted.Length == 0) return 0;
        var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static void RequireCount(int actual, int expected, string operation)
    {
        if (actual != expected)
            throw new InvalidOperationException($"{operation} expected {expected} but got {actual}.");
    }

    private readonly struct TraceAggregate
    {
        public TraceAggregate(int processed, int factCount, ulong checksum)
        {
            Processed = processed;
            FactCount = factCount;
            Checksum = checksum;
        }

        public int Processed { get; }
        public int FactCount { get; }
        public ulong Checksum { get; }
    }

    private sealed class NoOpRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => true;
        public void Regenerate(ProjectState project, ProjectElement element) => element.SetQuantity("PerfCount", 1.0);
    }

    private sealed class Options
    {
        public int Elements { get; private set; } = 10_000;
        public int Targets { get; private set; } = 256;
        public int Iterations { get; private set; } = 7;
        public int Warmups { get; private set; } = 2;
        public string Scenario { get; private set; } = "all";
        public string JsonPath { get; private set; } = string.Empty;
        public string Revision { get; private set; } = string.Empty;

        public static Options Parse(string[] args)
        {
            var options = new Options();
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                string Next()
                {
                    if (++i >= args.Length) throw new ArgumentException("Missing value for " + arg);
                    return args[i];
                }

                switch (arg)
                {
                    case "--elements": options.Elements = ParsePositive(Next(), arg); break;
                    case "--targets": options.Targets = ParsePositive(Next(), arg); break;
                    case "--iterations": options.Iterations = ParsePositive(Next(), arg); break;
                    case "--warmups": options.Warmups = ParseNonNegative(Next(), arg); break;
                    case "--scenario": options.Scenario = Next().Trim().ToLowerInvariant(); break;
                    case "--json": options.JsonPath = Next(); break;
                    case "--revision": options.Revision = Next().Trim(); break;
                    case "--help":
                    case "-h": throw new ArgumentException("Usage: dotnet run -c Release --project tests/QS3D.Core.PerfHarness -- [--elements N] [--targets N] [--iterations N] [--warmups N] [--scenario all|dependency-rebuild|dependency-closure|mark-changed|targeted-regeneration|quantity-trace-generation] [--revision SHA] [--json PATH|-]");
                    default: throw new ArgumentException("Unknown argument: " + arg);
                }
            }

            if (options.Elements > 250_000) throw new ArgumentOutOfRangeException("--elements", "Use 250000 or fewer elements per run to keep the harness bounded.");
            if (options.Targets > options.Elements) options.Targets = options.Elements;
            return options;
        }

        private static int ParsePositive(string raw, string name)
        {
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
                throw new ArgumentException(name + " must be a positive integer.");
            return value;
        }

        private static int ParseNonNegative(string raw, string name)
        {
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < 0)
                throw new ArgumentException(name + " must be a non-negative integer.");
            return value;
        }
    }

    private sealed class BenchmarkReport
    {
        public int SchemaVersion { get; set; }
        public DateTime RecordedUtc { get; set; }
        public string SourceRevision { get; set; } = string.Empty;
        public string Runtime { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public int ProcessorCount { get; set; }
        public int Elements { get; set; }
        public int Targets { get; set; }
        public int Iterations { get; set; }
        public int WarmupIterations { get; set; }
        public List<BenchmarkResult> Results { get; set; } = new();
    }

    private sealed class BenchmarkResult
    {
        public string Name { get; set; } = string.Empty;
        public double MedianMilliseconds { get; set; }
        public double P95Milliseconds { get; set; }
        public double MinMilliseconds { get; set; }
        public double MaxMilliseconds { get; set; }
        public long MedianAllocatedBytes { get; set; }
        public long P95AllocatedBytes { get; set; }
        public long PeakWorkingSetBytes { get; set; }
    }
}
