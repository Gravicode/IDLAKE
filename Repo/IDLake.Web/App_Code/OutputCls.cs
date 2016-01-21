using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for OutputCls
/// </summary>
namespace IDLake.Web
{
    public class OutputCls
    {
        public bool? Result { set; get; }
        public string Comment { set; get; }
        public List<dynamic> Params { set; get; }
    }
}