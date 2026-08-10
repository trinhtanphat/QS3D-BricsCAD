using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

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
        }
    }

    internal static class GeneratedOwnerSlotPolicySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Register() => GeneratedOwnerSlotPolicySmoke.Run();
    }
}
