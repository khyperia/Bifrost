using System;

namespace Bifrost
{
    public class GenericClock : IHardware
    {
        public int HardwareId { get { return 0x12d0b402; } }
        public short HardwareVersion { get { return 1; } }
        public int Manufacturer { get { return 0; } }

        private DateTime _lastTick;
        private uint _rate;
        private ushort _message;

        public int DoInterrupt(Dcpu cpu)
        {
            switch (cpu.A)
            {
                case 0:
                    _rate = cpu.B;
                    _lastTick = DateTime.UtcNow;
                    return 1;
                case 1:
                    cpu.C = (ushort)((DateTime.UtcNow - _lastTick).TotalSeconds * cpu.ClockCyclesPerSecond);
                    return 1;
                case 2:
                    if (cpu.B == 0)
                        _rate = 0;
                    else
                        _message = cpu.B;
                    return 1;
                default:
                    return 0;
            }
        }

        public void ExternalCallback(Dcpu cpu)
        {
            if (_rate == 0)
                return;
            var nextTickTime = _lastTick + TimeSpan.FromSeconds(_rate / 60.0);
            if (DateTime.UtcNow < nextTickTime)
                return;
            _lastTick = nextTickTime;
            cpu.SendInterrupt(new Interrupt(_message));
        }
    }
}