using System;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticPropertyPhysicalOpeningOwnershipSmoke
    {
        internal static void Run()
        {
            True(SemanticPropertyEditPolicy.IsEditablePropertyKey("FinishCode"), "ordinary semantic property remains editable");

            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("PhysicalOpeningCut.State"), "legacy physical opening state remains blocked");
            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("QS3D.PhysicalOpeningCut.State"), "namespaced physical opening state blocked");
            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("QS3D.PhysicalOpeningCut.Fingerprint"), "namespaced physical opening fingerprint blocked");
            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("QS3D.PhysicalOpeningCut.Targets"), "namespaced physical opening targets blocked");
            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("qs3d.physicalopeningcut.state"), "namespaced physical opening blocking is case insensitive");

            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("GeneratedSolidHandle"), "handle state remains blocked");
            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("QS3D.GeneratedSolid.State"), "generated state remains blocked");
            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("FamilyId"), "identity reference remains blocked");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new InvalidOperationException(label + ": expected false.");
        }
    }
}
