using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.AdoNet.MySql.Clustering;
using System.Threading;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Collections.Concurrent;

namespace Orleans.Im
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private IClusterClient ClusterClient { get; set; }
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string connectionString =
                @"server=101.200.75.205;database=Test;user id=root;password=mysql@mrh321;port=7001;persistsecurityinfo=False;SslMode=none";
            var client = new ClientBuilder()
                    .UseMySqlClustering(option =>
                        option.ConnectionString = connectionString).Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "Orleans.Im";
                            options.ServiceId = "Orleans.Im";
                        })
                    .ConfigureLogging(logging => logging.AddConsole())
                    .Build();
            client.Connect();
            Thread.Sleep(2000);
            ClusterClient = client;
            services.AddSingleton(client);
            services.AddControllers();
        }
        static bool isUseWebSockets = false;
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Map("/ws", appcur =>
            {
                // var grainFactory = (IGrainFactory)app.ApplicationServices.GetService(typeof(IGrainFactory));
                var server = new ImServer(app.ApplicationServices);
                if (isUseWebSockets == false)
                {
                    isUseWebSockets = true;
                    appcur.UseWebSockets();
                }
                appcur.Use((ctx, next) =>
                   server.Acceptor(ctx, next));
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
           {
               endpoints.MapControllers();
           });

        }



        //private static Func<Exception, Task<bool>> CreateRetryFilter(int maxAttempts = 5)
        //{
        //    var attempt = 0;
        //    return RetryFilter;

        //    async Task<bool> RetryFilter(Exception exception)
        //    {
        //        attempt++;
        //        Console.WriteLine($"Cluster client attempt {attempt} of {maxAttempts} failed to connect to cluster.  Exception: {exception}");
        //        if (attempt > maxAttempts)
        //        {
        //            return false;
        //        }

        //        await Task.Delay(TimeSpan.FromSeconds(4));
        //        return true;
        //    }
        //}
    }
}
