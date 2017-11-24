using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using System.IO;
using StackExchange.Redis;

namespace SignalR.CoreHost
{
    public class Startup
    {
        private static readonly string CorsPolicyName = "AllowAllOrigins";

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            Enum.TryParse(configuration["SignalR:BackPlain"], out SignalRBackPlain backplain);
            switch (backplain)
            {
                case SignalRBackPlain.Redis:
                    var conn = configuration["Redis:connection"];
                    services.AddSignalR().AddRedis(options =>
                    {
                        options.Factory = (textWriter) =>
                        {
                            return ConnectionMultiplexer.Connect(conn, textWriter);
                        };
                    });
                    break;
                default:
                    services.AddSignalR();
                    break;
            }

            services.AddSingleton(typeof(PerfTicker));
            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName,
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSignalR(routes =>
            {
                routes.MapHub<PerfHub>("perf");
            });

            app.UseCors(CorsPolicyName);
        }
    }
}
