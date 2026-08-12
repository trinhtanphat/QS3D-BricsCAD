using System;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticPropertyGeneratedOwnershipSmoke
    {
        internal static void Run()
        {
            True(SemanticPropertyEditPolicy.IsEditablePropertyKey("FinishCode"), "ordinary semantic property remains editable");

            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("GeneratedCurtainPanelBuildState"), "curtain-panel build state blocked");
            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("GeneratedStatus"), "unnamespaced generated state blocked");
            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("generatedstatus"), "unnamespaced generated state blocking is case insensitive");
            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("QS3D.GeneratedSolid.State"), "namespaced generated state remains blocked");
            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("GeneratedSolidHandle"), "generated handle remains blocked");
            False(SemanticPropertyEditPolicy.IsEditablePropertyKey("QS3D.PhysicalOpeningCut.State"), "physical-opening ownership remains blocked");
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
