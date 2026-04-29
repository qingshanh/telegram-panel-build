using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using TelegramPanel.Modules;
using TelegramPanel.Module.BotCheckinAssistant.Services;

namespace TelegramPanel.Module.BotCheckinAssistant;

public sealed class BotCheckinAssistantModule : ITelegramPanelModule, IModuleUiProvider
{
    public ModuleManifest Manifest { get; } = new()
    {
        Id = "community.bot-checkin-assistant",
        Name = "机器人签到助手",
        Version = "1.0.3",
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
        // 当前模块无需额外注册本地服务。
        services.AddScoped<BotCheckinAssistantPresetStore>();
        services.AddScoped<BotCheckinTelegramCompatService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, ModuleHostContext context)
    {
        // 当前模块只提供界面页面，不额外暴露接口。
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
            Title = "机器人签到助手",
            Icon = Icons.Material.Filled.FactCheck,
            Group = "运营工具",
            Order = 20,
            ComponentType = typeof(Pages.BotCheckinAssistantPage).AssemblyQualifiedName ?? string.Empty
        };
    }
}
