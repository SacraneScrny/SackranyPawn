using SackranyPawn.Components;
using SackranyPawn.Managers;

using UnityEngine;

namespace SackranyPawn.Extensions
{
    public static class PawnPoolExtensions
    {
        public static Pawn Pop(this Pawn pawn) => PawnPool.Pop(pawn);
        public static Pawn Pop(this GameObject pawnGameObject) => PawnPool.Pop(pawnGameObject);
        
        public static void Push(this Pawn pawn) => PawnPool.Push(pawn);
        public static void Push(this GameObject pawnGameObject) => PawnPool.Push(pawnGameObject);
        
        public static void PreWarmPool(this Pawn pawn, int count) => PawnPool.PreWarm(pawn, count);

        public static void ClearPool(this Pawn pawn) => PawnPool.Clear(pawn.Archetype);
        public static int GetCountPool(this Pawn pawn) => PawnPool.GetCount(pawn.Archetype);
    }
}