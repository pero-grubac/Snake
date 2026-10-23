namespace Snake.Core;

public enum GameAction
{
    P1Up,
    P1Down,
    P1Left,
    P1Right,
    P2Up,
    P2Down,
    P2Left,
    P2Right,
    Pause,
    Screenshot,
    Fullscreen
}

/// <summary>
/// Which key triggers which action. Keys are WPF <c>Key</c> names (e.g. <c>"W"</c>, <c>"Up"</c>)
/// so this class stays free of UI types. Instances are immutable.
/// </summary>
public sealed class KeyBindings
{
    /// <summary>Always means "back"; it cannot be bound to anything.</summary>
    public const string ReservedKey = "Escape";

    private readonly Dictionary<GameAction, string> keys;

    private KeyBindings(Dictionary<GameAction, string> keys)
    {
        this.keys = keys;
    }

    public static KeyBindings Default { get; } = new(new Dictionary<GameAction, string>
    {
        [GameAction.P1Up] = "W",
        [GameAction.P1Down] = "S",
        [GameAction.P1Left] = "A",
        [GameAction.P1Right] = "D",
        [GameAction.P2Up] = "Up",
        [GameAction.P2Down] = "Down",
        [GameAction.P2Left] = "Left",
        [GameAction.P2Right] = "Right",
        [GameAction.Pause] = "P",
        [GameAction.Screenshot] = "S",
        [GameAction.Fullscreen] = "F",
    });

    public string this[GameAction action] => keys[action];

    public IReadOnlyDictionary<GameAction, string> All => keys;

    public static bool IsMovement(GameAction action) => action <= GameAction.P2Right;

    /// <summary>Screenshot only works while paused, so it may share a key with a movement action.</summary>
    public static bool CanShareKey(GameAction a, GameAction b) =>
        (a == GameAction.Screenshot && IsMovement(b)) || (b == GameAction.Screenshot && IsMovement(a));

    /// <summary>
    /// Builds bindings from saved values. Missing entries use the defaults; a set with blank,
    /// reserved or clashing keys is thrown away as a whole.
    /// </summary>
    public static KeyBindings From(IReadOnlyDictionary<GameAction, string>? saved)
    {
        if (saved is null)
        {
            return Default;
        }

        var merged = new Dictionary<GameAction, string>(Default.keys);
        foreach ((GameAction action, string key) in saved)
        {
            if (!Enum.IsDefined(action) || !IsBindable(key))
            {
                return Default;
            }
            merged[action] = key;
        }

        var bindings = new KeyBindings(merged);
        return bindings.Conflicts().Any() ? Default : bindings;
    }

    public static bool IsBindable(string? key) =>
        !string.IsNullOrWhiteSpace(key) && !string.Equals(key, ReservedKey, StringComparison.OrdinalIgnoreCase);

    public IEnumerable<GameAction> ActionsFor(string key) =>
        keys.Where(entry => string.Equals(entry.Value, key, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Key);

    /// <summary>
    /// Binds <paramref name="key"/> to <paramref name="action"/>. An action that already used the key
    /// gets the old key of <paramref name="action"/> instead, so no key ever does two things at once.
    /// </summary>
    public KeyBindings Rebind(GameAction action, string key)
    {
        if (!IsBindable(key))
        {
            return this;
        }

        string oldKey = keys[action];
        var updated = new Dictionary<GameAction, string>(keys) { [action] = key };
        foreach (GameAction other in ActionsFor(key).ToList())
        {
            if (other != action && !CanShareKey(action, other))
            {
                updated[other] = oldKey;
            }
        }

        // Swapping can still clash, e.g. Pause taking Screenshot's key, which a movement action shares.
        var result = new KeyBindings(updated);
        return result.Conflicts().Any() ? this : result;
    }

    /// <summary>Pairs of actions that share a key although they must not.</summary>
    public IEnumerable<(GameAction, GameAction)> Conflicts() =>
        from a in keys
        from b in keys
        where a.Key < b.Key
              && string.Equals(a.Value, b.Value, StringComparison.OrdinalIgnoreCase)
              && !CanShareKey(a.Key, b.Key)
        select (a.Key, b.Key);
}
