using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SignalRCluster;
using System;
using System.Security.Claims;
using System.Threading.Tasks;


namespace StaticSample
{
    public class Startup
    {


        public void ConfigureServices(IServiceCollection services)
        {

            services.AddSignalR()
                .AddMessagePackProtocol()
                .AddCluster()     
                .AddClusterStaticMembership(new[]
                {
                    new StaticClusterMember("node_1","http://localhost:5000"),
                    new StaticClusterMember("node_2","http://localhost:5001")
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
           
            app.UseClusteredSignalR(options => options.MapHub<Broadcaster>("/broadcast"));

        }


    }
}
