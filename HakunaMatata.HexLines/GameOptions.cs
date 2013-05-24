using Alba.Framework.Mvvm.Models;
using Newtonsoft.Json;

namespace HakunaMatata.HexLines
{
    [JsonObject]
    public class GameOptions : ModelBase<GameOptions>
    {
        private int _tableCellWidth, _tableCellHeight, _ballColorsCount, _newBallsCount;

        public GameOptions ()
        {
            TableCellWidth = 20;
            TableCellHeight = 10;
            BallColorsCount = 8;
            NewBallsCount = 30;
        }

        public int TableCellWidth
        {
            get { return Get(ref _tableCellWidth); }
            set { Set(ref _tableCellWidth, value); }
        }

        public int TableCellHeight
        {
            get { return Get(ref _tableCellHeight); }
            set { Set(ref _tableCellHeight, value); }
        }

        public int BallColorsCount
        {
            get { return Get(ref _ballColorsCount); }
            set { Set(ref _ballColorsCount, value); }
        }

        public int NewBallsCount
        {
            get { return Get(ref _newBallsCount); }
            set { Set(ref _newBallsCount, value); }
        }
    }
}