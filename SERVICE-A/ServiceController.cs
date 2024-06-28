using ConfigurationReaderLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SERVICE_A
{
    public class ServiceController
    {
        private readonly ConfigurationReader _configurationReader;
        public ServiceController(ConfigurationReader configurationReader)
        {
            _configurationReader = configurationReader;
        }
        public async Task Run()
        {
            Console.WriteLine("SERVICE-A is running!");

            var siteName = await _configurationReader.GetValue<string>("SiteName");

            Console.WriteLine($"Site Name: {siteName}");

            var configurations = await _configurationReader.GetAllConfigurationsAsItsOwnTypeAsync();

            if (configurations.Count > 0)
            {
                foreach (var configuration in configurations)
                {
                    Console.WriteLine($"{configuration.Name} : {configuration.Value.ToString()}");
                }
            }

            Console.ReadKey();
        }
    }
}
