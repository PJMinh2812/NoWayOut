using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Runtime telemetry cho AI Director trong 1 session chơi.
    /// </summary>
    public static class RunAIDirectorTelemetry
    {
        private static int _totalDamageTaken;
        private static int _totalDeaths;
        private static int _totalTrapTriggers;

        public static int TotalDamageTaken => _totalDamageTaken;
        public static int TotalDeaths => _totalDeaths;
        public static int TotalTrapTriggers => _totalTrapTriggers;

        public static void RecordPlayerDamageTaken(int amount)
        {
            if (amount <= 0)
                return;

            _totalDamageTaken += amount;
        }

        public static void RecordPlayerDeath()
        {
            _totalDeaths++;
        }

        public static void RecordTrapTriggered(Object source)
        {
            _totalTrapTriggers++;
        }

        public static void ResetAll()
        {
            _totalDamageTaken = 0;
            _totalDeaths = 0;
            _totalTrapTriggers = 0;
        }
    }
}
