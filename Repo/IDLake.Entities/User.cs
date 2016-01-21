using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDLake.Entities
{
    public class User:AuditAttribute
    {
        public long Id { set; get; }
        public string UserName { set; get; }
        public string Password { set; get; }
        public string Email { set; get; }
        public string Question { set; get; }
        public string Answer { set; get; }
        public bool IsLocked { set; get; }
        public string FullName { set; get; }

    }
}
