using api_loja_venda_app.DependencyServices;
using MongoDB.Driver;
using api_loja_venda_app.Models;

namespace api_loja_venda_app.Services
{
    public class MongoDbService : IMongoDbService
    {
        protected readonly MongoClient _mongoClient;

        public MongoDbService(IDatabaseSetting databaseSetting)
        {
            _mongoClient = new MongoClient(databaseSetting.ConnectionString);
        }

        public MongoClient clientMongo()
        {
            return _mongoClient;
        }
    }
}