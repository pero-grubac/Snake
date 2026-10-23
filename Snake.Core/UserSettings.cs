using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snake.Core;

/// <summary>Everything the player can change in Settings and Controls.</summary>
public sealed record UserSettings
{
    public static readonly UserSettings Default = new();

    public MapSize Map { get; init; } = MapSize.Default;
    public SpeedLevel Speed { get; init; } = SpeedLevel.Normal;
    public WallMode Walls { get; init; } = WallMode.Solid;
    public bool Sound { get; init; } = true;
    public KeyBindings Keys { get; init; } = KeyBindings.Default;
}

/// <summary>Loads and saves <see cref="UserSettings"/> as JSON. A broken file falls back to the defaults.</summary>
public sealed class SettingsStore(string path)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string Path { get; } = path;

    public static string DefaultPath =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Blavor",
            "settings.json");

    public UserSettings Load()
    {
        if (!File.Exists(Path))
        {
            return UserSettings.Default;
        }

        try
        {
            return FromDocument(JsonSerializer.Deserialize<Document>(File.ReadAllText(Path), JsonOptions));
        }
        catch (JsonException)
        {
            return UserSettings.Default;
        }
    }

    public void Save(UserSettings settings)
    {
        string? directory = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = Path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(ToDocument(settings), JsonOptions));
        File.Move(temporaryPath, Path, overwrite: true);
    }

    private static UserSettings FromDocument(Document? document)
    {
        if (document is null)
        {
            return UserSettings.Default;
        }

        return new UserSettings
        {
            Map = MapSize.IsValid(document.Rows, document.Columns) ? new MapSize(document.Rows, document.Columns) : MapSize.Default,
            Speed = Enum.IsDefined(document.Speed) ? document.Speed : SpeedLevel.Normal,
            Walls = Enum.IsDefined(document.Walls) ? document.Walls : WallMode.Solid,
            Sound = document.Sound,
            Keys = KeyBindings.From(document.Keys),
        };
    }

    private static Document ToDocument(UserSettings settings) => new()
    {
        Rows = settings.Map.Rows,
        Columns = settings.Map.Columns,
        Speed = settings.Speed,
        Walls = settings.Walls,
        Sound = settings.Sound,
        Keys = new Dictionary<GameAction, string>(settings.Keys.All),
    };

    private sealed class Document
    {
        public int Rows { get; set; } = MapSize.Min;
        public int Columns { get; set; } = MapSize.Min;
        public SpeedLevel Speed { get; set; } = SpeedLevel.Normal;
        public WallMode Walls { get; set; } = WallMode.Solid;
        public bool Sound { get; set; } = true;
        public Dictionary<GameAction, string>? Keys { get; set; }
    }
}
