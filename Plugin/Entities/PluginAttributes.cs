using System;

namespace SackranyPawn.Plugin.Entities
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class InjectBeforePluginAttribute : Attribute
    {
        public Type Key;
        
        public InjectBeforePluginAttribute(Type key)
        {
            Key = key;
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class InjectAfterPluginAttribute : Attribute
    {
        public Type Key;
        
        public InjectAfterPluginAttribute(Type key)
        {
            Key = key;
        }
    }
}