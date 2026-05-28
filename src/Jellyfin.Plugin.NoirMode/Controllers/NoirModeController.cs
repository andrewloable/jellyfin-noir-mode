using Jellyfin.Plugin.NoirMode.Configuration;
using Jellyfin.Plugin.NoirMode.Core;
using Jellyfin.Plugin.NoirMode.Models;
using Jellyfin.Plugin.NoirMode.Services;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NoirMode.Controllers;

[ApiController]
[Authorize]
[Route("NoirMode")]
public sealed class NoirModeController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<NoirModeController> _logger;
    private readonly NoirPluginStateService _stateService;
    private readonly FFmpegWrapperService _wrapperService;
    private readonly NoirPresetService _presetService = new();

    public NoirModeController(
        ILibraryManager libraryManager,
        ILogger<NoirModeController> logger,
        NoirPluginStateService stateService,
        FFmpegWrapperService wrapperService)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _stateService = stateService;
        _wrapperService = wrapperService;
    }

    [Authorize(Policy = Policies.RequiresElevation)]
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

    [Authorize(Policy = Policies.RequiresElevation)]
    [HttpPost("config")]
    public ActionResult<NoirConfigDto> SaveConfig(NoirConfigDto request)
    {
        _logger.LogInformation(
            "Noir Mode config save requested: enabled={Enabled}; realFfmpegPath={RealFfmpegPath}; wrapperPath={WrapperPath}; itemOverrideCount={ItemOverrideCount}",
            request.Enabled,
            request.RealFFmpegPath,
            request.WrapperPath,
            request.ItemOverrides.Count);

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

    [AllowAnonymous]
    [HttpGet("web/video-page.js")]
    public IActionResult GetVideoPageScript()
    {
        var resourceName = $"{typeof(Plugin).Namespace}.Configuration.noirVideoPage.js";
        var assembly = typeof(Plugin).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            _logger.LogWarning("Noir Mode video page script resource was not found: {ResourceName}", resourceName);
            return NotFound();
        }

        using var reader = new StreamReader(stream);
        return Content(reader.ReadToEnd(), "application/javascript");
    }

    [Authorize(Policy = Policies.RequiresElevation)]
    [HttpGet("items/search")]
    public ActionResult<IReadOnlyCollection<NoirItemSearchResult>> SearchItems([FromQuery] string? query)
    {
        _logger.LogInformation("Noir Mode item search requested: query={Query}", query);

        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            SearchTerm = query,
            MediaTypes = [MediaType.Video],
            IsFolder = false,
            Recursive = true,
            Limit = 25
        });

        var results = items.Select(ToResult).ToArray();
        _logger.LogInformation("Noir Mode item search completed: query={Query}; resultCount={ResultCount}", query, results.Length);
        return Ok(results);
    }

    [HttpGet("items/{itemId}/override")]
    public ActionResult<NoirItemOverride> GetOverride(string itemId)
    {
        var item = TryGetItem(itemId);
        if (item is null)
        {
            return NotFound($"Item '{itemId}' was not found.");
        }

        if (!IsVideoItem(item))
        {
            return BadRequest("Noir Mode is configured per episode/video, not at the series level.");
        }

        var config = GetConfiguration();
        return config.ItemOverrides.FirstOrDefault(x => x.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase))
            ?? new NoirItemOverride { ItemId = itemId, Mode = NoirOverrideMode.Disabled };
    }

    [HttpPut("items/{itemId}/override")]
    public ActionResult<NoirItemOverride> PutOverride(string itemId, NoirItemOverride request)
    {
        _logger.LogInformation(
            "Noir Mode item override save requested: itemId={ItemId}; mode={Mode}; presetId={PresetId}; mediaPath={MediaPath}",
            itemId,
            request.Mode,
            request.PresetId,
            request.MediaPath);

        if (request.Mode == NoirOverrideMode.Preset)
        {
            _presetService.GetRequired(request.PresetId ?? string.Empty);
        }

        var config = GetConfiguration();
        config.ItemOverrides.RemoveAll(x => x.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));

        var item = TryGetItem(itemId);
        if (item is null)
        {
            _logger.LogWarning("Noir Mode item override save rejected: itemId={ItemId}; reason=item-not-found", itemId);
            return NotFound($"Item '{itemId}' was not found.");
        }

        if (!IsVideoItem(item))
        {
            _logger.LogWarning(
                "Noir Mode item override save rejected: itemId={ItemId}; itemName={ItemName}; mediaType={MediaType}; reason=not-video",
                itemId,
                item.Name,
                item.MediaType);
            return BadRequest("Noir Mode is configured per episode/video, not at the series level.");
        }

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
        _logger.LogInformation(
            "Noir Mode item override saved: itemId={ItemId}; mode={Mode}; presetId={PresetId}; normalizedMediaPath={NormalizedMediaPath}; mediaPathHash={MediaPathHash}",
            saved.ItemId,
            saved.Mode,
            saved.PresetId,
            saved.NormalizedMediaPath,
            saved.MediaPathHash);
        return saved;
    }

    [Authorize(Policy = Policies.RequiresElevation)]
    [HttpDelete("items/{itemId}/override")]
    public IActionResult DeleteOverride(string itemId)
    {
        _logger.LogInformation("Noir Mode item override delete requested: itemId={ItemId}", itemId);
        var config = GetConfiguration();
        config.ItemOverrides.RemoveAll(x => x.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));
        Save(config);
        _logger.LogInformation("Noir Mode item override deleted: itemId={ItemId}", itemId);
        return NoContent();
    }

    [Authorize(Policy = Policies.RequiresElevation)]
    [HttpGet("wrapper/status")]
    public ActionResult<WrapperStatusDto> GetWrapperStatus()
    {
        var config = GetConfiguration();
        return _wrapperService.GetStatus(config.Enabled, config.RealFFmpegPath, config.WrapperPath);
    }

    [Authorize(Policy = Policies.RequiresElevation)]
    [HttpPost("wrapper/install")]
    public ActionResult<WrapperStatusDto> ConfigureWrapper()
    {
        _logger.LogInformation("Noir Mode bundled wrapper configure endpoint called.");
        var config = GetConfiguration();
        var status = _wrapperService.ConfigureBundledWrapper(config.Enabled, config.RealFFmpegPath);
        if (status.WrapperExists && status.JellyfinUsesWrapper && !string.IsNullOrWhiteSpace(status.RealFFmpegPath))
        {
            config.Enabled = true;
            config.RealFFmpegPath = status.RealFFmpegPath;
            config.WrapperPath = status.WrapperPath;
            Save(config);
            status.Enabled = config.Enabled;
        }

        return status;
    }

    [Authorize(Policy = Policies.RequiresElevation)]
    [HttpPost("wrapper/rollback")]
    public ActionResult<WrapperStatusDto> RollbackWrapper()
    {
        _logger.LogInformation("Noir Mode wrapper rollback endpoint called.");
        var config = GetConfiguration();
        var status = _wrapperService.RestoreRealFfmpeg(config.Enabled, config.RealFFmpegPath, config.WrapperPath);
        if (status.RealFFmpegExists)
        {
            config.Enabled = false;
            Save(config);
            status.Enabled = config.Enabled;
        }

        return status;
    }

    [Authorize(Policy = Policies.RequiresElevation)]
    [HttpPost("wrapper/export-state")]
    public ActionResult<WrapperStatusDto> ExportState()
    {
        var config = GetConfiguration();
        _stateService.Export(config);
        _logger.LogInformation(
            "Noir Mode state exported manually: enabled={Enabled}; itemOverrideCount={ItemOverrideCount}",
            config.Enabled,
            config.ItemOverrides.Count);
        return _wrapperService.GetStatus(config.Enabled, config.RealFFmpegPath, config.WrapperPath);
    }

    [Authorize(Policy = Policies.RequiresElevation)]
    [HttpPost("wrapper/test")]
    public async Task<ActionResult<object>> TestWrapper(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Noir Mode wrapper test endpoint called.");
        var config = GetConfiguration();
        var result = await _wrapperService.ProbeAsync(config.WrapperPath, cancellationToken).ConfigureAwait(false);
        return Ok(new { result.Success, result.Output });
    }

    [Authorize(Policy = Policies.RequiresElevation)]
    [HttpGet("resolve")]
    public ActionResult<NoirResolveResult> Resolve([FromQuery] string? itemId, [FromQuery] string? mediaPath)
    {
        var state = _stateService.BuildState(GetConfiguration());
        var result = new NoirRuleService().Resolve(state, new NoirMediaLookup(itemId, mediaPath));
        _logger.LogInformation(
            "Noir Mode resolve requested: itemId={ItemId}; mediaPath={MediaPath}; enabled={Enabled}; applied={Applied}; reason={Reason}; presetId={PresetId}",
            itemId,
            mediaPath,
            state.Enabled,
            result.ShouldApply,
            result.Reason,
            result.Preset?.Id);
        return result;
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

    private static bool IsVideoItem(BaseItem item)
    {
        return item is Video || item.MediaType == MediaType.Video;
    }

    private static PluginConfiguration GetConfiguration()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
    }

    private void Save(PluginConfiguration configuration)
    {
        Plugin.Instance?.UpdateConfiguration(configuration);
        _stateService.Export(configuration);
        _logger.LogInformation(
            "Noir Mode configuration saved and state exported: enabled={Enabled}; realFfmpegPath={RealFfmpegPath}; wrapperPath={WrapperPath}; itemOverrideCount={ItemOverrideCount}",
            configuration.Enabled,
            configuration.RealFFmpegPath,
            configuration.WrapperPath,
            configuration.ItemOverrides.Count);
    }
}
