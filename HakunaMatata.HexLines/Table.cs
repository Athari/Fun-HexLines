using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Alba.Framework.Collections;
using Alba.Framework.Mvvm.Models;
using Alba.Framework.Wpf;

namespace HakunaMatata.HexLines
{
    using FindPathState = Tuple<IEnumerable<int>, int>;
    using IndexWithDir = Tuple<int, int>;

    public class Table : ModelBase<Table>
    {
        public const double CellXOffset = 45;
        public const double CellYOffset = 52;

        private Cell _selectedCell;
        private int _cellWidth;
        private int _cellHeight;

        public GameMode Mode { get; set; }
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
            BallColors.Replace(
                count.Range().Select(i => (double)i / count).Zip(
                    count.Range().Select(i => 0.3 + i * 0.7 / (count - 1)).Shuffle(),
                    count.Range().Select(i => 0.1 + Math.Sqrt(i / (count - 1d)) * 0.7).Shuffle(),
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

        private IEnumerable<IndexWithDir> GetSameNeighborsIndicesWithDirs (int n)
        {
            if (Cells[n].Ball == null)
                return new IndexWithDir[0];
            var color = Cells[n].Ball.Color;
            return GetNeighborsIndicesWithDirs(n).Where(iwd => Cells[iwd.Item1].Ball != null && Cells[iwd.Item1].Ball.Color == color);
        }

        private IEnumerable<IndexWithDir> GetNeighborsIndicesWithDirs (int n)
        {
            int h = CellHeight, d = ((n / h) & 1) - 1;
            return new[] { new[] { n - 1, -3 }, new[] { n - h + d, -2 }, new[] { n - h + d + 1, -1 }, new[] { n + 1, 1 }, new[] { n + h + d + 1, 2 }, new[] { n + h + d, 3 } }
                .Where(i => i[0] >= 0 && i[0] < Cells.Count && Math.Abs(i[0] % h - n % h) <= 1)
                .Select(i => new IndexWithDir(i[0], i[1]));
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
            var getDestroyableGroups = new Dictionary<GameMode, Func<int, IEnumerable<IEnumerable<int>>>> {
                { GameMode.Lines, GetDestroyableLines },
                { GameMode.Groups, GetDestroyableGroups },
            }[Mode];
            Cells.Count.Range().SelectMany(getDestroyableGroups).SelectMany()
                .Select(ic => Cells[ic].Ball).Where(ball => ball != null)
                .ForEach(DestroyBall);
        }

        private IEnumerable<IEnumerable<int>> GetDestroyableLines (int ic)
        {
            return 1.Range(3)
                .Select(dir => new IndexWithDir(ic, dir)
                    .TraverseList(iwdi => GetSameNeighborsIndicesWithDirs(iwdi.Item1).SingleOrDefault(iwd => iwd.Item2 == dir))
                    .ToList())
                .Where(group => group.Count >= GameConstants.MinLineLength)
                .Select(group => group.Select(iwd => iwd.Item1));
        }

        private IEnumerable<IEnumerable<int>> GetDestroyableGroups (int ic)
        {
            List<int> group = ic.TraverseGraph(i => GetSameNeighborsIndicesWithDirs(i).Select(iwd => iwd.Item1)).ToList();
            if (group.Count >= GameConstants.MinGroupLength)
                yield return group;
        }

        private void DestroyBall (Ball ball)
        {
            ball.Destroy();
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