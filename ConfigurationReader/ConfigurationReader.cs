using Newtonsoft.Json;
using Shared.Enums;
using Shared.Interfaces;
using Shared.Models;
using Shared.Repositories;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigurationReaderLib
{
    public class ConfigurationReader
    {
        private readonly string _applicationName;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly Dictionary<string, ConfigurationItem> _configCache;
        private readonly Timer _refreshTimer;
        private readonly ConnectionMultiplexer _redisConnection;
        private readonly IDatabase _redisDatabase;
        private readonly string _redisConnectionString;

        /// <summary>
        /// İlgili konfigürasyonları tutup dışarı açık olan alan
        /// </summary>
        public List<Configuration> ApplicationConfigurations { get; set; }

        #region CONSTRUCTOR  ****************************************************************************************************************************************************************************************
        public ConfigurationReader(string applicationName, string connectionString, int refreshTimerIntervalInMs)
        {
            _applicationName = applicationName;

            // Burada sınıfı oluşturan projenin adı ile gönderdiği ad uyuşuyor mu diye kontrol yapılır ve farklı bir projenin konfigürasyonunu çekmesi engellenir.
            if (_applicationName.ToUpper() != Assembly.GetCallingAssembly().GetName().Name.ToUpper())
            {
                throw new Exception("Given Application Name is Not Matching With The Requesting Service Name");
            }

            // MongoDB Reposu oluşturulur
            _configurationRepository = new MongoConfigurationRepository(connectionString);

            // Yerel cache
            _configCache = new Dictionary<string, ConfigurationItem>();

            // Redis konfigürasyonu
            _redisConnectionString = "redis:6379";
            _redisConnection = ConnectionMultiplexer.Connect(_redisConnectionString);
            _redisDatabase = _redisConnection.GetDatabase();

            // Veriler ilk kez çekilir ve önbelleklenir
            LoadInitialConfig().Wait();

            // Verilen metodun verilen periyotta çalışması sağanır
            _refreshTimer = new Timer(RefreshConfigData, null, refreshTimerIntervalInMs, refreshTimerIntervalInMs);
        }
        #endregion

        #region PUBIC METHODS  ****************************************************************************************************************************************************************************************

        /// <summary>
        /// Dışarıdan erişilebilen bu metot istenen anahtara sahip bir veri varsa döner istenen tipte döner.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<T> GetValue<T>(string key)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(_applicationName);
                sb.Append("_");
                sb.Append(key);

                key = sb.ToString();

                if (_configCache.TryGetValue(key, out var configItem))
                {
                    if (typeof(T) == typeof(bool))
                    {
                        return (T)Convert.ChangeType(configItem.Value != "0", typeof(T));
                    }

                    return (T)Convert.ChangeType(configItem.Value, typeof(T));
                }

                var cachedItem = await _redisDatabase.StringGetAsync($"{_applicationName}:{key}");
                if (cachedItem.HasValue)
                {
                    var configItemFromCache = JsonConvert.DeserializeObject<ConfigurationItem>(cachedItem);
                    _configCache[key] = configItemFromCache;

                    if (typeof(T) == typeof(bool))
                    {
                        return (T)Convert.ChangeType(configItemFromCache.Value != "0", typeof(T));
                    }

                    return (T)Convert.ChangeType(configItemFromCache.Value, typeof(T));
                }

                throw new KeyNotFoundException($"Config key '{key}' not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw new Exception("Something Went Wrong!");
            }
        }

        /// <summary>
        /// Her bir konfigürasyon kaydını yerel cache veya redisten çekerek dönüştürtüp geri döner.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<List<Configuration>> GetAllConfigurationsAsItsOwnTypeAsync()
        {
            try
            {
                if (_configCache.Count > 0)
                {
                    return ConvertRecords(_configCache.Select(c => c.Value).ToList());
                }

                return ConvertRecords(await GetConfigurationItemsByPartialKeyAsync(_applicationName));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw new Exception("Something Went Wrong!");
            }
        }
        #endregion

        #region PRIVATE METHODS  ****************************************************************************************************************************************************************************************

        /// <summary>
        /// Konfigürasyon kayıtlarını ilk defa çeken ve önbellekleyen metot.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task LoadInitialConfig()
        {
            try
            {
                var configItems = await _configurationRepository.GetActiveConfigurationItemsByApplicationNameAsync(_applicationName);

                if (configItems.Count > 0 && configItems != null)
                {
                    await DeleteRedisKeysByPartiaKey(_applicationName);

                    if (_configCache.Count > 0)
                    {
                        _configCache.Clear();
                    }

                    foreach (var item in configItems)
                    {
                        _configCache[$"{_applicationName}_{item.Name}"] = item;
                        _redisDatabase.StringSet($"{_applicationName}:{item.Name}", JsonConvert.SerializeObject(item));
                    }
                }

                ApplicationConfigurations = await GetAllConfigurationsAsItsOwnTypeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw new Exception("Something Went Wrong!");
            }
        }

        /// <summary>
        /// Verilen periyotta konfigürasyon kayıtlarını kontrol edip ilgili güncellemeleri yapar
        /// </summary>
        /// <param name="state"></param>
        private async void RefreshConfigData(object state)
        {
            try
            {
                var configItems = await _configurationRepository.GetActiveConfigurationItemsByApplicationNameAsync(_applicationName);

                if (configItems.Count > 0 && configItems != null)
                {
                    await DeleteRedisKeysByPartiaKey(_applicationName);

                    if (_configCache.Count > 0)
                    {
                        _configCache.Clear();
                    }

                    foreach (var item in configItems)
                    {
                        _configCache[$"{_applicationName}_{item.Name}"] = item;
                        _redisDatabase.StringSet($"{_applicationName}:{item.Name}", JsonConvert.SerializeObject(item));
                    }
                }

                if (ApplicationConfigurations.Count > 0)
                    ApplicationConfigurations.Clear();

                ApplicationConfigurations = await GetAllConfigurationsAsItsOwnTypeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("MongoDB'ye erişilemedi, redis cache kullanılacak. Message: " + ex.Message);
            }
        }

        /// <summary>
        /// Gelen değeri gelen tipe dönüştüren yardımcı metot.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private object ConvertValue(string type, string value)
        {
            try
            {
                return type switch
                {
                    nameof(ConfigurationTypes.Integer) => int.Parse(value),
                    nameof(ConfigurationTypes.String) => value,
                    nameof(ConfigurationTypes.Double) => double.Parse(value),
                    nameof(ConfigurationTypes.Boolean) => (value != "0"),
                    _ => throw new InvalidOperationException("Unsupported type")
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw new Exception("Something Went Wrong!");
            }
        }

        /// <summary>
        /// Gelen konfigürasyon kayıtlarını kendi tipine dönüştürtüp Configuration listesi olarak dönen yardımcı metot.
        /// </summary>
        /// <param name="configs"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private List<Configuration> ConvertRecords(List<ConfigurationItem> configs)
        {
            try
            {
                return configs
                        .Select(config => new Configuration
                        {
                            Name = config.Name,
                            Value = ConvertValue(config.Type, config.Value)
                        })
                        .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw new Exception("Something Went Wrong!");
            }
        }

        /// <summary>
        /// Verilen kısmi anahtar ile başlayan redis kayıtlarını ConfigurationItem listesi olarak döner.
        /// </summary>
        /// <param name="partialKey"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<List<ConfigurationItem>> GetConfigurationItemsByPartialKeyAsync(string partialKey)
        {
            try
            {
                var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
                var keys = server.Keys(pattern: $"{partialKey}*");

                var values = new List<ConfigurationItem>();
                foreach (var key in keys)
                {
                    var value = await _redisDatabase.StringGetAsync(key);
                    values.Add(JsonConvert.DeserializeObject<ConfigurationItem>(value));
                }

                return values;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw new Exception("Something Went Wrong!");
            }
        }

        /// <summary>
        /// Verilen kısmi anahtar ile başlayan redis kayıtlarını siler.
        /// </summary>
        /// <param name="partialKey"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task DeleteRedisKeysByPartiaKey(string partialKey)
        {
            try
            {
                var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
                var keys = server.Keys(pattern: $"{partialKey}*");

                foreach (var key in keys)
                {
                    var value = await _redisDatabase.KeyDeleteAsync(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw new Exception("Something Went Wrong!");
            }
        }

        #endregion
    }
}
