namespace Sugoroku.Board
{
    /// <summary>Project Settings の Sorting Layers（下→上: Background → Board → Player）。</summary>
    public static class BoardSortingLayers
    {
        public const string Background = "Background";
        public const string Board      = "Board";
        public const string Player     = "Player";

        public const int BackgroundOrder = 0;
        public const int PathOrder       = 10;
        public const int WaypointBaseOrder = 100;
    }
}
