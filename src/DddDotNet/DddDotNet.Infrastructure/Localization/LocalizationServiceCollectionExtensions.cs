using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Localization;

public static class LocalizationServiceCollectionExtensions
{
    public static IServiceCollection AddLocalization(this IServiceCollection services, LocalizationProviders providers)
    {
        if (providers?.SqlServer?.IsEnabled ?? false)
        {
            services.Configure<SqlServerOptions>(op =>
            {
                op.ConnectionString = providers.SqlServer.ConnectionString;
                op.SqlQuery = providers.SqlServer.SqlQuery;
                op.CacheMinutes = providers.SqlServer.CacheMinutes;
            });

            services.AddSingleton<IStringLocalizerFactory, SqlServerStringLocalizerFactory>();
            services.AddScoped(provider => provider.GetRequiredService<IStringLocalizerFactory>().Create(null));
            services.AddLocalization();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("vi-VN"),
                };

                options.DefaultRequestCulture = new RequestCulture(culture: "en-US", uiCulture: "en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                options.AddInitialRequestCultureProvider(new CustomRequestCultureProvider(context =>
                {
                    // My custom request culture logic
                    // return new ProviderCultureResult("vi-VN");
                    return Task.FromResult(new ProviderCultureResult("en-US"));
                }));
            });
        }

        return services;
    }
}
