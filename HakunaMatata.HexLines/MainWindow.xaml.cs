using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Alba.Framework.Collections;
using Alba.Framework.Mvvm.Models;

namespace HakunaMatata.HexLines
{
    public partial class MainWindow
    {
        private readonly Table _table = new Table();

        public MainWindow ()
        {
            _table.Resize(16, 10);
            _table.FillBalls(40);

            DataContext = _table;
            InitializeComponent();
        }

        private void FigCell_OnMouseDown (object sender, MouseButtonEventArgs e)
        {
            var cell = (Cell)((FrameworkElement)e.Source).DataContext;
            _table.SelectedCell = cell;
            Title = _table.Cells.IndexOf(cell).ToString();
        }

        private void LstTable_OnMouseDown (object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
                _table.SelectedCell = null;
        }
    }

    public class Table
    {
        public const double CellXOffset = 45;
        public const double CellYOffset = 52;

        private Cell _selectedCell;

        public int TableWidth { get; private set; }
        public int TableHeight { get; private set; }

        public ObservableCollectionEx<Cell> Cells { get; private set; }
        public ObservableCollectionEx<Ball> Balls { get; private set; }

        public Table ()
        {
            Cells = new ObservableCollectionEx<Cell>();
            Balls = new ObservableCollectionEx<Ball>();
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
            get { return (TableWidth + .5) * CellXOffset; }
        }

        public double Height
        {
            get { return (TableHeight + .6) * CellYOffset; }
        }

        public void Resize (int w, int h)
        {
            TableWidth = w;
            TableHeight = h;
            SelectedCell = null;
            Cells.Replace(Cell.GenerateTableCells(TableWidth, TableHeight));
        }

        public void FillBalls (int count)
        {
            Balls.Replace(Ball.GenerateBalls(Cells, TableWidth, TableHeight, count));
        }

        private void UpdateSelection (Cell cell, bool value)
        {
            if (cell == null)
                return;
            cell.IsSelected = value;
            GetNeighbors(cell).ForEach(c => c.IsAvailable = value && c.Ball == null);
        }

        private IEnumerable<Cell> GetNeighbors (Cell cell)
        {
            return GetNeighborsIndices(Cells.IndexOf(cell)).Select(i => Cells[i]);
        }

        private IEnumerable<int> GetNeighborsIndices (int n)
        {
            int h = TableHeight, d = ((n / h) & 1) - 1;
            return new[] { n - h + d, n - h + d + 1, n - 1, n + 1, n + h + d, n + h + d + 1 }
                .Where(i => i >= 0 && i < Cells.Count && Math.Abs(i % h - n % h) <= 1);
        }

        public static double GetCellX (int x, int y)
        {
            return x * CellXOffset;
        }

        public static double GetCellY (int x, int y)
        {
            return y * CellYOffset + ((x & 1) != 0 ? CellYOffset / 2 : 0);
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

        public static IEnumerable<Cell> GenerateTableCells (int w, int h)
        {
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    yield return new Cell(Table.GetCellX(x, y) + 30, Table.GetCellY(x, y) + 30);
        }
    }

    public class Ball
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public Color Color { get; private set; }
        public Cell Cell { get; private set; }

        private Ball (int x, int y, Cell cell, Color color)
        {
            X = Table.GetCellX(x, y) + 10;
            Y = Table.GetCellY(x, y) + 10;
            Color = color;
            Cell = cell;
            cell.Ball = this;
        }

        public void MoveTo (Cell cell)
        {
            Cell.Ball = null;
            Cell = cell;
            Cell.Ball = this;
        }

        public Brush BallColorBrush
        {
            get { return new SolidColorBrush(Color); }
        }

        public static IEnumerable<Ball> GenerateBalls (IList<Cell> cells, int w, int h, int count)
        {
            var rnd = new Random();
            return Enumerable.Range(0, w * h).OrderBy(i => rnd.Next()).Take(count)
                .Select(ic => new Ball(ic / h, ic % h, cells[ic], new Color {
                    ScR = (float)rnd.NextDouble(),
                    ScG = (float)rnd.NextDouble(),
                    ScB = (float)rnd.NextDouble(),
                    ScA = 255,
                }));
        }
    }
}