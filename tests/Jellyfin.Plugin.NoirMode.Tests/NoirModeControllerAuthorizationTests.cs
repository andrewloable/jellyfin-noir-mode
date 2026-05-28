using System.Reflection;
using Jellyfin.Plugin.NoirMode.Controllers;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class NoirModeControllerAuthorizationTests
{
    [Fact]
    public void ControllerRequiresElevatedPrivileges()
    {
        var authorize = Assert.Single(typeof(NoirModeController).GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(Policies.RequiresElevation, authorize.Policy);
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
