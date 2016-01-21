using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDLake.Entities
{
    public class Laporan
    {
        public string nama { set; get; }
        public string url { set; get; }
        public string keterangan { set; get; }
        public long Id { set; get; }
    }
    public enum StatusApproval { Pending=0, Approve, Reject}
    public class ApiAccess
    {
        public string Owner { set; get; }
        public string Requestor { set; get; }
        public string ApiKey { set; get; }
        public DateTime RequestDate { set; get; }
        public string SchemaName { set; get; }
        public long SchemaId { set; get; }
        public bool Create { set; get; }
        public bool Read { set; get; }
        public bool Update { set; get; }
        public bool Delete { set; get; }
        public StatusApproval Status { set; get; }
    }
    public enum AccessTypes { Publik=0, Private }
    public class SchemaEntity:AuditAttribute
    {
        public string GroupName { set; get; }
        public string FilePath { set; get; }
        public AccessTypes AccessType { set; get; }
        public string InternalName { set; get; }
        public string SchemaName { set; get; }
        public long Id { set; get; }
        public string JsonStructure { set; get; }
        public string XmlStructure { set; get; }
        public Dictionary<string,IDField> Fields { set; get; }
        public SchemaTypes SchemaType { set; get; }
        HashSet<string> SharedAccessTo { set; get; }
        public string Description { set; get; }
    }
    public enum SchemaTypes { StreamData=0, RelationalData, HistoricalData }
    public enum FieldTypes { SingleField=0, MultiField }
    //public enum IDType { Teks, Desimal, AngkaBulat, Tanggal, Karakter, Bit }
    public class IDField
    {
        public string Name { set; get; }
        public string Desc { set; get; }
        public Type NativeType { set; get; }
        public FieldTypes FieldType { set; get; }
        public bool IsMandatory { set; get; }
        public string RegexValidation { set; get; }
        public List<IDField> Children { set; get; }

    }
  
}
