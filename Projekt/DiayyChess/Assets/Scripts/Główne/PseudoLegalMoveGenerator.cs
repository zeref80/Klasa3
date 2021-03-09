namespace Chess {
	using System.Collections.Generic;
	using static PrecomputedMoveData;
	using static BoardRepresentation;
	
	public class PseudoLegalMoveGenerator {

		List<Move> moves;
		bool isWhiteToMove;
		int friendlyColour;
		int opponentColour;
		int friendlyKingSquare;
		int friendlyColourIndex;
		int opponentColourIndex;

		bool genQuiets;
		bool genUnderpromotions;
		Board board;

		// lista możliwych ruchów z danej pozycji
		public List<Move> GenerateMoves (Board board, bool includeQuietMoves = true, bool includeUnderPromotions = true) {
			this.board = board;
			genQuiets = includeQuietMoves;
			genUnderpromotions = includeUnderPromotions;
			Init ();
			GenerateKingMoves ();

			GenerateSlidingMoves ();
			GenerateKnightMoves ();
			GeneratePawnMoves ();

			return moves;
		}

		public bool Illegal () {
			return SquareAttacked (board.KingSquare[1 - board.ColourToMoveIndex], board.ColourToMove);
		}

		public bool SquareAttacked (int attackSquare, int attackerColour) {

			int attackerColourIndex = (attackerColour == Piece.White) ? Board.WhiteIndex : Board.BlackIndex;
			int friendlyColourIndex = 1 - attackerColourIndex;
			int friendlyColour = (attackerColour == Piece.White) ? Piece.Black : Piece.White;

			int startDirIndex = 0;
			int endDirIndex = 8;

			int opponentKingSquare = board.KingSquare[attackerColourIndex];
			if (kingDistance[opponentKingSquare, attackSquare] == 1) {
				return true;
			}

			if (board.queens[attackerColourIndex].Count == 0) {
				startDirIndex = (board.rooks[attackerColourIndex].Count > 0) ? 0 : 4;
				endDirIndex = (board.bishops[attackerColourIndex].Count > 0) ? 8 : 4;
			}

			for (int dir = startDirIndex; dir < endDirIndex; dir++) {
				bool isDiagonal = dir > 3;

				int n = numSquaresToEdge[attackSquare][dir];
				int directionOffset = directionOffsets[dir];

				for (int i = 0; i < n; i++) {
					int squareIndex = attackSquare + directionOffset * (i + 1);
					int piece = board.Square[squareIndex];

					if (piece != Piece.None) {
						if (Piece.IsColour (piece, friendlyColour)) {
							break;
						}
						else {
							int pieceType = Piece.PieceType (piece);

							if (isDiagonal && Piece.IsBishopOrQueen (pieceType) || !isDiagonal && Piece.IsRookOrQueen (pieceType)) {
								return true;
							} else {
								break;
							}
						}
					}
				}
			}

			var knightAttackSquares = knightMoves[attackSquare];
			for (int i = 0; i < knightAttackSquares.Length; i++) {
				if (board.Square[knightAttackSquares[i]] == (Piece.Knight | attackerColour)) {
					return true;
				}
			}

			//sprawdzenie czy przeciwny pion jest na danej pozycji
			for (int i = 0; i < 2; i++) {
				// Zabezpieczenie istnienia pola na ukos ( np na wypadek bycia poza planszą)
				if (numSquaresToEdge[attackSquare][pawnAttackDirections[friendlyColourIndex][i]] > 0) {
					// ruch na pole przeciwnika
					int s = attackSquare + directionOffsets[pawnAttackDirections[friendlyColourIndex][i]];

					int piece = board.Square[s];
					if (piece == (Piece.Pawn | attackerColour)) // is enemy pawn
					{
						return true;
					}
				}
			}

			return false;
		}

		public bool InCheck () {
			return false;
		}

		void Init () {
			moves = new List<Move> (64);

			isWhiteToMove = board.ColourToMove == Piece.White;
			friendlyColour = board.ColourToMove;
			opponentColour = board.OpponentColour;
			friendlyKingSquare = board.KingSquare[board.ColourToMoveIndex];
			friendlyColourIndex = (board.WhiteToMove) ? Board.WhiteIndex : Board.BlackIndex;
			opponentColourIndex = 1 - friendlyColourIndex;
		}

		void GenerateKingMoves () {
			for (int i = 0; i < kingMoves[friendlyKingSquare].Length; i++) {
				int targetSquare = kingMoves[friendlyKingSquare][i];
				int pieceOnTargetSquare = board.Square[targetSquare];

				if (Piece.IsColour (pieceOnTargetSquare, friendlyColour)) {
					continue;
				}

				bool isCapture = Piece.IsColour (pieceOnTargetSquare, opponentColour);
				if (!isCapture) {
					if (!genQuiets) {
						continue;
					}
				}

				// nieatakowane pole dla króla

				moves.Add (new Move (friendlyKingSquare, targetSquare));

				// Roszada:
				if (!isCapture && !SquareAttacked (friendlyKingSquare, opponentColour)) {
					// Królewska
					if ((targetSquare == f1 || targetSquare == f8) && HasKingsideCastleRight) {
						if (!SquareAttacked (targetSquare, opponentColour)) {
							int castleKingsideSquare = targetSquare + 1;
							if (board.Square[castleKingsideSquare] == Piece.None) {
								moves.Add (new Move (friendlyKingSquare, castleKingsideSquare, Move.Flag.Castling));

							}
						}
					}
					// Hetmańska
					else if ((targetSquare == d1 || targetSquare == d8) && HasQueensideCastleRight) {
						if (!SquareAttacked (targetSquare, opponentColour)) {
							int castleQueensideSquare = targetSquare - 1;
							if (board.Square[castleQueensideSquare] == Piece.None && board.Square[castleQueensideSquare - 1] == Piece.None) {
								moves.Add (new Move (friendlyKingSquare, castleQueensideSquare, Move.Flag.Castling));
							}
						}
					}
				}

			}
		}

		void GenerateSlidingMoves () {
			PieceList rooks = board.rooks[friendlyColourIndex];
			for (int i = 0; i < rooks.Count; i++) {
				GenerateSlidingPieceMoves (rooks[i], 0, 4);
			}

			PieceList bishops = board.bishops[friendlyColourIndex];
			for (int i = 0; i < bishops.Count; i++) {
				GenerateSlidingPieceMoves (bishops[i], 4, 8);
			}

			PieceList queens = board.queens[friendlyColourIndex];
			for (int i = 0; i < queens.Count; i++) {
				GenerateSlidingPieceMoves (queens[i], 0, 8);
			}

		}

		void GenerateSlidingPieceMoves (int startSquare, int startDirIndex, int endDirIndex) {

			for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++) {
				int currentDirOffset = directionOffsets[directionIndex];

				for (int n = 0; n < numSquaresToEdge[startSquare][directionIndex]; n++) {
					int targetSquare = startSquare + currentDirOffset * (n + 1);
					int targetSquarePiece = board.Square[targetSquare];

					if (Piece.IsColour (targetSquarePiece, friendlyColour)) {
						break;
					}
					bool isCapture = targetSquarePiece != Piece.None;

				if (genQuiets || isCapture) {
						moves.Add (new Move (startSquare, targetSquare));
					}

					if (isCapture) {
						break;
					}
				}
			}
		}

		void GenerateKnightMoves () {
			PieceList myKnights = board.knights[friendlyColourIndex];

			for (int i = 0; i < myKnights.Count; i++) {
				int startSquare = myKnights[i];

				for (int knightMoveIndex = 0; knightMoveIndex < knightMoves[startSquare].Length; knightMoveIndex++) {
					int targetSquare = knightMoves[startSquare][knightMoveIndex];
					int targetSquarePiece = board.Square[targetSquare];
					bool isCapture = Piece.IsColour (targetSquarePiece, opponentColour);
					if (genQuiets || isCapture) {
						if (Piece.IsColour (targetSquarePiece, friendlyColour)) {
							continue;
						}
						moves.Add (new Move (startSquare, targetSquare));
					}
				}
			}
		}

		void GeneratePawnMoves () {
			PieceList myPawns = board.pawns[friendlyColourIndex];
			int pawnOffset = (friendlyColour == Piece.White) ? 8 : -8;
			int startRank = (board.WhiteToMove) ? 1 : 6;
			int finalRankBeforePromotion = (board.WhiteToMove) ? 6 : 1;

			int enPassantFile = ((int) (board.currentGameState >> 4) & 15) - 1;
			int enPassantSquare = -1;
			if (enPassantFile != -1) {
				enPassantSquare = 8 * ((board.WhiteToMove) ? 5 : 2) + enPassantFile;
			}

			for (int i = 0; i < myPawns.Count; i++) {
				int startSquare = myPawns[i];
				int rank = RankIndex (startSquare);
				bool oneStepFromPromotion = rank == finalRankBeforePromotion;

				if (genQuiets) {

					int squareOneForward = startSquare + pawnOffset;

					if (board.Square[squareOneForward] == Piece.None) {

						if (oneStepFromPromotion) {
							MakePromotionMoves (startSquare, squareOneForward);
						} else {
							moves.Add (new Move (startSquare, squareOneForward));
						}

						// Możliwość ruchu o 2 jeśli linia początkowa 
						if (rank == startRank) {
							int squareTwoForward = squareOneForward + pawnOffset;
							if (board.Square[squareTwoForward] == Piece.None) {

								moves.Add (new Move (startSquare, squareTwoForward, Move.Flag.PawnTwoForward));

							}
						}

					}
				}

				for (int j = 0; j < 2; j++) {
					if (numSquaresToEdge[startSquare][pawnAttackDirections[friendlyColourIndex][j]] > 0) {
						int pawnCaptureDir = directionOffsets[pawnAttackDirections[friendlyColourIndex][j]];
						int targetSquare = startSquare + pawnCaptureDir;
						int targetPiece = board.Square[targetSquare];

						if (Piece.IsColour (targetPiece, opponentColour)) {

							if (oneStepFromPromotion) {
								MakePromotionMoves (startSquare, targetSquare);
							} else {
								moves.Add (new Move (startSquare, targetSquare));
							}
						}

						if (targetSquare == enPassantSquare) {
							int epCapturedPawnSquare = targetSquare + ((board.WhiteToMove) ? -8 : 8);

							moves.Add (new Move (startSquare, targetSquare, Move.Flag.EnPassantCapture));

						}
					}
				}
			}
		}

		void MakePromotionMoves (int fromSquare, int toSquare) {
			moves.Add (new Move (fromSquare, toSquare, Move.Flag.PromoteToQueen));
			if (genUnderpromotions) {
				moves.Add (new Move (fromSquare, toSquare, Move.Flag.PromoteToKnight));
				moves.Add (new Move (fromSquare, toSquare, Move.Flag.PromoteToRook));
				moves.Add (new Move (fromSquare, toSquare, Move.Flag.PromoteToBishop));
			}
		}

		bool HasKingsideCastleRight {
			get {
				int mask = (board.WhiteToMove) ? 1 : 4;
				return (board.currentGameState & mask) != 0;
			}
		}

		bool HasQueensideCastleRight {
			get {
				int mask = (board.WhiteToMove) ? 2 : 8;
				return (board.currentGameState & mask) != 0;
			}
		}

	}

}