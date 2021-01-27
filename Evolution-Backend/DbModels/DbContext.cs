using Evolution_Backend.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

using Serilog;

namespace Evolution_Backend.DbModels
{
    public class DbContext
    {
        private readonly IMongoDatabase _database = null;

        public DbContext(IOptions<Configuration> config)
        {
            var client = new MongoClient(config.Value.ConnectionString);
            if (client != null)
                _database = client.GetDatabase(config.Value.DatabaseName);
        }

        public ILogger log
        {
            get
            {
                return new LoggerConfiguration()
                    .WriteTo.MongoDB(_database, collectionName: "log")
                    .CreateLogger();
            }
        }

        public IMongoCollection<Num_Gen_Collection> num_gen
        {
            get
            {
                return _database.GetCollection<Num_Gen_Collection>("num_gen");
            }
        }

        public IMongoCollection<Action_Collection> action
        {
            get
            {
                return _database.GetCollection<Action_Collection>("action");
            }
        }

        public IMongoCollection<User_Collection> user
        {
            get
            {
                return _database.GetCollection<User_Collection>("user");
            }
        }

        public IMongoCollection<Customer_Collection> customer
        {
            get
            {
                return _database.GetCollection<Customer_Collection>("customer");
            }
        }

        public IMongoCollection<Item_Collection> item
        {
            get
            {
                return _database.GetCollection<Item_Collection>("item");
            }
        }

        public IMongoCollection<PO_Collection> po
        {
            get
            {
                return _database.GetCollection<PO_Collection>("po");
            }
        }

        public IMongoCollection<PO_Detail_Collection> po_detail
        {
            get
            {
                return _database.GetCollection<PO_Detail_Collection>("po_detail");
            }
        }

        public IMongoCollection<PL_Collection> pl
        {
            get
            {
                return _database.GetCollection<PL_Collection>("pl");
            }
        }

        public IMongoCollection<PL_Detail_Collection> pl_detail
        {
            get
            {
                return _database.GetCollection<PL_Detail_Collection>("pl_detail");
            }
        }
    }
}
