using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Alba.Framework.Collections;
using Alba.Framework.Mvvm.Models;
using Alba.Framework.Wpf;

namespace HakunaMatata.HexLines
{
    public class Ball : ModelBase<Ball>
    {
        public const int BallCellDelta = -20;

        private double _x, _y, _targetX, _targetY;
        private bool _isMoving;
        private Cell _cell;

        public Color Color { get; private set; }

        private Ball (Cell cell, Color color)
        {
            Cell = cell;
            Cell.Ball = this;
            Color = color;
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
            if (cell == null)
                return;
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

        public static IEnumerable<Ball> GenerateBalls (Table table, int count)
        {
            return Enumerable.Range(0, table.CellWidth * table.CellHeight).Shuffle().Take(count).Select(ic =>
                new Ball(table.Cells[ic], table.BallColors.RandomItem()));
        }
    }
}