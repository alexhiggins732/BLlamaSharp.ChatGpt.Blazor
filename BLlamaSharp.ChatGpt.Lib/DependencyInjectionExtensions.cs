using Microsoft.Extensions.DependencyInjection;

namespace BLlamaSharp.ChatGpt.Lib
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddBLlamaSharpLib(this IServiceCollection services)
        {
            services.AddSingleton<IModelSettings, ModelSettings>();
            services.AddScoped<IChatExecutor, ChatExecutor>();
            services.AddSingleton<IMessageOutputService, ConsoleMessageOutputService>();
            services.AddSingleton<IModelProvider, ModelProvider>();
            services.AddSingleton<IRootFolderProvider, RootFolderProvider>();
            // Other service registrations

            return services;
        }
    }
}
