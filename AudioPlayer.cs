using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace AnalogClock;

public static class AudioPlayer
{
    public static void Play(string? filePath, CancellationToken cancellationToken = default)
    {
        PlayBlocking(filePath, cancellationToken);
    }

    public static Task PlayAsync(string? filePath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => PlayBlocking(filePath, cancellationToken), cancellationToken);
    }

    private static void PlayBlocking(string? filePath, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            try
            {
                using var reader = new AudioFileReader(filePath);
                using var output = new WaveOutEvent();
                using var reg = cancellationToken.Register(() => output.Stop());
                output.Init(reader);
                output.Play();
                while (!cancellationToken.IsCancellationRequested && output.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(50);
                }

                return;
            }
            catch
            {
                // fall through to fallback
            }
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Console.Beep(880, 300);
            }
        }
        catch
        {
            // ignore
        }
    }
}
