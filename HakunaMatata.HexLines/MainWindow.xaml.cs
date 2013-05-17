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

            DataContext = _table;
            InitializeComponent();
        }

        private void FigCell_OnMouseDown (object sender, MouseButtonEventArgs e)
        {
            var cell = (Cell)((FrameworkElement)e.Source).DataContext;
            _table.SelectedCell = cell;
            Title = _table.Cells.IndexOf(cell).ToString();
        }
    }

    public class Table
    {
        private Cell _selectedCell;

        public int TableWidth { get; private set; }
        public int TableHeight { get; private set; }

        public ObservableCollectionEx<Cell> Cells { get; private set; }

        public Table ()
        {
            Cells = new ObservableCollectionEx<Cell>();
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
            get { return (TableWidth + .5) * Cell.CellXOffset; }
        }

        public double Height
        {
            get { return (TableHeight + .5) * Cell.CellYOffset; }
        }

        public void Resize (int w, int h)
        {
            TableWidth = w;
            TableHeight = h;
            SelectedCell = null;
            Cells.Replace(Cell.CreateTableCells(TableWidth, TableHeight));
        }

        private void UpdateSelection (Cell cell, bool value)
        {
            if (cell == null)
                return;
            cell.IsSelected = value;
            int n = Cells.IndexOf(cell), h = TableHeight, d = ((n / h) & 1) - 1;
            new[] { n - h + d, n - h + d + 1, n - 1, n + 1, n + h + d, n + h + d + 1 }
                .Where(i => i >= 0 && i < Cells.Count && Math.Abs(i % h - n % h) <= 1)
                .Select(i => Cells[i])
                .ForEach(c => c.IsAvailable = value && !c.HasBall);
        }
    }

    public class Cell : ModelBase<Cell>
    {
        public const double CellXOffset = 45;
        public const double CellYOffset = 52;

        private bool _isSelected;
        private bool _isAvailable;

        public double X { get; private set; }
        public double Y { get; private set; }
        public bool HasBall { get; private set; }
        public Color BallColor { get; private set; }

        private Cell ()
        {}

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

        public Brush BallColorBrush
        {
            get { return new SolidColorBrush(BallColor); }
        }

        public static IEnumerable<Cell> CreateTableCells (int w, int h)
        {
            var rnd = new Random();
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    yield return new Cell {
                        X = 30 + x * CellXOffset,
                        Y = 30 + y * CellYOffset + ((x & 1) != 0 ? CellYOffset / 2 : 0),
                        BallColor = new Color {
                            ScR = (float)rnd.NextDouble(),
                            ScG = (float)rnd.NextDouble(),
                            ScB = (float)rnd.NextDouble(),
                            ScA = 255,
                        },
                        HasBall = rnd.Next(3) == 0
                    };
        }
    }
}