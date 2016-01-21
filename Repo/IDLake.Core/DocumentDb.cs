using IDLake.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core;
using System.Dynamic;
using Gravicode.Transformer;

namespace IDLake.Core
{
    [Keterangan("DocumentDb", "Storage menggunakan mongoDb")]
    public class DocumentDb : IDataContext
    {
        protected static IMongoClient _client;
        //protected static IMongoDatabase _database;
        protected static ISchemaContext ctx;
        public string DBName
        {
            set; get;
        }
        public DocumentDb(string DBName,ISchemaContext SchemaContext)
        {
            _client = new MongoClient();
            //_database = _client.GetDatabase(DBName);
            ctx = SchemaContext;
            this.DBName = DBName;
            
        }

        public DocumentDb(ISchemaContext SchemaContext)
        {
            _client = new MongoClient();
            ctx = SchemaContext;
        }
        public void SetupDatabase(string DbName)
        {
            this.DBName = DbName;
            //MongoDatabaseSettings setting = new MongoDatabaseSettings();

            MongoClient client = new MongoClient();

            MongoServer server = client.GetServer();
            MongoDatabase db = server.GetDatabase(DbName);
           
        }

        public async Task<bool> DeleteAllData(string CollectionName)
        {
            IMongoDatabase _database= _client.GetDatabase(DBName);
           
            var collection = _database.GetCollection<BsonDocument>(CollectionName);
            var filter = new BsonDocument();
            var result = await collection.DeleteManyAsync(filter);
            return true;
        }

        public async Task<bool> DeleteData(dynamic id, string CollectionName)
        {
            IMongoDatabase _database = _client.GetDatabase(DBName);
            var collection = _database.GetCollection<BsonDocument>(CollectionName);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
            var result = await collection.DeleteManyAsync(filter);
            return true;
        }

        public async Task<bool> DeleteDataBulk(IEnumerable<dynamic> Ids, string CollectionName)
        {
            IMongoDatabase _database = _client.GetDatabase(DBName);
            var collection = _database.GetCollection<BsonDocument>(CollectionName);
            foreach (var itemId in Ids)
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", itemId);
                var result = await collection.DeleteManyAsync(filter);
            }
            return true;
        }

        public async Task<List<dynamic>> GetAllData(string CollectionName)
        {
            IMongoDatabase _database = _client.GetDatabase(DBName);
            var cols = new List<dynamic>();
            var collection = _database.GetCollection<BsonDocument>(CollectionName);
            var filter = new BsonDocument();
            var count = 0;
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        // process document
                        dynamic item = document.ToDynamic();
                        cols.Add(item);
                        count++;
                    }
                }
            }
            return cols;
        }

        public async Task<List<dynamic>> GetAllData(int Limit, string CollectionName)
        {
            IMongoDatabase _database = _client.GetDatabase(DBName);
            var cols = new List<dynamic>();
            var collection = _database.GetCollection<BsonDocument>(CollectionName);
            var filter = new BsonDocument();
            var count = 0;
            var list = await collection.Find(filter).Limit(Limit).ToListAsync();
            foreach (var document in list)
            {
                // process document
                dynamic item = document.ToDynamic();
                cols.Add(item);
                count++;
                if (count >= Limit) break;
            }
            /*
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        // process document
                        dynamic item = document.ToDynamic();
                        cols.Add(item);
                        count++;
                        if (count >= Limit) break;
                    }
                    if (count >= Limit) break;
                }
            }*/
            return cols;
        }

        public async Task<dynamic> GetDataById(dynamic Id, string CollectionName)
        {
            IMongoDatabase _database = _client.GetDatabase(DBName);
            dynamic node=null;
            var collection = _database.GetCollection<BsonDocument>(CollectionName);
            var filter = new BsonDocument() { { "_id", Id } };
            //Builders<BsonDocument>.Filter.Eq("_id",Id);

            //var result = await collection.FindAsync(filter).ToListAsync();
           
            var list = await collection.Find(filter).Limit(1).ToListAsync();
            foreach (var document in list)
            {
                // process document
                node = document.ToDynamic();
                break;
            }
            /*
            foreach (var item in result)
            {
                node = item.ToDynamic();
                break;
            }*/
            return node;
        }

        public async Task<List<dynamic>> GetDataByStartId(int Limit, long StartId, string CollectionName)
        {
            IMongoDatabase _database = _client.GetDatabase(DBName);
            var cols = new List<dynamic>();
            var collection = _database.GetCollection<BsonDocument>(CollectionName);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Gt("_id", StartId-1) & builder.Lt("_id", StartId+Limit+1);
            var result = await collection.Find(filter).ToListAsync();
            foreach (var document in result)
            {
                // process document
                dynamic item = document.ToDynamic();
                cols.Add(item);
                
            }
            return cols;
        }

        public long GetSequence(string CollectionName)
        {
            return ctx.GetSchemaSequence($"monggo_counter:{DBName}:{CollectionName}");
        }

        public async Task<bool> InsertBulkData(IEnumerable<dynamic> data, string CollectionName)
        {
            IMongoDatabase _database = _client.GetDatabase(DBName);
            var docs = new List<BsonDocument>();
            foreach(dynamic item in data)
            {
                item._id = this.GetSequence(CollectionName);//Guid.NewGuid().ToString().Replace("-", "");
                var doc = new BsonDocument(item);
                docs.Add(doc);
            }
           
            var collection = _database.GetCollection<BsonDocument>(CollectionName);
            await collection.InsertManyAsync(docs);
            return true;
        }

        public async Task<bool> InsertData(dynamic data, string CollectionName)
        {
            IMongoDatabase _database = _client.GetDatabase(DBName);
            data._id = this.GetSequence(CollectionName);//Guid.NewGuid().ToString().Replace("-", "");
            var doc = new BsonDocument(data as dynamic);
            var collection = _database.GetCollection<BsonDocument>(CollectionName);
            await collection.InsertOneAsync(doc);
            return true;
        }

        public async Task<bool> UpdateData(dynamic data, string CollectionName)
        {
            IMongoDatabase _database = _client.GetDatabase(DBName);
            var collection = _database.GetCollection<BsonDocument>(CollectionName);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", data._id);
            var doc = new BsonDocument(data as dynamic);
            await collection.ReplaceOneAsync(filter,doc);
            return true;
        }
        public void SetUserName(string UserName)
        {
            //do nothing
        }
    }
}
