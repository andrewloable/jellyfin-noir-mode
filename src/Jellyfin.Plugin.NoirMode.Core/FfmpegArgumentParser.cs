namespace Jellyfin.Plugin.NoirMode.Core;

public static class FfmpegArgumentParser
{
    private static readonly HashSet<string> InputOptionsWithValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "-i"
    };

    public static string? FindPrimaryInputPath(IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (InputOptionsWithValues.Contains(args[i]))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    public static bool ContainsOption(IReadOnlyList<string> args, params string[] names)
    {
        var options = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        return args.Any(options.Contains);
    }

    public static int FindVideoFilterOptionIndex(IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (args[i].Equals("-vf", StringComparison.OrdinalIgnoreCase)
                || args[i].Equals("-filter:v", StringComparison.OrdinalIgnoreCase)
                || args[i].Equals("-filter:v:0", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public static bool UsesVideoStreamCopy(IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            var key = args[i];
            if ((key.Equals("-c:v", StringComparison.OrdinalIgnoreCase)
                    || key.Equals("-codec:v", StringComparison.OrdinalIgnoreCase)
                    || key.Equals("-vcodec", StringComparison.OrdinalIgnoreCase)
                    || key.StartsWith("-c:v:", StringComparison.OrdinalIgnoreCase)
                    || key.StartsWith("-codec:v:", StringComparison.OrdinalIgnoreCase))
                && args[i + 1].Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool UsesLikelyHardwareFilterChain(IReadOnlyList<string> args)
    {
        var hardwareTokens = new[]
        {
            "vaapi",
            "qsv",
            "cuda",
            "videotoolbox",
            "opencl",
            "vulkan",
            "hwupload",
            "hwdownload",
            "scale_vaapi",
            "scale_qsv",
            "scale_cuda"
        };

        return args.Any(arg => hardwareTokens.Any(token => arg.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }

    public static int FindOutputInsertionIndex(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return 0;
        }

        return args.Count - 1;
    }
}
