using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDLake.Entities
{
    public class AuditAttribute
    {
        public string CreatedBy { set; get; }
        public string UpdatedBy { set; get; }
        public DateTime Created { set; get; }
        public DateTime Updated { set; get; }
    

    }
}
