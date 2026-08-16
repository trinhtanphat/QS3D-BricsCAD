using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Re-packs the already-qualified VẼ/Công cụ buttons into the compact three-row
    /// BLT3D owner-reference rhythm. The existing buttons are reused so command routing,
    /// handlers and semantic icons remain authoritative; only QS3D-owned Draw panel layout
    /// is changed.
    /// </summary>
    internal static class BltDrawRibbonLayoutRefiner
    {
        private const string AssemblyName = "BrxMgd";
        private const string DrawTabId = "QS3D_DRAW";
        private const string DrawPanelSourceId = "QS3D_DRAW_BLT_DRAW_PANEL_SOURCE";
        private const string ToolsPanelSourceId = "QS3D_DRAW_BLT_TOOLS_PANEL_SOURCE";

        private static readonly string[][] DrawColumns =
        {
            new[]
            {
                "QS3D_DRAW_BLT_POINT",
                "QS3D_DRAW_BLT_LINE",
                "QS3D_DRAW_BLT_TRACE"
            },
            new[]
            {
                "QS3D_DRAW_BLT_ARC",
                "QS3D_DRAW_BLT_RECTANGLE",
                "QS3D_DRAW_BLT_CIRCLE"
            },
            new[]
            {
                "QS3D_DRAW_BLT_BOUNDARY"
            }
        };

        private static readonly string[][] ToolsColumns =
        {
            new[]
            {
                "QS3D_DRAW_BLT_SLAB_SLOPE",
                "QS3D_DRAW_BLT_SLAB_CUT",
                "QS3D_DRAW_BLT_MOVE"
            },
            new[]
            {
                "QS3D_DRAW_BLT_ROTATE",
                "QS3D_DRAW_BLT_MIRROR",
                "QS3D_DRAW_BLT_COPY"
            },
            new[]
            {
                "QS3D_DRAW_BLT_BREAK",
                "QS3D_DRAW_BLT_JOIN",
                "QS3D_DRAW_BLT_DISTANCE"
            },
            new[]
            {
                "QS3D_DRAW_BLT_CORNER",
                "QS3D_DRAW_BLT_TEE"
            }
        };

        public static bool TryInitialize()
        {
            try
            {
                var panels = FindDrawPanels();
                if (panels == null)
                    return false;

                var drawSource = FindPanelSourceById(panels, DrawPanelSourceId);
                var toolsSource = FindPanelSourceById(panels, ToolsPanelSourceId);
                if (drawSource == null || toolsSource == null)
                    return false;

                var drawItems = GetProperty(drawSource, "Items");
                var toolsItems = GetProperty(toolsSource, "Items");
                if (drawItems == null || toolsItems == null)
                    return false;

                // A downstream augmenter can fail after this refiner has succeeded, causing the
                // coordinator to retry. Keep retries idempotent instead of nesting row panels.
                if (MatchesLayout(drawItems, DrawColumns) && MatchesLayout(toolsItems, ToolsColumns))
                    return true;

                var originalDrawItems = Snapshot(drawItems);
                var originalToolsItems = Snapshot(toolsItems);

                // The rich Draw augmenter deliberately emits flat buttons first. Refine only
                // that known shape; any unexpected host shape fails closed into BltDrawRibbonFailSafe.
                if (!AllRibbonButtons(originalDrawItems) || !AllRibbonButtons(originalToolsItems))
                    return false;

                var buttons = IndexButtons(originalDrawItems.Concat(originalToolsItems));
                EnsureButtons(buttons, DrawColumns);
                EnsureButtons(buttons, ToolsColumns);

                var drawColumns = CreateColumnShells(DrawColumns.Length);
                var toolsColumns = CreateColumnShells(ToolsColumns.Length);

                try
                {
                    RemoveAll(drawItems);
                    RemoveAll(toolsItems);

                    PopulateColumns(drawColumns, DrawColumns, buttons);
                    PopulateColumns(toolsColumns, ToolsColumns, buttons);

                    foreach (var column in drawColumns)
                        Add(drawItems, column.RowPanel);
                    foreach (var column in toolsColumns)
                        Add(toolsItems, column.RowPanel);
                }
                catch
                {
                    // If BricsCAD exposes a different Ribbon collection shape, release every
                    // moved button from the temporary row panels and restore the exact flat
                    // QS3D panel items. The outer fail-safe can then restore bootstrap panels.
                    TryRemoveAll(drawItems);
                    TryRemoveAll(toolsItems);
                    ReleaseColumns(drawColumns);
                    ReleaseColumns(toolsColumns);
                    TryRestore(drawItems, originalDrawItems);
                    TryRestore(toolsItems, originalToolsItems);
                    throw;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private sealed class ColumnShell
        {
            public ColumnShell(object rowPanel, object items)
            {
                RowPanel = rowPanel;
                Items = items;
            }

            public object RowPanel { get; }
            public object Items { get; }
        }

        private static ColumnShell[] CreateColumnShells(int count)
        {
            var result = new ColumnShell[count];
            for (var i = 0; i < count; i++)
            {
                var rowPanel = Create("Bricscad.Windows.RibbonRowPanel");
                var items = GetProperty(rowPanel, "Items")
                            ?? throw new InvalidOperationException("RibbonRowPanel.Items was not available.");
                result[i] = new ColumnShell(rowPanel, items);
            }

            return result;
        }

        private static void PopulateColumns(
            IReadOnlyList<ColumnShell> shells,
            IReadOnlyList<string[]> layout,
            IReadOnlyDictionary<string, object> buttons)
        {
            for (var columnIndex = 0; columnIndex < layout.Count; columnIndex++)
            {
                var ids = layout[columnIndex];
                var items = shells[columnIndex].Items;
                for (var rowIndex = 0; rowIndex < ids.Length; rowIndex++)
                {
                    Add(items, buttons[ids[rowIndex]]);
                    if (rowIndex + 1 < ids.Length)
                        Add(items, Create("Bricscad.Windows.RibbonRowBreak"));
                }
            }
        }

        private static Dictionary<string, object> IndexButtons(IEnumerable<object> items)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                var id = GetProperty(item, "Id") as string;
                if (id == null || string.IsNullOrWhiteSpace(id))
                    throw new InvalidOperationException("Draw ribbon contains a missing or duplicate button id.");
                if (result.ContainsKey(id))
                    throw new InvalidOperationException("Draw ribbon contains a missing or duplicate button id.");
                result.Add(id, item);
            }

            return result;
        }

        private static void EnsureButtons(
            IReadOnlyDictionary<string, object> buttons,
            IReadOnlyList<string[]> layout)
        {
            foreach (var column in layout)
            {
                foreach (var id in column)
                {
                    if (!buttons.ContainsKey(id))
                        throw new InvalidOperationException("Required Draw ribbon button was not available: " + id);
                }
            }
        }

        private static bool MatchesLayout(object sourceItems, IReadOnlyList<string[]> expectedColumns)
        {
            var columns = Snapshot(sourceItems);
            if (columns.Count != expectedColumns.Count)
                return false;

            for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                var rowPanel = columns[columnIndex];
                if (!string.Equals(rowPanel.GetType().Name, "RibbonRowPanel", StringComparison.Ordinal))
                    return false;

                var rowItems = GetProperty(rowPanel, "Items");
                if (rowItems == null)
                    return false;

                var actual = Snapshot(rowItems);
                var expected = expectedColumns[columnIndex];
                var expectedItemCount = expected.Length == 0 ? 0 : expected.Length * 2 - 1;
                if (actual.Count != expectedItemCount)
                    return false;

                for (var rowIndex = 0; rowIndex < expected.Length; rowIndex++)
                {
                    var button = actual[rowIndex * 2];
                    if (!string.Equals(button.GetType().Name, "RibbonButton", StringComparison.Ordinal))
                        return false;
                    if (!string.Equals(GetProperty(button, "Id") as string, expected[rowIndex], StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (rowIndex + 1 < expected.Length &&
                        !string.Equals(actual[rowIndex * 2 + 1].GetType().Name, "RibbonRowBreak", StringComparison.Ordinal))
                        return false;
                }
            }

            return true;
        }

        private static bool AllRibbonButtons(IReadOnlyList<object> items) =>
            items.All(item => string.Equals(item.GetType().Name, "RibbonButton", StringComparison.Ordinal));

        private static List<object> Snapshot(object collection)
        {
            if (!(collection is IEnumerable enumerable))
                throw new InvalidOperationException("Ribbon collection was not enumerable.");

            var result = new List<object>();
            foreach (var item in enumerable)
            {
                if (item != null)
                    result.Add(item);
            }

            return result;
        }

        private static void ReleaseColumns(IEnumerable<ColumnShell> columns)
        {
            foreach (var column in columns)
                TryRemoveAll(column.Items);
        }

        private static void TryRestore(object collection, IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                try { Add(collection, item); }
                catch { }
            }
        }

        private static void RemoveAll(object collection)
        {
            foreach (var item in Snapshot(collection))
                Remove(collection, item);
        }

        private static void TryRemoveAll(object collection)
        {
            try { RemoveAll(collection); }
            catch { }
        }

        private static object? FindDrawPanels()
        {
            var control = FindRibbonControl();
            if (control == null)
                return null;

            var tabs = GetProperty(control, "Tabs");
            var drawTab = tabs == null ? null : FindById(tabs, DrawTabId);
            return drawTab == null ? null : GetProperty(drawTab, "Panels");
        }

        private static object? FindPanelSourceById(object panels, string sourceId)
        {
            if (!(panels is IEnumerable enumerable))
                return null;

            foreach (var panel in enumerable)
            {
                if (panel == null)
                    continue;
                var source = GetProperty(panel, "Source");
                if (source == null)
                    continue;
                if (string.Equals(GetProperty(source, "Id") as string, sourceId, StringComparison.OrdinalIgnoreCase))
                    return source;
            }

            return null;
        }

        private static object? FindById(object collection, string id)
        {
            if (!(collection is IEnumerable enumerable))
                return null;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;
                if (string.Equals(GetProperty(item, "Id") as string, id, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            return null;
        }

        private static object? FindRibbonControl()
        {
            var servicesType = Type.GetType("Bricscad.Ribbon.RibbonServices, " + AssemblyName, false);
            if (servicesType == null)
                return null;

            var paletteProperty = servicesType.GetProperty("RibbonPaletteSet", BindingFlags.Public | BindingFlags.Static);
            var palette = paletteProperty?.GetValue(null, null);
            if (palette == null)
            {
                servicesType.GetMethod("CreateRibbonPaletteSet", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                palette = paletteProperty?.GetValue(null, null);
            }

            if (palette == null)
                return null;
            if (palette.GetType().Name == "RibbonControl")
                return palette;

            var direct = GetProperty(palette, "RibbonControl");
            if (direct != null)
                return direct;

            foreach (var property in palette.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.PropertyType.Name != "RibbonControl" || property.GetIndexParameters().Length != 0)
                    continue;
                var value = property.GetValue(palette, null);
                if (value != null)
                    return value;
            }

            return null;
        }

        private static object Create(string typeName)
        {
            var type = Type.GetType(typeName + ", " + AssemblyName, true)
                       ?? throw new InvalidOperationException("BricsCAD ribbon type not found: " + typeName);
            return Activator.CreateInstance(type)
                   ?? throw new InvalidOperationException("Could not create BricsCAD ribbon type: " + typeName);
        }

        private static object? GetProperty(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target, null);

        private static void Add(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "Add"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null)
                throw new InvalidOperationException("Collection does not expose a compatible Add method.");
            method.Invoke(collection, new[] { item });
        }

        private static void Remove(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "Remove"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null)
                throw new InvalidOperationException("Collection does not expose a compatible Remove method.");
            method.Invoke(collection, new[] { item });
        }
    }
}
