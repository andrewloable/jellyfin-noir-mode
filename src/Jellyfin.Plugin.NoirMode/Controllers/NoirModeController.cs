using Jellyfin.Plugin.NoirMode.Configuration;
using Jellyfin.Plugin.NoirMode.Core;
using Jellyfin.Plugin.NoirMode.Models;
using Jellyfin.Plugin.NoirMode.Services;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.NoirMode.Controllers;

[ApiController]
[Authorize]
[Route("NoirMode")]
public sealed class NoirModeController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly NoirPluginStateService _stateService;
    private readonly FFmpegWrapperService _wrapperService;
    private readonly NoirPresetService _presetService = new();

    public NoirModeController(
        ILibraryManager libraryManager,
        NoirPluginStateService stateService,
        FFmpegWrapperService wrapperService)
    {
        _libraryManager = libraryManager;
        _stateService = stateService;
        _wrapperService = wrapperService;
    }

    [HttpGet("config")]
    public ActionResult<NoirConfigDto> GetConfig()
    {
        var config = GetConfiguration();
        return new NoirConfigDto
        {
            Enabled = config.Enabled,
            AllowCustomFilters = config.AllowCustomFilters,
            ForceTranscodeNoticeShown = config.ForceTranscodeNoticeShown,
            RealFFmpegPath = config.RealFFmpegPath,
            WrapperPath = config.WrapperPath,
            ItemOverrides = config.ItemOverrides
        };
    }

    [HttpPost("config")]
    public ActionResult<NoirConfigDto> SaveConfig(NoirConfigDto request)
    {
        var config = GetConfiguration();
        config.Enabled = request.Enabled;
        config.AllowCustomFilters = false;
        config.ForceTranscodeNoticeShown = request.ForceTranscodeNoticeShown;
        config.RealFFmpegPath = request.RealFFmpegPath;
        config.WrapperPath = request.WrapperPath;
        Save(config);
        return GetConfig();
    }

    [HttpGet("presets")]
    public ActionResult<IReadOnlyCollection<NoirPreset>> GetPresets()
    {
        return Ok(_presetService.GetAll());
    }

    [HttpGet("items/search")]
    public ActionResult<IReadOnlyCollection<NoirItemSearchResult>> SearchItems([FromQuery] string? query)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            SearchTerm = query,
            MediaTypes = [MediaType.Video],
            IsFolder = false,
            Recursive = true,
            Limit = 25
        });

        return Ok(items.Select(ToResult).ToArray());
    }

    [HttpGet("items/{itemId}/override")]
    public ActionResult<NoirItemOverride> GetOverride(string itemId)
    {
        var config = GetConfiguration();
        return config.ItemOverrides.FirstOrDefault(x => x.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase))
            ?? new NoirItemOverride { ItemId = itemId, Mode = NoirOverrideMode.Disabled };
    }

    [HttpPut("items/{itemId}/override")]
    public ActionResult<NoirItemOverride> PutOverride(string itemId, NoirItemOverride request)
    {
        if (request.Mode == NoirOverrideMode.Preset)
        {
            _presetService.GetRequired(request.PresetId ?? string.Empty);
        }

        var config = GetConfiguration();
        config.ItemOverrides.RemoveAll(x => x.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));

        var item = TryGetItem(itemId);
        var mediaPath = request.MediaPath ?? item?.Path;
        var normalizedPath = NoirPath.Normalize(mediaPath);
        var saved = new NoirItemOverride
        {
            ItemId = itemId,
            MediaPath = mediaPath,
            NormalizedMediaPath = normalizedPath,
            MediaPathHash = NoirPath.Hash(normalizedPath),
            Mode = request.Mode,
            PresetId = request.Mode == NoirOverrideMode.Preset ? request.PresetId : null
        };

        if (saved.Mode != NoirOverrideMode.Disabled)
        {
            config.ItemOverrides.Add(saved);
        }

        Save(config);
        return saved;
    }

    [HttpDelete("items/{itemId}/override")]
    public IActionResult DeleteOverride(string itemId)
    {
        var config = GetConfiguration();
        config.ItemOverrides.RemoveAll(x => x.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));
        Save(config);
        return NoContent();
    }

    [HttpGet("wrapper/status")]
    public ActionResult<WrapperStatusDto> GetWrapperStatus()
    {
        var config = GetConfiguration();
        return _wrapperService.GetStatus(config.Enabled, config.RealFFmpegPath, config.WrapperPath);
    }

    [HttpPost("wrapper/install")]
    public ActionResult<WrapperStatusDto> ExportState()
    {
        var config = GetConfiguration();
        _stateService.Export(config);
        return _wrapperService.GetStatus(config.Enabled, config.RealFFmpegPath, config.WrapperPath);
    }

    [HttpPost("wrapper/test")]
    public async Task<ActionResult<object>> TestWrapper(CancellationToken cancellationToken)
    {
        var config = GetConfiguration();
        var result = await _wrapperService.ProbeAsync(config.WrapperPath, cancellationToken).ConfigureAwait(false);
        return Ok(new { result.Success, result.Output });
    }

    [HttpGet("resolve")]
    public ActionResult<NoirResolveResult> Resolve([FromQuery] string? itemId, [FromQuery] string? mediaPath)
    {
        var state = _stateService.BuildState(GetConfiguration());
        return new NoirRuleService().Resolve(state, new NoirMediaLookup(itemId, mediaPath));
    }

    private static NoirItemSearchResult ToResult(BaseItem item)
    {
        return new NoirItemSearchResult
        {
            ItemId = item.Id.ToString("N"),
            Name = item.Name,
            MediaPath = item.Path
        };
    }

    private BaseItem? TryGetItem(string itemId)
    {
        return Guid.TryParse(itemId, out var guid) ? _libraryManager.GetItemById(guid) : null;
    }

    private static PluginConfiguration GetConfiguration()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
    }

    private void Save(PluginConfiguration configuration)
    {
        Plugin.Instance?.UpdateConfiguration(configuration);
        _stateService.Export(configuration);
    }
}
