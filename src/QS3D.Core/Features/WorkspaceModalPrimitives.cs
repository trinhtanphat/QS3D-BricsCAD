using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.Features
{
    public enum WorkspaceModalKind
    {
        RecipeChooser,
        SchemaForm,
        Confirmation,
        ValidationError,
        Wizard
    }

    public enum WorkspaceModalOutcomeKind
    {
        Accepted,
        Cancelled
    }

    public sealed class WorkspaceModalLayoutPolicy
    {
        public WorkspaceModalLayoutPolicy(
            double maxWidthDip = 720d,
            double maxHeightRatio = 0.85d,
            double minimumViewportWidthDip = 320d,
            double minimumViewportHeightDip = 240d)
        {
            if (maxWidthDip <= 0d || double.IsNaN(maxWidthDip) || double.IsInfinity(maxWidthDip))
                throw new ArgumentOutOfRangeException(nameof(maxWidthDip));
            if (maxHeightRatio <= 0d || maxHeightRatio > 1d || double.IsNaN(maxHeightRatio))
                throw new ArgumentOutOfRangeException(nameof(maxHeightRatio));
            if (minimumViewportWidthDip <= 0d || minimumViewportHeightDip <= 0d)
                throw new ArgumentOutOfRangeException("Minimum viewport dimensions must be positive.");

            MaxWidthDip = maxWidthDip;
            MaxHeightRatio = maxHeightRatio;
            MinimumViewportWidthDip = minimumViewportWidthDip;
            MinimumViewportHeightDip = minimumViewportHeightDip;
        }

        public double MaxWidthDip { get; }
        public double MaxHeightRatio { get; }
        public double MinimumViewportWidthDip { get; }
        public double MinimumViewportHeightDip { get; }
        public bool IsScrollable => true;

        public WorkspaceModalViewport Resolve(double availableWidthDip, double availableHeightDip)
        {
            if (availableWidthDip <= 0d || availableHeightDip <= 0d)
                throw new ArgumentOutOfRangeException("Available viewport dimensions must be positive.");

            var width = Math.Min(MaxWidthDip, Math.Max(MinimumViewportWidthDip, availableWidthDip));
            var height = Math.Max(MinimumViewportHeightDip, availableHeightDip * MaxHeightRatio);
            height = Math.Min(availableHeightDip, height);
            return new WorkspaceModalViewport(width, height, IsScrollable);
        }
    }

    public sealed class WorkspaceModalViewport
    {
        internal WorkspaceModalViewport(double widthDip, double heightDip, bool isScrollable)
        {
            WidthDip = widthDip;
            HeightDip = heightDip;
            IsScrollable = isScrollable;
        }

        public double WidthDip { get; }
        public double HeightDip { get; }
        public bool IsScrollable { get; }
    }

    public sealed class WorkspaceModalDescriptor
    {
        private readonly ReadOnlyCollection<string> _recipeIds;
        private readonly ReadOnlyCollection<string> _wizardSteps;

        public WorkspaceModalDescriptor(
            WorkspaceModalKind kind,
            string contextKey,
            string title,
            string? schemaKey = null,
            IEnumerable<string>? recipeIds = null,
            IEnumerable<string>? wizardSteps = null,
            bool isDestructive = false,
            string? defaultFocusKey = null)
        {
            if (string.IsNullOrWhiteSpace(contextKey)) throw new ArgumentException("Context key cannot be blank.", nameof(contextKey));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Modal title cannot be blank.", nameof(title));

            Kind = kind;
            ContextKey = contextKey.Trim();
            Title = title.Trim();
            SchemaKey = string.IsNullOrWhiteSpace(schemaKey) ? null : schemaKey.Trim();
            IsDestructive = isDestructive;
            DefaultFocusKey = string.IsNullOrWhiteSpace(defaultFocusKey) ? null : defaultFocusKey.Trim();
            _recipeIds = Snapshot(recipeIds, nameof(recipeIds));
            _wizardSteps = Snapshot(wizardSteps, nameof(wizardSteps));

            ValidateShape();
        }

        public WorkspaceModalKind Kind { get; }
        public string ContextKey { get; }
        public string Title { get; }
        public string? SchemaKey { get; }
        public bool IsDestructive { get; }
        public string? DefaultFocusKey { get; }
        public IReadOnlyList<string> RecipeIds => _recipeIds;
        public IReadOnlyList<string> WizardSteps => _wizardSteps;
        public bool EnterAccepts => Kind != WorkspaceModalKind.ValidationError;
        public bool EscapeCancels => true;

        private void ValidateShape()
        {
            if (Kind == WorkspaceModalKind.RecipeChooser && (_recipeIds.Count < 2 || _recipeIds.Count > 5))
                throw new ArgumentException("Recipe chooser must expose between 2 and 5 choices.", nameof(RecipeIds));
            if (Kind == WorkspaceModalKind.SchemaForm && string.IsNullOrWhiteSpace(SchemaKey))
                throw new ArgumentException("Schema form requires a schema key.", nameof(SchemaKey));
            if (Kind == WorkspaceModalKind.Wizard && _wizardSteps.Count < 2)
                throw new ArgumentException("Wizard requires at least two named steps.", nameof(WizardSteps));
            if (Kind != WorkspaceModalKind.Confirmation && IsDestructive)
                throw new ArgumentException("Only confirmation dialogs may be destructive.", nameof(IsDestructive));
        }

        private static ReadOnlyCollection<string> Snapshot(IEnumerable<string>? values, string parameterName)
        {
            var result = new List<string>();
            foreach (var value in values ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Modal option identifiers cannot be blank.", parameterName);
                var normalized = value.Trim();
                if (result.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                    throw new ArgumentException("Modal option identifiers must be unique.", parameterName);
                result.Add(normalized);
            }
            return new ReadOnlyCollection<string>(result);
        }
    }

    public sealed class WorkspaceModalOutcome
    {
        internal WorkspaceModalOutcome(WorkspaceModalOutcomeKind kind, string contextKey, object? value)
        {
            Kind = kind;
            ContextKey = contextKey;
            Value = value;
        }

        public WorkspaceModalOutcomeKind Kind { get; }
        public string ContextKey { get; }
        public object? Value { get; }
    }

    public sealed class WorkspaceModalSession
    {
        private readonly WorkspaceModalHost _host;

        internal WorkspaceModalSession(WorkspaceModalHost host, WorkspaceModalDescriptor descriptor)
        {
            _host = host;
            Descriptor = descriptor;
            IsOpen = true;
        }

        public WorkspaceModalDescriptor Descriptor { get; }
        public bool IsOpen { get; private set; }

        public WorkspaceModalOutcome Accept(object? value = null)
        {
            EnsureOpen();
            if (!Descriptor.EnterAccepts && value == null)
                throw new InvalidOperationException("This modal requires an explicit action rather than default acceptance.");
            return Close(WorkspaceModalOutcomeKind.Accepted, value);
        }

        public WorkspaceModalOutcome Cancel()
        {
            EnsureOpen();
            return Close(WorkspaceModalOutcomeKind.Cancelled, null);
        }

        private WorkspaceModalOutcome Close(WorkspaceModalOutcomeKind kind, object? value)
        {
            IsOpen = false;
            var outcome = new WorkspaceModalOutcome(kind, Descriptor.ContextKey, value);
            _host.Release(this);
            return outcome;
        }

        private void EnsureOpen()
        {
            if (!IsOpen)
                throw new InvalidOperationException("Modal session is already closed.");
        }
    }

    public sealed class WorkspaceModalHost
    {
        private WorkspaceModalSession? _active;

        public WorkspaceModalHost(WorkspaceModalLayoutPolicy? layoutPolicy = null)
        {
            LayoutPolicy = layoutPolicy ?? new WorkspaceModalLayoutPolicy();
        }

        public WorkspaceModalLayoutPolicy LayoutPolicy { get; }
        public WorkspaceModalSession? ActiveSession => _active;
        public bool HasBlockingModal => _active != null;

        public WorkspaceModalSession Open(WorkspaceModalDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            if (_active != null)
                throw new InvalidOperationException("Workspace already has an active blocking modal session.");

            _active = new WorkspaceModalSession(this, descriptor);
            return _active;
        }

        internal void Release(WorkspaceModalSession session)
        {
            if (!ReferenceEquals(_active, session))
                throw new InvalidOperationException("Only the active Workspace modal session may be released.");
            _active = null;
        }
    }
}
