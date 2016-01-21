using System;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using IDLake.Core;
using IDLake.Entities;
using System.Configuration;
using System.Linq;
using System.Web.Security;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml.Linq;
//using IDLake.DynamicQuery;
using System.Linq.Dynamic;

namespace IDLake.Web
{
    [HubName("WebHub")]
    public class WebHub : Hub
    {
        #region Variables
        static string DownloadPath { set; get; }
        static LibraryContainer _container;
        public LibraryContainer container
        {
            set
            {
                _container = value;
            }
            get
            {   
                return _container;
            }
        }
        #endregion

        #region Initialization
        public WebHub()
        {
            DownloadPath = ConfigurationManager.AppSettings["DownloadPath"];
            //singleton + container pattern
            if (container == null)
            {
                /* # INVERSION OF CONTROL SCANNER/CONTAINER # uncomment this when u deploy in production area

                 Gravicode.Transformer.LibraryScanner scanner = new Gravicode.Transformer.LibraryScanner();
                 scanner.ScanLibrary<IDataContext>(ConfigurationManager.AppSettings["LibraryPath"]);
                 foreach (string a in scanner.GetLibraryList())
                 {
                     Console.WriteLine("Nama fungsi :" + a);
                 }
                 IDataContext redis = scanner.getInstance<IDataContext>(ConfigurationManager.AppSettings["RedisLibPath"]);
                 IDataContext mongo = scanner.getInstance<IDataContext>(ConfigurationManager.AppSettings["MongoLibPath"]);
                 */
                container = new LibraryContainer();
                //database aplikasi dan desain schema
                container.RegisterLibrary<SchemaDb>(new SchemaDb());
                //database untuk relatime data
                container.RegisterLibrary<InMemoryDb>(new InMemoryDb(container.Get<SchemaDb>()));
                //database untuk relational data
                container.RegisterLibrary<DocumentDb>(new DocumentDb(container.Get<SchemaDb>()));
                //database untuk Big Data (historikal) - casandra
                container.RegisterLibrary<ColumnarDb>(new ColumnarDb(container.Get<SchemaDb>()));

            }
        }

        void SetDatabaseName(string username=null)
        {
            if (username == null && Context == null) return;   
            if (username == null)
            {
                username = Context.User.Identity.Name;
            }
            //setup database name per user
            var dbName = SchemaDb.GetDbName(username);
            container.Get<InMemoryDb>().SetupDatabase(dbName);
            container.Get<DocumentDb>().SetupDatabase(dbName);
            container.Get<ColumnarDb>().SetupDatabase(dbName);
        }
        #endregion

        #region Authentication
        [HubMethodName("Login")]
        public OutputCls Login(string username, string password)
        {
            OutputCls res = new OutputCls() { Result = false, Comment = "Username & password invalid." };
            var db = container.Get<SchemaDb>();
            var datas = from c in db.GetAllData<User>()
                        where c.UserName == username
                        select c;
            foreach (var item in datas)
            {
                if (item.Password == password)
                {
                    res.Comment = "Login succeed.";
                    res.Result = true;
                    SetDatabaseName(username);                    
                    return res;
                }
            }
            return res;
            // Call the broadcastMessage method to update clients.
            //Clients.All.displayData(datas);
        }

        [HubMethodName("CreateUser")]
        public OutputCls CreateUser(string username, string password, string Email)
        {
            OutputCls res = new OutputCls() { Result = false, Comment = "Create user failed." };
            var db = container.Get<SchemaDb>();
            var datas = from c in db.GetAllData<User>()
                        where c.UserName == username
                        select c;
            if (datas.Count() > 0)
            {
                res.Comment = "User is already exists.";
                return res;
            }
            db.InsertData<User>(new User() { UserName = username, Password = password, Email = Email, IsLocked = false, Id = db.GetSequence<User>() });
            res.Result = true;
            res.Comment = "User registered.";
            return res;
        }
        #endregion

