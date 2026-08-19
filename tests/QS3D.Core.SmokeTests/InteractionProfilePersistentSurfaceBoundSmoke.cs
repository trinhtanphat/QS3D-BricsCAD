using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class InteractionProfilePersistentSurfaceBoundSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ExactBoundPreservesOrder();
            ThirdSurfaceFailsBeforeFourthMove();
        }

        private static void ExactBoundPreservesOrder()
        {
            var profile = CreateProfile(new[]
            {
                InteractionSurface.PrimaryInspector,
                InteractionSurface.SecondaryInspector
            });

            if (profile.PersistentSurfaces.Count != 2)
                throw new InvalidOperationException("Exact two-surface input must remain accepted.");
            if (profile.PersistentSurfaces[0] != InteractionSurface.PrimaryInspector ||
                profile.PersistentSurfaces[1] != InteractionSurface.SecondaryInspector)
                throw new InvalidOperationException("Persistent surface snapshot must preserve source order.");
        }

        private static void ThirdSurfaceFailsBeforeFourthMove()
        {
            var source = new ThreeSurfaceProbe();
            try
            {
                CreateProfile(source);
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(
                    ex.Message,
                    "Normal Workspace interaction profiles support at most two persistent surfaces.",
                    StringComparison.Ordinal))
                    throw new InvalidOperationException("Persistent surface overflow must keep the max-two diagnostic.", ex);

                if (source.OverConsumed)
                    throw new InvalidOperationException("Persistent surface overflow must fail before requesting a fourth element.");
                return;
            }

            throw new InvalidOperationException("A third persistent surface must fail closed.");
        }

        private static InteractionProfile CreateProfile(IEnumerable<InteractionSurface> persistentSurfaces)
        {
            return new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                Array.Empty<CreateRecipeDescriptor>(),
                null,
                persistentSurfaces,
                FeatureCapability.None);
        }

        private sealed class ThreeSurfaceProbe : IEnumerable<InteractionSurface>
        {
            public bool OverConsumed { get; private set; }

            public IEnumerator<InteractionSurface> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<InteractionSurface>
            {
                private readonly ThreeSurfaceProbe _owner;
                private int _moveCount;

                public Enumerator(ThreeSurfaceProbe owner)
                {
                    _owner = owner;
                }

                public InteractionSurface Current
                {
                    get
                    {
                        if (_moveCount == 1) return InteractionSurface.PrimaryInspector;
                        if (_moveCount == 2) return InteractionSurface.SecondaryInspector;
                        if (_moveCount == 3) return InteractionSurface.PrimaryInspector;
                        throw new InvalidOperationException("Enumerator has no current surface.");
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _moveCount++;
                    if (_moveCount <= 3) return true;
                    _owner.OverConsumed = true;
                    throw new InvalidOperationException("Persistent surface source was consumed after the max-two contract was already violated.");
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
