namespace OrbitalForge.ServiceMesh.Core.Impl
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Reflection;

    internal class TypeResolver : ITypeResolver
    {
        ConcurrentDictionary<string, Type> knownTypes = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// 
        /// </summary>
        public Type Resolve(string name) 
        {
            if(knownTypes.TryGetValue(name, out Type resolved)) 
            {
                return resolved;
            }

            return knownTypes.Values.Where(t => t.FullName.Equals(name)).SingleOrDefault();
        }

        public bool Register(Type type)
        {
            throw new NotImplementedException();
        }

        public bool Register(Assembly assembly)
        {
            throw new NotImplementedException();
        }

        public bool Register<T>() 
        {
            return Register(typeof(T));
        }
    }
}