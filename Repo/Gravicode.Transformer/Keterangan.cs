using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravicode.Transformer
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class Keterangan : Attribute
    {
        public Keterangan(string Nama)
            : this(Nama, string.Empty)
        {
        }

        public Keterangan(string Nama, string Penjelasan)
        {
            this.Nama = Nama;
            this.Penjelasan = Penjelasan;
        }

        public string Nama { get; private set; }
        public string Penjelasan { get; private set; }
    }
}
