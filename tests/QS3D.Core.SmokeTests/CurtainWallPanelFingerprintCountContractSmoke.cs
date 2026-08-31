using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallPanelFingerprintCountContractSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            NegativeKnownCountFailsBeforeItemAccess();
            KnownCountChangesAfterSnapshotFailClosed();
            ValidKnownCountPreservesFingerprint();
        }

        private static void NegativeKnownCountFailsBeforeItemAccess()
        {
            var pieces = new NegativeCountPieces();
            var error = Capture<InvalidOperationException>(() => CurtainWallPanelFingerprint.Compute(Create(pieces)));
            Require(error.Message.IndexOf("Count", StringComparison.Ordinal) >= 0,
                "Negative curtain panel Pieces Count did not report the invalid Count contract.");
            Require(!pieces.IndexerAccessed,
                "Negative curtain panel Pieces Count reached the list indexer before failing closed.");
            Require(!pieces.EnumeratorAccessed,
                "Negative curtain panel Pieces Count reached enumeration before failing closed.");
        }

        private static void KnownCountChangesAfterSnapshotFailClosed()
        {
            var growsAfterOneItem = new ChangingCountPieces(
                new[] { Piece() },
                initialCount: 1,
                changedCount: 2);
            var growError = Capture<InvalidOperationException>(() => CurtainWallPanelFingerprint.Compute(Create(growsAfterOneItem)));
            Require(growError.Message.IndexOf("Count", StringComparison.Ordinal) >= 0,
                "Curtain panel Pieces Count growth did not report the changed Count contract.");
            Require(growsAfterOneItem.CountReadCount >= 2,
                "Curtain panel Pieces Count was not re-read after admission.");
            Require(growsAfterOneItem.IndexerAccessCount == 0,
                "Curtain panel Pieces Count growth must fail before indexed snapshot access.");

            var becomesNonEmpty = new ChangingCountPieces(
                Array.Empty<CurtainWallPanelPiece>(),
                initialCount: 0,
                changedCount: 1);
            var emptyError = Capture<InvalidOperationException>(() => CurtainWallPanelFingerprint.Compute(Create(becomesNonEmpty)));
            Require(emptyError.Message.IndexOf("Count", StringComparison.Ordinal) >= 0,
                "Curtain panel Pieces zero-to-nonzero Count change did not report the changed Count contract.");
            Require(becomesNonEmpty.CountReadCount >= 2,
                "Zero-length curtain panel Pieces snapshot did not re-check the Count contract.");
            Require(becomesNonEmpty.IndexerAccessCount == 0,
                "Zero-length curtain panel Pieces snapshot unexpectedly accessed an item.");
        }

        private static void ValidKnownCountPreservesFingerprint()
        {
            var piece = Piece();
            var arrayHash = CurtainWallPanelFingerprint.Compute(Create(new[] { piece }));
            var listHash = CurtainWallPanelFingerprint.Compute(Create(new List<CurtainWallPanelPiece> { piece }));
            Require(string.Equals(arrayHash, listHash, StringComparison.Ordinal),
                "Valid curtain panel piece collection type changed the canonical fingerprint.");
        }

        private static CurtainWallPanelFingerprintInput Create(IReadOnlyList<CurtainWallPanelPiece> pieces)
        {
            return new CurtainWallPanelFingerprintInput
            {
                SourceLengthM = 4d,
                HeightM = 3d,
                BottomOffsetM = 0d,
                PanelDepthM = 0.12d,
                SourceKind = "Line",
                PathSegmentCount = 0,
                Pieces = pieces
            };
        }

        private static CurtainWallPanelPiece Piece()
        {
            return new CurtainWallPanelPiece
            {
                SourcePanelIndex = 0,
                X_M = 0d,
                Z_M = 0d,
                WidthM = 4d,
                HeightM = 3d
            };
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class NegativeCountPieces : IReadOnlyList<CurtainWallPanelPiece>
        {
            public bool IndexerAccessed { get; private set; }
            public bool EnumeratorAccessed { get; private set; }
            public int Count => -1;

            public CurtainWallPanelPiece this[int index]
            {
                get
                {
                    IndexerAccessed = true;
                    throw new InvalidOperationException("Indexer must not be reached for a negative Count contract.");
                }
            }

            public IEnumerator<CurtainWallPanelPiece> GetEnumerator()
            {
                EnumeratorAccessed = true;
                throw new InvalidOperationException("Enumeration must not be reached for a negative Count contract.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ChangingCountPieces : IReadOnlyList<CurtainWallPanelPiece>
        {
            private readonly IReadOnlyList<CurtainWallPanelPiece> _items;
            private readonly int _initialCount;
            private readonly int _changedCount;

            internal ChangingCountPieces(
                IReadOnlyList<CurtainWallPanelPiece> items,
                int initialCount,
                int changedCount)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
                _initialCount = initialCount;
                _changedCount = changedCount;
            }

            public int CountReadCount { get; private set; }
            public int IndexerAccessCount { get; private set; }

            public int Count
            {
                get
                {
                    CountReadCount++;
                    return CountReadCount == 1 ? _initialCount : _changedCount;
                }
            }

            public CurtainWallPanelPiece this[int index]
            {
                get
                {
                    IndexerAccessCount++;
                    return _items[index];
                }
            }

            public IEnumerator<CurtainWallPanelPiece> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
