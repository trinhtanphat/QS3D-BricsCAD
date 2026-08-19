using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace QS3D.Core.Features
{
    public sealed class RoomFinishHostContext
    {
        public RoomFinishHostContext(string roomId, bool isDirty = false, bool isValid = true)
        {
            RoomId = NormalizeRequired(roomId, nameof(roomId));
            IsDirty = isDirty;
            IsValid = isValid;
        }

        public string RoomId { get; }
        public bool IsDirty { get; }
        public bool IsValid { get; }

        private static string NormalizeRequired(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Room host id cannot be blank.", paramName);
            return value.Trim();
        }
    }

    public sealed class RoomFinishRegenerationHandoff
    {
        internal RoomFinishRegenerationHandoff(FeatureId featureId, string roomId, string recipeId)
        {
            FeatureId = featureId;
            RoomId = roomId;
            RecipeId = recipeId;
        }

        public FeatureId FeatureId { get; }
        public string RoomId { get; }
        public string RecipeId { get; }
        public bool RequiresRegeneration => true;
    }

    public sealed class RoomFinishCreateSession
    {
        private readonly RoomFinishHostContext _host;
        private readonly AddCreateStateMachine _machine;

        internal RoomFinishCreateSession(FeatureDescriptor feature, RoomFinishHostContext host)
        {
            Feature = feature ?? throw new ArgumentNullException(nameof(feature));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _machine = new AddCreateStateMachine(feature);
        }

        public FeatureDescriptor Feature { get; }
        public RoomFinishHostContext Host => _host;
        public AddCreateState State => _machine.State;
        public AddCreateDirective Directive => _machine.Directive;

        public AddCreateDirective Begin(string? recipeId = null)
        {
            EnsureHostUsable();
            return _machine.Begin(recipeId);
        }

        public AddCreateDirective SubmitForm(IEnumerable<KeyValuePair<string, string>> values)
        {
            EnsureHostUsable();
            return _machine.SubmitForm(values, request => RoomFinishInteractionProfiles.ValidateForm(request));
        }

        public AddCreateDirective SubmitCadInput(object cadInput)
        {
            EnsureHostUsable();
            return _machine.SubmitCadInput(cadInput);
        }

        public AddCreateRequest GetCreateRequest()
        {
            EnsureHostUsable();
            return _machine.GetCreateRequest();
        }

        public RoomFinishRegenerationHandoff CompleteCreate()
        {
            var request = GetCreateRequest();
            _machine.CompleteCreate();
            return new RoomFinishRegenerationHandoff(request.FeatureId, _host.RoomId, request.Recipe.Id);
        }

        public void Cancel() => _machine.Cancel();

        private void EnsureHostUsable()
        {
            if (!_host.IsValid)
                throw new InvalidOperationException("Selected Room host is no longer valid. Select a valid Room and retry.");
            if (_host.IsDirty)
                throw new InvalidOperationException("Selected Room host has pending changes. Regenerate the Room before creating finishes.");
        }
    }

    public static class RoomFinishInteractionProfiles
    {
        public static readonly FeatureId FloorFinishId = new FeatureId("room-finish.floor");
        public static readonly FeatureId WaterproofingId = new FeatureId("room-finish.waterproofing");
        public static readonly FeatureId SkirtingId = new FeatureId("room-finish.skirting");

        public static FeatureRegistry CreateRegistry()
        {
            return new FeatureRegistry(new[]
            {
                CreateFloorFinish(),
                CreateWaterproofing(),
                CreateSkirting()
            });
        }

        public static RoomFinishCreateSession Start(FeatureId featureId, RoomFinishHostContext host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            var feature = CreateRegistry().GetRequired(featureId);
            return new RoomFinishCreateSession(feature, host);
        }

        public static bool ValidateForm(AddCreateRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.FormValues.Count == 0) return true;

            if (request.FormValues.TryGetValue("material", out var material) && string.IsNullOrWhiteSpace(material))
                return false;
            if (request.FormValues.TryGetValue("thickness", out var thickness) && !IsPositiveFinite(thickness))
                return false;
            if (request.FormValues.TryGetValue("height", out var height) && !IsPositiveFinite(height))
                return false;
            if (request.FormValues.TryGetValue("scope", out var scope) && string.IsNullOrWhiteSpace(scope))
                return false;
            if (request.FormValues.TryGetValue("profile", out var profile) && string.IsNullOrWhiteSpace(profile))
                return false;

            return true;
        }

        private static FeatureDescriptor CreateFloorFinish()
        {
            var profile = new InteractionProfile(
                FeatureOnSelectBehavior.SelectAndRefresh,
                new[]
                {
                    new CreateRecipeDescriptor("floor-finish.from-room", CreateInputMode.Direct),
                    new CreateRecipeDescriptor("floor-finish.material-thickness", CreateInputMode.FormThenCreate, "ProjectFamilyQuickSchema.FloorFinish")
                },
                "floor-finish.from-room",
                new[] { InteractionSurface.PrimaryInspector, InteractionSurface.SecondaryInspector },
                FeatureCapability.Create | FeatureCapability.EditParameters | FeatureCapability.Material | FeatureCapability.Quantity | FeatureCapability.Regenerate,
                allowsModal: true,
                propertySchemaKey: "ProjectFamilyQuickSchema.FloorFinish",
                dependencyPolicyKey: "RoomHost.RequiredClean",
                semanticMappingKey: "ElementCategory.FloorFinish");
            return new FeatureDescriptor(FloorFinishId, "room-finish", 10, "Feature.FloorFinish", profile);
        }

        private static FeatureDescriptor CreateWaterproofing()
        {
            var profile = new InteractionProfile(
                FeatureOnSelectBehavior.SelectAndRefresh,
                new[]
                {
                    new CreateRecipeDescriptor("waterproofing.form-pick", CreateInputMode.FormThenPick, "ProjectFamilyQuickSchema.Waterproofing"),
                    new CreateRecipeDescriptor("waterproofing.pick-form", CreateInputMode.PickThenForm, "ProjectFamilyQuickSchema.Waterproofing")
                },
                "waterproofing.form-pick",
                new[] { InteractionSurface.PrimaryInspector, InteractionSurface.SecondaryInspector },
                FeatureCapability.Create | FeatureCapability.EditParameters | FeatureCapability.Material | FeatureCapability.Quantity | FeatureCapability.Regenerate,
                allowsModal: true,
                propertySchemaKey: "ProjectFamilyQuickSchema.Waterproofing",
                dependencyPolicyKey: "RoomHost.RequiredClean",
                semanticMappingKey: "ElementCategory.Waterproofing");
            return new FeatureDescriptor(WaterproofingId, "room-finish", 20, "Feature.Waterproofing", profile);
        }

        private static FeatureDescriptor CreateSkirting()
        {
            var profile = new InteractionProfile(
                FeatureOnSelectBehavior.SelectAndRefresh,
                new[]
                {
                    new CreateRecipeDescriptor("skirting.from-room-perimeter", CreateInputMode.Direct),
                    new CreateRecipeDescriptor("skirting.profile-height-material", CreateInputMode.FormThenCreate, "ProjectFamilyQuickSchema.Skirting")
                },
                "skirting.from-room-perimeter",
                new[] { InteractionSurface.PrimaryInspector },
                FeatureCapability.Create | FeatureCapability.EditParameters | FeatureCapability.Material | FeatureCapability.Quantity | FeatureCapability.Regenerate,
                allowsModal: true,
                propertySchemaKey: "ProjectFamilyQuickSchema.Skirting",
                dependencyPolicyKey: "RoomHost.RequiredClean",
                semanticMappingKey: "ElementCategory.Skirting");
            return new FeatureDescriptor(SkirtingId, "room-finish", 30, "Feature.Skirting", profile);
        }

        private static bool IsPositiveFinite(string value)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                return false;
            return !double.IsNaN(parsed) && !double.IsInfinity(parsed) && parsed > 0d;
        }
    }
}
