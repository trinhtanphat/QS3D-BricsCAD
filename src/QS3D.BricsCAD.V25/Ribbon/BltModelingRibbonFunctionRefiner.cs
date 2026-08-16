using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Pins the functional contract of every owner-reference MODELING button after the visual
    /// surface is built. This prevents a label/icon parity change from silently leaving a stale,
    /// blank or semantically weaker CommandParameter behind.
    /// </summary>
    internal static class BltModelingRibbonFunctionRefiner
    {
        private const string AssemblyName = "BrxMgd";
        private const string ModelingTabId = "QS3D_MODELING";
        private const string ButtonPrefix = "QS3D_MODELING_BLT_";

        private static readonly IReadOnlyDictionary<string, string> ExpectedRoutes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ButtonPrefix + "MATERIAL"] = "_.MATERIALS",
                [ButtonPrefix + "STEEL_PROFILE"] = "_.BIMPROFILES",
                [ButtonPrefix + "CREATE_DETAIL"] = "_.BIMCREATEDETAIL",
                [ButtonPrefix + "PLANE_XY"] = "_.UCS _World",
                [ButtonPrefix + "LINE"] = "_.LINE",
                [ButtonPrefix + "POLYLINE"] = "_.PLINE",
                [ButtonPrefix + "RECTANGLE"] = "_.RECTANG",
                [ButtonPrefix + "CIRCLE"] = "_.CIRCLE",
                [ButtonPrefix + "ARC"] = "_.ARC",
                [ButtonPrefix + "JOIN_POLYLINE"] = "_.JOIN",
                [ButtonPrefix + "OFFSET"] = "_.OFFSET",
                [ButtonPrefix + "MOVE"] = "_.MOVE",
                [ButtonPrefix + "COPY"] = "_.COPY",
                [ButtonPrefix + "MOVE_Z"] = "QS3DMOVEZ",
                [ButtonPrefix + "EXTRUDE"] = "_.EXTRUDE",
                [ButtonPrefix + "SWEEP"] = "_.SWEEP",
                [ButtonPrefix + "LOFT"] = "_.LOFT",
                [ButtonPrefix + "ATTACH_FAMILY"] = "QS3DFAMILIES",
                [ButtonPrefix + "UNION"] = "_.UNION",
                [ButtonPrefix + "SUBTRACT"] = "_.SUBTRACT",
                [ButtonPrefix + "INTERSECT"] = "_.INTERSECT",
            };

        private static bool _initialized;

        public static bool TryInitialize()
        {
            if (_initialized)
                return true;

            try
            {
                var ribbon = FindRibbonControl();
                if (ribbon == null)
                    return false;

                var tabs = GetProperty(ribbon, "Tabs");
                var modeling = tabs == null ? null : FindById(tabs, ModelingTabId);
                if (modeling == null)
                    return false;

                var panels = GetProperty(modeling, "Panels");
                if (panels == null)
                    return false;

                var buttons = FindOwnedButtons(panels);
                if (buttons.Count != ExpectedRoutes.Count)
                    return false;

                foreach (var expected in ExpectedRoutes)
                {
                    if (!buttons.TryGetValue(expected.Key, out var button))
                        return false;
                    if (GetProperty(button, "CommandHandler") == null)
                        return false;

                    SetProperty(button, "CommandParameter", expected.Value);
                    if (!string.Equals(
                            GetProperty(button, "CommandParameter") as string,
                            expected.Value,
                            StringComparison.Ordinal))
                        return false;
                }

                // The reference label says movement is along Z. Route that button to a dedicated
                // selection-aware helper instead of the old unrestricted MOVE + manual-coordinate
                // tooltip, while the helper itself delegates mutation back to native MOVE.
                var moveZ = buttons[ButtonPrefix + "MOVE_Z"];
                const string moveZHelp = "Di chuyển lựa chọn theo trục Z của UCS hiện tại; nhập độ dịch chuyển dương hoặc âm.";
                SetProperty(moveZ, "Description", moveZHelp);
                SetProperty(moveZ, "ToolTip", moveZHelp);

                // Family assignment remains routed through the production modeless Family Manager,
                // which owns CRUD/properties/inheritance-safe semantic assignment for the active DWG.
                var family = buttons[ButtonPrefix + "ATTACH_FAMILY"];
                const string familyHelp = "Mở Family Manager để gắn/chỉnh Family, Type và semantic assignment cho bản vẽ hiện tại.";
                SetProperty(family, "Description", familyHelp);
                SetProperty(family, "ToolTip", familyHelp);

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static Dictionary<string, object> FindOwnedButtons(object panels)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in EnumerateRibbonItems(panels))
            {
                if (!string.Equals(item.GetType().Name, "RibbonButton", StringComparison.Ordinal))
                    continue;

                var id = GetProperty(item, "Id") as string;
                if (string.IsNullOrWhiteSpace(id) || !id.StartsWith(ButtonPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (result.ContainsKey(id))
                    return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                result[id] = item;
            }

            return result;
        }

        private static IEnumerable<object> EnumerateRibbonItems(object root)
        {
            if (!(root is IEnumerable enumerable))
                yield break;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                yield return item;

                var source = GetProperty(item, "Source");
                var sourceItems = source == null ? null : GetProperty(source, "Items");
                if (sourceItems != null)
                {
                    foreach (var nested in EnumerateRibbonItems(sourceItems))
                        yield return nested;
                }

                var childItems = GetProperty(item, "Items");
                if (childItems != null)
                {
                    foreach (var nested in EnumerateRibbonItems(childItems))
                        yield return nested;
                }
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
