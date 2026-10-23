namespace Snake.Core;

public enum SpeedLevel
{
    Slow,
    Normal,
    Fast,

    /// <summary>Starts slow and gets faster with every mouse.</summary>
    Progressive
}

public static class GameSpeed
{
    public static readonly TimeSpan ProgressiveStart = TimeSpan.FromMilliseconds(140);
    public static readonly TimeSpan ProgressiveStep = TimeSpan.FromMilliseconds(4);
    public static readonly TimeSpan Fastest = TimeSpan.FromMilliseconds(55);

    /// <summary>Time between two moves at <paramref name="level"/> when the best score on the board is <paramref name="score"/>.</summary>
    public static TimeSpan TickInterval(SpeedLevel level, int score) => level switch
    {
        SpeedLevel.Slow => TimeSpan.FromMilliseconds(150),
        SpeedLevel.Normal => TimeSpan.FromMilliseconds(100),
        SpeedLevel.Fast => TimeSpan.FromMilliseconds(65),
        SpeedLevel.Progressive => Max(Fastest, ProgressiveStart - ProgressiveStep * Math.Max(0, score)),
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
    };

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
