using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

internal static class QsQuantBimViewportMultiSelectionSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var reducer = new QuantBimViewportMultiSelection();
        var current = new IfcSelectionSet("Viewport selection", new[] { "B", "a" });

        var add = reducer.ApplyGuid(current, "c", QuantBimSelectionInteractionMode.Add, false);
        Require(add.Guids.SequenceEqual(new[] { "a", "B", "c" }, StringComparer.OrdinalIgnoreCase), "add preserves deterministic GUID set");

        var duplicate = reducer.ApplyGuid(add, "A", QuantBimSelectionInteractionMode.Add, false);
        Require(duplicate.Guids.Count == 3, "add is case-insensitive and idempotent");

        var toggleOff = reducer.ApplyGuid(duplicate, "b", QuantBimSelectionInteractionMode.Toggle, false);
        Require(toggleOff.Guids.Count == 2 && !toggleOff.Guids.Contains("B", StringComparer.OrdinalIgnoreCase), "toggle removes existing GUID");

        var toggleOn = reducer.ApplyGuid(toggleOff, "D", QuantBimSelectionInteractionMode.Toggle, false);
        Require(toggleOn.Guids.Contains("D", StringComparer.OrdinalIgnoreCase), "toggle adds missing GUID");

        var replace = reducer.ApplyGuid(toggleOn, "Z", QuantBimSelectionInteractionMode.Replace, false);
        Require(replace.Guids.Count == 1 && string.Equals(replace.Guids[0], "Z", StringComparison.Ordinal), "replace yields single picked GUID");

        var keepOnMiss = reducer.ApplyGuid(replace, null, QuantBimSelectionInteractionMode.Replace, false);
        Require(keepOnMiss.Guids.Count == 1, "miss can preserve selection");

        var clearOnMiss = reducer.ApplyGuid(replace, null, QuantBimSelectionInteractionMode.Replace, true);
        Require(clearOnMiss.Guids.Count == 0, "miss can clear selection explicitly");

        Expect<ArgumentOutOfRangeException>(() => reducer.ApplyGuid(current, "X", (QuantBimSelectionInteractionMode)99, false), "invalid interaction mode");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("QuantBIM viewport multi-selection smoke failed: " + label + ".");
    }

    private static void Expect<T>(Action action, string label) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("QuantBIM viewport multi-selection smoke failed: expected " + typeof(T).Name + " for " + label + ".");
    }
}
