using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using TelegramPanel.Modules;

namespace TelegramPanel.Module.BotCheckinAssistant;

public sealed class BotCheckinAssistantModule : ITelegramPanelModule, IModuleUiProvider
{
    public ModuleManifest Manifest { get; } = new()
    {
        Id = "community.bot-checkin-assistant",
        Name = "Bot Check-in Assistant",
        Version = "1.0.0",
        Host = new HostCompatibility
        {
            Min = "1.0.0",
            Max = "2.0.0"
        },
        Entry = new ModuleEntryPoint
        {
            Assembly = "TelegramPanel.Module.BotCheckinAssistant.dll",
            Type = typeof(BotCheckinAssistantModule).FullName ?? "TelegramPanel.Module.BotCheckinAssistant.BotCheckinAssistantModule"
        }
    };

    public void ConfigureServices(IServiceCollection services, ModuleHostContext context)
    {
        // No module-local DI services currently required.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, ModuleHostContext context)
    {
        // This module currently exposes only UI pages.
    }

    public IEnumerable<ModuleNavItem> GetNavItems(ModuleHostContext context)
    {
        yield break;
    }

    public IEnumerable<ModulePageDefinition> GetPages(ModuleHostContext context)
    {
        yield return new ModulePageDefinition
        {
            Key = "checkin",
            Title = "Bot Check-in Assistant",
            Icon = Icons.Material.Filled.FactCheck,
            Group = "Operations",
            Order = 20,
            ComponentType = typeof(Pages.BotCheckinAssistantPage).AssemblyQualifiedName ?? string.Empty
        };
    }
}

