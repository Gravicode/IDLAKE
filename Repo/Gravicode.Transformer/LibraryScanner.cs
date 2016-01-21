using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gravicode.Transformer
{
    public class LibraryScanner
    {
        private readonly Dictionary<Keterangan, Type> _assemblies = new Dictionary<Keterangan, Type>();
        public void ScanLibrary<IContract>(string DirPath)
        {
            string[] files = Directory.GetFiles(DirPath, "*.dll");
            foreach (string fileName in files)
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(fileName);
                    foreach (var type in assembly.GetTypes())
                    {
                        // Ignore Abstract
                        if (type.IsAbstract) continue;

                        // Ignore those not implementing IContract
                        if (!typeof(IContract).IsAssignableFrom(type)) continue;

                        // Ignore those without attribute [Plugin]
                        if (!type.IsDefined(typeof(Keterangan), true)) continue;

                        // Get the Plugin Key
                        var attrib = Attribute.GetCustomAttribute(type, typeof(Keterangan)) as Keterangan;
                        if (attrib == null) continue;

                        // Add it to Object Factory dictionary
                        _assemblies.Add(attrib,type);
                    }
                   
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
          
        }
        public List<string> GetLibraryList()
        {
            List<string> data = new List<string>();
            foreach (KeyValuePair<Keterangan, Type> entry in _assemblies)
            {
                data.Add(entry.Key.Nama);
            }
            return data;
        }
        public T getInstance<T>(string Nama){

            foreach (KeyValuePair<Keterangan, Type> entry in _assemblies)
            {
                if (entry.Key.Nama.Equals(Nama, StringComparison.CurrentCultureIgnoreCase))
                {
                    return (T)Activator.CreateInstance(entry.Value);
                }
            }
            Debug.WriteLine("Tidak ada implementasi {0}", Nama);
            return default(T);
        
        }
        public T getInstanceByIndex<T>(int num)
        {
            int count = 0;
            if (num > _assemblies.Count || num<0) return default(T);
            foreach (KeyValuePair<Keterangan, Type> entry in _assemblies)
            {
                if (count==num)
                {
                    return (T)Activator.CreateInstance(entry.Value);
                }
                count++;
            }
            Debug.WriteLine("Tidak ada library ke {0}", num);
            return default(T);

        }

    }
}
