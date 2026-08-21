using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.Features
{
    public enum AddCreateState
    {
        Selected,
        Preparing,
        WaitingForCadInput,
        Creating,
        Created,
        Cancelled,
        Error
    }

    public enum AddCreateDirectiveKind
    {
        None,
        ChooseRecipe,
        ShowForm,
        RequestCadInput,
        Create
    }

    public sealed class AddCreateDirective
    {
        internal AddCreateDirective(AddCreateDirectiveKind kind, string? schemaKey = null)
        {
            Kind = kind;
            SchemaKey = schemaKey;
        }

        public AddCreateDirectiveKind Kind { get; }
        public string? SchemaKey { get; }
    }

    public sealed class AddCreateRequest
    {
        internal AddCreateRequest(
            FeatureId featureId,
            CreateRecipeDescriptor recipe,
            IReadOnlyDictionary<string, string> formValues,
            object? cadInput)
        {
            FeatureId = featureId;
            Recipe = recipe;
            FormValues = formValues;
            CadInput = cadInput;
        }

        public FeatureId FeatureId { get; }
        public CreateRecipeDescriptor Recipe { get; }
        public IReadOnlyDictionary<string, string> FormValues { get; }
        public object? CadInput { get; }
    }

    public sealed class AddCreateStateMachine
    {
        // Current create schemas are compact property forms. Keep a deliberately generous
        // ceiling so normal UI schemas remain unaffected while arbitrary/lazy callers cannot
        // turn form ingestion into an unbounded enumeration/allocation path.
        internal const int MaximumFormFields = 64;

        private readonly FeatureDescriptor _feature;
        private readonly Dictionary<string, string> _formValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private CreateRecipeDescriptor? _recipe;
        private object? _cadInput;
        private bool _formValidated;
        private bool _createHandedOff;

        public AddCreateStateMachine(FeatureDescriptor feature)
        {
            _feature = feature ?? throw new ArgumentNullException(nameof(feature));
            if ((_feature.InteractionProfile.Capabilities & FeatureCapability.Create) == 0)
                throw new InvalidOperationException("Feature does not support create sessions.");

            State = AddCreateState.Selected;
            Directive = new AddCreateDirective(AddCreateDirectiveKind.None);
        }

        public AddCreateState State { get; private set; }
        public AddCreateDirective Directive { get; private set; }
        public Exception? Error { get; private set; }
        public bool IsTerminal => State == AddCreateState.Created || State == AddCreateState.Cancelled || State == AddCreateState.Error;
        public bool CreateWasHandedOff => _createHandedOff;
        public bool RequiresRollback => State == AddCreateState.Error && _createHandedOff;
        public string? SelectedRecipeId => _recipe?.Id;

        public AddCreateDirective Begin(string? recipeId = null)
        {
            EnsureState(AddCreateState.Selected);
            State = AddCreateState.Preparing;

            var profile = _feature.InteractionProfile;
            if (!string.IsNullOrWhiteSpace(recipeId))
                return SelectRecipeCore(recipeId!);

            if (profile.Recipes.Count == 1)
                return SelectRecipeCore(profile.Recipes[0].Id);

            var primary = profile.Recipes.FirstOrDefault(x => string.Equals(x.Id, profile.PrimaryRecipeId, StringComparison.OrdinalIgnoreCase));
            if (primary != null && primary.InputMode != CreateInputMode.ChooseRecipe)
                return SelectRecipeCore(primary.Id);

            Directive = new AddCreateDirective(AddCreateDirectiveKind.ChooseRecipe);
            return Directive;
        }

        public AddCreateDirective SelectRecipe(string recipeId)
        {
            EnsurePreparing();
            if (Directive.Kind != AddCreateDirectiveKind.ChooseRecipe)
                throw new InvalidOperationException("The create session is not waiting for recipe selection.");
            return SelectRecipeCore(recipeId);
        }

        public AddCreateDirective SubmitForm(IEnumerable<KeyValuePair<string, string>> values, Func<AddCreateRequest, bool> validate)
        {
            EnsurePreparing();
            if (Directive.Kind != AddCreateDirectiveKind.ShowForm || _recipe == null)
                throw new InvalidOperationException("The create session is not waiting for form input.");
            if (validate == null) throw new ArgumentNullException(nameof(validate));

            var pendingValues = SnapshotFormValues(values ?? Enumerable.Empty<KeyValuePair<string, string>>());
            var request = BuildRequest(pendingValues);
            if (!validate(request))
            {
                _formValues.Clear();
                _formValidated = false;
                throw new InvalidOperationException("Create form validation failed before mutation handoff.");
            }

            _formValues.Clear();
            foreach (var item in pendingValues)
                _formValues.Add(item.Key, item.Value);

            _formValidated = true;
            switch (_recipe.InputMode)
            {
                case CreateInputMode.FormThenCreate:
                    return HandoffCreate();
                case CreateInputMode.FormThenPick:
                case CreateInputMode.Wizard:
                    State = AddCreateState.WaitingForCadInput;
                    Directive = new AddCreateDirective(AddCreateDirectiveKind.RequestCadInput);
                    return Directive;
                case CreateInputMode.PickThenForm:
                    return HandoffCreate();
                default:
                    throw new InvalidOperationException("Selected recipe does not accept schema form input at this stage.");
            }
        }

        public AddCreateDirective SubmitCadInput(object cadInput)
        {
            if (State != AddCreateState.WaitingForCadInput || _recipe == null)
                throw new InvalidOperationException("The create session is not waiting for CAD input.");
            if (cadInput == null) throw new ArgumentNullException(nameof(cadInput));

            _cadInput = cadInput;
            State = AddCreateState.Preparing;

            if (_recipe.InputMode == CreateInputMode.PickThenForm)
            {
                Directive = new AddCreateDirective(AddCreateDirectiveKind.ShowForm, _recipe.SchemaKey);
                return Directive;
            }

            return HandoffCreate();
        }

        public void CompleteCreate()
        {
            EnsureState(AddCreateState.Creating);
            State = AddCreateState.Created;
            Directive = new AddCreateDirective(AddCreateDirectiveKind.None);
            Error = null;
        }

        public void FailCreate(Exception error)
        {
            EnsureState(AddCreateState.Creating);
            Error = error ?? throw new ArgumentNullException(nameof(error));
            State = AddCreateState.Error;
            Directive = new AddCreateDirective(AddCreateDirectiveKind.None);
        }

        public void AcknowledgeRollback()
        {
            if (!RequiresRollback)
                throw new InvalidOperationException("The create session has no failed mutation handoff requiring rollback acknowledgement.");
            _createHandedOff = false;
        }

        public void Cancel()
        {
            if (IsTerminal)
                return;
            if (_createHandedOff)
                throw new InvalidOperationException("A create handoff must finish or fail before cancellation can complete.");

            _formValues.Clear();
            _cadInput = null;
            _formValidated = false;
            _recipe = null;
            Error = null;
            State = AddCreateState.Cancelled;
            Directive = new AddCreateDirective(AddCreateDirectiveKind.None);
        }

        public AddCreateRequest GetCreateRequest()
        {
            EnsureState(AddCreateState.Creating);
            return BuildRequest(_formValues);
        }

        private AddCreateDirective SelectRecipeCore(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId)) throw new ArgumentException("Recipe id cannot be blank.", nameof(recipeId));
            var recipe = _feature.InteractionProfile.Recipes.FirstOrDefault(x => string.Equals(x.Id, recipeId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (recipe == null) throw new KeyNotFoundException("Create recipe is not registered for feature: " + recipeId);

            _recipe = recipe;
            _formValues.Clear();
            _cadInput = null;
            _formValidated = false;
            _createHandedOff = false;

            switch (recipe.InputMode)
            {
                case CreateInputMode.Direct:
                    return HandoffCreate();
                case CreateInputMode.ChooseRecipe:
                    Directive = new AddCreateDirective(AddCreateDirectiveKind.ChooseRecipe);
                    return Directive;
                case CreateInputMode.FormThenCreate:
                case CreateInputMode.FormThenPick:
                case CreateInputMode.Wizard:
                    Directive = new AddCreateDirective(AddCreateDirectiveKind.ShowForm, recipe.SchemaKey);
                    return Directive;
                case CreateInputMode.PickThenForm:
                    State = AddCreateState.WaitingForCadInput;
                    Directive = new AddCreateDirective(AddCreateDirectiveKind.RequestCadInput);
                    return Directive;
                default:
                    throw new InvalidOperationException("Unsupported create input mode.");
            }
        }

        private AddCreateDirective HandoffCreate()
        {
            if (_recipe == null) throw new InvalidOperationException("No create recipe is selected.");
            if (_createHandedOff) throw new InvalidOperationException("Duplicate create invocation is not allowed for an active session.");
            if (_recipe.RequiresForm && !_formValidated)
                throw new InvalidOperationException("Form-driven create recipes must validate before mutation handoff.");

            _createHandedOff = true;
            State = AddCreateState.Creating;
            Directive = new AddCreateDirective(AddCreateDirectiveKind.Create);
            return Directive;
        }

        private AddCreateRequest BuildRequest(Dictionary<string, string> formValues)
        {
            if (_recipe == null) throw new InvalidOperationException("No create recipe is selected.");
            var snapshot = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(formValues, StringComparer.OrdinalIgnoreCase));
            return new AddCreateRequest(_feature.Id, _recipe, snapshot, _cadInput);
        }

        private static Dictionary<string, string> SnapshotFormValues(IEnumerable<KeyValuePair<string, string>> values)
        {
            var knownCount = SnapshotKnownCount(values);
            var snapshot = knownCount.HasValue
                ? new Dictionary<string, string>(knownCount.Value, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var observed = 0;
            foreach (var item in values)
            {
                observed++;
                if (observed > MaximumFormFields)
                    throw TooManyFormFields();
                snapshot[item.Key ?? string.Empty] = item.Value ?? string.Empty;
            }

            if (knownCount.HasValue && observed != knownCount.Value)
                throw new InvalidOperationException("Create form field count changed during enumeration.");

            return snapshot;
        }

        private static int? SnapshotKnownCount(IEnumerable<KeyValuePair<string, string>> values)
        {
            int? knownCount = null;
            if (values is ICollection<KeyValuePair<string, string>> genericCollection)
                AcceptKnownCount(genericCollection.Count, ref knownCount);
            if (values is IReadOnlyCollection<KeyValuePair<string, string>> readOnlyCollection)
                AcceptKnownCount(readOnlyCollection.Count, ref knownCount);
            if (values is System.Collections.ICollection nonGenericCollection)
                AcceptKnownCount(nonGenericCollection.Count, ref knownCount);
            return knownCount;
        }

        private static void AcceptKnownCount(int count, ref int? knownCount)
        {
            if (count < 0)
                throw new InvalidOperationException("Create form exposes an invalid negative field count.");
            if (count > MaximumFormFields)
                throw TooManyFormFields();
            if (knownCount.HasValue && knownCount.Value != count)
                throw new InvalidOperationException("Create form exposes conflicting known field counts.");
            knownCount = count;
        }

        private static InvalidOperationException TooManyFormFields()
            => new InvalidOperationException("Create form supports at most " + MaximumFormFields + " fields.");

        private void EnsurePreparing()
        {
            if (State != AddCreateState.Preparing)
                throw new InvalidOperationException("Expected create session state Preparing but was " + State + ".");
        }

        private void EnsureState(AddCreateState expected)
        {
            if (State != expected)
                throw new InvalidOperationException("Expected create session state " + expected + " but was " + State + ".");
        }
    }
}
