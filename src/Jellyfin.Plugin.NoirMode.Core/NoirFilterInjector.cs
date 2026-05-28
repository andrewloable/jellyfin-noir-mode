namespace Jellyfin.Plugin.NoirMode.Core;

public sealed class NoirFilterInjector
{
    private readonly NoirRuleService _ruleService;

    public NoirFilterInjector(NoirRuleService? ruleService = null)
    {
        _ruleService = ruleService ?? new NoirRuleService();
    }

    public FfmpegInjectionDecision Inject(IReadOnlyList<string> args, NoirState state)
    {
        var originalArgs = args.ToArray();
        var inputPath = FfmpegArgumentParser.FindPrimaryInputPath(originalArgs);
        var resolveResult = _ruleService.Resolve(state, new NoirMediaLookup(null, inputPath));
        if (!resolveResult.ShouldApply || resolveResult.Preset is null)
        {
            return new FfmpegInjectionDecision(originalArgs, false, false, resolveResult.Reason, inputPath);
        }

        if (FfmpegArgumentParser.ContainsOption(originalArgs, "-filter_complex"))
        {
            return new FfmpegInjectionDecision(originalArgs, false, false, "filter-complex-unsupported", inputPath);
        }

        if (FfmpegArgumentParser.UsesVideoStreamCopy(originalArgs))
        {
            return new FfmpegInjectionDecision(originalArgs, false, false, "video-stream-copy-unsupported", inputPath);
        }

        if (FfmpegArgumentParser.UsesLikelyHardwareFilterChain(originalArgs))
        {
            return new FfmpegInjectionDecision(originalArgs, false, false, "hardware-filter-chain-unsupported", inputPath);
        }

        var updatedArgs = originalArgs.ToList();
        var filterIndex = FfmpegArgumentParser.FindVideoFilterOptionIndex(updatedArgs);
        if (filterIndex >= 0)
        {
            updatedArgs[filterIndex + 1] = $"{updatedArgs[filterIndex + 1]},{resolveResult.Preset.Filter}";
            return new FfmpegInjectionDecision(updatedArgs, true, true, "appended-vf", inputPath);
        }

        var insertIndex = FfmpegArgumentParser.FindOutputInsertionIndex(updatedArgs);
        updatedArgs.Insert(insertIndex, "-vf");
        updatedArgs.Insert(insertIndex + 1, resolveResult.Preset.Filter);

        return new FfmpegInjectionDecision(updatedArgs, true, true, "inserted-vf", inputPath);
    }
}
