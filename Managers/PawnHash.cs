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
        
        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            _nextId = int.MinValue;
        }
    }
}