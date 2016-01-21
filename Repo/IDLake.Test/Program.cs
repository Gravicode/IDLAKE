using IDLake.Core;
using IDLake.Entities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDLake.Tools;
using static System.Console;
using System.Net.Http;

namespace IDLake.Test
{
    class Program
    {
      
        static void Main(string[] args)
        {
            HttpClient client = new HttpClient();
            var nameValues = new Dictionary<string, string>();
            nameValues.Add("data", @"{""Nama"":""SENSOR 004"",""Lokasi"":""L003"",""Nilai"":22}");
            var Name = new FormUrlEncodedContent(nameValues);
            client.PostAsync("http://localhost:2959/api/rest/DataService.ashx?schemaid=1&op=create", Name).ContinueWith(task =>
            {
                var responseNew = task.Result;
                Console.WriteLine(responseNew.Content.ReadAsStringAsync().Result);
            });
            Console.ReadLine();
            return;

            var userid = "asep";
            var SchemaId = 21;
            var ItemId = 2;

            var db = new IDLake.Core.SchemaDb();
            var selData = (from c in db.GetAllData<IDLake.Entities.SchemaEntity>()
                           where c.Id == SchemaId
                           orderby c.GroupName ascending
                           select c).SingleOrDefault();
            IDataContext dx = null;
            var DBName = SchemaDb.GetDbName(userid);
            if (selData.SchemaType == SchemaTypes.StreamData)
            {
                dx = new InMemoryDb(DBName, db);
            }
            else if (selData.SchemaType == SchemaTypes.RelationalData)
            {
                dx = new DocumentDb(DBName, db);
            }
            else
            {
                dx = new ColumnarDb(DBName, db, userid);
            }
            Task<dynamic> tsk = dx.GetDataById(ItemId, selData.SchemaName);
            //tsk.Start();
            tsk.Wait();
            IDictionary<String, Object> selItem = null;
            if (tsk.Result is ExpandoObject)
            {
                selItem = tsk.Result as IDictionary<String, Object>;
            }
            return;
            //test sql to caml
            string SqlStr = @"select id,evid_cislo,nazov,adresa,ulica,vec,datum_zal,datum_odos,ukoncene_dna  from koresp  
where ((id_typ <= 3 or id_typ = 4) 
 and (datum_zal > datum_odos)) or (id > 21) 
order by nazov desc ,id asc";
            string CAML = SqlToCAML.TextSqlToCAML(SqlStr);
            WriteLine(CAML);

            //test caml to sql where
            string CAML2 = @"<Where>
  <And>
    <Or>
      <Geq>
        <FieldRef Name='Microfilm' />
        <Value Type='Text'>10</Value>
      </Geq>
      <Leq>
        <FieldRef Name='Microfilm' />
        <Value Type='Text'>50</Value>
      </Leq>
    </Or>
    <BeginsWith>
      <FieldRef Name='Title' />
      <Value Type='Text'>Ice</Value>
    </BeginsWith> 
  </And>
</Where>";
            string Sql2 = CamlToSql.CamlToSqlWhere(CAML2);
            WriteLine(Sql2);

            WriteLine(SampleLinqToCaml.GetLinqToCaml());
            Console.ReadLine();
            /*
            ISchemaContext ctx = new SchemaDb();
            dynamic contacts = new System.Dynamic.ExpandoObject();
            (contacts as IDictionary<string, object>).Add("Usia", 40);
            contacts.Name = "Patrick Hines";
            contacts.Phone = "206-555-0144";
            (contacts as IDictionary<string, object>).Add("House", new List<dynamic>());
            contacts.House.Add(new System.Dynamic.ExpandoObject());
            contacts.House[0].Address = "Jln. Klumeten";
            contacts.House[0].POBox = "7658";

            dynamic contacts2 = new ExpandoObject();
            (contacts2 as IDictionary<string, object>).Add("Usia", 39);
            contacts2.Name = "Ellen Adams";
            contacts2.Phone = "206-555-0155";
            contacts2.House = new List<dynamic>();
            contacts2.House.Add(new ExpandoObject());
            contacts2.House[0].Address = "Jln. Klumet";
            contacts2.House[0].POBox = "23456";
           
            Console.WriteLine(SchemaConverter.AreExpandoStructureEquals(contacts, contacts2));
            //GetData();
            //dtx.InsertData<dynamic>(contacts, "contacts");
            SchemaEntity item = SchemaConverter.ExpandoToSchema(contacts, nameof(contacts));
            item.Id = ctx.GetSequence<SchemaEntity>();
            ctx.InsertData<SchemaEntity>(item);
            */
            /*
            var data = ctx.GetAllData<SchemaEntity>();
            foreach(var item in data)
            {
                dynamic obj = SchemaConverter.JsonToExpando(item.JsonStructure);
                if (obj is ExpandoObject)
                {
                    foreach (var property in (IDictionary<String, Object>)obj)
                    {
                        if (property.Value is ExpandoObject)
                        {
                            //do nothing

                        }
                        else if (property.Value is List<dynamic>)
                        {
                            foreach (var element in (List<dynamic>)property.Value)
                            {
                                if (element is ExpandoObject)
                                {
                                    foreach (var pr in (IDictionary<String, Object>)element)
                                    {
                                        Console.WriteLine($"{pr.Key} as {pr.Value.GetType().ToString()} = {pr.Value}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{property.Key} as {property.Value.GetType().ToString()} = {property.Value}");
                        }

                    }
                }
                
            }*/
            Console.ReadLine();
        }

        async static void GetData()
        {
            ISchemaContext ctx = new SchemaDb();
            dynamic contacts2 = new ExpandoObject();
            (contacts2 as IDictionary<string, object>).Add("Usia", 39);
            contacts2.Name = "Asep XX";
            contacts2.Phone = "206-555-0155";
            contacts2.House = new List<dynamic>();
            contacts2.House.Add(new ExpandoObject());
            contacts2.House[0].Address = "Jln. Klumet";
            contacts2.House[0].POBox = "23456";
            contacts2._id = 1;
           

            IDataContext dtx = new InMemoryDb("lake",ctx);

            dtx.InsertData(contacts2, "contacts");
            //dtx.InsertData(contacts2, "contacts");
            var datas = await dtx.GetAllData("contacts");
            //var datas = await dtx.GetDataByStartId(2,1,"contacts");
            foreach (dynamic item in datas)
            {
                Console.WriteLine($"{item.House[0].Address}");
                //Console.WriteLine(SchemaConverter.AreExpandoStructureEquals(item, contacts2));
                //(item as IDictionary<string, object>)["Usia"]= 31;
               
                //dtx.UpdateData(item, "contacts");
            }
        }
    }
}
