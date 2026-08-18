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
    }
}
