using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NoirMode.Services;

public sealed class NoirWebInjectionService : IHostedService
{
    private const string MarkerStart = "<!-- Jellyfin.Plugin.NoirMode web integration start -->";
    private const string MarkerEnd = "<!-- Jellyfin.Plugin.NoirMode web integration end -->";
    private const string ScriptTag = "<script defer src=\"../NoirMode/web/video-page.js\"></script>";

    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<NoirWebInjectionService> _logger;

    public NoirWebInjectionService(
        IApplicationPaths applicationPaths,
        ILogger<NoirWebInjectionService> logger)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        InjectVideoPageScript();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void InjectVideoPageScript()
    {
        if (string.IsNullOrWhiteSpace(_applicationPaths.WebPath))
        {
            _logger.LogWarning("Noir Mode web integration skipped: Jellyfin Web path is unavailable.");
            return;
        }

        var indexPath = Path.Combine(_applicationPaths.WebPath, "index.html");
        if (!File.Exists(indexPath))
        {
            _logger.LogWarning("Noir Mode web integration skipped: index.html was not found at {IndexPath}.", indexPath);
            return;
        }

        try
        {
            var html = File.ReadAllText(indexPath);
            var withoutExistingBlock = RemoveExistingBlock(html);
            var block = $"{MarkerStart}{Environment.NewLine}{ScriptTag}{Environment.NewLine}{MarkerEnd}";

            string updated;
            var bodyIndex = withoutExistingBlock.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (bodyIndex >= 0)
            {
                updated = withoutExistingBlock.Insert(bodyIndex, block + Environment.NewLine);
            }
            else
            {
                updated = withoutExistingBlock + Environment.NewLine + block + Environment.NewLine;
            }

            if (string.Equals(html, updated, StringComparison.Ordinal))
            {
                _logger.LogInformation("Noir Mode web integration already present in {IndexPath}.", indexPath);
                return;
            }

            File.WriteAllText(indexPath, updated);
            _logger.LogInformation("Noir Mode web integration injected into {IndexPath}.", indexPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Noir Mode web integration could not update {IndexPath}: access denied.", indexPath);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Noir Mode web integration could not update {IndexPath}: IO error.", indexPath);
        }
    }

    private static string RemoveExistingBlock(string html)
    {
        var startIndex = html.IndexOf(MarkerStart, StringComparison.Ordinal);
        if (startIndex < 0)
        {
            return html;
        }

        var endIndex = html.IndexOf(MarkerEnd, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            return html;
        }

        endIndex += MarkerEnd.Length;
        while (endIndex < html.Length && (html[endIndex] == '\r' || html[endIndex] == '\n'))
        {
            endIndex++;
        }

        return html.Remove(startIndex, endIndex - startIndex);
    }
}
