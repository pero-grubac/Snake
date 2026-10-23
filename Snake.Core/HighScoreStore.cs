namespace Snake.Core;

/// <summary>Loads and saves a <see cref="HighScoreTable"/> as a text file.</summary>
public sealed class HighScoreStore(string path)
{
    public string Path { get; } = path;

    public static string DefaultPath =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Blavor",
            "score.txt");

    public HighScoreTable Load() =>
        File.Exists(Path) ? HighScoreTable.Parse(File.ReadAllLines(Path)) : new HighScoreTable();

    /// <summary>Writes a temporary file first, so a crash never leaves a half-written file.</summary>
    public void Save(HighScoreTable table)
    {
        string? directory = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = Path + ".tmp";
        File.WriteAllLines(temporaryPath, table.Serialize());
        File.Move(temporaryPath, Path, overwrite: true);
    }
}
