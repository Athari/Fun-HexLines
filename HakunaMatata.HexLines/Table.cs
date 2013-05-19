using System;
using System.Collections.Generic;
using System.Linq;
using Alba.Framework.Collections;
using Alba.Framework.Mvvm.Models;
using FindPathState = System.Tuple<System.Collections.Generic.IEnumerable<int>, int>;

namespace HakunaMatata.HexLines
{
    public class Table : ModelBase<Table>
    {
        public const double CellXOffset = 45;
        public const double CellYOffset = 52;

        private Cell _selectedCell;
        private int _cellWidth;
        private int _cellHeight;

        public ObservableCollectionEx<Cell> Cells { get; private set; }
        public ObservableCollectionEx<Ball> Balls { get; private set; }
        public Ball MovingBall { get; set; }

        public Table ()
        {
            Cells = new ObservableCollectionEx<Cell>();
            Balls = new ObservableCollectionEx<Ball>();
        }

        public int CellWidth
        {
            get { return _cellWidth; }
            private set { Set(ref _cellWidth, value, "CellWidth", "Width"); }
        }

        public int CellHeight
        {
            get { return _cellHeight; }
            private set { Set(ref _cellHeight, value, "CellHeight", "Height"); }
        }

        public Cell SelectedCell
        {
            get { return _selectedCell; }
            set
            {
                UpdateSelection(_selectedCell, false);
                UpdateSelection(_selectedCell = value, true);
            }
        }

        public double Width
        {
            get { return (CellWidth + .5) * CellXOffset; }
        }

        public double Height
        {
            get { return (CellHeight + .6) * CellYOffset; }
        }

        public void Resize (int w, int h)
        {
            CellWidth = w;
            CellHeight = h;
            SelectedCell = null;
            Cells.Replace(Cell.GenerateTableCells(CellWidth, CellHeight));
            Balls.Clear();
        }

        public void FillBalls (int count)
        {
            Balls.Replace(Ball.GenerateBalls(Cells, CellWidth, CellHeight, count));
        }

        private void UpdateSelection (Cell cell, bool value)
        {
            if (cell == null)
                return;
            cell.IsSelected = value;
            GetAllFreeNeighbors(cell).ForEach(c => c.IsAvailable = value);
        }

        private IEnumerable<Cell> GetAllFreeNeighbors (Cell cell)
        {
            return Cells.IndexOf(cell).TraverseGraph(GetFreeNeighborsIndices).Select(i => Cells[i]).Where(c => c != cell);
        }

        private IEnumerable<int> GetFreeNeighborsIndices (int n)
        {
            int h = CellHeight, d = ((n / h) & 1) - 1;
            return new[] { n - h + d, n - h + d + 1, n - 1, n + 1, n + h + d, n + h + d + 1 }
                .Where(i => i >= 0 && i < Cells.Count && Math.Abs(i % h - n % h) <= 1 && Cells[i].Ball == null);
        }

        public IEnumerable<Cell> FindPath (Cell from, Cell to)
        {
            return FindPath(Cells.IndexOf(from), Cells.IndexOf(to)).Select(i => Cells[i]);
        }

        private IEnumerable<int> FindPath (int from, int to)
        {
            var visited = new HashSet<int>();
            var queue = new Queue<FindPathState>();
            queue.Enqueue(new FindPathState(new int[0], from));
            while (true) {
                var state = queue.Dequeue();
                if (state.Item2 == to)
                    return state.Item1;
                if (visited.Add(state.Item2))
                    foreach (int ic in GetFreeNeighborsIndices(state.Item2))
                        queue.Enqueue(new FindPathState(state.Item1.Concat(ic), ic));
            }
        }

        public static double GetCellX (int x, int y)
        {
            return 30 + x * CellXOffset;
        }

        public static double GetCellY (int x, int y)
        {
            return 30 + y * CellYOffset + ((x & 1) != 0 ? CellYOffset / 2 : 0);
        }
    }
}