        #region Data Management and Operation
        [HubMethodName("GetDataBySchema")]
        public async Task<List<dynamic>> GetDataBySchema(int SchemaId)
        {
            SetDatabaseName();
            var db = container.Get<SchemaDb>();
            var SchemaName = string.Empty;
            var data = (from c in db.GetAllData<SchemaEntity>()
                        where c.Id==SchemaId
                        select c).SingleOrDefault();
            IDataContext dx = null;
            dx = GetDB(data.SchemaType);
            var datas = await dx.GetAllData(data.SchemaName);
            return datas;
        }
        [HubMethodName("SaveData")]
        public async Task<OutputCls> SaveData(List<dynamic> Fields, string SchemaName)
        {
            SetDatabaseName();
            
            /*
            foreach (dynamic newObj in Fields)
            {

                (Entity as IDictionary<string, object>).Add((string)newObj.fieldname, (string)newObj.fieldvalue);

            }*/
            OutputCls res = new OutputCls() { Result = false, Comment = "Create user failed." };
            var username = System.Web.HttpContext.Current.User.Identity.Name;
            var db = container.Get<SchemaDb>();
            var data = (from c in db.GetAllData<SchemaEntity>()
                        where c.SchemaName == SchemaName && c.CreatedBy == username
                        select c).SingleOrDefault();
            IDataContext dx = null;
            dx = GetDB(data.SchemaType);
            dynamic Entity = new ExpandoObject();
            foreach (dynamic newObj in Fields)
            {
                dynamic obj;
                if (data.Fields[(string)newObj.fieldname].NativeType == typeof(double))
                {
                    obj = Convert.ToDouble(newObj.fieldvalue);
                }
                else if (data.Fields[(string)newObj.fieldname].NativeType == typeof(DateTime))
                {
                    obj = Convert.ToDateTime(newObj.fieldvalue);
                }
                else if (data.Fields[(string)newObj.fieldname].NativeType == typeof(char))
                {
                    obj = Convert.ToChar(newObj.fieldvalue);
                }
                else if (data.Fields[(string)newObj.fieldname].NativeType == typeof(int))
                {
                    obj = Convert.ToInt32(newObj.fieldvalue);
                }
                else if (data.Fields[(string)newObj.fieldname].NativeType == typeof(Int64))
                {
                    obj = Convert.ToInt64(newObj.fieldvalue);
                }
                else
                {
                    obj = Convert.ToString(newObj.fieldvalue);
                }
               (Entity as IDictionary<string, object>).Add((string)newObj.fieldname, obj);

            }
            res.Result = await dx.InsertData(Entity, SchemaName);
            res.Comment = "ok";
            return res;
        }
        [HubMethodName("UpdateData")]
        public async Task<OutputCls> UpdateData(int FID, List<dynamic> Fields, string SchemaName)
        {
            SetDatabaseName();
           
            OutputCls res = new OutputCls() { Result = false, Comment = "Update data failed." };
            var username = System.Web.HttpContext.Current.User.Identity.Name;
            var db = container.Get<SchemaDb>();
            var data = (from c in db.GetAllData<SchemaEntity>()
                        where c.SchemaName == SchemaName && c.CreatedBy == username
                        select c).SingleOrDefault();
            IDataContext dx = null;
            dx = GetDB(data.SchemaType);
            dynamic Entity = new ExpandoObject();
            foreach (dynamic newObj in Fields)
            {
                dynamic obj;
                if (data.Fields[(string)newObj.fieldname].NativeType == typeof(double))
                {
                    obj = Convert.ToDouble(newObj.fieldvalue);
                }
                else if (data.Fields[(string)newObj.fieldname].NativeType == typeof(DateTime))
                {
                    obj = Convert.ToDateTime(newObj.fieldvalue);
                }
                else if (data.Fields[(string)newObj.fieldname].NativeType == typeof(char))
                {
                    obj = Convert.ToChar(newObj.fieldvalue);
                }
                else if (data.Fields[(string)newObj.fieldname].NativeType == typeof(int))
                {
                    obj = Convert.ToInt32(newObj.fieldvalue);
                }
                else if (data.Fields[(string)newObj.fieldname].NativeType == typeof(Int64))
                {
                    obj = Convert.ToInt64(newObj.fieldvalue);
                }
                else
                {
                    obj = Convert.ToString(newObj.fieldvalue);
                }
                (Entity as IDictionary<string, object>).Add((string)newObj.fieldname, obj);

            }
            Entity._id = FID;
            res.Result = await dx.UpdateData(Entity, SchemaName);
            res.Comment = "ok";
            return res;
        }
        [HubMethodName("DeleteData")]
        public async Task<OutputCls> DeleteData(int FID, string SchemaName)
        {
            SetDatabaseName();
           
            OutputCls res = new OutputCls() { Result = false, Comment = "Update data failed." };
            var username = System.Web.HttpContext.Current.User.Identity.Name;
            var db = container.Get<SchemaDb>();
            var data = (from c in db.GetAllData<SchemaEntity>()
                        where c.SchemaName == SchemaName && c.CreatedBy == username
                        select c).SingleOrDefault();
            IDataContext dx = null;
            dx = GetDB(data.SchemaType);

            res.Result = await dx.DeleteData(FID, SchemaName);
            res.Comment = "ok";
            return res;
        }

