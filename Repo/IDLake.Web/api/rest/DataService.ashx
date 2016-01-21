<%@ WebHandler Language="C#" Class="DataService" %>
using System.Linq;
using System;
using System.Web;
using System.Xml.Linq;
using System.Configuration;
using System.IO;
using IDLake.Core;
using IDLake.Entities;
using Newtonsoft.Json;
using System.Dynamic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using IDLake.Web;


public class DataService : HttpTaskAsyncHandler
{

    Dictionary<SchemaTypes, IDataContext> Db = new Dictionary<SchemaTypes, IDataContext>();
    static ISchemaContext ctx = null;

    public DataService()
    {

        if (ctx == null) { ctx = new SchemaDb(); }
        if (Db.Count <= 0)
        {
            Db.Add(SchemaTypes.StreamData, new InMemoryDb(ctx));
            Db.Add(SchemaTypes.RelationalData, new DocumentDb(ctx));
            Db.Add(SchemaTypes.HistoricalData, new ColumnarDb(ctx));
        }
    }

    public override async Task ProcessRequestAsync(HttpContext context)
    {

        //context.Response.ContentType = "application/json";
        OutputCls output = new OutputCls() { Result = false, Comment = "-" };
        int SchemaId = 0;
        int.TryParse(context.Request.QueryString["schemaid"], out SchemaId);
        var Operation = context.Request.QueryString["op"];
        var LimitStr = context.Request.QueryString["limit"];
        var Limit = string.IsNullOrEmpty(LimitStr) ? 1000 : int.Parse(LimitStr);
        var ApiKey = context.Request.QueryString["apikey"];
        var KeyIdStr = context.Request.QueryString["keyid"];
        var KeyId = string.IsNullOrEmpty(KeyIdStr) ? -1 : int.Parse(KeyIdStr);
        var Filter = context.Request.QueryString["filter"];
        var Query = context.Request.QueryString["qry"];
        //init vars
        IEnumerable<SchemaEntity> datas = null;
        var SchemaType = SchemaTypes.StreamData;
        var SchemaName = string.Empty;
        //filter single entity
        if (SchemaId > 0)
        {
            datas = from c in ctx.GetAllData<SchemaEntity>()
                    where c.Id == SchemaId
                    select c;

            foreach (var item in datas)
            {
                SchemaType = item.SchemaType;
                SchemaName = item.SchemaName;
                var DbName = SchemaDb.GetDbName(item.CreatedBy);
                foreach (var iDb in Db)
                {
                    iDb.Value.SetupDatabase(DbName);
                }
                   //only casandra
                   (Db[SchemaTypes.HistoricalData] as ColumnarDb).UserName = item.CreatedBy;
                break;
            }
        }
        switch (Operation)
        {
            //post data
            case "create":
                {
                    var datastr = context.Request["data"];
                    if (!string.IsNullOrEmpty(datastr))
                    {
                        dynamic data = SchemaConverter.JsonToExpando(datastr);
                        output.Result = Db[SchemaType].InsertData(data, SchemaName);
                        if (output.Result.HasValue && output.Result.Value)
                        {
                            output.Comment = "Insert succeed.";
                        }
                    }
                }
                break;
            case "update":
                {
                    var datastr = context.Request["data"];
                    if (!string.IsNullOrEmpty(datastr))
                    {
                        dynamic data = SchemaConverter.JsonToExpando(datastr);
                        Db[SchemaType].UpdateData(data, SchemaName);
                        if (output.Result.HasValue && output.Result.Value)
                        {
                            output.Comment = "Update succeed.";
                        }
                    }
                }
                break;
            case "delete":
                if (KeyId > 0)
                {
                    output.Result = await Db[SchemaType].DeleteData(KeyId, SchemaName);
                    if (output.Result.HasValue && output.Result.Value)
                    {
                        output.Comment = "Delete succeed.";
                    }
                }
                break;
            case "read":
                {
                    //Task<List<dynamic>> tsk=null;
                    List<dynamic> temp = null;

                    temp = await Db[SchemaType].GetAllData(Limit, SchemaName);

                    //tsk.Wait();
                    var hasil = JsonConvert.SerializeObject(temp);
                    context.Response.Write(hasil);
                    return;
                }

            case "filter":
                {
                    List<dynamic> temp = null;

                    temp = await Db[SchemaType].GetAllData(SchemaName);
                    //I don't have better way to do this.. roslyn doesn't support dynamic lambda
                    var dt = SchemaConverter.ExpandoToDataTable(temp);
                    //using dynamic linq to filter data
                    var filteredData = dt.Select(Filter).Take(Limit).Select(r => r.Table.Columns.Cast<DataColumn>()
            .Select(c => new KeyValuePair<string, object>(c.ColumnName, r[c.Ordinal])
           ).ToDictionary(z => z.Key, z => z.Value)
    ).ToList();

                    var hasil = JsonConvert.SerializeObject(filteredData, Formatting.Indented);
                    context.Response.Write(hasil);
                }
                return;
            case "query":
                {
                    try
                    {
                        //var dbService = new DBService();
                        var temp = await DBService.Execute(Query, Limit);
                        context.Response.Write(JsonConvert.SerializeObject(temp));
                        return;

                    }
                    catch (Exception ex)
                    {
                        output.Comment = ex.Message + "_" + ex.StackTrace;

                    }
                }
                break;
        }
        context.Response.Write(JsonConvert.SerializeObject(output));
    }

    public override bool IsReusable
    {
        get
        {
            return false;
        }
    }

}


