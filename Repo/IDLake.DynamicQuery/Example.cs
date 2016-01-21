using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
namespace IDLake.DynamicQuery
{
    public class EntityContainer<T> : List<T>
    {
        public List<T> GetAllData
        {
            get
            {
                return this;
            }
        }
    }
    [Collection("Manusia")]
    public class Manusia
    {
        public int Id { set; get; }
        public string Nama { set; get; }
        public int Usia { set; get; }
        public enum Kelamin { Cowok, Cewek }
        public Kelamin JenisKelamin { set; get; }

    }
    public class Example
    {
        public static EntityContainer<Manusia> DaftarManusia { set; get; }
        public AssemblyTypeResolver DaftarEntity { set; get; }
        public Example()
        {
            DaftarEntity = new AssemblyTypeResolver();
            //register your entity
            if (typeof(Manusia).Assembly != null)
                DaftarEntity.AddReference(typeof(Manusia).Assembly);
            if (typeof(Manusia).Namespace != null)
                DaftarEntity.AddNamespace(typeof(Manusia).Namespace);
            //init list
            if (DaftarManusia == null)
            {
                DaftarManusia = new EntityContainer<Manusia>();
                DaftarManusia.AddRange(GetSampleData());
            }
        }
        private object ToPocoType(JObject jobj, Type objtype)
        {
            var serializer = new JsonSerializer();
            return serializer.Deserialize(new JTokenReader(jobj), objtype);
        }

        public object ContohFilter(string collection, IDictionary<string, string> query)
        {
           
            try
            {
                DynamicFilter myfilter = new DynamicFilter();
                var q = query ?? new Dictionary<string, string>();
                var param = q.ToNameValueCollection();
                Type type = DaftarEntity.Resolve(collection);
                return myfilter.Filter(DaftarManusia, type, param);
      
            }
            catch (Exception ex)
            {

                return null;

            }


        }
        public object Query(string query)
        {
           
            try
            {

                var host = new ScriptingHost(DaftarManusia) { };
                var Refs = DaftarEntity.GetReferences();
                var Nms = DaftarEntity.GetNamespaces();
                Refs.ToList().ForEach(x => host.AddReference(x));
                Nms.ToList().ForEach(x => host.ImportNamespace(x));
                return host.Execute(query);
               
            }
            catch (Exception ex)
            {
                return null;

            }
        }
        private List<Manusia> GetSampleData()
        {
            return new List<Manusia>(new Manusia[] {
                new Manusia() { Id = 1, Nama = "sotong", Usia = 12, JenisKelamin = Manusia.Kelamin.Cewek },
                new Manusia() { Id = 2, Nama = "tumbra", Usia = 22, JenisKelamin = Manusia.Kelamin.Cowok } });
        }
    }
}
