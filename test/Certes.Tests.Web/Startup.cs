using System;
using System.IO;
using System.Text;
using Certes.Jws;
using Certes.Pkcs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Certes.Tests.Web
{
    public class Startup
    {
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
                    var request = context.Request;
                    var path = request.Path.ToUriComponent();
                    if (request.Method == "GET" && path?.Length > 1 && path.StartsWith("/"))
                    {
                        var host = request.Host.Host;
                        var key =
                            host.IndexOf(".es256.", StringComparison.OrdinalIgnoreCase) >= 0 ? Keys.ES256Key :
                            host.IndexOf(".es384.", StringComparison.OrdinalIgnoreCase) >= 0 ? Keys.ES384Key :
                            host.IndexOf(".es512.", StringComparison.OrdinalIgnoreCase) >= 0 ? Keys.ES512Key :
                            Keys.RS256Key;

                        AccountKey accountKey;
                        using (var buffer = new MemoryStream(Encoding.UTF8.GetBytes(key)))
                        {
                            accountKey = new AccountKey(KeyInfo.From(buffer));
                        }

                        var thumbPrint = JwsConvert.ToBase64String(accountKey.GenerateThumbprint());

                        context.Response.ContentType = "plain/text";
                        await context.Response.WriteAsync($"{path.Substring(1)}.{thumbPrint}");
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
