using System;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Converts the final clean-room NHẬN DẠNG artwork into exact-size frozen bitmaps.
    /// BricsCAD's Ribbon is not reliable with a raw DrawingImage as a button ImageSource;
    /// the host-facing bitmap form mirrors the proven RibbonIconFactory/QS3D brand path.
    /// </summary>
    internal static class BltRecognitionBitmapFinalizer
    {
        private const string AssemblyName = "BrxMgd";
        private const string RecognitionTabId = "QS3D_RECOGNIZE";
        private const int ExpectedButtonCount = 8;

        public static bool TryInitialize()
        {
            try
            {
                var control = FindRibbonControl();
                if (control == null)
                    return false;

                var tabs = GetProperty(control, "Tabs");
                var tab = tabs == null ? null : FindById(tabs, RecognitionTabId);
                if (tab == null)
                    return false;

                var panels = GetProperty(tab, "Panels");
                if (!(panels is IEnumerable enumerablePanels))
                    return false;

                var finalized = 0;
                foreach (var panel in enumerablePanels)
                {
                    if (panel == null)
                        continue;

                    var source = GetProperty(panel, "Source");
                    var items = source == null ? null : GetProperty(source, "Items");
                    if (items != null)
                        FinalizeCollection(items, ref finalized);
                }

                return finalized == ExpectedButtonCount;
            }
            catch
            {
                return false;
            }
        }

        private static void FinalizeCollection(object collection, ref int finalized)
        {
            if (!(collection is IEnumerable enumerable))
                return;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                var id = GetProperty(item, "Id") as string;
                if (!string.IsNullOrWhiteSpace(id) && IsRecognitionButton(id!))
                {
                    var image = GetProperty(item, "Image") as ImageSource;
                    var largeImage = GetProperty(item, "LargeImage") as ImageSource;
                    var source = image ?? largeImage;
                    if (source == null)
                        continue;

                    // Keep independent native target sizes: reusing one source for both Ribbon
                    // slots is precisely the host-scaling path this finalizer is designed to avoid.
                    var smallBitmap = Rasterize(source, 16);
                    var largeBitmap = Rasterize(source, 32);
                    SetProperty(item, "Image", smallBitmap);
                    SetProperty(item, "LargeImage", largeBitmap);
                    SetProperty(item, "ShowImage", true);

                    if (HasExactBitmap(item, "Image", 16) && HasExactBitmap(item, "LargeImage", 32))
                        finalized++;
                }

                var children = GetProperty(item, "Items");
                if (children != null)
                    FinalizeCollection(children, ref finalized);
            }
        }

        private static ImageSource Rasterize(ImageSource source, int pixelSize)
        {
            if (source is BitmapSource bitmap &&
                bitmap.PixelWidth == pixelSize &&
                bitmap.PixelHeight == pixelSize &&
                bitmap.IsFrozen)
            {
                return bitmap;
            }

            var visual = new DrawingVisual();
            using (var drawing = visual.RenderOpen())
            {
                drawing.DrawImage(source, new Rect(0, 0, pixelSize, pixelSize));
            }

            var rendered = new RenderTargetBitmap(
                pixelSize,
                pixelSize,
                96,
                96,
                PixelFormats.Pbgra32);
            rendered.Render(visual);
            rendered.Freeze();
            return rendered;
        }

        private static bool HasExactBitmap(object item, string propertyName, int pixelSize)
        {
            var bitmap = GetProperty(item, propertyName) as BitmapSource;
            return bitmap != null &&
                   bitmap.PixelWidth == pixelSize &&
                   bitmap.PixelHeight == pixelSize &&
                   bitmap.IsFrozen;
        }

        private static bool IsRecognitionButton(string id)
        {
            switch (id)
            {
                case "QS3D_RECOGNIZE_BLT_RESTORE":
                case "QS3D_RECOGNIZE_BLT_TEXT":
                case "QS3D_RECOGNIZE_BLT_OPTIONS":
                case "QS3D_RECOGNIZE_BLT_TABLE":
                case "QS3D_RECOGNIZE_BLT_BOUNDARY":
                case "QS3D_RECOGNIZE_BLT_LABEL":
                case "QS3D_RECOGNIZE_BLT_AUTO":
                case "QS3D_RECOGNIZE_BLT_VALIDATE":
                    return true;
                default:
                    return false;
            }
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

        private static object? GetProperty(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target, null);

        private static void SetProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite)
                return;
            if (property.PropertyType.IsInstanceOfType(value) || property.PropertyType == value.GetType())
                property.SetValue(target, value, null);
        }
    }
}
