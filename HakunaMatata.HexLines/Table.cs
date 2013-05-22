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

    public class Table : ModelBase<Table>
    {
        public const double CellXOffset = 45;
        public const double CellYOffset = 52;

        private int _score, _cellWidth, _cellHeight;
        private bool _isGameOver;
        private Cell _selectedCell;

        public GameMode Mode { get; set; }
        public ObservableCollectionEx<Cell> Cells { get; private set; }
        public ObservableCollectionEx<Ball> Balls { get; private set; }
        public ObservableCollectionEx<Ball> NewBalls { get; private set; }
        public ObservableCollectionEx<Color> BallColors { get; private set; }
        public Ball MovingBall { get; private set; }

        public Table ()
        {
            Cells = new ObservableCollectionEx<Cell>();
            Balls = new ObservableCollectionEx<Ball>();
            NewBalls = new ObservableCollectionEx<Ball>();
            BallColors = new ObservableCollectionEx<Color> { Colors.Orange };
        }

        public int Score
        {
            get { return Get(ref _score); }
            private set { Set(ref _score, value); }
        }

        public bool IsGameOver
        {
            get { return Get(ref _isGameOver); }
            private set { Set(ref _isGameOver, value); }
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
                if (_selectedCell == value)
                    return;
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

        public IEnumerable<Cell> EmptyCells
        {
            get { return Cells.Where(c => c.Ball == null); }
        }

        public void NewGame (GameMode mode = GameMode.Lines)
        {
            Mode = mode;
            Score = 0;
            IsGameOver = false;
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
                count.Range().Select(i => (0.3 + (double)i / count) % 1d).Zip(
                    count.Range().Select(i => 0.3 + Math.Sqrt(i / (count - 1d)) * 0.7).Shuffle(),
                    count.Range().Select(i => 0.1 + Math.Sqrt(i / (count - 1d)) * 0.7).Shuffle(),
                    (h, s, l) => new HslColor(h, s, l).ToColor()));
        }

        public void GenerateBalls (int count)
        {
            Balls.Replace(Ball.GenerateBalls(this, count, isNew: false));
            DestroyBallGroups(silent: true);
            AddNewBalls();
        }

        private void GrowBalls ()
        {
            Balls.Where(b => b.IsNew).ForEach(b => b.Grow());
        }

        private void AddNewBalls ()
        {
            List<Ball> newBalls = Ball.GenerateBalls(this, GameConstants.NewBallsCount, isNew: true).ToList();
            Balls.AddRange(newBalls);
            NewBalls.Replace(newBalls);
            if (!EmptyCells.Any())
                IsGameOver = true;
        }

        private void UpdateSelection (Cell cell, bool value)
        {
            if (cell == null)
                return;
            cell.IsSelected = value;
            GetAllFreeNeighbors(cell).ForEach(c => c.IsAvailable = value);
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
            if (toCell.Ball != null) {
                Ball newBall = toCell.Ball;
                List<Cell> emptyCells = EmptyCells.ToList();
                newBall.StartMoveTo(emptyCells.RandomItem());
                newBall.EndMoveTo();
            }
            MovingBall = ball;
            MovingBall.StartMoveTo(toCell);
        }

        public void EndMoveBallTo ()
        {
            MovingBall.EndMoveTo();
            if (DestroyBallGroups(silent: false) > 0)
                return;
            GrowBalls();
            AddNewBalls();
        }

        private int DestroyBallGroups (bool silent)
        {
            var getDestroyableGroups = new Dictionary<GameMode, Func<int, IEnumerable<IEnumerable<int>>>> {
                { GameMode.Lines, GetDestroyableLinesAndUpdateScore },
                { GameMode.Groups, GetDestroyableGroupsAndUpdateScore },
            }[Mode];
            return Cells.Count.Range().SelectMany(getDestroyableGroups).Flatten()
                .Select(ic => Cells[ic].Ball).Where(ball => ball != null)
                .Do(ball => DestroyBall(ball, silent))
                .Count();
        }

        private IEnumerable<IEnumerable<int>> GetDestroyableLinesAndUpdateScore (int ic)
        {
            return 1.Range(3)
                .Select(dir => new IndexWithDir(this, ic, dir)
                    .TraverseList(iwdi => GetSameNeighborsIndicesWithDirs(iwdi.Index).SingleOrDefault(iwd => iwd.Dir == dir))
                    .ToList())
                .Where(line => line.Count >= GameConstants.MinLineLength)
                .Do(line => Score += GameConstants.PointsPerLine + (line.Count - GameConstants.MinLineLength) * GameConstants.PointsPerLineExtra)
                .Select(line => line.Select(iwd => iwd.Index));
        }

        private IEnumerable<IEnumerable<int>> GetDestroyableGroupsAndUpdateScore (int ic)
        {
            return EnumerableEx.Return(0)
                .Select(dir => ic.TraverseGraph(i => GetSameNeighborsIndicesWithDirs(i).Select(iwd => iwd.Index)).ToList())
                .Where(group => group.Count >= GameConstants.MinGroupLength)
                .Do(group => Score += GameConstants.PointsPerGroup + (group.Count - GameConstants.MinGroupLength) * GameConstants.PointsPerGroupExtra);
        }

        private void DestroyBall (Ball ball, bool silent)
        {
            if (silent) {
                ball.StartMoveTo(null);
                Balls.Remove(ball);
            }
            else
                ball.Destroy();
        }

        private IEnumerable<Cell> GetAllFreeNeighbors (Cell cell)
        {
            return Cells.IndexOf(cell).TraverseGraph(GetFreeNeighborsIndices).Select(i => Cells[i]).Where(c => c != cell);
        }

        private IEnumerable<int> GetFreeNeighborsIndices (int n)
        {
            return GetNeighborsIndices(n).Where(i => Cells[i].Ball == null || Cells[i].Ball.IsNew);
        }

        private IEnumerable<IndexWithDir> GetSameNeighborsIndicesWithDirs (int n)
        {
            if (Cells[n].Ball == null)
                return new IndexWithDir[0];
            var color = Cells[n].Ball.Color;
            return GetNeighborsIndicesWithDirs(n).Where(iwd => iwd.Ball != null && iwd.Ball.Color == color && !iwd.Ball.IsNew);
        }

        private IEnumerable<IndexWithDir> GetNeighborsIndicesWithDirs (int n)
        {
            int h = CellHeight, d = ((n / h) & 1) - 1;
            return new[] { new[] { n - 1, -3 }, new[] { n - h + d, -2 }, new[] { n - h + d + 1, -1 }, new[] { n + 1, 1 }, new[] { n + h + d + 1, 2 }, new[] { n + h + d, 3 } }
                .Where(i => i[0] >= 0 && i[0] < Cells.Count && Math.Abs(i[0] % h - n % h) <= 1)
                .Select(i => new IndexWithDir(this, i[0], i[1]));
        }

        private IEnumerable<int> GetNeighborsIndices (int n)
        {
            int h = CellHeight, d = ((n / h) & 1) - 1;
            return new[] { n - 1, n - h + d, n - h + d + 1, n + 1, n + h + d + 1, n + h + d }
                .Where(i => i >= 0 && i < Cells.Count && Math.Abs(i % h - n % h) <= 1);
        }

        public static double GetCellX (int x, int y)
        {
            return 30 + x * CellXOffset;
        }

        public static double GetCellY (int x, int y)
        {
            return 30 + y * CellYOffset + ((x & 1) != 0 ? CellYOffset / 2 : 0);
        }

        private class IndexWithDir : IEquatable<IndexWithDir>
        {
            private readonly Table _table;

            public int Index { get; private set; }

            public int Dir { get; private set; }

            private Cell Cell
            {
                get { return _table.Cells[Index]; }
            }

            public Ball Ball
            {
                get { return Cell.Ball; }
            }

            public IndexWithDir (Table table, int index, int dir)
            {
                _table = table;
                Index = index;
                Dir = dir;
            }

            public bool Equals (IndexWithDir other)
            {
                return Index == other.Index && Dir == other.Dir;
            }

            public override bool Equals (object obj)
            {
                return Equals((IndexWithDir)obj);
            }

            public override int GetHashCode ()
            {
                return (Index * 397) ^ Dir;
            }
        }
    }
}