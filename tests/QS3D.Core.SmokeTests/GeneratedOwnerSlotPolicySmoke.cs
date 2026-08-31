using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedOwnerSlotPolicySmoke
    {
        public static void Run()
        {
            var expected = new[]
            {
                "GeneratedRebarHandles",
                "GeneratedShapeRebarHandles",
                "GeneratedTieRebarHandles",
                "GeneratedBeamStirrupHandles",
                "GeneratedSlabMeshHandles",
                "GeneratedWallMeshHandles",
                "GeneratedFoundationMeshHandles"
            };
            if (!expected.SequenceEqual(GeneratedHandleOwnershipPolicy.RebarHandleKeys, StringComparer.Ordinal))
                throw new Exception("Generated rebar owner-slot policy drifted from the supported families.");
            foreach (var key in expected)
                if (!GeneratedHandleOwnershipPolicy.IsRebarOwnerSlot(key) || !GeneratedHandleOwnershipPolicy.IsOwnerSlot(key))
                    throw new Exception("Expected rebar owner slot: " + key);
            if (!GeneratedHandleOwnershipPolicy.IsOwnerSlot("GeneratedCurtainFrameHandles"))
                throw new Exception("Curtain frames must remain a generated owner slot.");
            if (GeneratedHandleOwnershipPolicy.IsRebarOwnerSlot("GeneratedCurtainFrameHandles"))
                throw new Exception("Curtain frames must not be classified as rebar ownership.");
            if (GeneratedHandleOwnershipPolicy.IsOwnerSlot("PreviewHandle"))
                throw new Exception("Non-generated preview metadata must not become an owner slot.");

            if (!GeneratedHandleOwnershipPolicy.AreSameLogicalOwnerSlots("GeneratedSolidHandle", "PhysicalOpeningCutSolidHandle"))
                throw new Exception("Generated host solid and physical opening-cut handle must remain logical aliases.");
            if (!string.Equals(GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot("PhysicalOpeningCutSolidHandle"), "GeneratedSolidHandle", StringComparison.Ordinal))
                throw new Exception("Opening-cut owner alias must canonicalize to GeneratedSolidHandle.");

            var element = new ProjectElement("W", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedSolidHandle"] = "AA";
            element.Properties["PhysicalOpeningCutSolidHandle"] = "AA";
            element.Properties["GeneratedCurtainFrameHandles"] = "AA";
            var logical = GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(element).ToList();
            if (logical.Count(x => string.Equals(x.Key, "AA", StringComparison.OrdinalIgnoreCase) && string.Equals(x.Value, "GeneratedSolidHandle", StringComparison.OrdinalIgnoreCase)) != 1)
                throw new Exception("Logical host aliases must collapse to one canonical owner entry.");
            if (logical.Count(x => string.Equals(x.Key, "AA", StringComparison.OrdinalIgnoreCase) && string.Equals(x.Value, "GeneratedCurtainFrameHandles", StringComparison.OrdinalIgnoreCase)) != 1)
                throw new Exception("A different generated owner family sharing the same handle must not be hidden by alias canonicalization.");
        }
    }

    internal static class GeneratedOwnerSlotPolicySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Register() => GeneratedOwnerSlotPolicySmoke.Run();
    }
}
