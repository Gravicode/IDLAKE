using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace IDLake.DynamicQuery
{
    public static class GeneralHelpers
    {
        public static NameValueCollection ToNameValueCollection<TValue>(this IDictionary<string, TValue> dictionary)
        {
            var collection = new NameValueCollection();
            foreach (var pair in dictionary)
                collection.Add(pair.Key, pair.Value.ToString());
            return collection;
        }

        public static string FirstCharToLower(this string input)
        {
            if (String.IsNullOrEmpty(input))
                return string.Empty;
            return input.First().ToString(CultureInfo.InvariantCulture).ToLower() + String.Join("", input.Skip(1));
        }


        public static IEnumerable<Type> GetTypesWithAttribute<T>
            (this IEnumerable<Assembly> assemblies) where T : class
        {
            return from assembly in assemblies
                   from type in assembly.GetTypes()
                   let attrib = type.GetCustomAttributes
                       (typeof(T), true).FirstOrDefault()
                   let collectionAttribute = attrib as T
                   where collectionAttribute != null && (attrib != null)
                   select type;
        }

        public static IEnumerable<Type> GetTypesWithAttribute<T>
            (this Assembly assembly) where T : class
        {
            return new List<Assembly> { assembly }.GetTypesWithAttribute<T>();
        }


    }
   
}
