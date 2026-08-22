using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using CoreOwnershipPolicy = QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy;

namespace QS3D.BricsCAD.V25.Cad
{
    /// <summary>
    /// Prevents a native cleanup transaction from accepting generated ownership metadata
    /// that the current BricsCAD invalidator does not know how to erase. Core ownership is
    /// deliberately extensible (Generated*Handle/Generated*Handles), so every native caller
    /// must fail closed until a new owner slot has an explicit liveness + ownership + erase path.
    /// </summary>
    internal static class GeneratedNativeCleanupCoverageGuard
    {
        private const string GeneratedSolidHandleKey = "GeneratedSolidHandle";
        private const string PhysicalOpeningCutSolidHandleKey = "PhysicalOpeningCutSolidHandle";
        private const string CurtainFrameHandlesKey = "GeneratedCurtainFrameHandles";
        private const string CurtainPanelHandlesKey = "GeneratedCurtainPanelHandles";

        public static void EnsureSupported(IEnumerable<ProjectElement> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            foreach (var element in elements.Where(x => x != null))
            {
                foreach (var property in element.Properties)
                {
                    var ownerSlot = (property.Key ?? string.Empty).Trim();
                    if (ownerSlot.Length == 0 || !CoreOwnershipPolicy.IsOwnerSlot(ownerSlot) || string.IsNullOrWhiteSpace(property.Value)) continue;
                    if (IsSupportedOwnerSlot(ownerSlot)) continue;

                    throw new InvalidOperationException(
                        "Generated ownership slot '" + ownerSlot + "' on element " + element.Id +
                        " has no BricsCAD native cleanup handler. Refusing destructive invalidation before any generated entity is erased or ownership metadata is cleared.");
                }

                EnsurePhysicalOpeningAliasMatchesHostSolid(element);
            }
        }

        private static bool IsSupportedOwnerSlot(string ownerSlot)
        {
            if (string.Equals(ownerSlot, GeneratedSolidHandleKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ownerSlot, PhysicalOpeningCutSolidHandleKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ownerSlot, CurtainFrameHandlesKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ownerSlot, CurtainPanelHandlesKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ownerSlot, GridAnnotationBuilder.HandlesKey, StringComparison.OrdinalIgnoreCase))
                return true;

            return CoreOwnershipPolicy.IsRebarOwnerSlot(ownerSlot);
        }

        private static void EnsurePhysicalOpeningAliasMatchesHostSolid(ProjectElement element)
        {
            if (!element.Properties.TryGetValue(PhysicalOpeningCutSolidHandleKey, out var openingRaw) ||
                string.IsNullOrWhiteSpace(openingRaw))
                return;

            if (!element.Properties.TryGetValue(GeneratedSolidHandleKey, out var generatedRaw) ||
                string.IsNullOrWhiteSpace(generatedRaw))
                throw new InvalidOperationException(
                    PhysicalOpeningCutSolidHandleKey + " for " + element.Id +
                    " has no matching " + GeneratedSolidHandleKey + ". Refusing destructive invalidation because the physical-opening entity would not be erased by the native host-solid cleanup path.");

            var openingHandle = NormalizeSingleHandle(openingRaw, element, PhysicalOpeningCutSolidHandleKey);
            var generatedHandle = NormalizeSingleHandle(generatedRaw, element, GeneratedSolidHandleKey);
            if (!string.Equals(openingHandle, generatedHandle, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    PhysicalOpeningCutSolidHandleKey + " for " + element.Id + " does not match " + GeneratedSolidHandleKey +
                    ". Refusing destructive invalidation because the native cleanup path cannot prove both owner aliases identify the same Solid3d.");
        }

        private static string NormalizeSingleHandle(string raw, ProjectElement element, string propertyKey)
        {
            var handles = (raw ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Select(x => CadHandleService.NormalizeHexHandle(x))
                .ToArray();

            if (handles.Length != 1 || handles[0] == null)
                throw new InvalidOperationException(
                    propertyKey + " for " + element.Id +
                    " must contain exactly one valid CAD handle before destructive invalidation can proceed.");

            return handles[0]!;
        }
    }
}
