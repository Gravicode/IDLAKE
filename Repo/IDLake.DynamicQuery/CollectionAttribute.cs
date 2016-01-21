using System;

namespace IDLake.DynamicQuery
{
    public class CollectionAttribute : Attribute
    {
        public CollectionAttribute(string name)
        {
            CollectionName = name;
        }

        public string CollectionName { get; set; }
    }
}