using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace AnalogClock;

public static class AudioPlayer
{
    public static void Play(string? filePath, CancellationToken cancellationToken = default, bool loop = true)
    {
        if (loop)
        {
            PlayLoop(filePath, cancellationToken);
        }
        else
        {
            PlayOnce(filePath, cancellationToken);
        }
    }

    public static Task PlayAsync(string? filePath, CancellationToken cancellationToken = default, bool loop = true)
    {
        return loop
            ? Task.Run(() => PlayLoop(filePath, cancellationToken), cancellationToken)
            : Task.Run(() => PlayOnce(filePath, cancellationToken), cancellationToken);
    }

    private static void PlayLoop(string? filePath, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!PlayOnce(filePath, cancellationToken))
            {
                return;
            }

            // Small pause between repetitions.
            if (cancellationToken.WaitHandle.WaitOne(1000))
            {
                return;
            }
        }
    }

    private static bool PlayOnce(string? filePath, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            try
            {
                using var reader = new AudioFileReader(filePath);
                using var output = new WaveOutEvent();
                using var reg = cancellationToken.Register(() => output.Stop());
                output.Init(reader);
                output.Play();

                while (!cancellationToken.IsCancellationRequested &&
                       output.PlaybackState == PlaybackState.Playing)
                {
                    cancellationToken.WaitHandle.WaitOne(50);
                }

                return !cancellationToken.IsCancellationRequested;
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

        return !cancellationToken.IsCancellationRequested;
    }
}
