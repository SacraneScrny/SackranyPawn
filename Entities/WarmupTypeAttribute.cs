using System;

namespace SackranyPawn.Entities
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class WarmupTypeAttribute : Attribute
    {
        public readonly Type Type;
        public WarmupTypeAttribute(Type type) => Type = type;
    }
}