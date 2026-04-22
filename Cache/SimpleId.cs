using System;

namespace SackranyPawn.Cache
{
    public static class SimpleId
    {
        private static uint lastTimestamp;
        private static uint sequence;

        private const int SequenceBits = 12;                  // 4096 per second
        private const uint MaxSequence = (1u << SequenceBits) - 1;

        // Epoch: 2024-01-01 (seconds)
        private const uint Epoch = 1704067200u;

        public static uint Next()
        {
            uint ts = Timestamp();

            if (ts < lastTimestamp)
            {
                // Часы поехали — Unity moment
                ts = lastTimestamp;
            }

            if (ts == lastTimestamp)
            {
                sequence = (sequence + 1) & MaxSequence;
                // НИКАКИХ ожиданий. Переполнился — поехали дальше.
            }
            else
            {
                sequence = 0;
                lastTimestamp = ts;
            }

            return ((ts - Epoch) << SequenceBits) | sequence;
        }

        private static uint Timestamp()
            => (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}