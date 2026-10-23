using System.Windows.Input;
using Snake.Core;

namespace Snake.Tests;

public class KeyNamesTests
{
    [Theory]
    [InlineData("Up", "↑")]
    [InlineData("Left", "←")]
    [InlineData("Space", "SPACE")]
    [InlineData("D7", "7")]
    [InlineData("NumPad7", "NUMPAD7")]
    [InlineData("w", "W")]
    public void Keys_get_short_labels(string key, string label)
    {
        Assert.Equal(label, KeyNames.Display(key));
    }

    [Fact]
    public void Every_default_binding_is_a_real_wpf_key()
    {
        foreach (string name in KeyBindings.Default.All.Values)
        {
            Assert.True(KeyNames.TryParse(name, out Key key), name);
            Assert.Equal(name, key.ToString());
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("None")]
    [InlineData("NotAKey")]
    public void Unknown_names_do_not_parse(string name)
    {
        Assert.False(KeyNames.TryParse(name, out _));
    }

    [Fact]
    public void The_reserved_back_key_is_a_real_wpf_key()
    {
        Assert.True(KeyNames.TryParse(KeyBindings.ReservedKey, out Key key));
        Assert.Equal(Key.Escape, key);
    }
}

public class SpriteTintTests
{
    [Fact]
    public void Red_and_blue_swap_while_green_and_alpha_stay()
    {
        // Two BGRA pixels: an orange one and a half-transparent grey one.
        byte[] pixels = [60, 140, 230, 255, 90, 90, 90, 128];

        SpriteTint.SwapRedAndBlue(pixels);

        Assert.Equal(new byte[] { 230, 140, 60, 255, 90, 90, 90, 128 }, pixels);
    }

    [Fact]
    public void Swapping_twice_restores_the_image()
    {
        byte[] original = Enumerable.Range(0, 64).Select(i => (byte)(i * 7)).ToArray();
        byte[] pixels = (byte[])original.Clone();

        SpriteTint.SwapRedAndBlue(pixels);
        SpriteTint.SwapRedAndBlue(pixels);

        Assert.Equal(original, pixels);
    }
}
