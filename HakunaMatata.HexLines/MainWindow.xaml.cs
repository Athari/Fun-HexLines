using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Alba.Framework.Collections;
using Alba.Framework.Mvvm.Models;
using Alba.Framework.Wpf;
using FindPathState = System.Tuple<System.Collections.Generic.IEnumerable<int>, int>;

namespace HakunaMatata.HexLines
{
    public partial class MainWindow
    {
        private const double AnimMovingBallStep = 0.05;

        private readonly Table _table = new Table();
        private Storyboard _animMovingBall;

        public MainWindow ()
        {
            _table.Resize(20, 10);
            _table.FillBalls(60);

            DataContext = _table;
            InitializeComponent();
        }

        protected override void OnKeyDown (KeyEventArgs e)
        {
            switch (e.Key) {
                case Key.F2:
                    var rnd = new Random();
                    _table.Resize(rnd.Next(8, 30), rnd.Next(8, 20));
                    _table.FillBalls(rnd.Next(10, _table.Cells.Count));
                    break;
            }
            base.OnKeyDown(e);
        }

        private void FigCell_OnMouseDown (object sender, MouseButtonEventArgs e)
        {
            var cell = e.GetSourceDataContext<Cell>();
            if (_animMovingBall != null || e.ChangedButton != MouseButton.Left)
                return;
            if (cell.IsAvailable) {
                Cell selectedCell = _table.SelectedCell;
                Ball selectedBall = selectedCell.Ball;
                _table.SelectedCell = null;
                AnimateMoveBall(selectedBall, selectedCell, cell);
            }
            else if (cell.Ball != null) {
                _table.SelectedCell = cell;
            }
        }

        private void AnimateMoveBall (Ball ball, Cell fromCell, Cell toCell)
        {
            List<Point> path = _table.FindPath(fromCell, toCell).Select(c => c.BallPoint).ToList();
            _table.MovingBall = ball;
            ball.StartMoveTo(toCell);

            TimeSpan duration = TimeSpan.FromSeconds(AnimMovingBallStep * path.Count);
            DoubleAnimationUsingPath animX;
            _animMovingBall = new Storyboard {
                Children = {
                    (animX = new DoubleAnimationUsingPath {
                        PathGeometry = new PathGeometry {
                            Figures = new PathFigureCollection {
                                new PathFigure(fromCell.BallPoint, path.Select(p => new LineSegment(p, false)), false)
                            }
                        },
                        Duration = new Duration(duration),
                        AccelerationRatio = .1, DecelerationRatio = .7, Source = PathAnimationSource.X,
                    }.SetTarget(Canvas.LeftProperty.ToPath())),
                    animX.CloneForY().SetTarget(Canvas.TopProperty.ToPath()),
                }
            };
            _animMovingBall.AddCompleted(AnimMove_OnCompleted);
            _animMovingBall.Begin(lstBalls.GetItemContainer(toCell.Ball));
        }

        private void AnimMove_OnCompleted (object sender, EventArgs e)
        {
            _animMovingBall.Remove(lstBalls.GetItemContainer(_table.MovingBall));
            _animMovingBall = null;
            _table.MovingBall.EndMoveTo();
        }

        private void LstTable_OnMouseDown (object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
                _table.SelectedCell = null;
        }
    }

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

    public class Cell : ModelBase<Cell>
    {
        private bool _isSelected;
        private bool _isAvailable;

        public double X { get; private set; }
        public double Y { get; private set; }
        public Ball Ball { get; internal set; }

        public Cell (double x, double y)
        {
            X = x;
            Y = y;
        }

        public bool IsSelected
        {
            get { return Get(ref _isSelected); }
            set { Set(ref _isSelected, value); }
        }

        public bool IsAvailable
        {
            get { return Get(ref _isAvailable); }
            set { Set(ref _isAvailable, value); }
        }

        public Point BallPoint
        {
            get { return new Point(X + Ball.BallCellDelta, Y + Ball.BallCellDelta); }
        }

        public static IEnumerable<Cell> GenerateTableCells (int w, int h)
        {
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    yield return new Cell(Table.GetCellX(x, y), Table.GetCellY(x, y));
        }
    }

    public class Ball : ModelBase<Ball>
    {
        public const int BallCellDelta = -20;

        private double _x, _y, _targetX, _targetY;
        private bool _isMoving;
        private Cell _cell;

        public Color Color { get; private set; }

        private Ball (Cell cell, Color color)
        {
            Color = color;
            Cell = cell;
            Cell.Ball = this;
            X = cell.X + BallCellDelta;
            Y = cell.Y + BallCellDelta;
        }

        public Cell Cell
        {
            get { return Get(ref _cell); }
            private set { Set(ref _cell, value); }
        }

        public double X
        {
            get { return Get(ref _x); }
            private set { Set(ref _x, value); }
        }

        public double Y
        {
            get { return Get(ref _y); }
            private set { Set(ref _y, value); }
        }

        public double TargetX
        {
            get { return Get(ref _targetX); }
            private set { Set(ref _targetX, value); }
        }

        public double TargetY
        {
            get { return Get(ref _targetY); }
            private set { Set(ref _targetY, value); }
        }

        public bool IsMoving
        {
            get { return Get(ref _isMoving); }
            private set { Set(ref _isMoving, value); }
        }

        public Brush BallColorBrush
        {
            get
            {
                //return new SolidColorBrush(Color);
                return new RadialGradientBrush(new GradientStopCollection {
                    new GradientStop(Colors.WhiteSmoke, 0),
                    new GradientStop(Color, 0.2),
                    new GradientStop(Color.Darker(0.5f), 0.7),
                    new GradientStop(Color.Darker(0.9f), 1),
                }) {
                    GradientOrigin = new Point(.3, .35),
                    MappingMode = BrushMappingMode.RelativeToBoundingBox,
                    SpreadMethod = GradientSpreadMethod.Pad,
                };
            }
        }

        public void StartMoveTo (Cell cell)
        {
            if (Cell != null)
                Cell.Ball = null;
            Cell = cell;
            Cell.Ball = this;
            TargetX = cell.X + BallCellDelta;
            TargetY = cell.Y + BallCellDelta;
            IsMoving = true;
        }

        public void EndMoveTo ()
        {
            X = TargetX;
            Y = TargetY;
            IsMoving = false;
        }

        public static IEnumerable<Ball> GenerateBalls (IList<Cell> cells, int w, int h, int count)
        {
            var rnd = new Random();
            return Enumerable.Range(0, w * h).OrderBy(i => rnd.Next()).Take(count)
                .Select(ic => new Ball(cells[ic], new Color {
                    ScR = (float)rnd.NextDouble(),
                    ScG = (float)rnd.NextDouble(),
                    ScB = (float)rnd.NextDouble(),
                    ScA = 255,
                }));
        }
    }
}