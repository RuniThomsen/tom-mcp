using Microsoft.Extensions.DependencyInjection;

namespace Filters;

public static class ServiceCollectionExtensions
{
    public static IMvcBuilder AddMcpExceptionFilter(this IServiceCollection services)
        => services.AddControllers(opts => opts.Filters.Add<GlobalExceptionFilter>());
}
