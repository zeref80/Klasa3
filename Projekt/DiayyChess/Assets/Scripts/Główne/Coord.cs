using System;
namespace Chess {
	public struct Coord : IComparable<Coord> {
		public readonly int fileIndex;
		public readonly int rankIndex;

        //kordy w tablicy pól
		public Coord (int fileIndex, int rankIndex) {
			this.fileIndex = fileIndex;
			this.rankIndex = rankIndex;
		}

        //określenie koloru pola
		public bool IsLightSquare () {
			return (fileIndex + rankIndex) % 2 != 0;
		}
        // porównanie koord.
		public int CompareTo (Coord other) {
			return (fileIndex == other.fileIndex && rankIndex == other.rankIndex) ? 0 : 1;
		}
	}
}