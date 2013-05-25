using Alba.Framework.Sys;

namespace HakunaMatata.HexLines
{
    public static class GameConstants
    {
        public const string CompanyName = "Alba";
        public const string ProductName = "HexLines";

        public const int MinLineLength = 5;
        public const int MinGroupLength = 6;

        public const int PointsPerLine = 10;
        public const int PointsPerLineExtra = 5;
        public const int PointsPerGroup = 10;
        public const int PointsPerGroupExtra = 5;

        public const double StartBallFillRatio = 0.10;

        public static string RoamingAppDir
        {
            get { return Paths.GetRoamingAppDir(CompanyName, ProductName); }
        }
    }
}