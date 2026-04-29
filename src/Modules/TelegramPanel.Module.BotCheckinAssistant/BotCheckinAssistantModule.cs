using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using TelegramPanel.Module.BotCheckinAssistant.Components;
using TelegramPanel.Module.BotCheckinAssistant.Services;
using TelegramPanel.Modules;

namespace TelegramPanel.Module.BotCheckinAssistant;

public sealed class BotCheckinAssistantModule : ITelegramPanelModule, IModuleUiProvider, IModuleTaskProvider
{
    public ModuleManifest Manifest { get; } = new()
    {
        Id = "community.bot-checkin-assistant",
        Name = "\u673A\u5668\u4EBA\u7B7E\u5230\u52A9\u624B",
        Version = "1.2.0",
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
        services.AddScoped<BotCheckinAssistantPresetStore>();
        services.AddScoped<BotCheckinTelegramCompatService>();
        services.AddScoped<IModuleTaskHandler, BotCheckinModuleTaskHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, ModuleHostContext context)
    {
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
            Title = "\u673A\u5668\u4EBA\u7B7E\u5230\u52A9\u624B",
            Icon = Icons.Material.Filled.FactCheck,
            Group = "\u8FD0\u8425\u5DE5\u5177",
            Order = 20,
            ComponentType = typeof(Pages.BotCheckinAssistantPage).AssemblyQualifiedName ?? string.Empty
        };
    }

    public IEnumerable<ModuleTaskDefinition> GetTasks(ModuleHostContext context)
    {
        yield return new ModuleTaskDefinition
        {
            Category = "user",
            TaskType = BotCheckinModuleTaskConstants.TaskType,
            DisplayName = "\u673A\u5668\u4EBA\u7B7E\u5230\u52A9\u624B",
            Description = "\u6309\u8D26\u53F7\u6267\u884C\u6307\u5B9A\u673A\u5668\u4EBA\u7684\u7B7E\u5230\u811A\u672C\uFF0C\u652F\u6301\u56DE\u590D\u7B49\u5F85\u3001\u968F\u673A\u5EF6\u8FDF\u548C Cron \u8BA1\u5212\u4EFB\u52A1\u3002",
            Icon = Icons.Material.Filled.FactCheck,
            EditorComponentType = typeof(BotCheckinTaskEditor).AssemblyQualifiedName ?? string.Empty,
            TaskCenter = new ModuleTaskCenterCapabilities
            {
                CanPause = true,
                CanResume = true,
                CanEdit = true,
                CanRerun = false,
                EditComponentType = typeof(BotCheckinTaskEditor).AssemblyQualifiedName ?? string.Empty,
                AutoPauseBeforeEdit = true
            },
            Order = 160
        };
    }
}
