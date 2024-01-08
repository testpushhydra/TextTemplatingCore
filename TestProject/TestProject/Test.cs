using Microsoft.Extensions.DependencyInjection;

namespace CloudIDEaaS.TextTemplatingCore.TestProject
{
    public sealed class Test
    {
        public void Run(IServiceCollection services)
        {
            services.AddScoped<IHello>();
        }
    }
}
