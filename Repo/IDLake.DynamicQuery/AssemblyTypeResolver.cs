using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IDLake.DynamicQuery
{
    public class AssemblyTypeResolver : ITypeResolver
    {
        private readonly List<Assembly> _asm = new List<Assembly>();
        private readonly List<string> _ns = new List<string>();

        #region ITypeResolver Members

        public Type Resolve(string type)
        {
            Type t = _asm.GetTypesWithAttribute<CollectionAttribute>().FirstOrDefault
                (item => item.GetCustomAttribute<CollectionAttribute>().CollectionName == type);
            return t;
        }

        public Assembly[] GetReferences()
        {
            return _asm.ToArray();
        }

        public string[] GetNamespaces()
        {
            return _ns.ToArray();
        }

        public string[] GetAssemblyNames()
        {
            var list = new List<string>();
            _asm.ForEach(x => list.Add(x.GetName().Name));
            return list.ToArray();
        }

        #endregion

        public AssemblyTypeResolver AddReference(Assembly a)
        {
            _asm.Add(a);
            return this;
        }

        public AssemblyTypeResolver AddNamespace(string s)
        {
            _ns.Add(s);
            return this;
        }
    }
}