        [HubMethodName("FilterData")]
        public async Task<string> FilterData(string SchemaName, string Query)
        {
            var username = System.Web.HttpContext.Current.User.Identity.Name;
            var db = container.Get<SchemaDb>();
            var data = (from c in db.GetAllData<SchemaEntity>()
                        where c.SchemaName == SchemaName && c.CreatedBy == username
                        select c).SingleOrDefault();
            IDataContext dx = null;
            dx = GetDB(data.SchemaType);
            var AllData = await dx.GetAllData(SchemaName);
            //I don't have better way to do this.. roslyn doesn't support dynamic lambda
            var dt = SchemaConverter.ExpandoToDataTable(AllData);
            //using dynamic linq to filter data
            var filteredData = dt.Select(Query);
            return JsonConvert.SerializeObject(filteredData, Formatting.Indented);

        }
        #endregion

        #region Schema Generator
        [HubMethodName("DesignSchema")]
        public OutputCls DesignSchema(dynamic Entity,List<dynamic> Rows)
        {
            ISchemaContext ctx = new SchemaDb();
            var Fields = new List<IDField>();
            OutputCls res = new OutputCls() { Result = false, Comment = "Schema invalid." };
            foreach(dynamic item in Rows)
            {   
                var Field = new IDField();
                Field.Name = item.field_name;
                Field.IsMandatory = (bool)item.field_mandatory;
                Field.FieldType = FieldTypes.SingleField;
                switch ((string)item.field_type)
                {
                    case "string":
                        Field.NativeType = typeof(string);
                        break;
                    case "integer":
                        Field.NativeType = typeof(Int64);
                        break;
                    case "double":
                        Field.NativeType = typeof(double);
                        break;
                    case "datetime":
                        Field.NativeType = typeof(DateTime);
                        break;
                    case "boolean":
                        Field.NativeType = typeof(bool);
                        break;
                    case "character":
                        Field.NativeType = typeof(char);
                        break;
                    default:
                        Field.NativeType = typeof(string);
                        break;                  
                }
                Field.RegexValidation = item.field_regex;
                Field.Desc = item.field_description;
                Fields.Add(Field);
            }
            string SchemaName = Entity.SchemaName;
            SchemaEntity newEntity = SchemaConverter.DesignToSchema(Fields, SchemaName);
            newEntity.GroupName = Entity.GroupName;
            newEntity.SchemaType = (SchemaTypes)Entity.SchemaType;
            newEntity.Description = Entity.Description;
            newEntity.AccessType = (AccessTypes)Entity.AccessType;
            newEntity.CreatedBy = Entity.UserName;
            newEntity.Id = ctx.GetSequence<SchemaEntity>();
            ctx.InsertData<SchemaEntity>(newEntity);
            res.Result = true;
            res.Comment = "ok";

            //khusus columnar (casandra)
            if ((SchemaTypes)Entity.SchemaType == SchemaTypes.HistoricalData)
            {
                SetDatabaseName();
                var dx = container.Get<ColumnarDb>();
                dx.SetupKeyspaceAndTable(SchemaName, newEntity.Fields);
            }
            return res;
        }
        #endregion

