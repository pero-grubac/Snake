using System.IO;
using System.Media;
using Snake.Core;

namespace Snake
{
    /// <summary>Plays the synthesised cues from <see cref="SoundBank"/>.</summary>
    internal sealed class SoundEffects
    {
        // Players are kept alive while their sound plays; a collected player would cut it off.
        private readonly Dictionary<SoundCue, SoundPlayer> players = new();

        public bool Enabled { get; set; } = true;

        public void Play(SoundCue cue)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                if (!players.TryGetValue(cue, out SoundPlayer? player))
                {
                    player = new SoundPlayer(new MemoryStream(SoundBank.WavFor(cue)));
                    player.Load();
                    players[cue] = player;
                }
                player.Play();
            }
            catch (Exception e) when (e is InvalidOperationException or IOException or TimeoutException)
            {
                // No audio device: the game is still playable.
                Enabled = false;
            }
        }
    }
}
