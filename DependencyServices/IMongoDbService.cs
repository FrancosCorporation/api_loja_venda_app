using MongoDB.Driver;
using api_loja_venda_app.Models;

namespace api_loja_venda_app.DependencyServices
{
    public interface IMongoDbService
    {
        MongoClient clientMongo();
    }
}