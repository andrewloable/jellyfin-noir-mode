using System.Reflection;
using Jellyfin.Plugin.NoirMode.Controllers;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class NoirModeControllerAuthorizationTests
{
    [Fact]
    public void ControllerRequiresAuthenticatedUser()
    {
        var authorize = Assert.Single(typeof(NoirModeController).GetCustomAttributes<AuthorizeAttribute>());

        Assert.Null(authorize.Policy);
    }

    [Fact]
    public void AdminActionsRequireElevatedPrivileges()
    {
        var elevatedActions = new[]
        {
            nameof(NoirModeController.GetConfig),
            nameof(NoirModeController.SaveConfig),
            nameof(NoirModeController.SearchItems),
            nameof(NoirModeController.DeleteOverride),
            nameof(NoirModeController.GetWrapperStatus),
            nameof(NoirModeController.ConfigureWrapper),
            nameof(NoirModeController.RollbackWrapper),
            nameof(NoirModeController.ExportState),
            nameof(NoirModeController.TestWrapper),
            nameof(NoirModeController.Resolve)
        };

        foreach (var actionName in elevatedActions)
        {
            var method = typeof(NoirModeController).GetMethod(actionName);
            Assert.NotNull(method);

            var authorize = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>());
            Assert.Equal(Policies.RequiresElevation, authorize.Policy);
        }
    }

    [Fact]
    public void VideoPageActionsUseAuthenticatedUserAuthorization()
    {
        var videoPageActions = new[]
        {
            nameof(NoirModeController.GetPresets),
            nameof(NoirModeController.GetOverride),
            nameof(NoirModeController.PutOverride)
        };

        foreach (var actionName in videoPageActions)
        {
            var method = typeof(NoirModeController).GetMethod(actionName);
            Assert.NotNull(method);

            Assert.Empty(method.GetCustomAttributes<AuthorizeAttribute>());
            Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        }
    }

    [Fact]
    public void OnlyVideoPageScriptAllowsAnonymousAccess()
    {
        var publicActions = typeof(NoirModeController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        foreach (var action in publicActions.Where(action => action.Name != nameof(NoirModeController.GetVideoPageScript)))
        {
            Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>());
        }

        var scriptAction = Assert.Single(publicActions, action => action.Name == nameof(NoirModeController.GetVideoPageScript));
        Assert.NotNull(scriptAction.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}
