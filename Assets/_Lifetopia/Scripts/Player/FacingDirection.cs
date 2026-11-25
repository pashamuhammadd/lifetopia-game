namespace Lifetopia.Player
{
    /// <summary>
    /// Four art directions. Order matches the canonical sprite-sheet row order D, L, R, U
    /// (Documentation/DECISIONS.md), so the int value can index sheet rows in M2.
    /// Down = front, Up = back.
    /// </summary>
    public enum FacingDirection
    {
        Down = 0,
        Left = 1,
        Right = 2,
        Up = 3
    }
}
