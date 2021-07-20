using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace LSportsServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CGlobal.InitProcess();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls($"http://*:{CDefine.SERVER_HTTP}");
                });
    }
}
