using System;
using UnityEngine;

namespace Game.Services
{
    [Serializable]
    public class EnergyState
    {
        public int CurrentEnergy = 20;
        public int MaxEnergy = 20;
        public long LastRegenUtcTicks;

        public EnergyState() { }

        public EnergyState(int maxEnergy, long initialUtcTicks)
        {
            CurrentEnergy = maxEnergy;
            MaxEnergy = maxEnergy;
            LastRegenUtcTicks = initialUtcTicks;
        }
    }

    public class EnergyManager
    {
        private readonly EnergyState _state;
        private readonly IGameTimeService _timeService;
        private readonly float _secondsPerEnergyUnit;

        public int CurrentEnergy => _state.CurrentEnergy;
        public int MaxEnergy => _state.MaxEnergy;
        public long LastRegenUtcTicks => _state.LastRegenUtcTicks;

        public EnergyManager(EnergyState state, IGameTimeService timeService, float secondsPerEnergyUnit = 300f)
        {
            _state = state ?? new EnergyState(20, timeService.CurrentUtcTicks);
            _timeService = timeService;
            _secondsPerEnergyUnit = Mathf.Max(10f, secondsPerEnergyUnit); // Default 5 mins (300s)

            RecalculateEnergy();
        }

        public void RecalculateEnergy()
        {
            if (_state.CurrentEnergy >= _state.MaxEnergy)
            {
                _state.LastRegenUtcTicks = _timeService.CurrentUtcTicks;
                return;
            }

            long currentTicks = _timeService.CurrentUtcTicks;
            long elapsedTicks = currentTicks - _state.LastRegenUtcTicks;

            if (elapsedTicks <= 0) return;

            double elapsedSeconds = TimeSpan.FromTicks(elapsedTicks).TotalSeconds;
            int energyToAdd = (int)(elapsedSeconds / _secondsPerEnergyUnit);

            if (energyToAdd > 0)
            {
                _state.CurrentEnergy = Mathf.Min(_state.MaxEnergy, _state.CurrentEnergy + energyToAdd);
                long ticksConsumed = TimeSpan.FromSeconds(energyToAdd * _secondsPerEnergyUnit).Ticks;
                _state.LastRegenUtcTicks += ticksConsumed;
            }
        }

        public bool CanConsumeEnergy(int amount)
        {
            RecalculateEnergy();
            return amount > 0 && _state.CurrentEnergy >= amount;
        }

        public bool ConsumeEnergy(int amount)
        {
            if (!CanConsumeEnergy(amount)) return false;

            if (_state.CurrentEnergy == _state.MaxEnergy)
            {
                _state.LastRegenUtcTicks = _timeService.CurrentUtcTicks;
            }

            _state.CurrentEnergy -= amount;
            return true;
        }

        public void RefillEnergy()
        {
            _state.CurrentEnergy = _state.MaxEnergy;
            _state.LastRegenUtcTicks = _timeService.CurrentUtcTicks;
        }

        public float GetSecondsToNextEnergy()
        {
            RecalculateEnergy();
            if (_state.CurrentEnergy >= _state.MaxEnergy) return 0f;

            long elapsedTicks = _timeService.CurrentUtcTicks - _state.LastRegenUtcTicks;
            double elapsedSeconds = TimeSpan.FromTicks(elapsedTicks).TotalSeconds;
            double remainingSeconds = _secondsPerEnergyUnit - (elapsedSeconds % _secondsPerEnergyUnit);
            return Mathf.Max(0f, (float)remainingSeconds);
        }

        public EnergyState ExportState() => _state;
    }
}
