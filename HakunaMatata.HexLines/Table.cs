using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Alba.Framework.Collections;
using Alba.Framework.Mvvm.Models;
using Alba.Framework.Wpf;
using FindPathState = System.Tuple<System.Collections.Generic.IEnumerable<int>, int>;
using IndexWithDirection = System.Tuple<int, int>;

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
        public ObservableCollectionEx<Color> BallColors { get; private set; }
        public Ball MovingBall { get; set; }

        public Table ()
        {
            Cells = new ObservableCollectionEx<Cell>();
            Balls = new ObservableCollectionEx<Ball>();
            BallColors = new ObservableCollectionEx<Color> { Colors.Orange };
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

        public void GenerateBallColors (int count)
        {
            /*BallColors.Replace(Enumerable.Range(0, count).Select(i =>
                HslColor.From255(255 * i / count, rnd.Next(0, 255), rnd.Next(30, 150)).ToColor()));*/
            BallColors.Replace(
                Enumerable.Range(0, count).Select(i => (double)i / count).Zip(
                    Enumerable.Range(0, count).Select(i => 0.2 + i * 0.8 / (count - 1)).Shuffle(),
                    Enumerable.Range(0, count).Select(i => 0.1 + Math.Sqrt(i / (count - 1d)) * 0.5).Shuffle(),
                    (hue, saturation, lightness) => new { hue, saturation, lightness })
                    .Select(hsl => new HslColor(hsl.hue, hsl.saturation, hsl.lightness).ToColor()));
        }

        public void GenerateBalls (int count)
        {
            Balls.Replace(Ball.GenerateBalls(this, count));
            DestroyBallGroups();
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
            return GetNeighborsIndices(n).Where(i => Cells[i].Ball == null);
        }

        private IEnumerable<IndexWithDirection> GetSameNeighborsIndicesWithDirections (int n)
        {
            if (Cells[n].Ball == null)
                return new IndexWithDirection[0];
            var color = Cells[n].Ball.Color;
            return GetNeighborsIndicesWithDirections(n).Where(iwd => Cells[iwd.Item1].Ball != null && Cells[iwd.Item1].Ball.Color == color);
        }

        private IEnumerable<IndexWithDirection> GetNeighborsIndicesWithDirections (int n)
        {
            int h = CellHeight, d = ((n / h) & 1) - 1;
            return new[] { new[] { n - 1, -3 }, new[] { n - h + d, -2 }, new[] { n - h + d + 1, -1 }, new[] { n + 1, 1 }, new[] { n + h + d + 1, 2 }, new[] { n + h + d, 3 } }
                .Where(i => i[0] >= 0 && i[0] < Cells.Count && Math.Abs(i[0] % h - n % h) <= 1)
                .Select(i => new IndexWithDirection(i[0], i[1]));
        }

        private IEnumerable<int> GetNeighborsIndices (int n)
        {
            int h = CellHeight, d = ((n / h) & 1) - 1;
            return new[] { n - 1, n - h + d, n - h + d + 1, n + 1, n + h + d + 1, n + h + d }
                .Where(i => i >= 0 && i < Cells.Count && Math.Abs(i % h - n % h) <= 1);
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

        public void StartMoveBallTo (Ball ball, Cell toCell)
        {
            ball.StartMoveTo(toCell);
        }

        public void EndMoveBallTo ()
        {
            MovingBall.EndMoveTo();
            DestroyBallGroups();
        }

        private void DestroyBallGroups ()
        {
            Enumerable.Range(0, Cells.Count)
                .SelectMany(ic => new[] { 1, 2, 3 }, (ic, dir) => GetLineGroup(ic, dir).ToList())
                .Where(group => group.Count >= GameConstants.MinLineLength)
                .SelectMany()
                .ForEach(iwd => DestroyBall(Cells[iwd.Item1].Ball));
        }

        private IEnumerable<IndexWithDirection> GetLineGroup (int n, int dir)
        {
            return new IndexWithDirection(n, dir).TraverseList(iwdi =>
                GetSameNeighborsIndicesWithDirections(iwdi.Item1).SingleOrDefault(iwd => iwd.Item2 == dir));
        }

        private void DestroyBall (Ball ball)
        {
            ball.StartMoveTo(null);
            Balls.Remove(ball);
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