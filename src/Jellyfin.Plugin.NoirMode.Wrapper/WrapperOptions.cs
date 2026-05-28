namespace Jellyfin.Plugin.NoirMode.Wrapper;

public sealed record WrapperOptions(string RealFfmpegPath, string StateFilePath)
{
    public static WrapperOptions FromEnvironment()
    {
        var realFfmpeg = Environment.GetEnvironmentVariable("NOIR_REAL_FFMPEG");
        var stateFile = Environment.GetEnvironmentVariable("NOIR_STATE_FILE");

        if (string.IsNullOrWhiteSpace(realFfmpeg))
        {
            throw new InvalidOperationException("NOIR_REAL_FFMPEG is required.");
        }

        if (string.IsNullOrWhiteSpace(stateFile))
        {
            throw new InvalidOperationException("NOIR_STATE_FILE is required.");
        }

        return new WrapperOptions(realFfmpeg, stateFile);
    }
}
