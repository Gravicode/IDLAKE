using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDLake.Entities
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(toCheck)) return false;
            return source.IndexOf(toCheck, comp) >= 0;
        }
    }

    public interface IDataContext
    {
        void SetupDatabase(string DbName);
        Task<bool> InsertBulkData(IEnumerable<dynamic> data,string CollectionName);
        Task<bool> UpdateData(dynamic data, string CollectionName);
        Task<bool> InsertData(dynamic data, string CollectionName);
        Task<bool> DeleteAllData(string CollectionName);
        Task<bool> DeleteData(dynamic id, string CollectionName);
        Task<bool> DeleteDataBulk(IEnumerable<dynamic> Ids, string CollectionName);
        Task<List<dynamic>> GetAllData(string CollectionName);
        Task<dynamic> GetDataById(dynamic Id, string CollectionName);
        Task<List<dynamic>> GetAllData(int Limit, string CollectionName);
        Task<List<dynamic>> GetDataByStartId(int Limit, long StartId, string CollectionName);
        long GetSequence(string CollectionName);

        void SetUserName(string UserName);
    }
    public interface ISchemaContext
    {
        bool InsertBulkData<T>(IEnumerable<T> data);
        bool InsertData<T>(T data);
        bool DeleteAllData<T>();
        bool DeleteData<T>(long id);
        bool DeleteDataBulk<T>(IEnumerable<T> Ids);
     
        List<T> GetAllData<T>();
        T GetDataById<T>(long Id);
      
        List<T> GetAllData<T>(int Limit);
     
        List<T> GetDataByStartId<T>(int Limit, long StartId);
        long GetSequence<T>();
        long GetSchemaSequence(string Key);
    }
}
