using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Im.Grains;
using Orleans.AdoNet.MySql.Clustering;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Orleans.Im.Common;
using System.Net.WebSockets;
using System.Threading;
using Orleans.AdoNet;

namespace Orleans.Im
{
    public class Program
    {
        static bool isUseWebSockets = false;
        public static Task Main(string[] args) =>
           Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    GlobalVariable.Configuration = config.Build();
                })
               .UseOrleans(builder =>
               {
                   var connectionString = GlobalVariable.Configuration.GetValue<string>("OrleansConfig:ConnectionString");
                   builder.UseMySqlClustering(option =>
                   {
                       option.ConnectionString = connectionString;
                   }).Configure<ClusterOptions>(options =>
                   {
                       options.ClusterId = GlobalVariable.Configuration.GetValue<string>("OrleansConfig:ClusterId");
                       options.ServiceId = GlobalVariable.Configuration.GetValue<string>("OrleansConfig:ServiceId");
                   }).ConfigureEndpoints(new Random().Next(10001, 20000), new Random().Next(20001, 30000));

                   //builder.AddAdoNetGrainStorage("store", op =>
                   //{
                   //    op.Invariant = AdoNetInvariants.InvariantNameMySql;
                   //    op.ConnectionString = connectionString;
                   //    op.UseJsonFormat = true;
                   //});
                   builder.AddMemoryGrainStorage(Constant.PUBSUB_PROVIDER);
                   builder.AddSimpleMessageStreamProvider(Constant.STREAM_PROVIDER, opt => opt.FireAndForgetDelivery = true);
                   builder.UseTransactions();

                   builder.ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IChatGrain).Assembly).WithReferences());
               })
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder.Configure((ctx, app) =>
                   {
                       //var grainFactory = (IGrainFactory)app.ApplicationServices.GetService(typeof(IGrainFactory));
                       var provider = app.ApplicationServices;
                       app.Map("/ws", appcur =>
                       {
                           var server = new ImServer(provider);
                           if (isUseWebSockets == false)
                           {
                               isUseWebSockets = true;
                               appcur.UseWebSockets();
                           }
                           appcur.Use((ctx, next) =>
                             server.Acceptor(ctx, next));
                       });
                       if (ctx.HostingEnvironment.IsDevelopment())
                       {
                           app.UseDeveloperExceptionPage();
                       }

                       app.UseHttpsRedirection();
                       app.UseStaticFiles();
                       app.UseRouting();
                       app.UseAuthorization();
                       app.UseEndpoints(endpoints =>
                       {
                           endpoints.MapControllers();
                       });
                   });
               })
               .ConfigureServices(services =>
               {

                   services.AddControllers();
               })
           .RunConsoleAsync();
    }
}
