namespace Snake.Core;

public enum WallMode
{
    /// <summary>Hitting the edge ends the game.</summary>
    Solid,

    /// <summary>Leaving one edge comes back in on the opposite side.</summary>
    Wrap
}

[Flags]
public enum MoveResult
{
    None = 0,
    Ate = 1,
    Died = 2,
    BoardFull = 4
}

/// <summary>Rules that stay fixed for one round.</summary>
public sealed record GameOptions
{
    public static readonly GameOptions Default = new();

    public int PlayerCount { get; init; } = 1;
    public WallMode Walls { get; init; } = WallMode.Solid;
}
