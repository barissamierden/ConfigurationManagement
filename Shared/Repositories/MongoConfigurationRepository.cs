using Microsoft.VisualBasic;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Interfaces;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Repositories
{
    public class MongoConfigurationRepository : IConfigurationRepository
    {
        private readonly IMongoCollection<ConfigurationItem> _configCollection;

        public MongoConfigurationRepository(string connectionString)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("ConfigurationDB");
            _configCollection = database.GetCollection<ConfigurationItem>("ConfigurationItems");
        }

        public async Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync(bool isActiveFilter = false)
        {
            if (isActiveFilter)
            {
                return await _configCollection.Find(item => item.IsActive).ToListAsync();
            }

            return await _configCollection.Find(_ => true).ToListAsync();
        }

        public async Task<List<ConfigurationItem>> GetActiveConfigurationItemsByApplicationNameAsync(string applicationName)
        {
            try
            {
                var h = await _configCollection.Find(_ => true).ToListAsync();

                //var c = await _configCollection.CountDocumentsAsync(x => x.IsActive);
                var a = await _configCollection.Find(item => item.ApplicationName.ToUpper() == "SERVICE-B").ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return await _configCollection.Find(item => item.ApplicationName == applicationName && item.IsActive).ToListAsync();
        }

        public async Task<List<ConfigurationItem>> GetAllConfigurationItemsWithPaginationAsync(int pageSize, int page, bool isActiveFilter = false, string searchText = null)
        {
            var filter = Builders<ConfigurationItem>.Filter.Empty;

            if (isActiveFilter)
            {
                filter = filter & Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, true);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filter = filter & Builders<ConfigurationItem>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(searchText));
            }

            return await _configCollection.Find(filter).Skip(pageSize * (page)).Limit(pageSize).ToListAsync();
        }

        public async Task<ConfigurationItem> GetActiveConfigurationItemAsync(string applicationName, string key)
        {
            return await _configCollection.Find(item => item.ApplicationName == applicationName && item.Name == key && item.IsActive).FirstOrDefaultAsync();
        }

        public async Task<long> GetCountAsync(bool isActiveFilter = false, string searchText = null)
        {
            var filter = Builders<ConfigurationItem>.Filter.Empty;

            if (isActiveFilter)
            {
                filter = filter & Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, true);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filter = filter & Builders<ConfigurationItem>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(searchText));
            }

            return await _configCollection.Find(filter).CountDocumentsAsync();
        }

        public async Task InsertConfigItemAsync(ConfigurationItem configurationItem)
        {
            await _configCollection.InsertOneAsync(configurationItem);
        }

        public async Task<bool> UpdateConfigItemAsync(ConfigurationItem configurationItem)
        {
            ObjectId.TryParse(configurationItem.Id, out ObjectId objectId);
            
            var filter = Builders<ConfigurationItem>.Filter.Eq("_id", objectId);

            var update = Builders<ConfigurationItem>.Update
                .Set(nameof(ConfigurationItem.Name), configurationItem?.Name)
                .Set(nameof(ConfigurationItem.IsActive), configurationItem?.IsActive)
                .Set(nameof(ConfigurationItem.ApplicationName), configurationItem?.ApplicationName)
                .Set(nameof(ConfigurationItem.Value), configurationItem?.Value)
                .Set(nameof(ConfigurationItem.Type), configurationItem?.Type);

            var result = await _configCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteConfigItemAsync(string id)
        {
            ObjectId.TryParse(id, out ObjectId objectId);

            var filter = Builders<ConfigurationItem>.Filter.Eq("_id", objectId);

            var result = await _configCollection.DeleteOneAsync(filter);

            if (result.DeletedCount > 0)
            {
                return true;
            }

            return false;
        }
    }
}
