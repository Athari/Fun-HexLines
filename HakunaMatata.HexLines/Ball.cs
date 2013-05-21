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
        private bool _isMoving, _isDestroyed, _isNew;
        private Cell _cell;

        public Color Color { get; private set; }

        private Ball (Cell cell, Color color, bool isNew)
        {
            Cell = cell;
            Cell.Ball = this;
            Color = color;
            IsNew = isNew;
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

        public bool IsNew
        {
            get { return Get(ref _isNew); }
            private set { Set(ref _isNew, value); }
        }

        public bool IsMoving
        {
            get { return Get(ref _isMoving); }
            private set { Set(ref _isMoving, value); }
        }

        public bool IsDestroyed
        {
            get { return Get(ref _isDestroyed); }
            private set { Set(ref _isDestroyed, value); }
        }

        public Brush BallColorBrush
        {
            get
            {
                return new RadialGradientBrush { GradientOrigin = new Point(.3, .35) }
                    .Add(Colors.WhiteSmoke, 0)
                    .Add(Color, 0.2)
                    .Add(Color.Darker(0.5f), 0.7)
                    .Add(Color.Darker(0.9f), 1);
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

        public void Grow ()
        {
            IsNew = false;
        }

        public void Destroy ()
        {
            StartMoveTo(null);
            IsDestroyed = true;
        }

        public static IEnumerable<Ball> GenerateBalls (Table table, int count, bool isNew)
        {
            return table.EmptyCells.Shuffle().Take(count).Select(c =>
                new Ball(c, table.BallColors.RandomItem(), isNew));
        }
    }
}