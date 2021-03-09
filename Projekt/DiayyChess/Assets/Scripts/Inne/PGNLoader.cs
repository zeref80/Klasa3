using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess {
	using static BoardRepresentation;

    //opis ostatniego ruchu w notacji szachowej

	public static class PGNLoader {

		public static Move[] MovesFromPGN (string pgn, int maxPlyCount = int.MaxValue) {
			List<string> algebraicMoves = new List<string> ();

			string[] entries = pgn.Replace ("\n", " ").Split (' ');
			for (int i = 0; i < entries.Length; i++) {
				// Do osiągnięcia n-limitu
				// (Np kiedy szukamy książkowego posunięcia do n-tego posunięcia)
				if (algebraicMoves.Count == maxPlyCount) {
					break;
				}

				string entry = entries[i].Trim ();

				if (entry.Contains (".") || entry == "1/2-1/2" || entry == "1-0" || entry == "0-1") {
					continue;
				}

				if (!string.IsNullOrEmpty (entry)) {
					algebraicMoves.Add (entry);
				}
			}

			return MovesFromAlgebraic (algebraicMoves.ToArray ());
		}

		static Move[] MovesFromAlgebraic (string[] algebraicMoves) {
			Board board = new Board ();
			board.LoadStartPosition ();
			var moves = new List<Move> ();

			for (int i = 0; i < algebraicMoves.Length; i++) {
				Move move = MoveFromAlgebraic (board,algebraicMoves[i].Trim ());
				if (move.IsInvalid) {
					UnityEngine.Debug.Log ("illegal move in supplied pgn: " + algebraicMoves[i] + " move index: " + i);
					string pgn = "";
					foreach (string s in algebraicMoves) {
						pgn += s + " ";
					}
					Debug.Log ("problematic pgn: " + pgn);
					moves.ToArray ();
				} else {
					moves.Add (move);
				}
				board.MakeMove (move);
			}
			return moves.ToArray ();
		}

		static Move MoveFromAlgebraic (Board board, string algebraicMove) {
			MoveGenerator moveGenerator = new MoveGenerator ();

			// Usunięcie niepotrzebnych informacji z ciągu
			algebraicMove = algebraicMove.Replace ("+", "").Replace ("#", "").Replace ("x", "").Replace ("-", "");
			var allMoves = moveGenerator.GenerateMoves (board);

			Move move = new Move ();

			foreach (Move moveToTest in allMoves) {
				move = moveToTest;

				int moveFromIndex = move.StartSquare;
				int moveToIndex = move.TargetSquare;
				int movePieceType = Piece.PieceType (board.Square[moveFromIndex]);
				Coord fromCoord = BoardRepresentation.CoordFromIndex (moveFromIndex);
				Coord toCoord = BoardRepresentation.CoordFromIndex (moveToIndex);
				if (algebraicMove == "OO") {
					if (movePieceType == Piece.King && moveToIndex - moveFromIndex == 2) {
						return move;
					}
				} else if (algebraicMove == "OOO") { 
					if (movePieceType == Piece.King && moveToIndex - moveFromIndex == -2) {
						return move;
					}
				}
				else if (fileNames.Contains (algebraicMove[0].ToString ())) {
					if (movePieceType != Piece.Pawn) {
						continue;
					}
					if (fileNames.IndexOf (algebraicMove[0]) == fromCoord.fileIndex) { 
						if (algebraicMove.Contains ("=")) {
							if (toCoord.rankIndex == 0 || toCoord.rankIndex == 7) {

								if (algebraicMove.Length == 5)
								{
									char targetFile = algebraicMove[1];
									if (BoardRepresentation.fileNames.IndexOf (targetFile) != toCoord.fileIndex) {
										continue;
									}
								}
								char promotionChar = algebraicMove[algebraicMove.Length - 1];

								if (move.PromotionPieceType != GetPieceTypeFromSymbol (promotionChar)) {
									continue; // złe wybranie promocji
								}

								return move;
							}
						} else {

							char targetFile = algebraicMove[algebraicMove.Length - 2];
							char targetRank = algebraicMove[algebraicMove.Length - 1];

							if (BoardRepresentation.fileNames.IndexOf (targetFile) == toCoord.fileIndex) {
								if (targetRank.ToString () == (toCoord.rankIndex + 1).ToString ()) { 
									break;
								}
							}
						}
					}
				} else { 

					char movePieceChar = algebraicMove[0];
					if (GetPieceTypeFromSymbol (movePieceChar) != movePieceType) {
						continue; 
					}

					char targetFile = algebraicMove[algebraicMove.Length - 2];
					char targetRank = algebraicMove[algebraicMove.Length - 1];
					if (BoardRepresentation.fileNames.IndexOf (targetFile) == toCoord.fileIndex) {
						if (targetRank.ToString () == (toCoord.rankIndex + 1).ToString ()) { 

							if (algebraicMove.Length == 4) { 
								char disambiguationChar = algebraicMove[1];

								if (BoardRepresentation.fileNames.Contains (disambiguationChar.ToString ())) { // 
									if (BoardRepresentation.fileNames.IndexOf (disambiguationChar) != fromCoord.fileIndex) { //
										continue;
									}
								} else {
									if (disambiguationChar.ToString () != (fromCoord.rankIndex + 1).ToString ()) { // 
										continue;
									}

								}
							}
							break;
						}
					}
				}
			}
			return move;
		}

		static int GetPieceTypeFromSymbol (char symbol) {
			switch (symbol) {
				case 'R':
					return Piece.Rook;
				case 'N':
					return Piece.Knight;
				case 'B':
					return Piece.Bishop;
				case 'Q':
					return Piece.Queen;
				case 'K':
					return Piece.King;
				default:
					return Piece.None;
			}
		}
	}

}