using UnityEngine;

namespace SackranyPawn.Managers
{
    public static class PawnHash
    {
        static int _nextId;
        public static int GetId()
        {
            _nextId++;
            return _nextId;
        }
        
        internal static void Init()
        {
            _nextId = int.MinValue;
        }
    }
}