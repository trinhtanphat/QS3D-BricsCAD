using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Final host-facing icon pass for the QS3D-owned TOOL tab.
    ///
    /// BltToolRibbonAugmenter deliberately keeps the compact clean-room BLT3D-familiar
    /// vector artwork in one place. BricsCAD's Ribbon host is not reliable when those
    /// glyphs are supplied as raw DrawingImage objects, so this pass reuses the exact
    /// vector definitions and rasterizes them to deterministic 16/32 px bitmap sources.
    /// </summary>
    internal static class BltToolRibbonIconPolisher
    {
        private const string AssemblyName = "BrxMgd";
        private const string ToolTabId = "QS3D_TOOL";
        private const string ReferencePrefix = "QS3D_TOOL_BLT_";
        private const int VectorViewportSize = 32;

        private static readonly Dictionary<string, string> IconKinds =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ReferencePrefix + "PILE_LOWER", "PileDown" },
                { ReferencePrefix + "LEAN_CONCRETE", "LeanConcrete" },
                { ReferencePrefix + "EXCAVATE_FOUNDATION", "Excavation" },
                { ReferencePrefix + "SLAB_OPENING", "SlabOpening" },
                { ReferencePrefix + "MCP_SETTINGS", "McpSettings" },
                { ReferencePrefix + "MCP_DOCS", "McpDocs" },
                { ReferencePrefix + "AI_DASHBOARD", "AiDashboard" },
                { ReferencePrefix + "MCP_CONNECTION", "Connection" },
                { ReferencePrefix + "CAD_TO_BLT", "CadToBlt" }
            };

        private static bool _initialized;

        public static bool TryInitialize()
        {
            if (_initialized)
                return true;

            try
            {
                var control = FindRibbonControl();
                if (control == null)
                    return false;

                var tabs = GetProperty(control, "Tabs");
                var toolTab = tabs == null ? null : FindById(tabs, ToolTabId);
                if (toolTab == null)
                    return false;

                var panels = GetProperty(toolTab, "Panels");
                if (!(panels is IEnumerable enumerablePanels))
                    return false;

                var polished = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var panel in enumerablePanels)
                {
                    if (panel == null)
                        continue;

                    var source = GetProperty(panel, "Source");
                    var items = source == null ? null : GetProperty(source, "Items");
                    if (items != null)
                        PolishCollection(items, polished);
                }

                // TOOL owns exactly nine semantic action buttons. A partial host tree is not
                // considered initialized; the coordinator will retry instead of freezing a
                // mixed text-only / missing-image surface.
                if (polished.Count != IconKinds.Count)
                    return false;

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static void PolishCollection(object collection, HashSet<string> polished)
        {
            if (!(collection is IEnumerable enumerable))
                return;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                var id = GetProperty(item, "Id") as string;
                if (!string.IsNullOrWhiteSpace(id)
                    && IconKinds.TryGetValue(id!, out var kindName))
                {
                    if (!polished.Add(id!))
                        throw new InvalidOperationException("Duplicate TOOL ribbon button id: " + id);

                    var vector = CreateVectorIcon(kindName);
                    SetRequiredProperty(item, "Image", Rasterize(vector, 16));
                    SetRequiredProperty(item, "LargeImage", Rasterize(vector, 32));
                    SetRequiredProperty(item, "ShowImage", true);
                }

                var children = GetProperty(item, "Items");
                if (children != null)
                    PolishCollection(children, polished);
            }
        }

        private static ImageSource CreateVectorIcon(string kindName)
        {
            var augmenterType = typeof(BltToolRibbonAugmenter);
            var iconKindType = augmenterType.GetNestedType("IconKind", BindingFlags.NonPublic)
                               ?? throw new InvalidOperationException("TOOL icon kind type was not available.");
            var createMethod = augmenterType.GetMethod("CreateIcon", BindingFlags.NonPublic | BindingFlags.Static)
                               ?? throw new InvalidOperationException("TOOL vector icon factory was not available.");
            var parameters = createMethod.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != iconKindType)
                throw new InvalidOperationException("TOOL vector icon factory signature changed unexpectedly.");

            var kind = Enum.Parse(iconKindType, kindName, false);

            // Geometry.Parse path decimals are culture-sensitive in the WPF vector path.
            // BricsCAD runs under the user's Windows locale, so use invariant culture only for
            // vector construction and restore the host culture immediately afterwards.
            var thread = Thread.CurrentThread;
            var previousCulture = thread.CurrentCulture;
            try
            {
                thread.CurrentCulture = CultureInfo.InvariantCulture;
                var vector = createMethod.Invoke(null, new[] { kind }) as ImageSource;
                return vector ?? throw new InvalidOperationException("TOOL vector icon factory returned no image.");
            }
            finally
            {
                thread.CurrentCulture = previousCulture;
            }
        }

        private static ImageSource Rasterize(ImageSource vector, int pixelSize)
        {
            if (pixelSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(pixelSize));

            var visual = new DrawingVisual();
            using (var drawing = visual.RenderOpen())
            {
                if (vector is DrawingImage drawingImage && drawingImage.Drawing != null)
                {
                    var scale = pixelSize / (double)VectorViewportSize;
                    drawing.PushTransform(new ScaleTransform(scale, scale));
                    drawing.DrawDrawing(drawingImage.Drawing);
                    drawing.Pop();
                }
                else
                {
                    drawing.DrawImage(vector, new Rect(0, 0, pixelSize, pixelSize));
                }
            }

            var bitmap = new RenderTargetBitmap(
                pixelSize,
                pixelSize,
                96,
                96,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
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

        private static object? GetProperty(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target, null);

        private static void SetRequiredProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite)
                throw new InvalidOperationException(target.GetType().Name + "." + name + " is not writable.");
            if (!property.PropertyType.IsInstanceOfType(value) && property.PropertyType != value.GetType())
                throw new InvalidOperationException(target.GetType().Name + "." + name + " rejected " + value.GetType().Name + ".");

            property.SetValue(target, value, null);
        }
    }
}