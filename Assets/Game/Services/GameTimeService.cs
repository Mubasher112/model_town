using System;

namespace Game.Services
{
    public interface IGameTimeService
    {
        long CurrentUtcTicks { get; }
        DateTime CurrentUtcDateTime { get; }
        double GetElapsedSeconds(long startTicks, long currentTicks);
    }

    public class StandardGameTimeService : IGameTimeService
    {
        private long _lastRecordedTicks;

        public StandardGameTimeService()
        {
            _lastRecordedTicks = DateTime.UtcNow.Ticks;
        }

        public long CurrentUtcTicks
        {
            get;
            private set;
        }

        public DateTime CurrentUtcDateTime => new DateTime(CurrentUtcTicks, DateTimeKind.Utc);

        public void UpdateCurrentTime()
        {
            long nowTicks = DateTime.UtcNow.Ticks;

            // Anti-exploit check: Detect backward clock changes
            if (nowTicks < _lastRecordedTicks)
            {
                // Clock moved backwards; retain last recorded ticks
                CurrentUtcTicks = _lastRecordedTicks;
            }
            else
            {
                CurrentUtcTicks = nowTicks;
                _lastRecordedTicks = nowTicks;
            }
        }

        public double GetElapsedSeconds(long startTicks, long endTicks)
        {
            if (endTicks < startTicks) return 0.0;
            long diffTicks = endTicks - startTicks;
            return TimeSpan.FromTicks(diffTicks).TotalSeconds;
        }
    }
}
