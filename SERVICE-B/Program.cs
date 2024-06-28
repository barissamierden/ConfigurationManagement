using ConfigurationReaderLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SERVICE_B;
using System;
using System.Threading.Tasks;

namespace SERVICE_B
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                var serviceProvider = serviceCollection.BuildServiceProvider();

                var controller = serviceProvider.GetService<ServiceController>();

                await controller.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var configurationReader = new ConfigurationReader("SERVICE-B", "mongodb://mongodb:27017", 10000);

            services.AddSingleton(configurationReader);
            services.AddSingleton<ServiceController>();
        }
    }
}