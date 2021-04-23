using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mcma.Modules.FFmpegService.Worker
{
    public static class FFmpegProcessServiceCollectionExtensions
    {
        public static IServiceCollection AddFFmpeg(this IServiceCollection services, string executablePath)
            => services.AddFFmpeg(opts => opts.ExecutablePath = executablePath); 
        
        public static IServiceCollection AddFFmpeg(this IServiceCollection services, Action<FFmpegOptions> configureOptions = null)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);
            return services.AddSingleton<IFFmpegProcess, FFmpegProcess>();
        }
        
        public static IServiceCollection AddFFmpeg<T>(this IServiceCollection services) where T : class, IConfigureOptions<FFmpegOptions>
        {
            services.AddOptions().AddSingleton<IConfigureOptions<FFmpegOptions>, T>();

            return services.AddSingleton<IFFmpegProcess, FFmpegProcess>();
        }
    }
}