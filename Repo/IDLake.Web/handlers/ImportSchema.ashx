<%@ WebHandler Language="C#" Class="ImportSchema" %>

using System;
using System.Web;
using IDLake.Web;
using System.Configuration;
using System.IO;
using IDLake.Core;
using IDLake.Entities;

public class ImportSchema : IHttpHandler {

    public void ProcessRequest (HttpContext context) {
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
        var Description = context.Request["Description"];
        var GroupName = context.Request["GroupName"];
        var InternalName = SchemaName.Replace(" ", "_");
        var SchemaType = (SchemaTypes) int.Parse(context.Request["SchemaType"]);
        var AccessType = (AccessTypes) int.Parse(context.Request["AccessType"]);
        var TempStr = ConfigurationManager.AppSettings["TempPath"];
        var DirStr = string.Format("{0}/{1}/{2}", TempStr, Username, SchemaName);
        if (!Directory.Exists(DirStr)) Directory.CreateDirectory(DirStr);
        ISchemaContext ctx = new SchemaDb();
        for (int i=0;i< context.Request.Files.Count;i++)
        {
            HttpPostedFile file = context.Request.Files[i];
            var PathStr = string.Format("{0}/{1}/{2}/{3}", TempStr, Username, SchemaName, new FileInfo(file.FileName).Name);
            file.SaveAs(PathStr);
            var FileContent = File.ReadAllText(PathStr);
            SchemaEntity item = null;
            switch (Path.GetExtension(file.FileName).ToLower())
            {
                case ".xml":
                    {
                        item = SchemaConverter.XmlToSchema(FileContent, SchemaName);


                    }
                    break;

                case ".json":
                    {
                        item = SchemaConverter.JsonToSchema(FileContent, SchemaName);

                    }
                    break;

                case ".csv":
                    {
                        item = SchemaConverter.CsvToSchema(PathStr, SchemaName);

                    }
                    break;
                default:
                    output.Comment = "Invalid file extension. Only accept (.csv | .json | .xml)";
                    context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(output));
                    context.Response.End();
                    return;
            }
            //insert schema
            item.InternalName = InternalName;
            item.AccessType = AccessType;
            item.CreatedBy = Username;
            item.Description = Description;
            item.FilePath = PathStr;
            item.SchemaType = SchemaType;
            item.GroupName = GroupName;
            item.Id = ctx.GetSequence<SchemaEntity>();
            
            ctx.InsertData<SchemaEntity>(item);
            output.Result = true;
            output.Comment = "Schema saved.";

            //khusus columnar (casandra)
            if (SchemaType == SchemaTypes.HistoricalData)
            {
                var dx = new ColumnarDb(SchemaDb.GetDbName(Username),ctx,Username);
                dx.SetupKeyspaceAndTable(SchemaName, item.Fields);
            }

            context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(output));
            context.Response.End();
        }
    }

    public bool IsReusable {
        get {
            return false;
        }
    }

}