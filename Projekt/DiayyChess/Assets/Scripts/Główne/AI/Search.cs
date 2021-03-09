namespace Chess {
	using System.Collections.Generic;
	using System.Threading;
	using UnityEngine;
	using static System.Math;

	public class Search {

		const int transpositionTableSize = 64000;
		const int immediateMateScore = 100000;
		const int positiveInfinity = 9999999;
		const int negativeInfinity = -positiveInfinity;

		public event System.Action<Move> onSearchComplete;

		TranspositionTable tt;
		MoveGenerator moveGenerator;

		Move bestMoveThisIteration;
		int bestEvalThisIteration;
		Move bestMove;
		int bestEval;
		int currentIterativeSearchDepth;
		bool abortSearch;

		Move invalidMove;
		MoveOrdering moveOrdering;
		AISettings settings;
		Board board;
		Evaluation evaluation;

		// Diagnostic
		public SearchDiagnostics searchDiagnostics;
		int numNodes;
		int numQNodes;
		int numCutoffs;
		int numTranspositions;
		System.Diagnostics.Stopwatch searchStopwatch;

		public Search (Board board, AISettings settings) {
			this.board = board;
			this.settings = settings;
			evaluation = new Evaluation ();
			moveGenerator = new MoveGenerator ();
			tt = new TranspositionTable (board, transpositionTableSize);
			moveOrdering = new MoveOrdering (moveGenerator, tt);
			invalidMove = Move.InvalidMove;
			int s = TranspositionTable.Entry.GetSize ();
			//Debug.Log ("TT entry: " + s + " bytes. Total size: " + ((s * transpositionTableSize) / 1000f) + " mb.");
		}

		public void StartSearch () {
			InitDebugInfo ();

			bestEvalThisIteration = bestEval = 0;
			bestMoveThisIteration = bestMove = Move.InvalidMove;
			tt.enabled = settings.useTranspositionTable;

			// Czyszczenie transposition table przed głębokim searchem ( w innym wypadku występuje błąd) 
			if (settings.clearTTEachMove) {
				tt.Clear ();
			}

			moveGenerator.promotionsToGenerate = settings.promotionsToSearch;
			currentIterativeSearchDepth = 0;
			abortSearch = false;
			searchDiagnostics = new SearchDiagnostics ();

			if (settings.useIterativeDeepening) {
				int targetDepth = (settings.useFixedDepthSearch) ? settings.depth : int.MaxValue;

				for (int searchDepth = 1; searchDepth <= targetDepth; searchDepth++) {
					SearchMoves (searchDepth, 0, negativeInfinity, positiveInfinity);
					if (abortSearch) {
						break;
					} else {
						currentIterativeSearchDepth = searchDepth;
						bestMove = bestMoveThisIteration;
						bestEval = bestEvalThisIteration;

						// Aktualizacja diagnostica
						searchDiagnostics.lastCompletedDepth = searchDepth;
						searchDiagnostics.move = bestMove.Name;
						searchDiagnostics.eval = bestEval;
						searchDiagnostics.moveVal = Chess.PGNCreator.NotationFromMove (FenUtility.CurrentFen (board), bestMove);

						// koniec jeśli mat
						if (IsMateScore (bestEval) && !settings.endlessSearchMode) {
							break;
						}
					}
				}
			} else {
				SearchMoves (settings.depth, 0, negativeInfinity, positiveInfinity);
				bestMove = bestMoveThisIteration;
				bestEval = bestEvalThisIteration;
			}

			onSearchComplete?.Invoke (bestMove);

			if (!settings.useThreading) {
				LogDebugInfo ();
			}
		}

		public (Move move, int eval) GetSearchResult () {
			return (bestMove, bestEval);
		}

		public void EndSearch () {
			abortSearch = true;
		}

		int SearchMoves (int depth, int plyFromRoot, int alpha, int beta) {
			if (abortSearch) {
				return 0;
			}

			if (plyFromRoot > 0) {
				if (board.RepetitionPositionHistory.Contains (board.ZobristKey)) {
					return 0;
				}

				alpha = Max (alpha, -immediateMateScore + plyFromRoot);
				beta = Min (beta, immediateMateScore - plyFromRoot);
				if (alpha >= beta) {
					return alpha;
				}
			}

			int ttVal = tt.LookupEvaluation (depth, plyFromRoot, alpha, beta);
			if (ttVal != TranspositionTable.lookupFailed) {
				numTranspositions++;
				if (plyFromRoot == 0) {
					bestMoveThisIteration = tt.GetStoredMove ();
					bestEvalThisIteration = tt.entries[tt.Index].value;
					//Debug.Log ("move retrieved " + bestMoveThisIteration.Name + " Node type: " + tt.entries[tt.Index].nodeType + " depth: " + tt.entries[tt.Index].depth);
				}
				return ttVal;
			}

			if (depth == 0) {
				int evaluation = QuiescenceSearch (alpha, beta);
				return evaluation;
			}

			List<Move> moves = moveGenerator.GenerateMoves (board);
			moveOrdering.OrderMoves (board, moves, settings.useTranspositionTable);
			// Jeśli nie ma więcej legalnych ruchów to remis lub mat
			if (moves.Count == 0) {
				if (moveGenerator.InCheck ()) {
					int mateScore = immediateMateScore - plyFromRoot;
					return -mateScore;
				} else {
					return 0;
				}
			}

			int evalType = TranspositionTable.UpperBound;
			Move bestMoveInThisPosition = invalidMove;

			for (int i = 0; i < moves.Count; i++) {
				board.MakeMove (moves[i], inSearch : true);
				int eval = -SearchMoves (depth - 1, plyFromRoot + 1, -beta, -alpha);
				board.UnmakeMove (moves[i], inSearch : true);
				numNodes++;

				// Osiągnięcie "zbyt" dobrego ruchu więc niedpouszczenie do tego przez przeciwnika
				if (eval >= beta) {
					tt.StoreEvaluation (depth, plyFromRoot, beta, TranspositionTable.LowerBound, moves[i]);
					numCutoffs++;
					return beta;
				}

				// Znalezienie najlepszego ruchu dla pozycji
				if (eval > alpha) {
					evalType = TranspositionTable.Exact;
					bestMoveInThisPosition = moves[i];

					alpha = eval;
					if (plyFromRoot == 0) {
						bestMoveThisIteration = moves[i];
						bestEvalThisIteration = eval;
					}
				}
			}

			tt.StoreEvaluation (depth, plyFromRoot, alpha, evalType, bestMoveInThisPosition);

			return alpha;

		}

		int QuiescenceSearch (int alpha, int beta) {
			int eval = evaluation.Evaluate (board);
			searchDiagnostics.numPositionsEvaluated++;
			if (eval >= beta) {
				return beta;
			}
			if (eval > alpha) {
				alpha = eval;
			}

			var moves = moveGenerator.GenerateMoves (board, false);
			moveOrdering.OrderMoves (board, moves, false);
			for (int i = 0; i < moves.Count; i++) {
				board.MakeMove (moves[i], true);
				eval = -QuiescenceSearch (-beta, -alpha);
				board.UnmakeMove (moves[i], true);
				numQNodes++;

				if (eval >= beta) {
					numCutoffs++;
					return beta;
				}
				if (eval > alpha) {
					alpha = eval;
				}
			}

			return alpha;
		}

		public static bool IsMateScore (int score) {
			const int maxMateDepth = 1000;
			return System.Math.Abs (score) > immediateMateScore - maxMateDepth;
		}

		public static int NumPlyToMateFromScore (int score) {
			return immediateMateScore - System.Math.Abs (score);

		}

		void LogDebugInfo () {
			AnnounceMate ();
			Debug.Log ($"Best move: {bestMoveThisIteration.Name} Eval: {bestEvalThisIteration} Search time: {searchStopwatch.ElapsedMilliseconds} ms.");
			Debug.Log ($"Num nodes: {numNodes} num Qnodes: {numQNodes} num cutoffs: {numCutoffs} num TThits {numTranspositions}");
		}

		void AnnounceMate () {

			if (IsMateScore (bestEvalThisIteration)) {
				int numPlyToMate = NumPlyToMateFromScore (bestEvalThisIteration);
				//int numPlyToMateAfterThisMove = numPlyToMate - 1;

				int numMovesToMate = (int) Ceiling (numPlyToMate / 2f);

				string sideWithMate = (bestEvalThisIteration * ((board.WhiteToMove) ? 1 : -1) < 0) ? "Black" : "White";

				Debug.Log ($"{sideWithMate} can mate in {numMovesToMate} move{((numMovesToMate>1)?"s":"")}");
			}
		}

		void InitDebugInfo () {
			searchStopwatch = System.Diagnostics.Stopwatch.StartNew ();
			numNodes = 0;
			numQNodes = 0;
			numCutoffs = 0;
			numTranspositions = 0;
		}

		[System.Serializable]
		public class SearchDiagnostics {
			public int lastCompletedDepth;
			public string moveVal;
			public string move;
			public int eval;
			public bool isBook;
			public int numPositionsEvaluated;
		}

	}
}