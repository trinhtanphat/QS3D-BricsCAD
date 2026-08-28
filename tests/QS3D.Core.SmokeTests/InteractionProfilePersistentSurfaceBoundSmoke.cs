using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class InteractionProfilePersistentSurfaceBoundSmoke
    {
        private const string LimitMessage = "Normal Workspace interaction profiles support at most two persistent surfaces.";

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ExactBoundPreservesOrder();
            ThirdSurfaceFailsBeforeFourthMove();
            InvalidKnownCountsFailBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
            KnownCountTraversalMismatchFailsClosed();
            HonestNonGenericKnownCountRemainsAccepted();
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
                if (!string.Equals(ex.Message, LimitMessage, StringComparison.Ordinal))
                    throw new InvalidOperationException("Persistent surface overflow must keep the max-two diagnostic.", ex);

                if (source.OverConsumed)
                    throw new InvalidOperationException("Persistent surface overflow must fail before requesting a fourth element.");
                return;
            }

            throw new InvalidOperationException("A third persistent surface must fail closed.");
        }

        private static void InvalidKnownCountsFailBeforeEnumeration()
        {
            var negative = new ReadOnlyKnownCountProbe(-1, true, InteractionSurface.PrimaryInspector);
            ExpectCountFailure(
                () => CreateProfile(negative),
                "Negative persistent-surface Count must fail before enumeration.");
            if (negative.EnumerationCount != 0)
                throw new InvalidOperationException("Negative persistent-surface Count must not invoke GetEnumerator().");

            var oversized = new ReadOnlyKnownCountProbe(3, true, InteractionSurface.PrimaryInspector);
            try
            {
                CreateProfile(oversized);
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, LimitMessage, StringComparison.Ordinal))
                    throw new InvalidOperationException("Known oversized persistent-surface Count must keep the max-two diagnostic.", ex);
                if (oversized.EnumerationCount != 0)
                    throw new InvalidOperationException("Known oversized persistent-surface Count must fail before enumeration.");
                return;
            }

            throw new InvalidOperationException("Known persistent-surface Count above two must fail closed.");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new ConflictingKnownCountProbe(1, 1, 2);
            ExpectCountFailure(
                () => CreateProfile(source),
                "Conflicting persistent-surface Count contracts must fail before enumeration.");
            if (source.EnumerationCount != 0)
                throw new InvalidOperationException("Conflicting persistent-surface Count contracts must not invoke GetEnumerator().");
        }

        private static void KnownCountTraversalMismatchFailsClosed()
        {
            var underYielding = new ReadOnlyKnownCountProbe(
                2,
                false,
                InteractionSurface.PrimaryInspector);
            ExpectCountFailure(
                () => CreateProfile(underYielding),
                "Persistent-surface Count greater than traversal cardinality must fail closed.");
            if (underYielding.EnumerationCount != 1)
                throw new InvalidOperationException("Valid known Count must enumerate once before cardinality mismatch validation.");

            var overYielding = new ReadOnlyKnownCountProbe(
                1,
                false,
                InteractionSurface.PrimaryInspector,
                InteractionSurface.SecondaryInspector);
            ExpectCountFailure(
                () => CreateProfile(overYielding),
                "Persistent-surface traversal cardinality greater than known Count must fail closed.");
            if (overYielding.EnumerationCount != 1)
                throw new InvalidOperationException("Known Count/traversal mismatch must use exactly one source enumeration.");
        }

        private static void HonestNonGenericKnownCountRemainsAccepted()
        {
            var source = new NonGenericKnownCountProbe(
                2,
                InteractionSurface.PrimaryInspector,
                InteractionSurface.SecondaryInspector);
            var profile = CreateProfile(source);

            if (source.EnumerationCount != 1)
                throw new InvalidOperationException("Honest non-generic persistent-surface input must be enumerated exactly once.");
            if (profile.PersistentSurfaces.Count != 2 ||
                profile.PersistentSurfaces[0] != InteractionSurface.PrimaryInspector ||
                profile.PersistentSurfaces[1] != InteractionSurface.SecondaryInspector)
                throw new InvalidOperationException("Honest non-generic persistent-surface Count must preserve accepted order.");
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

        private static void ExpectCountFailure(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("Count", StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException(message + " Unexpected diagnostic: " + ex.Message, ex);
            }

            throw new InvalidOperationException(message);
        }

        private sealed class ReadOnlyKnownCountProbe : IReadOnlyCollection<InteractionSurface>
        {
            private readonly InteractionSurface[] _items;
            private readonly bool _throwOnEnumeration;

            internal ReadOnlyKnownCountProbe(int count, bool throwOnEnumeration, params InteractionSurface[] items)
            {
                Count = count;
                _throwOnEnumeration = throwOnEnumeration;
                _items = items ?? Array.Empty<InteractionSurface>();
            }

            public int Count { get; }
            internal int EnumerationCount { get; private set; }

            public IEnumerator<InteractionSurface> GetEnumerator()
            {
                EnumerationCount++;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Invalid persistent-surface known Count must fail before enumeration.");
                return ((IEnumerable<InteractionSurface>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericKnownCountProbe : IEnumerable<InteractionSurface>, ICollection
        {
            private readonly int _count;
            private readonly InteractionSurface[] _items;

            internal NonGenericKnownCountProbe(int count, params InteractionSurface[] items)
            {
                _count = count;
                _items = items ?? Array.Empty<InteractionSurface>();
            }

            int ICollection.Count => _count;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int EnumerationCount { get; private set; }

            public IEnumerator<InteractionSurface> GetEnumerator()
            {
                EnumerationCount++;
                return ((IEnumerable<InteractionSurface>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection.CopyTo(Array array, int index)
            {
                for (var i = 0; i < _items.Length; i++) array.SetValue(_items[i], index + i);
            }
        }

        private sealed class ConflictingKnownCountProbe : ICollection<InteractionSurface>, IReadOnlyCollection<InteractionSurface>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal ConflictingKnownCountProbe(int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            int ICollection<InteractionSurface>.Count => _genericCount;
            int IReadOnlyCollection<InteractionSurface>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<InteractionSurface>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int EnumerationCount { get; private set; }

            public IEnumerator<InteractionSurface> GetEnumerator()
            {
                EnumerationCount++;
                throw new InvalidOperationException("Conflicting persistent-surface Count contracts must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<InteractionSurface>.Add(InteractionSurface item) => throw new NotSupportedException();
            void ICollection<InteractionSurface>.Clear() => throw new NotSupportedException();
            bool ICollection<InteractionSurface>.Contains(InteractionSurface item) => false;
            void ICollection<InteractionSurface>.CopyTo(InteractionSurface[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<InteractionSurface>.Remove(InteractionSurface item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
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
