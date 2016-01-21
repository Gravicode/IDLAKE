using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using IDLake.Core;
using IDLake.Entities;
using IDLake.Web;
using IDLake.Tools;
using System.Configuration;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Threading;
using Newtonsoft.Json;
using System.Data;
using System.Activities.Statements;
using System.Dynamic;
/// <summary>
/// Summary description for DataService
/// </summary>
[WebService(Namespace = "http://gravicode.id/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
[System.Web.Script.Services.ScriptService]
public class DataService : System.Web.Services.WebService
{
    Dictionary<SchemaTypes, IDataContext> Db = new Dictionary<SchemaTypes, IDataContext>();
    static ISchemaContext ctx = null;
    public enum MediaTypes {JSON,XML }
    public DataService()
    {

        if (ctx == null) { ctx = new SchemaDb(); }
        Db.Add(SchemaTypes.StreamData, new InMemoryDb(ctx));
        Db.Add(SchemaTypes.RelationalData, new DocumentDb(ctx));
        Db.Add(SchemaTypes.HistoricalData, new ColumnarDb(ctx));

        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }
    private SchemaEntity getSchemaById(long SchemaId)
    {
        SchemaEntity selItem = null;
        var datas = from c in ctx.GetAllData<SchemaEntity>()
                    where c.Id == SchemaId
                    select c;
        foreach (var item in datas)
        {
            selItem = item;
            break;
        }
        return selItem;
    }
    [WebMethod]
    public string HelloWorld()
    {
        return "Hello World";
    }
    [WebMethod]
    public OutputCls DeleteData(long SchemaId,long KeyId)
    {
        OutputCls output = new OutputCls() { Result = false, Comment = "No params provided." };
        //get schemaid to determine dbtype
        var item = getSchemaById(SchemaId);
        if (item != null)
        {

            if (KeyId > 0)
            {
                output.Result = Task.Run(async () => await Db[item.SchemaType].DeleteData(KeyId, item.SchemaName)).Result;
            }
            else
            {
                output.Comment = "KeyId cannot less than 0!";
            }
        }
        else
        {
            output.Comment = "Schema not found!";
        }
        return output;
    }
    [WebMethod]
    public OutputCls InsertData(string ObjInJson, long SchemaId)
    {
        OutputCls output = new OutputCls() { Result = false, Comment = "No params provided." };
        //get schemaid to determine dbtype
        var item = getSchemaById(SchemaId);
        if (item != null)
        {

            if (!string.IsNullOrEmpty(ObjInJson))
            {
                dynamic data = SchemaConverter.JsonToExpando(ObjInJson);
                output.Result = Db[item.SchemaType].InsertData(data, item.SchemaName);
            }
            else
            {
                output.Comment = "Object cannot be empty!";
            }
        }
        else
        {
            output.Comment = "Schema not found!";
        }
        return output;
    }
    [WebMethod]
    public OutputCls UpdateData(string ObjInJson, long SchemaId)
    {
        OutputCls output = new OutputCls() { Result = false, Comment = "No params provided." };
        //get schemaid to determine dbtype
        var item = getSchemaById(SchemaId);
        if (item != null)
        {
            if (!string.IsNullOrEmpty(ObjInJson))
            {
                dynamic data = SchemaConverter.JsonToExpando(ObjInJson);
                Db[item.SchemaType].UpdateData(data, item.SchemaName);
            }
            else
            {
                output.Comment = "Object cannot be empty!";
            }
        }
        else
        {
            output.Comment = "Schema not found!";
        }
        return output;
    }

    [WebMethod]
    public string GetAllData(long SchemaId, string ApiKey, MediaTypes MediaType, int Limit = 100)
    {
        //cek akses by ApiKey

        //get schemaid to determine dbtype
        var item = getSchemaById(SchemaId);
        if (item != null)
        {
            var DbName = SchemaDb.GetDbName(item.CreatedBy);
            foreach (var iDb in Db)
            {
                iDb.Value.SetupDatabase(DbName);
            }
            (Db[SchemaTypes.HistoricalData] as ColumnarDb).UserName = item.CreatedBy;
            List<dynamic> tsk = Task.Run(async () =>
                await Db[item.SchemaType].GetAllData(Limit, item.SchemaName)).Result;
            if (MediaType == MediaTypes.XML)
            {
                XElement hasil = SchemaConverter.ExpandoToXML(tsk, item.SchemaName);
                return hasil.ToString();
            }
            else
            {
                return JsonConvert.SerializeObject(tsk);

            }
        }
        return string.Empty;
    }
    [WebMethod]
    public string GetDataWithFilter(long SchemaId, string ApiKey, MediaTypes MediaType,string Filter, int Limit = 100)
    {
        //cek akses by ApiKey

        //get schemaid to determine dbtype
        var item = getSchemaById(SchemaId);
        if (item != null)
        {
            var DbName = SchemaDb.GetDbName(item.CreatedBy);
            foreach (var iDb in Db)
            {
                iDb.Value.SetupDatabase(DbName);
            }
            (Db[SchemaTypes.HistoricalData] as ColumnarDb).UserName = item.CreatedBy;

            List<dynamic> temp = Task.Run(async () =>
                await Db[item.SchemaType].GetAllData(Limit, item.SchemaName)).Result;

            //I don't have better way to do this.. roslyn doesn't support dynamic lambda
            var dt = SchemaConverter.ExpandoToDataTable(temp);
            //using dynamic linq to filter data
            var filteredData = dt.Select(Filter).Take(Limit).Select(r => r.Table.Columns.Cast<DataColumn>()
    .Select(c => new KeyValuePair<string, object>(c.ColumnName, r[c.Ordinal])
   ).ToDictionary(z => z.Key, z => z.Value)
).ToList();
            if (MediaType == MediaTypes.XML)
            {
                XElement hasil = SchemaConverter.ExpandoToXML(filteredData, item.SchemaName);
                return hasil.ToString();
            }
            else
            {
                var hasil = JsonConvert.SerializeObject(filteredData, Formatting.Indented);
                return hasil;
            }
        }
        return string.Empty;
    }
    [WebMethod]
    public string GetDataWithQuery(string ApiKey, MediaTypes MediaType, string Query, int Limit = 100)
    {
        //cek akses by ApiKey
        var uid = HttpContext.Current.User.Identity.Name;
        if (!string.IsNullOrEmpty(uid))
        {
            var DbName = SchemaDb.GetDbName(uid);
            foreach (var iDb in Db)
            {
                iDb.Value.SetupDatabase(DbName);
            }
            (Db[SchemaTypes.HistoricalData] as ColumnarDb).UserName = uid;
            //var dbService = new DBService();
            var temp = Task.Run(async () => await DBService.Execute(Query, Limit)).Result;

            if (MediaType == MediaTypes.XML)
            {
                XElement hasil = SchemaConverter.ExpandoToXML(temp, "data");
                return hasil.ToString();
            }
            else
            {
                var hasil = JsonConvert.SerializeObject(temp);
                return hasil;
            }
        }
        return string.Empty;
    }
}
