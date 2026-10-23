using System.Diagnostics;
using System.IO;
using System.Windows.Automation;

namespace Snake.Tests;

/// <summary>
/// Starts the real game and drives it through UI Automation. They need a desktop session,
/// so CI skips them: run with <c>dotnet test --filter Category=UI</c>.
/// </summary>
[Trait("Category", "UI")]
[Collection("UI")]
public sealed class UiTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly string dataDirectory = Path.Combine(Path.GetTempPath(), "blavor-ui-" + Guid.NewGuid().ToString("N"));
    private readonly Process process;
    private readonly AutomationElement window;

    public UiTests()
    {
        process = Process.Start(new ProcessStartInfo(GameExe(), ["--data-dir", dataDirectory]))!;
        process.WaitForInputIdle(10_000);
        window = WaitFor(() =>
        {
            process.Refresh();
            return process.MainWindowHandle == IntPtr.Zero ? null : AutomationElement.FromHandle(process.MainWindowHandle);
        });
    }

    public void Dispose()
    {
        if (!process.HasExited)
        {
            process.Kill();
            process.WaitForExit(5_000);
        }
        process.Dispose();
        if (Directory.Exists(dataDirectory))
        {
            Directory.Delete(dataDirectory, recursive: true);
        }
    }

    [Fact]
    public void Starts_on_the_main_menu()
    {
        Assert.Equal("1 PLAYER", Element("OnePlayerButton").Current.Name);
        Assert.Equal("BEST ON 15X15: 0", Element("MenuBestText").Current.Name);
        Assert.StartsWith("15X15", Element("ModeText").Current.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_change_the_board_and_are_saved()
    {
        Invoke("SettingsButton");
        SetValue("HeightSlider", 20);
        SetValue("WidthSlider", 22);
        Invoke("WallsWrap");
        Invoke("SaveButton");

        WaitUntil(() => Element("ModeText").Current.Name.StartsWith("20X22 · WRAP-AROUND WALLS", StringComparison.Ordinal));
        string json = File.ReadAllText(Path.Combine(dataDirectory, "settings.json"));
        Assert.Contains("\"Rows\": 20", json, StringComparison.Ordinal);
        Assert.Contains("\"Walls\": \"Wrap\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void A_snake_left_alone_hits_the_wall_and_the_game_is_over()
    {
        Invoke("OnePlayerButton");

        // Food is random, so now and then a mouse lands on the snake's path and the score is a record.
        WaitUntil(() => !Element("PlayAgainButton").Current.IsOffscreen);
        Assert.Contains(Element("MessageText").Current.Name, new[] { "GAME OVER", "NEW HIGHEST SCORE" });
        Assert.Matches(@"^SCORE [01]$", Element("MessageDetail").Current.Name);
    }

    [Fact]
    public void Two_snakes_left_alone_crash_at_the_same_time_and_draw()
    {
        Invoke("TwoPlayersButton");

        // Both heads leave the board on the same tick, even if one of them ate a mouse on the way.
        WaitUntil(() => !Element("PlayAgainButton").Current.IsOffscreen);
        Assert.Equal("DRAW", Element("MessageText").Current.Name);
        Assert.Matches(@"^P1 [01]  :  [01] P2$", Element("MessageDetail").Current.Name);

        Invoke("BackToMenuButton");
        WaitUntil(() => !Element("OnePlayerButton").Current.IsOffscreen);
    }

    [Fact]
    public void Controls_show_the_bindings_and_wait_for_a_key()
    {
        Invoke("ControlsButton");

        Assert.Equal("W", Element("P1UpKey").Current.Name);
        Assert.Equal("↑", Element("P2UpKey").Current.Name);

        Invoke("PauseKey");
        Assert.Equal("PRESS…", Element("PauseKey").Current.Name);
    }

    private AutomationElement Element(string automationId) => WaitFor(() =>
        window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId)));

    private void Invoke(string automationId)
    {
        AutomationElement element = Element(automationId);
        if (element.TryGetCurrentPattern(InvokePattern.Pattern, out object? invoke))
        {
            ((InvokePattern)invoke).Invoke();
        }
        else
        {
            ((SelectionItemPattern)element.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
        }
    }

    private void SetValue(string automationId, double value) =>
        ((RangeValuePattern)Element(automationId).GetCurrentPattern(RangeValuePattern.Pattern)).SetValue(value);

    private static T WaitFor<T>(Func<T?> probe) where T : class
    {
        var clock = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                if (probe() is { } value)
                {
                    return value;
                }
            }
            catch (ElementNotAvailableException)
            {
                // The tree is being rebuilt; try again.
            }

            if (clock.Elapsed > Timeout)
            {
                throw new TimeoutException("The game did not reach the expected state in time.");
            }
            Thread.Sleep(100);
        }
    }

    private static void WaitUntil(Func<bool> condition) =>
        WaitFor(() => condition() ? new object() : null);

    /// <summary>The game built next to these tests, in the same configuration.</summary>
    private static string GameExe()
    {
        string exe = Path.Combine(AppContext.BaseDirectory, "Snake.exe");
        if (!File.Exists(exe))
        {
            throw new FileNotFoundException("Build the Snake project first.", exe);
        }
        return exe;
    }
}
