using Alba.Framework.Mvvm.Models;
using Alba.Framework.Sys;
using Newtonsoft.Json;

namespace HakunaMatata.HexLines
{
    [JsonObject]
    public class GameOptions : ModelBase<GameOptions>
    {
        public const int
            MinTableCellWidth = 10, MaxTableCellWidth = 40,
            MinTableCellHeight = 6, MaxTableCellHeight = 16,
            MinBallColorsCount = 3, MaxBallColorsCount = 20,
            MinNewBallsCount = 1, MaxNewBallsCount = 10;

        private int _tableCellWidth, _tableCellHeight, _ballColorsCount, _newBallsCount;
        private GameMode _mode;

        public GameOptions ()
        {
            TableCellWidth = 20;
            TableCellHeight = 10;
            BallColorsCount = 8;
            NewBallsCount = 30;
            Mode = GameMode.Lines;
        }

        public int TableCellWidth
        {
            get { return Get(ref _tableCellWidth); }
            set { Set(ref _tableCellWidth, value.MinMax(MinTableCellWidth, MaxTableCellWidth)); }
        }

        public int TableCellHeight
        {
            get { return Get(ref _tableCellHeight); }
            set { Set(ref _tableCellHeight, value.MinMax(MinTableCellHeight, MaxTableCellHeight)); }
        }

        public int BallColorsCount
        {
            get { return Get(ref _ballColorsCount); }
            set { Set(ref _ballColorsCount, value.MinMax(MinBallColorsCount, MaxBallColorsCount)); }
        }

        public int NewBallsCount
        {
            get { return Get(ref _newBallsCount); }
            set { Set(ref _newBallsCount, value.MinMax(MinNewBallsCount, MaxNewBallsCount)); }
        }

        public GameMode Mode
        {
            get { return Get(ref _mode); }
            set { Set(ref _mode, value); }
        }
    }
}