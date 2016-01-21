using System;
using System.Reflection;

namespace IDLake.DynamicQuery
{
    public interface ITypeResolver
    {
        Type Resolve(string type);
        Assembly[] GetReferences();
        string[] GetNamespaces();
    }
}