        #region Data Migration
        [HubMethodName("GetSchemas")]
        public List<SchemaEntity> GetSchemas(string username)
        {
            OutputCls res = new OutputCls() { Result = false, Comment = "get schema error." };
            var db = container.Get<SchemaDb>();
            var datas = from c in db.GetAllData<SchemaEntity>()
                        where c.CreatedBy == username
                        orderby c.GroupName ascending
                        select c;
            
            return datas.ToList();
            // Call the broadcastMessage method to update clients.
            //Clients.All.displayData(datas);
        }
        [HubMethodName("ExportData")]
        public async Task<OutputCls> ExportData(long SchemaId,string UserName,string Tipe)
        {
            SetDatabaseName();
            OutputCls output = new OutputCls() { Result = false, Comment = "export data error." };
            try {
                string SchemaName = string.Empty;
                SchemaTypes SchemaType = SchemaTypes.StreamData;
               
                ISchemaContext ctx = new SchemaDb();
                var data = from c in ctx.GetAllData<SchemaEntity>()
                           where c.Id == SchemaId
                           select c;
                foreach (var item in data)
                {
                    SchemaName = item.SchemaName;
                    SchemaType = item.SchemaType;
                }

                IDataContext dtx = null;
                dtx = GetDB(SchemaType);
                string PathToFile = string.Format(string.Format("{0}/{1}/{2}", DownloadPath, UserName, SchemaName));
                if (!Directory.Exists(PathToFile)) Directory.CreateDirectory(PathToFile);
                var temp = await dtx.GetAllData(SchemaName);
                var FileName = string.Empty;
                switch (Tipe)
                {
                    case "csv":
                        string csvstr = SchemaConverter.ExpandoToCsv(temp);
                        FileName = SchemaName + DateTime.Now.ToString("_yyyyMMdd_HHmm") + ".csv";
                        File.WriteAllText(string.Format("{0}/{1}", PathToFile, FileName), csvstr);
                        break;
                    case "json":
                        string jsonstr = JsonConvert.SerializeObject(temp);
                        FileName = SchemaName + DateTime.Now.ToString("_yyyyMMdd_HHmm") + ".json";
                        File.WriteAllText(string.Format("{0}/{1}", PathToFile, FileName), jsonstr);
                        break;
                    case "xml":
                        XElement el = SchemaConverter.ExpandoToXML(temp, SchemaName);
                        FileName = SchemaName + DateTime.Now.ToString("_yyyyMMdd_HHmm") + ".xml";
                        File.WriteAllText(string.Format("{0}/{1}", PathToFile, FileName), el.ToString());
                        break;

                }
                output.Params = new List<dynamic>();
                output.Params.Add(string.Format("/downloads/{0}/{1}/{2}", UserName, SchemaName, FileName));
                output.Result = true;
                output.Comment = "ok";
                return output;
            }
            catch (Exception ex)
            {
                output.Comment = ex.Message;
                return output;
            }
        }
        #endregion

        #region Report
        [HubMethodName("SaveLapor")]
        public OutputCls SaveLapor(string nama, string url, string keterangan)
        {
            OutputCls res = new OutputCls() { Result = false, Comment = "Create user failed." };
            var db = container.Get<SchemaDb>();

            res.Result = db.InsertData<Laporan>(new Laporan() { nama = nama, url = url, keterangan = keterangan, Id = db.GetSequence<Laporan>() });
            res.Comment = "User registered.";

            return res;
        }
        #endregion

        #region Helper
        IDataContext GetDB(SchemaTypes tipe)
        {
            if (tipe == SchemaTypes.StreamData)
            {
                return container.Get<InMemoryDb>();
            }
            else if (tipe == SchemaTypes.RelationalData)
            {
                return container.Get<DocumentDb>();
            }
            else
            {
                var dx = container.Get<ColumnarDb>();
                if (Context != null)
                {
                    dx.UserName = Context.User.Identity.Name;
                }
                return dx;
            }
        }
        #endregion
    }
}