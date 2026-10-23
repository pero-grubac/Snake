namespace Snake.Core;

public enum SoundCue
{
    Eat,
    Die,
    Countdown,
    Go,
    Win,
    Record,
    Click
}

/// <summary>A tone; a frequency of 0 is a rest.</summary>
public readonly record struct Note(double Frequency, int Milliseconds);

/// <summary>Builds tiny 8-bit style sound effects as WAV files in memory, so the game ships no audio files.</summary>
public static class ToneSynth
{
    public const int SampleRate = 22050;
    private const short BitsPerSample = 16;
    private const short Channels = 1;

    public static byte[] Wav(IEnumerable<Note> notes, double volume = 0.22)
    {
        var samples = new List<short>();
        foreach (Note note in notes)
        {
            int count = SampleRate * note.Milliseconds / 1000;
            int fade = Math.Max(1, Math.Min(count / 4, SampleRate / 200));
            for (int i = 0; i < count; i++)
            {
                double envelope = Math.Min(1.0, Math.Min(i, count - 1 - i) / (double)fade);
                double t = i / (double)SampleRate;
                // A square wave softened with its fundamental sounds like an old console.
                double wave = note.Frequency <= 0
                    ? 0
                    : 0.6 * Math.Sign(Math.Sin(2 * Math.PI * note.Frequency * t)) + 0.4 * Math.Sin(2 * Math.PI * note.Frequency * t);
                samples.Add((short)(wave * envelope * volume * short.MaxValue));
            }
        }

        int dataLength = samples.Count * BitsPerSample / 8;
        using var stream = new MemoryStream(44 + dataLength);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(Channels);
        writer.Write(SampleRate);
        writer.Write(SampleRate * Channels * BitsPerSample / 8);
        writer.Write((short)(Channels * BitsPerSample / 8));
        writer.Write(BitsPerSample);
        writer.Write("data"u8);
        writer.Write(dataLength);
        foreach (short sample in samples)
        {
            writer.Write(sample);
        }
        writer.Flush();
        return stream.ToArray();
    }
}

public static class SoundBank
{
    private static readonly Dictionary<SoundCue, Note[]> Tunes = new()
    {
        [SoundCue.Eat] = [new(880, 45), new(1320, 70)],
        [SoundCue.Die] = [new(392, 120), new(311, 120), new(247, 160), new(185, 280)],
        [SoundCue.Countdown] = [new(660, 90)],
        [SoundCue.Go] = [new(990, 200)],
        [SoundCue.Win] = [new(523, 110), new(659, 110), new(784, 110), new(1047, 320)],
        [SoundCue.Record] = [new(659, 100), new(784, 100), new(988, 100), new(1319, 260)],
        [SoundCue.Click] = [new(1200, 25)],
    };

    private static readonly Dictionary<SoundCue, byte[]> Cache = new();

    public static IReadOnlyList<Note> NotesFor(SoundCue cue) => Tunes[cue];

    public static byte[] WavFor(SoundCue cue)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue(cue, out byte[]? wav))
            {
                wav = ToneSynth.Wav(Tunes[cue]);
                Cache[cue] = wav;
            }
            return wav;
        }
    }
}
