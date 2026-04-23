using System;

namespace SackranyPawn.Entities.Modules
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DependencyAttribute : Attribute
    {
        public bool Optional;
        public DependencyAttribute(bool optional = false)
        {
            Optional = optional;
        }
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HashKeyAttribute : Attribute
    {
        public readonly int Precision;
        public readonly bool IgnoreDefault;

        public HashKeyAttribute(int precision = 3, bool ignoreDefault = false)
        {
            Precision = precision;
            IgnoreDefault = ignoreDefault;
        }
        public HashKeyAttribute(bool ignoreDefault = false)
        {
            Precision = 3;
            IgnoreDefault = ignoreDefault;
        }
        public HashKeyAttribute()
        {
            Precision = 3;
            IgnoreDefault = false;
        }
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UpdateOrderAttribute : Attribute
    {
        public int _order;
        public UpdateOrderAttribute(int order)
        {
            _order = order;
        }
        public UpdateOrderAttribute(Order order)
        {
            _order = (int)order;
        }
    }
    public enum Order
    {
        AfterAll = int.MaxValue,
        BeforeAll = int.MinValue,
    }
}