using CSScriptLibrary;
using IDLake.Core;
using IDLake.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;


namespace IDLake.Web
{
    /// <summary>
    /// Summary description for DBService
    /// </summary>
    public interface IScript
    {
        Task<List<dynamic>> ExecuteQuery(string query,int Limit=100);
    }
    public class DBService
    {
        static Dictionary<SchemaTypes, IDataContext> Db = new Dictionary<SchemaTypes, IDataContext>();
        static ISchemaContext ctx = null;

        public DBService()
        {
            if (ctx == null) { ctx = new SchemaDb(); }
            if (Db.Count <= 0)
            {
                Db.Add(SchemaTypes.StreamData, new InMemoryDb(ctx));
                Db.Add(SchemaTypes.RelationalData, new DocumentDb(ctx));
                Db.Add(SchemaTypes.HistoricalData, new ColumnarDb(ctx));
            }
        }

        public async Task<List<dynamic>> GetDb(int SchemaId)
        {
            var data = (from c in ctx.GetAllData<SchemaEntity>()
                        where c.Id == SchemaId
                        select c).SingleOrDefault();
            IDataContext dx = Db[data.SchemaType];
            var dbName = SchemaDb.GetDbName(data.CreatedBy);
            Db[data.SchemaType].SetupDatabase(dbName);
            if (data.SchemaType == SchemaTypes.HistoricalData)
            {
                Db[data.SchemaType].SetUserName(data.CreatedBy);
            }
            var datas = await dx.GetAllData(data.SchemaName);
            return datas;
        }

        public static async Task<List<dynamic>> Execute(string query,int Limit=100)
        {
            /*Sample query join
            from a in await db.GetDb(21) join b in await db.GetDb(20) on a.NoKTP equals b.KTP select New.NewObject(NoKTP: a.NoKTP, Pengaduan : b.Pengaduan, Nama : b.Nama )
            */
            
            dynamic script = CSScript.Evaluator
                         .LoadCode<IScript>(@"
using System.Threading.Tasks;
using System.Collections.Generic;
using IDLake.Web;
using System.Linq;
using System.Dynamic;
using System;
using Dynamitey;

public class Script{
public async Task<List<dynamic>> ExecuteQuery(string query,int Limit=100)
                                       {
                                            dynamic New = Builder.New<ExpandoObject>();
                                            DBService db = new DBService();
                                            var data = " + query+@";
                                            return data.Take(Limit).ToList();

                                       }
}
");
            var data = await script.ExecuteQuery(query,Limit);
            return data;
        }

    }
}