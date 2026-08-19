using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallPanelPieceAreaIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OrdinaryAreaRemainsRepresentable();
            OverflowFailsClosed();
            NaNFailsClosed();
            UnderflowFailsClosed();
            ZeroDimensionRemainsZero();
            MutationPathIsGuarded();
        }

        private static void OrdinaryAreaRemainsRepresentable()
        {
            var piece = new CurtainWallPanelPiece
            {
                WidthM = 2.5d,
                HeightM = 4d
            };

            if (piece.AreaM2 != 10d)
                throw new InvalidOperationException("Ordinary curtain panel piece area must remain unchanged.");
        }

        private static void OverflowFailsClosed()
        {
            var piece = new CurtainWallPanelPiece
            {
                WidthM = double.MaxValue,
                HeightM = 2d
            };

            ExpectOverflow(() => _ = piece.AreaM2, "Overflowing curtain panel piece area");
        }

        private static void NaNFailsClosed()
        {
            var piece = new CurtainWallPanelPiece
            {
                WidthM = double.NaN,
                HeightM = 1d
            };

            ExpectOverflow(() => _ = piece.AreaM2, "NaN curtain panel piece area");
        }

        private static void UnderflowFailsClosed()
        {
            var piece = new CurtainWallPanelPiece
            {
                WidthM = double.Epsilon,
                HeightM = 0.5d
            };

            ExpectOverflow(() => _ = piece.AreaM2, "Underflowing curtain panel piece area");
        }

        private static void ZeroDimensionRemainsZero()
        {
            var piece = new CurtainWallPanelPiece
            {
                WidthM = 0d,
                HeightM = double.MaxValue
            };

            if (piece.AreaM2 != 0d)
                throw new InvalidOperationException("A genuine zero-dimension curtain panel piece must still report zero area.");
        }

        private static void MutationPathIsGuarded()
        {
            var piece = new CurtainWallPanelPiece
            {
                WidthM = 2d,
                HeightM = 3d
            };
            if (piece.AreaM2 != 6d)
                throw new InvalidOperationException("Curtain panel piece mutation regression precondition failed.");

            piece.WidthM = double.MaxValue;
            piece.HeightM = 2d;
            ExpectOverflow(() => _ = piece.AreaM2, "Mutated curtain panel piece area");
        }

        private static void ExpectOverflow(Action action, string label)
        {
            try
            {
                action();
            }
            catch (OverflowException)
            {
                return;
            }

            throw new InvalidOperationException(label + " must fail closed with OverflowException.");
        }
    }
}
