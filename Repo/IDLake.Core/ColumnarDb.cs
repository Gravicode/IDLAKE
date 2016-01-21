using Cassandra;
using IDLake.Entities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDLake.Core
{
    public class ColumnarDb : IDataContext
    {

        ISchemaContext ctx { set; get; }
        public string DBName { private set; get; }
        public string ClusterAddress { set; get; }
        const string DefaultCluster = "127.0.0.1";
        public int ReplicationFactor { set; get; }
        public string Strategy { set; get; }
        public string UserName { set; get; }
        public void SetDefaultConfig()
        {
            ReplicationFactor = 1;
            Strategy = "SimpleStrategy";
        }
        Cluster cluster { set; get; }
        public ColumnarDb(string DBName, ISchemaContext ctx, string UserName, string ClusterAddress = DefaultCluster)
        {
            this.UserName = UserName;
            this.DBName = DBName;
            this.ctx = ctx;
            cluster = Cluster.Builder().AddContactPoint(ClusterAddress).Build();
            SetDefaultConfig();
            //ISession session = cluster.Connect("demo");
        }
        public ColumnarDb(ISchemaContext ctx)
        {
            cluster = Cluster.Builder().AddContactPoint(DefaultCluster).Build();
            this.ctx = ctx;
            SetDefaultConfig();
        }
        public void SetupDatabase(string DbName)
        {
            this.DBName = DbName;
        }

        public void SetupKeyspaceAndTable(string CollectionName, Dictionary<string, IDField> Fields)
        {
            ISession session = cluster.Connect();
            var statement = "CREATE KEYSPACE IF NOT EXISTS " + DBName + " WITH replication = {'class': '" + Strategy + "', 'replication_factor' : " + ReplicationFactor + "}";
            session.Execute(statement);

            session = cluster.Connect(DBName);
            bool CreateTable = false;
            try
            {

                var res = session.Execute($"SELECT 1 FROM {CollectionName}");
                if (res == null || res.Count() <= 0)
                {
                    CreateTable = true;
                }
            }
            catch { CreateTable = true; }
            if (CreateTable)
            {
                StringBuilder CreateSql = new StringBuilder();
                CreateSql.Append($"CREATE TABLE {DBName}.{CollectionName}");
                CreateSql.Append($"(sys_id bigint");
                var tipedata = string.Empty;
                foreach (var field in Fields)
                {
                    if (field.Value.NativeType == typeof(string))
                    {
                        tipedata = "varchar";
                    }
                    else if (field.Value.NativeType == typeof(double))
                    {
                        tipedata = "double";
                    }
                    else
                        if (field.Value.NativeType == typeof(int))
                    {
                        tipedata = "int";
                    }
                    else
                        if (field.Value.NativeType == typeof(Int64))
                    {
                        tipedata = "bigint";
                    }
                    else
                        if (field.Value.NativeType == typeof(DateTime))
                    {
                        tipedata = "timestamp";
                    }
                    else
                        if (field.Value.NativeType == typeof(char))
                    {
                        tipedata = "ascii";
                    }
                    else
                    {
                        tipedata = "varchar";
                    }
                    CreateSql.Append($", {field.Key} {tipedata}");
                }
                CreateSql.Append($", PRIMARY KEY(sys_id));");
                session.Execute(CreateSql.ToString());
            }
        }
        public Task<bool> DeleteAllData(string CollectionName)
        {
            ISession session = cluster.Connect(DBName);
            var statement = string.Format("delete from {0}", CollectionName);
            session.Execute(statement);
            //session.Execute(statement.Bind("aValue", "bValue"));
            return Task.FromResult(true);
        }

        public Task<bool> DeleteData(dynamic id, string CollectionName)
        {
            ISession session = cluster.Connect(DBName);
            var statement = string.Format("delete from {0} where sys_id = {1}", CollectionName, id);
            session.Execute(statement);
            //session.Execute(statement.Bind("aValue", "bValue"));
            return Task.FromResult(true);
        }

        public Task<bool> DeleteDataBulk(IEnumerable<dynamic> Ids, string CollectionName)
        {
            ISession session = cluster.Connect(DBName);
            StringBuilder sb = new StringBuilder();
            int counter = 0;
            foreach (var fid in Ids)
            {
                if (counter > 0)
                {
                    sb.Append($",");
                }
                sb.Append($"{fid}");
                counter++;
            }
            var statement = string.Format("delete from {0} where sys_id in ({1})", CollectionName, sb.ToString());
            session.Execute(statement);
            return Task.FromResult(true);
        }

        public Task<List<dynamic>> GetAllData(string CollectionName)
        {
            ISession session = cluster.Connect(DBName);
            RowSet rows = session.Execute($"select * from {CollectionName}");
            var data = (from c in ctx.GetAllData<SchemaEntity>()
                        where c.SchemaName == CollectionName && c.CreatedBy == this.UserName
                        select c).SingleOrDefault();
            var result = new List<dynamic>();
            foreach (Row row in rows)
            {
                dynamic NewNode = new ExpandoObject();
                foreach (var field in data.Fields)
                {

                    (NewNode as IDictionary<string, object>).Add(field.Key, row[field.Key.ToLower()]);

                }
                (NewNode as IDictionary<string, object>).Add("_id", row["sys_id"]);
                result.Add(NewNode);
            }
            return Task.FromResult<List<dynamic>>(result);
        }

        public Task<List<dynamic>> GetAllData(int Limit, string CollectionName)
        {
            ISession session = cluster.Connect(DBName);
            RowSet rows = session.Execute($"select * from {CollectionName} limit {Limit}");
            var data = (from c in ctx.GetAllData<SchemaEntity>()
                        where c.SchemaName == CollectionName && c.CreatedBy == UserName
                        select c).SingleOrDefault();
            var result = new List<dynamic>();
            foreach (Row row in rows)
            {
                dynamic NewNode = new ExpandoObject();
                foreach (var field in data.Fields)
                {

                    (NewNode as IDictionary<string, object>).Add(field.Key, row[field.Key.ToLower()]);

                }
                (NewNode as IDictionary<string, object>).Add("_id", row["sys_id"]);
                result.Add(NewNode);
            }
            return Task.FromResult<List<dynamic>>(result);
        }

        public Task<dynamic> GetDataById(dynamic Id, string CollectionName)
        {
            ISession session = cluster.Connect(DBName);
            RowSet rows = session.Execute($"select * from {CollectionName} where sys_id = {Id}");
            var data = (from c in ctx.GetAllData<SchemaEntity>()
                        where c.SchemaName == CollectionName && c.CreatedBy == UserName
                        select c).SingleOrDefault();
            dynamic NewNode = new ExpandoObject();
            foreach (Row row in rows)
            {
                foreach (var field in data.Fields)
                {

                    (NewNode as IDictionary<string, object>).Add(field.Key, row[field.Key.ToLower()]);

                }
                (NewNode as IDictionary<string, object>).Add("_id", row["sys_id"]);
                break;
            }
            return Task.FromResult<dynamic>(NewNode);
        }

        public Task<List<dynamic>> GetDataByStartId(int Limit, long StartId, string CollectionName)
        {
            ISession session = cluster.Connect(DBName);
            RowSet rows = session.Execute($"select * from {CollectionName} where sys_id >= {StartId}   limit {Limit}");
            var data = (from c in ctx.GetAllData<SchemaEntity>()
                        where c.SchemaName == CollectionName && c.CreatedBy == UserName
                        select c).SingleOrDefault();
            var result = new List<dynamic>();
            foreach (Row row in rows)
            {
                dynamic NewNode = new ExpandoObject();
                foreach (var field in data.Fields)
                {

                    (NewNode as IDictionary<string, object>).Add(field.Key, row[field.Key.ToLower()]);

                }
                (NewNode as IDictionary<string, object>).Add("_id", row["sys_id"]);
                result.Add(NewNode);
            }
            return Task.FromResult<List<dynamic>>(result);
        }

        public long GetSequence(string CollectionName)
        {
            return ctx.GetSchemaSequence($"casandra_counter:{DBName}:{CollectionName}");
        }

        public Task<bool> InsertBulkData(IEnumerable<dynamic> data, string CollectionName)
        {
            ISession session = cluster.Connect(DBName);

            foreach (dynamic item in data)
            {
                item.sys_id = this.GetSequence(CollectionName);
                StringBuilder sb = new StringBuilder();
                int counter = 0;
                if (item is ExpandoObject)
                {
                    string ColNames = string.Empty;
                    List<object> ColValues = new List<object>();
                    string QMark = string.Empty;
                    foreach (var field in (IDictionary<string, object>)item)
                    {
                        if (counter > 0)
                        {
                            ColNames += ",";
                            QMark += ",";
                        }
                        QMark += "?";
                        ColNames += field.Key == "_id" ? "sys_id" : field.Key;
                        ColValues.Add(field.Value);
                        counter++;
                    }
                    sb.Append($"insert into {CollectionName} ({ColNames}) values ({QMark})");
                    var statement = session.Prepare(sb.ToString());
                    // Bind parameter by marker position 
                    var CmdEx = statement.Bind(ColValues.ToArray());
                    session.Execute(CmdEx);
                }
            }
            return Task.FromResult(true);
        }

        public Task<bool> InsertData(dynamic data, string CollectionName)
        {
            ISession session = cluster.Connect(DBName);
            data.sys_id = this.GetSequence(CollectionName);
            StringBuilder sb = new StringBuilder();
            int counter = 0;
            if (data is ExpandoObject)
            {
                string ColNames = string.Empty;
                List<object> ColValues = new List<object>();
                string QMark = string.Empty;
                foreach (var field in (IDictionary<string, object>)data)
                {
                    if (counter > 0)
                    {
                        ColNames += ",";
                        QMark += ",";
                    }
                    QMark += "?";
                    ColNames += field.Key == "_id" ? "sys_id" : field.Key;
                    ColValues.Add(field.Value);
                    counter++;
                }
                sb.Append($"insert into {CollectionName} ({ColNames}) values ({QMark})");
                var statement = session.Prepare(sb.ToString());
                // Bind parameter by marker position 
                var CmdEx = statement.Bind(ColValues.ToArray());
                session.Execute(CmdEx);
            }

            return Task.FromResult(true);
        }

        public Task<bool> UpdateData(dynamic data, string CollectionName)
        {
            ISession session = cluster.Connect(DBName);
            StringBuilder sb = new StringBuilder();
            int counter = 0;
            if (data is ExpandoObject)
            {
                string ColNames = string.Empty;
                List<object> ColValues = new List<object>();
                foreach (var field in (IDictionary<string, object>)data)
                {
                    if (field.Key != "_id" && field.Key != "sys_id")
                    {
                        if (counter > 0)
                        {
                            ColNames += ",";
                        }
                        ColNames += $"{field.Key} = ?";
                        ColValues.Add(field.Value);
                        counter++;
                    }
                }
                sb.Append($"update {CollectionName} set {ColNames} where sys_id = {data._id}");
                var statement = session.Prepare(sb.ToString());
                session.Execute(statement.Bind(ColValues.ToArray()));
            }

            return Task.FromResult(true);
        }

        public void SetUserName(string UserName)
        {
            this.UserName = UserName;
            //do nothing
        }
    }
}
