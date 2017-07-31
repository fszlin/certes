using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Certes.Web
{
    public class Startup
    {
        private const string ThumbPrint = "cSTPyZ48ZK6w7Z_ndytGxoz5XNR6-ycwGEs_VI6nj44";

        public void ConfigureServices(IServiceCollection services)
        {
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Map("/.well-known/acme-challenge", sub =>
            {
                sub.Run(async context =>
                {
                    var path = context.Request.Path.ToUriComponent();
                    if (path?.Length > 1 && path.StartsWith("/"))
                    {
                        context.Response.ContentType = "plain/text";
                        await context.Response.WriteAsync($"{path.Substring(1)}.{ThumbPrint}");
                    }
                });
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Find Certes project on GitHub - https://goo.gl/beyaxD");
            });
        }
    }
}
