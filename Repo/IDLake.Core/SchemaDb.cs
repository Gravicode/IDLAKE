using IDLake.Entities;
using MoreLinq;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDLake.Core
{
    public class SchemaDb:ISchemaContext
    {
        public static string GetDbName(string username)
        {
            return "lake_" + username + "_db";
        }
        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }
        public bool InsertBulkData<T>(IEnumerable<T> data)
        {
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                var redisStream = redis.As<T>();
                redisStream.StoreAll(data);
                return true;
            }
        }

        public bool InsertData<T>(T data)
        {
            try
            {
                if (data == null) return false;
                
                using (var redisManager = new PooledRedisClientManager())
                using (var redis = redisManager.GetClient())
                {
                    var redisStream = redis.As<T>();
                    redisStream.Store(data);
                    return true;
                }
            }
            catch
            {
                //print ke log
                //throw;
                return false;
            }
        }
      
        public bool DeleteData<T>(long id)
        {
            try
            {
                using (var redisManager = new PooledRedisClientManager())
                using (var redis = redisManager.GetClient())
                {
                    var redisStream = redis.As<T>();

                    redisStream.DeleteById(id);
                    return true;
                }
            }
            catch
            {
                //print ke log
                //throw;
                return false;
            }
        }
        public bool DeleteAllData<T>()
        {
            try
            {
                using (var redisManager = new PooledRedisClientManager())
                using (var redis = redisManager.GetClient())
                {
                    IRedisTypedClient<T> redis1 = redis.As<T>();

                    var datas = redis1.GetAll();
                    foreach (var item in datas)
                    {
                        redis1.Delete(item);
                    }

                    return true;
                }
            }
            catch
            {
                //print ke log
                //throw;
                return false;
            }
        }
        public bool DeleteDataBulk<T>(IEnumerable<T> Ids)
        {
            try
            {
                using (var redisManager = new PooledRedisClientManager())
                using (var redis = redisManager.GetClient())
                {
                    var redisStream = redis.As<T>();
                    redisStream.DeleteByIds(Ids);
                    //redisStream.Save();
                    return true;
                }
            }
            catch
            {
                //print ke log
                //throw;
                return false;
            }
        }

        public T GetDataById<T>(long Id)
        {
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                var redisStream = redis.As<T>();

                return redisStream.GetById(Id);

            }
        }
       
        public List<T> GetDataByStartId<T>(int Limit, long StartId)
        {
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                var redisStream = redis.As<T>();
                if (typeof(T) == typeof(SchemaEntity))
                {
                    var data = from c in redisStream.GetAll()
                               where (c is SchemaEntity) && (c as SchemaEntity).Id >= StartId
                               orderby (c as SchemaEntity).Id
                               select c;
                    return data.Take(Limit).ToList();
                }
                else
                {
                    var data = from c in redisStream.GetAll()
                               where Convert.ToInt32(GetPropValue(c, "Id")) >= StartId
                               select c;
                    return data.Take(Limit).ToList();
                }
            }
        }
        public List<T> GetAllData<T>(int Limit)
        {
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                var redisStream = redis.As<T>();
                if (typeof(T) == typeof(SchemaEntity))
                {
                    var data = from c in redisStream.GetAll()
                               where c is SchemaEntity
                               orderby (c as SchemaEntity).Id
                               select c;

                    return data.TakeLast(Limit).ToList();
                }
                else
                {
                    var data = from c in redisStream.GetAll()
                               select c;

                    return data.TakeLast(Limit).ToList();
                }
            }
        }
        public List<T> GetAllData<T>()
        {
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                var redisStream = redis.As<T>();
                if (typeof(T) == typeof(SchemaEntity))
                {
                    var data = from c in redisStream.GetAll()
                               orderby (c as SchemaEntity).Id
                               select c;
                    return data.ToList();
                }
                else
                {
                    var data = from c in redisStream.GetAll()
                               select c;
                    return data.ToList();
                }
            }
        }
        public long GetSequence<T>()
        {
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                var redisStream = redis.As<T>();
                long Id = redisStream.GetNextSequence();
                return Id;
            }
        }

        public long GetSchemaSequence(string Key)
        {
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                using (var redisCache = redisManager.GetCacheClient())
                {
                    var item = redisCache.Get<long>(Key);
                    if (item == default(long))
                    {
                        item = 1;

                    }
                    else
                    {
                        item++;

                    }
                    redisCache.Set<long>(Key, item);
                    return item;
                }
            }
        }
    }
}
