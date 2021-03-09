namespace Chess {
	using System.Collections.Generic;
	using static PrecomputedMoveData;
	using static BoardRepresentation;

	public class MoveGenerator {

		public enum PromotionMode { All, QueenOnly, QueenAndKnight }

		public PromotionMode promotionsToGenerate = PromotionMode.All;

		// ---- Instance variables ----
		List<Move> moves;
		bool isWhiteToMove;
		int friendlyColour;
		int opponentColour;
		int friendlyKingSquare;
		int friendlyColourIndex;
		int opponentColourIndex;

		bool inCheck;
		bool inDoubleCheck;
		bool pinsExistInPosition;
		ulong checkRayBitmask;
		ulong pinRayBitmask;
		ulong opponentKnightAttacks;
		ulong opponentAttackMapNoPawns;
		public ulong opponentAttackMap;
		public ulong opponentPawnAttackMap;
		ulong opponentSlidingAttackMap;

		bool genQuiets;
		Board board;

		// Generowanie listy możliwych ruchów z danej pozycji.
		public List<Move> GenerateMoves (Board board, bool includeQuietMoves = true) {
			this.board = board;
			genQuiets = includeQuietMoves;
			Init ();

			CalculateAttackData ();
			GenerateKingMoves ();

			if (inDoubleCheck) {
				return moves;
			}

			GenerateSlidingMoves ();
			GenerateKnightMoves ();
			GeneratePawnMoves ();

			return moves;
		}

		public bool InCheck () {
			return inCheck;
		}

		void Init () {
			moves = new List<Move> (64);
			inCheck = false;
			inDoubleCheck = false;
			pinsExistInPosition = false;
			checkRayBitmask = 0;
			pinRayBitmask = 0;

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

				// Blokada ruchu jeśli zajęte przez sojuszniczą bierkę
				if (Piece.IsColour (pieceOnTargetSquare, friendlyColour)) {
					continue;
				}

				bool isCapture = Piece.IsColour (pieceOnTargetSquare, opponentColour);
				if (!isCapture) {
					if (!genQuiets || SquareIsInCheckRay (targetSquare)) {
						continue;
					}
				}

				if (!SquareIsAttacked (targetSquare)) {
					moves.Add (new Move (friendlyKingSquare, targetSquare));

					// Roszada:
					if (!inCheck && !isCapture) {
						// Roszada królewska
						if ((targetSquare == f1 || targetSquare == f8) && HasKingsideCastleRight) {
							int castleKingsideSquare = targetSquare + 1;
							if (board.Square[castleKingsideSquare] == Piece.None) {
								if (!SquareIsAttacked (castleKingsideSquare)) {
									moves.Add (new Move (friendlyKingSquare, castleKingsideSquare, Move.Flag.Castling));
								}
							}
						}
						// Roszada hetmańska
						else if ((targetSquare == d1 || targetSquare == d8) && HasQueensideCastleRight) {
							int castleQueensideSquare = targetSquare - 1;
							if (board.Square[castleQueensideSquare] == Piece.None && board.Square[castleQueensideSquare - 1] == Piece.None) {
								if (!SquareIsAttacked (castleQueensideSquare)) {
									moves.Add (new Move (friendlyKingSquare, castleQueensideSquare, Move.Flag.Castling));
								}
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
			bool isPinned = IsPinned (startSquare);

			// Blokada ruchu bierki związanej
			if (inCheck && isPinned) {
				return;
			}

			for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++) {
				int currentDirOffset = directionOffsets[directionIndex];

				if (isPinned && !IsMovingAlongRay (currentDirOffset, friendlyKingSquare, startSquare)) {
					continue;
				}

				for (int n = 0; n < numSquaresToEdge[startSquare][directionIndex]; n++) {
					int targetSquare = startSquare + currentDirOffset * (n + 1);
					int targetSquarePiece = board.Square[targetSquare];

					if (Piece.IsColour (targetSquarePiece, friendlyColour)) {
						break;
					}
					bool isCapture = targetSquarePiece != Piece.None;

					bool movePreventsCheck = SquareIsInCheckRay (targetSquare);
					if (movePreventsCheck || !inCheck) {
						if (genQuiets || isCapture) {
							moves.Add (new Move (startSquare, targetSquare));
						}
					}

					if (isCapture || movePreventsCheck) {
						break;
					}
				}
			}
		}

		void GenerateKnightMoves () {
			PieceList myKnights = board.knights[friendlyColourIndex];

			for (int i = 0; i < myKnights.Count; i++) {
				int startSquare = myKnights[i];

				if (IsPinned (startSquare)) {
					continue;
				}

				for (int knightMoveIndex = 0; knightMoveIndex < knightMoves[startSquare].Length; knightMoveIndex++) {
					int targetSquare = knightMoves[startSquare][knightMoveIndex];
					int targetSquarePiece = board.Square[targetSquare];
					bool isCapture = Piece.IsColour (targetSquarePiece, opponentColour);
					if (genQuiets || isCapture) {
						if (Piece.IsColour (targetSquarePiece, friendlyColour) || (inCheck && !SquareIsInCheckRay (targetSquare))) {
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
						if (!IsPinned (startSquare) || IsMovingAlongRay (pawnOffset, startSquare, friendlyKingSquare)) {
							if (!inCheck || SquareIsInCheckRay (squareOneForward)) {
								if (oneStepFromPromotion) {
									MakePromotionMoves (startSquare, squareOneForward);
								} else {
									moves.Add (new Move (startSquare, squareOneForward));
								}
							}

							if (rank == startRank) {
								int squareTwoForward = squareOneForward + pawnOffset;
								if (board.Square[squareTwoForward] == Piece.None) {
									if (!inCheck || SquareIsInCheckRay (squareTwoForward)) {
										moves.Add (new Move (startSquare, squareTwoForward, Move.Flag.PawnTwoForward));
									}
								}
							}
						}
					}
				}

				// Atak piona
				for (int j = 0; j < 2; j++) {
					if (numSquaresToEdge[startSquare][pawnAttackDirections[friendlyColourIndex][j]] > 0) {
						int pawnCaptureDir = directionOffsets[pawnAttackDirections[friendlyColourIndex][j]];
						int targetSquare = startSquare + pawnCaptureDir;
						int targetPiece = board.Square[targetSquare];

						// Pion związany i nie bije zgodnie z związaniem
						if (IsPinned (startSquare) && !IsMovingAlongRay (pawnCaptureDir, friendlyKingSquare, startSquare)) {
							continue;
						}

						// Zwykłe bicie
						if (Piece.IsColour (targetPiece, opponentColour)) {
							if (inCheck && !SquareIsInCheckRay (targetSquare)) {
								continue;
							}
							if (oneStepFromPromotion) {
								MakePromotionMoves (startSquare, targetSquare);
							} else {
								moves.Add (new Move (startSquare, targetSquare));
							}
						}

						// En-passant
						if (targetSquare == enPassantSquare) {
							int epCapturedPawnSquare = targetSquare + ((board.WhiteToMove) ? -8 : 8);
							if (!InCheckAfterEnPassant (startSquare, targetSquare, epCapturedPawnSquare)) {
								moves.Add (new Move (startSquare, targetSquare, Move.Flag.EnPassantCapture));
							}
						}
					}
				}
			}
		}

		void MakePromotionMoves (int fromSquare, int toSquare) {
			moves.Add (new Move (fromSquare, toSquare, Move.Flag.PromoteToQueen));
			if (promotionsToGenerate == PromotionMode.All) {
				moves.Add (new Move (fromSquare, toSquare, Move.Flag.PromoteToKnight));
				moves.Add (new Move (fromSquare, toSquare, Move.Flag.PromoteToRook));
				moves.Add (new Move (fromSquare, toSquare, Move.Flag.PromoteToBishop));
			} else if (promotionsToGenerate == PromotionMode.QueenAndKnight) {
				moves.Add (new Move (fromSquare, toSquare, Move.Flag.PromoteToKnight));
			}

		}

		bool IsMovingAlongRay (int rayDir, int startSquare, int targetSquare) {
			int moveDir = directionLookup[targetSquare - startSquare + 63];
			return (rayDir == moveDir || -rayDir == moveDir);
		}


		bool IsPinned (int square) {
			return pinsExistInPosition && ((pinRayBitmask >> square) & 1) != 0;
		}

		bool SquareIsInCheckRay (int square) {
			return inCheck && ((checkRayBitmask >> square) & 1) != 0;
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

		void GenSlidingAttackMap () {
			opponentSlidingAttackMap = 0;

			PieceList enemyRooks = board.rooks[opponentColourIndex];
			for (int i = 0; i < enemyRooks.Count; i++) {
				UpdateSlidingAttackPiece (enemyRooks[i], 0, 4);
			}

			PieceList enemyQueens = board.queens[opponentColourIndex];
			for (int i = 0; i < enemyQueens.Count; i++) {
				UpdateSlidingAttackPiece (enemyQueens[i], 0, 8);
			}

			PieceList enemyBishops = board.bishops[opponentColourIndex];
			for (int i = 0; i < enemyBishops.Count; i++) {
				UpdateSlidingAttackPiece (enemyBishops[i], 4, 8);
			}
		}

		void UpdateSlidingAttackPiece (int startSquare, int startDirIndex, int endDirIndex) {

			for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++) {
				int currentDirOffset = directionOffsets[directionIndex];
				for (int n = 0; n < numSquaresToEdge[startSquare][directionIndex]; n++) {
					int targetSquare = startSquare + currentDirOffset * (n + 1);
					int targetSquarePiece = board.Square[targetSquare];
					opponentSlidingAttackMap |= 1ul << targetSquare;
					if (targetSquare != friendlyKingSquare) {
						if (targetSquarePiece != Piece.None) {
							break;
						}
					}
				}
			}
		}

		void CalculateAttackData () {
			GenSlidingAttackMap ();
			int startDirIndex = 0;
			int endDirIndex = 8;

			if (board.queens[opponentColourIndex].Count == 0) {
				startDirIndex = (board.rooks[opponentColourIndex].Count > 0) ? 0 : 4;
				endDirIndex = (board.bishops[opponentColourIndex].Count > 0) ? 8 : 4;
			}

			for (int dir = startDirIndex; dir < endDirIndex; dir++) {
				bool isDiagonal = dir > 3;

				int n = numSquaresToEdge[friendlyKingSquare][dir];
				int directionOffset = directionOffsets[dir];
				bool isFriendlyPieceAlongRay = false;
				ulong rayMask = 0;

				for (int i = 0; i < n; i++) {
					int squareIndex = friendlyKingSquare + directionOffset * (i + 1);
					rayMask |= 1ul << squareIndex;
					int piece = board.Square[squareIndex];

					if (piece != Piece.None) {
						if (Piece.IsColour (piece, friendlyColour)) {
							if (!isFriendlyPieceAlongRay) {
								isFriendlyPieceAlongRay = true;
							}
							else {
								break;
							}
						}
						else {
							int pieceType = Piece.PieceType (piece);

							if (isDiagonal && Piece.IsBishopOrQueen (pieceType) || !isDiagonal && Piece.IsRookOrQueen (pieceType)) {
								if (isFriendlyPieceAlongRay) {
									pinsExistInPosition = true;
									pinRayBitmask |= rayMask;
								}
								else {
									checkRayBitmask |= rayMask;
									inDoubleCheck = inCheck;
									inCheck = true;
								}
								break;
							} else {
								break;
							}
						}
					}
				}
				if (inDoubleCheck) {
					break;
				}

			}

			PieceList opponentKnights = board.knights[opponentColourIndex];
			opponentKnightAttacks = 0;
			bool isKnightCheck = false;

			for (int knightIndex = 0; knightIndex < opponentKnights.Count; knightIndex++) {
				int startSquare = opponentKnights[knightIndex];
				opponentKnightAttacks |= knightAttackBitboards[startSquare];

				if (!isKnightCheck && BitBoardUtility.ContainsSquare (opponentKnightAttacks, friendlyKingSquare)) {
					isKnightCheck = true;
					inDoubleCheck = inCheck; 
					inCheck = true;
					checkRayBitmask |= 1ul << startSquare;
				}
			}

			PieceList opponentPawns = board.pawns[opponentColourIndex];
			opponentPawnAttackMap = 0;
			bool isPawnCheck = false;

			for (int pawnIndex = 0; pawnIndex < opponentPawns.Count; pawnIndex++) {
				int pawnSquare = opponentPawns[pawnIndex];
				ulong pawnAttacks = pawnAttackBitboards[pawnSquare][opponentColourIndex];
				opponentPawnAttackMap |= pawnAttacks;

				if (!isPawnCheck && BitBoardUtility.ContainsSquare (pawnAttacks, friendlyKingSquare)) {
					isPawnCheck = true;
					inDoubleCheck = inCheck; 
					inCheck = true;
					checkRayBitmask |= 1ul << pawnSquare;
				}
			}

			int enemyKingSquare = board.KingSquare[opponentColourIndex];

			opponentAttackMapNoPawns = opponentSlidingAttackMap | opponentKnightAttacks | kingAttackBitboards[enemyKingSquare];
			opponentAttackMap = opponentAttackMapNoPawns | opponentPawnAttackMap;
		}

		bool SquareIsAttacked (int square) {
			return BitBoardUtility.ContainsSquare (opponentAttackMap, square);
		}

		bool InCheckAfterEnPassant (int startSquare, int targetSquare, int epCapturedPawnSquare) {
			board.Square[targetSquare] = board.Square[startSquare];
			board.Square[startSquare] = Piece.None;
			board.Square[epCapturedPawnSquare] = Piece.None;

			bool inCheckAfterEpCapture = false;
			if (SquareAttackedAfterEPCapture (epCapturedPawnSquare, startSquare)) {
				inCheckAfterEpCapture = true;
			}

			board.Square[targetSquare] = Piece.None;
			board.Square[startSquare] = Piece.Pawn | friendlyColour;
			board.Square[epCapturedPawnSquare] = Piece.Pawn | opponentColour;
			return inCheckAfterEpCapture;
		}

		bool SquareAttackedAfterEPCapture (int epCaptureSquare, int capturingPawnStartSquare) {
			if (BitBoardUtility.ContainsSquare (opponentAttackMapNoPawns, friendlyKingSquare)) {
				return true;
			}

			int dirIndex = (epCaptureSquare < friendlyKingSquare) ? 2 : 3;
			for (int i = 0; i < numSquaresToEdge[friendlyKingSquare][dirIndex]; i++) {
				int squareIndex = friendlyKingSquare + directionOffsets[dirIndex] * (i + 1);
				int piece = board.Square[squareIndex];
				if (piece != Piece.None) {
					if (Piece.IsColour (piece, friendlyColour)) {
						break;
					}
					else {
						if (Piece.IsRookOrQueen (piece)) {
							return true;
						} else {
							break;
						}
					}
				}
			}

			for (int i = 0; i < 2; i++) {
				if (numSquaresToEdge[friendlyKingSquare][pawnAttackDirections[friendlyColourIndex][i]] > 0) {
					int piece = board.Square[friendlyKingSquare + directionOffsets[pawnAttackDirections[friendlyColourIndex][i]]];
					if (piece == (Piece.Pawn | opponentColour))
					{
						return true;
					}
				}
			}

			return false;
		}
	}

}