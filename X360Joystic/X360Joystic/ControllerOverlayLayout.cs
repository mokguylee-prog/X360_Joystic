namespace X360Joystic
{
    internal readonly record struct OverlayPoint(double Left, double Top);

    internal static class ControllerOverlayLayout
    {
        public static readonly OverlayPoint LT = new(95, 75);
        public static readonly OverlayPoint RT = new(345, 73);

        public static readonly OverlayPoint LB = new(72, 112);
        public static readonly OverlayPoint RB = new(345, 112);

        public static readonly OverlayPoint Back = new(180, 198);
        public static readonly OverlayPoint Start = new(281, 198);

        public static readonly OverlayPoint L3 = new(82, 178);
        public static readonly OverlayPoint R3 = new(285, 264);

        public static readonly OverlayPoint DPadUp = new(162, 251);
        public static readonly OverlayPoint DPadDown = new(162, 298);
        public static readonly OverlayPoint DPadLeft = new(138, 274);
        public static readonly OverlayPoint DPadRight = new(191, 275);

        public static readonly OverlayPoint Y = new(371, 158);
        public static readonly OverlayPoint X = new(333, 193);
        public static readonly OverlayPoint B = new(408, 195);
        public static readonly OverlayPoint A = new(370, 232);

        public static readonly OverlayPoint LeftStickDotNeutral = new(101, 197);
        public static readonly OverlayPoint RightStickDotNeutral = new(304, 283);

        public static readonly OverlayPoint LeftStickCenter = new(108, 204);
        public static readonly OverlayPoint RightStickCenter = new(311, 290);

        public const double StickRange = 22;
        public const double DotHalf = 7;
    }
}
