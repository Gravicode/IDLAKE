<%@ WebHandler Language="C#" Class="ImportData" %>

using System;
using System.Web;
using System.Xml.Linq;

using IDLake.Web;
using System.Configuration;
using System.IO;
using IDLake.Core;
using IDLake.Entities;
using Newtonsoft.Json;
using System.Dynamic;
using System.Collections.Generic;

public class ImportData : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "application/json";

        if (context.Request.Files.Count <= 0)
        {
            OutputCls cls = new OutputCls() { Result = false, Comment = "No file attach." };
            context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(cls));
            context.Response.End();
            return;
        }
        OutputCls output = new OutputCls() { Result = false };
        var Username = context.Request["UserName"];
        var SchemaName = context.Request["SchemaName"];
        var SchemaType = (SchemaTypes)int.Parse(context.Request["SchemaType"]);
        var TempStr = ConfigurationManager.AppSettings["TempPath"];
        var DirStr = string.Format("{0}/{1}/Dumps/{2}/", TempStr, Username, SchemaName);
        if (!Directory.Exists(DirStr)) Directory.CreateDirectory(DirStr);
        ISchemaContext ctx = new SchemaDb();
        for (int i = 0; i < context.Request.Files.Count; i++)
        {
            HttpPostedFile file = context.Request.Files[i];
            var PathStr = string.Format("{0}/{1}/Dumps/{2}/{3}", TempStr, Username, SchemaName, new FileInfo(file.FileName).Name);
            file.SaveAs(PathStr);
            var FileContent = File.ReadAllText(PathStr);
            var DbName = SchemaDb.GetDbName(Username);
            IDataContext dtx = null;
            if (SchemaType == SchemaTypes.StreamData)
                dtx = new InMemoryDb(DbName, ctx); //redis
            else if (SchemaType == SchemaTypes.RelationalData)
                dtx = new DocumentDb(DbName, ctx); //mongodb
            else
                 dtx = new ColumnarDb(DbName, ctx,Username); //casandra
            switch (Path.GetExtension(file.FileName).ToLower())
            {
                case ".xml":
                    {
                        dynamic temp = SchemaConverter.XMLtoExpando(null, XElement.Parse(FileContent));
                        if (temp is List<dynamic>)
                        {
                            var datas = temp as List<dynamic>;
                            dtx.InsertBulkData(datas, SchemaName);

                        }

                    }
                    break;

                case ".json":
                    {

                        var datas = JsonConvert.DeserializeObject<List<dynamic>>(FileContent);
                        dtx.InsertBulkData(datas, SchemaName);

                    }
                    break;

                case ".csv":
                    {
                        dynamic temp = SchemaConverter.CsvToExpando(PathStr);
                        if (temp is List<dynamic>)
                        {
                            var datas = temp as List<dynamic>;
                            dtx.InsertBulkData(datas, SchemaName);

                        }


                    }
                    break;
                default:
                    output.Comment = "Invalid file extension. Only accept (.csv | .json | .xml)";
                    context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(output));
                    context.Response.End();
                    return;
            }
            //insert schema


            output.Result = true;
            output.Comment = "Data imported.";
            context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(output));
            context.Response.End();
        }
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}