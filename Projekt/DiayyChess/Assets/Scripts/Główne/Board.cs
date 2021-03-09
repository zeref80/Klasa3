﻿namespace Chess {
	using System.Collections.Generic;

	public class Board {

		public const int WhiteIndex = 0;
		public const int BlackIndex = 1;

		// Hash figury zapisany jest w formacie bierka | kolor
		public int[] Square;

		public bool WhiteToMove;
		public int ColourToMove;
		public int OpponentColour;
		public int ColourToMoveIndex;

		Stack<uint> gameStateHistory;
		public uint currentGameState;

		public int plyCount;
		public int fiftyMoveCounter; // zmienna do zasady 50 ruchów

		public ulong ZobristKey;
		/// Przechowywanie klucza Zobrista
		public Stack<ulong> RepetitionPositionHistory;

		public int[] KingSquare; 

		public PieceList[] rooks;
		public PieceList[] bishops;
		public PieceList[] queens;
		public PieceList[] knights;
		public PieceList[] pawns;

		PieceList[] allPieceLists;

		const uint whiteCastleKingsideMask = 0b1111111111111110;
		const uint whiteCastleQueensideMask = 0b1111111111111101;
		const uint blackCastleKingsideMask = 0b1111111111111011;
		const uint blackCastleQueensideMask = 0b1111111111110111;

		const uint whiteCastleMask = whiteCastleKingsideMask & whiteCastleQueensideMask;
		const uint blackCastleMask = blackCastleKingsideMask & blackCastleQueensideMask;

		PieceList GetPieceList (int pieceType, int colourIndex) {
			return allPieceLists[colourIndex * 8 + pieceType];
		}


		public void MakeMove (Move move, bool inSearch = false) {
			uint oldEnPassantFile = (currentGameState >> 4) & 15;
			uint originalCastleState = currentGameState & 15;
			uint newCastleState = originalCastleState;
			currentGameState = 0;

			int opponentColourIndex = 1 - ColourToMoveIndex;
			int moveFrom = move.StartSquare;
			int moveTo = move.TargetSquare;

			int capturedPieceType = Piece.PieceType (Square[moveTo]);
			int movePiece = Square[moveFrom];
			int movePieceType = Piece.PieceType (movePiece);

			int moveFlag = move.MoveFlag;
			bool isPromotion = move.IsPromotion;
			bool isEnPassant = moveFlag == Move.Flag.EnPassantCapture;

			currentGameState |= (ushort) (capturedPieceType << 8);
			if (capturedPieceType != 0 && !isEnPassant) {
				ZobristKey ^= Zobrist.piecesArray[capturedPieceType, opponentColourIndex, moveTo];
				GetPieceList (capturedPieceType, opponentColourIndex).RemovePieceAtSquare (moveTo);
			}

			if (movePieceType == Piece.King) {
				KingSquare[ColourToMoveIndex] = moveTo;
				newCastleState &= (WhiteToMove) ? whiteCastleMask : blackCastleMask;
			} else {
				GetPieceList (movePieceType, ColourToMoveIndex).MovePiece (moveFrom, moveTo);
			}

			int pieceOnTargetSquare = movePiece;

			if (isPromotion) {
				int promoteType = 0;
				switch (moveFlag) {
					case Move.Flag.PromoteToQueen:
						promoteType = Piece.Queen;
						queens[ColourToMoveIndex].AddPieceAtSquare (moveTo);
						break;
					case Move.Flag.PromoteToRook:
						promoteType = Piece.Rook;
						rooks[ColourToMoveIndex].AddPieceAtSquare (moveTo);
						break;
					case Move.Flag.PromoteToBishop:
						promoteType = Piece.Bishop;
						bishops[ColourToMoveIndex].AddPieceAtSquare (moveTo);
						break;
					case Move.Flag.PromoteToKnight:
						promoteType = Piece.Knight;
						knights[ColourToMoveIndex].AddPieceAtSquare (moveTo);
						break;

				}
				pieceOnTargetSquare = promoteType | ColourToMove;
				pawns[ColourToMoveIndex].RemovePieceAtSquare (moveTo);
			} else {
				switch (moveFlag) {
					case Move.Flag.EnPassantCapture:
						int epPawnSquare = moveTo + ((ColourToMove == Piece.White) ? -8 : 8);
						currentGameState |= (ushort) (Square[epPawnSquare] << 8); 
						Square[epPawnSquare] = 0; 
						pawns[opponentColourIndex].RemovePieceAtSquare (epPawnSquare);
						ZobristKey ^= Zobrist.piecesArray[Piece.Pawn, opponentColourIndex, epPawnSquare];
						break;
					case Move.Flag.Castling:
						bool kingside = moveTo == BoardRepresentation.g1 || moveTo == BoardRepresentation.g8;
						int castlingRookFromIndex = (kingside) ? moveTo + 1 : moveTo - 2;
						int castlingRookToIndex = (kingside) ? moveTo - 1 : moveTo + 1;

						Square[castlingRookFromIndex] = Piece.None;
						Square[castlingRookToIndex] = Piece.Rook | ColourToMove;

						rooks[ColourToMoveIndex].MovePiece (castlingRookFromIndex, castlingRookToIndex);
						ZobristKey ^= Zobrist.piecesArray[Piece.Rook, ColourToMoveIndex, castlingRookFromIndex];
						ZobristKey ^= Zobrist.piecesArray[Piece.Rook, ColourToMoveIndex, castlingRookToIndex];
						break;
				}
			}

			// Odświeżenie tablicy
			Square[moveTo] = pieceOnTargetSquare;
			Square[moveFrom] = 0;

			// Podwójny ruch piona do bicia w przelocie
			if (moveFlag == Move.Flag.PawnTwoForward) {
				int file = BoardRepresentation.FileIndex (moveFrom) + 1;
				currentGameState |= (ushort) (file << 4);
				ZobristKey ^= Zobrist.enPassantFile[file];
			}

			if (originalCastleState != 0) {
				if (moveTo == BoardRepresentation.h1 || moveFrom == BoardRepresentation.h1) {
					newCastleState &= whiteCastleKingsideMask;
				} else if (moveTo == BoardRepresentation.a1 || moveFrom == BoardRepresentation.a1) {
					newCastleState &= whiteCastleQueensideMask;
				}
				if (moveTo == BoardRepresentation.h8 || moveFrom == BoardRepresentation.h8) {
					newCastleState &= blackCastleKingsideMask;
				} else if (moveTo == BoardRepresentation.a8 || moveFrom == BoardRepresentation.a8) {
					newCastleState &= blackCastleQueensideMask;
				}
			}

			// Odświeżenie kluczu Zobrista
			ZobristKey ^= Zobrist.sideToMove;
			ZobristKey ^= Zobrist.piecesArray[movePieceType, ColourToMoveIndex, moveFrom];
			ZobristKey ^= Zobrist.piecesArray[Piece.PieceType (pieceOnTargetSquare), ColourToMoveIndex, moveTo];

			if (oldEnPassantFile != 0)
				ZobristKey ^= Zobrist.enPassantFile[oldEnPassantFile];

			if (newCastleState != originalCastleState) {
				ZobristKey ^= Zobrist.castlingRights[originalCastleState]; 
				ZobristKey ^= Zobrist.castlingRights[newCastleState]; 
			}
			currentGameState |= newCastleState;
			currentGameState |= (uint) fiftyMoveCounter << 14;
			gameStateHistory.Push (currentGameState);

			WhiteToMove = !WhiteToMove;
			ColourToMove = (WhiteToMove) ? Piece.White : Piece.Black;
			OpponentColour = (WhiteToMove) ? Piece.Black : Piece.White;
			ColourToMoveIndex = 1 - ColourToMoveIndex;
			plyCount++;
			fiftyMoveCounter++;

			if (!inSearch) {
				if (movePieceType == Piece.Pawn || capturedPieceType != Piece.None) {
					RepetitionPositionHistory.Clear ();
					fiftyMoveCounter = 0;
				} else {
					RepetitionPositionHistory.Push (ZobristKey);
				}
			}

		}

		// Undo
		public void UnmakeMove (Move move, bool inSearch = false) {

			int opponentColourIndex = ColourToMoveIndex;
			bool undoingWhiteMove = OpponentColour == Piece.White;
			ColourToMove = OpponentColour; 
			OpponentColour = (undoingWhiteMove) ? Piece.Black : Piece.White;
			ColourToMoveIndex = 1 - ColourToMoveIndex;
			WhiteToMove = !WhiteToMove;

			uint originalCastleState = currentGameState & 0b1111;

			int capturedPieceType = ((int) currentGameState >> 8) & 63;
			int capturedPiece = (capturedPieceType == 0) ? 0 : capturedPieceType | OpponentColour;

			int movedFrom = move.StartSquare;
			int movedTo = move.TargetSquare;
			int moveFlags = move.MoveFlag;
			bool isEnPassant = moveFlags == Move.Flag.EnPassantCapture;
			bool isPromotion = move.IsPromotion;

			int toSquarePieceType = Piece.PieceType (Square[movedTo]);
			int movedPieceType = (isPromotion) ? Piece.Pawn : toSquarePieceType;

			ZobristKey ^= Zobrist.sideToMove;
			ZobristKey ^= Zobrist.piecesArray[movedPieceType, ColourToMoveIndex, movedFrom]; 
			ZobristKey ^= Zobrist.piecesArray[toSquarePieceType, ColourToMoveIndex, movedTo]; 

			uint oldEnPassantFile = (currentGameState >> 4) & 15;
			if (oldEnPassantFile != 0)
				ZobristKey ^= Zobrist.enPassantFile[oldEnPassantFile];


			if (capturedPieceType != 0 && !isEnPassant) {
				ZobristKey ^= Zobrist.piecesArray[capturedPieceType, opponentColourIndex, movedTo];
				GetPieceList (capturedPieceType, opponentColourIndex).AddPieceAtSquare (movedTo);
			}

			if (movedPieceType == Piece.King) {
				KingSquare[ColourToMoveIndex] = movedFrom;
			} else if (!isPromotion) {
				GetPieceList (movedPieceType, ColourToMoveIndex).MovePiece (movedTo, movedFrom);
			}

			Square[movedFrom] = movedPieceType | ColourToMove; 
			Square[movedTo] = capturedPiece; 

			if (isPromotion) {
				pawns[ColourToMoveIndex].AddPieceAtSquare (movedFrom);
				switch (moveFlags) {
					case Move.Flag.PromoteToQueen:
						queens[ColourToMoveIndex].RemovePieceAtSquare (movedTo);
						break;
					case Move.Flag.PromoteToKnight:
						knights[ColourToMoveIndex].RemovePieceAtSquare (movedTo);
						break;
					case Move.Flag.PromoteToRook:
						rooks[ColourToMoveIndex].RemovePieceAtSquare (movedTo);
						break;
					case Move.Flag.PromoteToBishop:
						bishops[ColourToMoveIndex].RemovePieceAtSquare (movedTo);
						break;
				}
			} else if (isEnPassant) { 
				int epIndex = movedTo + ((ColourToMove == Piece.White) ? -8 : 8);
				Square[movedTo] = 0;
				Square[epIndex] = (int) capturedPiece;
				pawns[opponentColourIndex].AddPieceAtSquare (epIndex);
				ZobristKey ^= Zobrist.piecesArray[Piece.Pawn, opponentColourIndex, epIndex];
			} else if (moveFlags == Move.Flag.Castling) { 

				bool kingside = movedTo == 6 || movedTo == 62;
				int castlingRookFromIndex = (kingside) ? movedTo + 1 : movedTo - 2;
				int castlingRookToIndex = (kingside) ? movedTo - 1 : movedTo + 1;

				Square[castlingRookToIndex] = 0;
				Square[castlingRookFromIndex] = Piece.Rook | ColourToMove;

				rooks[ColourToMoveIndex].MovePiece (castlingRookToIndex, castlingRookFromIndex);
				ZobristKey ^= Zobrist.piecesArray[Piece.Rook, ColourToMoveIndex, castlingRookFromIndex];
				ZobristKey ^= Zobrist.piecesArray[Piece.Rook, ColourToMoveIndex, castlingRookToIndex];

			}

			gameStateHistory.Pop (); 
			currentGameState = gameStateHistory.Peek ();

			fiftyMoveCounter = (int) (currentGameState & 4294950912) >> 14;
			int newEnPassantFile = (int) (currentGameState >> 4) & 15;
			if (newEnPassantFile != 0)
				ZobristKey ^= Zobrist.enPassantFile[newEnPassantFile];

			uint newCastleState = currentGameState & 0b1111;
			if (newCastleState != originalCastleState) {
				ZobristKey ^= Zobrist.castlingRights[originalCastleState]; 
				ZobristKey ^= Zobrist.castlingRights[newCastleState]; 
			}

			plyCount--;

			if (!inSearch && RepetitionPositionHistory.Count > 0) {
				RepetitionPositionHistory.Pop ();
			}

		}


		public void LoadStartPosition () {
			LoadPosition (FenUtility.startFen);
		}

		public void LoadPosition (string fen) {
			Initialize ();
			var loadedPosition = FenUtility.PositionFromFen (fen);

			// Załadowanie bierek
			for (int squareIndex = 0; squareIndex < 64; squareIndex++) {
				int piece = loadedPosition.squares[squareIndex];
				Square[squareIndex] = piece;

				if (piece != Piece.None) {
					int pieceType = Piece.PieceType (piece);
					int pieceColourIndex = (Piece.IsColour (piece, Piece.White)) ? WhiteIndex : BlackIndex;
					if (Piece.IsSlidingPiece (piece)) {
						if (pieceType == Piece.Queen) {
							queens[pieceColourIndex].AddPieceAtSquare (squareIndex);
						} else if (pieceType == Piece.Rook) {
							rooks[pieceColourIndex].AddPieceAtSquare (squareIndex);
						} else if (pieceType == Piece.Bishop) {
							bishops[pieceColourIndex].AddPieceAtSquare (squareIndex);
						}
					} else if (pieceType == Piece.Knight) {
						knights[pieceColourIndex].AddPieceAtSquare (squareIndex);
					} else if (pieceType == Piece.Pawn) {
						pawns[pieceColourIndex].AddPieceAtSquare (squareIndex);
					} else if (pieceType == Piece.King) {
						KingSquare[pieceColourIndex] = squareIndex;
					}
				}
			}

			// Ułożenie planszy i ruch w zależności od koloru, którym zaczynamy
			WhiteToMove = loadedPosition.whiteToMove;
			ColourToMove = (WhiteToMove) ? Piece.White : Piece.Black;
			OpponentColour = (WhiteToMove) ? Piece.Black : Piece.White;
			ColourToMoveIndex = (WhiteToMove) ? 0 : 1;

			int whiteCastle = ((loadedPosition.whiteCastleKingside) ? 1 << 0 : 0) | ((loadedPosition.whiteCastleQueenside) ? 1 << 1 : 0);
			int blackCastle = ((loadedPosition.blackCastleKingside) ? 1 << 2 : 0) | ((loadedPosition.blackCastleQueenside) ? 1 << 3 : 0);
			int epState = loadedPosition.epFile << 4;
			ushort initialGameState = (ushort) (whiteCastle | blackCastle | epState);
			gameStateHistory.Push (initialGameState);
			currentGameState = initialGameState;
			plyCount = loadedPosition.plyCount;

			ZobristKey = Zobrist.CalculateZobristKey (this);
		}

		void Initialize () {
			Square = new int[64];
			KingSquare = new int[2];

			gameStateHistory = new Stack<uint> ();
			ZobristKey = 0;
			RepetitionPositionHistory = new Stack<ulong> ();
			plyCount = 0;
			fiftyMoveCounter = 0;

			knights = new PieceList[] { new PieceList (10), new PieceList (10) };
			pawns = new PieceList[] { new PieceList (8), new PieceList (8) };
			rooks = new PieceList[] { new PieceList (10), new PieceList (10) };
			bishops = new PieceList[] { new PieceList (10), new PieceList (10) };
			queens = new PieceList[] { new PieceList (9), new PieceList (9) };
			PieceList emptyList = new PieceList (0);
			allPieceLists = new PieceList[] {
				emptyList,
				emptyList,
				pawns[WhiteIndex],
				knights[WhiteIndex],
				emptyList,
				bishops[WhiteIndex],
				rooks[WhiteIndex],
				queens[WhiteIndex],
				emptyList,
				emptyList,
				pawns[BlackIndex],
				knights[BlackIndex],
				emptyList,
				bishops[BlackIndex],
				rooks[BlackIndex],
				queens[BlackIndex],
			};
		}
	}
}