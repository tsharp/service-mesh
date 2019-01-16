namespace OrbitalForge.ServiceMesh.Core 
{
    using System;
    using System.Reflection;

    public interface ITypeResolver
    {
        Type Resolve(string name);

        bool Register(Type type);

        bool Register(Assembly assembly);

        bool Register<T>();
    }
}