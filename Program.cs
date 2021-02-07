using System;
using System.Net.Http;
using System.Threading.Tasks;
using GrooprWASM.Data.GroupGeneration;
using GrooprWASM.Data.Settings;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Groopr
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddScoped<SettingsContainer>();
            builder.Services.AddScoped<RandomGroupsetStrategy>();
            
            await builder.Build().RunAsync();
        }
    }
}
