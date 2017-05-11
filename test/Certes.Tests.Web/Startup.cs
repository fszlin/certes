using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Certes.Tests.Web
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

            app.Map("/md", sub =>
            {
                sub.Run(context =>
                {
                    var referer = context.Request.Headers["Referer"].FirstOrDefault() ?? "";
                    referer = referer.ToUpperInvariant();

                    var targetUrl = context.Request.Query["r"].FirstOrDefault();
                    var parts = targetUrl.Split('{', '}');

                    var buffer = new StringBuilder();
                    for (int i = 0; i < parts.Length; ++i)
                    {
                        if (i % 2 == 1)
                        {
                            var options = parts[i].Split('|');
                            var branch = options
                                .Where(b => referer.Contains($"/blob/{b}/") || referer.Contains($"/tree/{b}/") || referer.EndsWith($"/tree/{b}"))
                                .FirstOrDefault() ?? options.FirstOrDefault();
                            buffer.Append(branch ?? "master");
                        }
                        else
                        {
                            buffer.Append(parts[i]);
                        }
                    }

                    context.Response.Redirect(buffer.ToString(), true);

                    return Task.CompletedTask;
                });
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Find Certes project on GitHub - https://goo.gl/beyaxD");
            });
        }
    }
}
