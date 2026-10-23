using System.Globalization;

namespace Snake.Core;

/// <summary>
/// Best score per map size, stored as one <c>rows;columns;score</c> line per map.
/// </summary>
public sealed class HighScoreTable
{
    private readonly Dictionary<MapSize, int> scores = new();

    public IReadOnlyDictionary<MapSize, int> Scores => scores;

    public int Get(MapSize map) => scores.GetValueOrDefault(map);

    /// <returns><c>true</c> when <paramref name="score"/> beats the stored best.</returns>
    public bool Submit(MapSize map, int score)
    {
        if (score <= Get(map))
        {
            return false;
        }

        scores[map] = score;
        return true;
    }

    /// <summary>Reads saved lines. Broken lines are skipped and the rest is kept.</summary>
    public static HighScoreTable Parse(IEnumerable<string> lines)
    {
        var table = new HighScoreTable();
        foreach (string line in lines)
        {
            string[] parts = line.Split(';');
            if (parts.Length == 3
                && TryParse(parts[0], out int rows)
                && TryParse(parts[1], out int columns)
                && TryParse(parts[2], out int score)
                && MapSize.IsValid(rows, columns)
                && score >= 0)
            {
                var map = new MapSize(rows, columns);
                table.scores[map] = Math.Max(score, table.Get(map));
            }
        }
        return table;
    }

    public IEnumerable<string> Serialize() =>
        scores
            .OrderBy(entry => entry.Key.Rows)
            .ThenBy(entry => entry.Key.Columns)
            .Select(entry => string.Join(';', entry.Key.Rows, entry.Key.Columns, entry.Value));

    private static bool TryParse(string text, out int value) =>
        int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
