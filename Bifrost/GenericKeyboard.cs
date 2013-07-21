using System;
using System.Collections.Generic;

namespace Bifrost
{
    public interface ISystemKeyboard
    {
        event Action<ushort> KeyUp;
        event Action<ushort> KeyDown;
    }

    public class GenericKeyboard : IHardware
    {
        private readonly Queue<ushort> _keyQueue = new Queue<ushort>();
        private readonly bool[] _keyState = new bool[0x92];
        private bool _doInterrupt;
        private ushort _interruptMessage;

        public int HardwareId { get { return 0x30cf7406; } }
        public short HardwareVersion { get { return 1; } }
        public int Manufacturer { get { return 0; } }

        public GenericKeyboard(ISystemKeyboard systemKeyboard)
        {
            systemKeyboard.KeyDown += SystemKeyboardOnKeyDown;
            systemKeyboard.KeyUp += SystemKeyboardOnKeyUp;
        }

        private void SystemKeyboardOnKeyDown(ushort b)
        {
            _doInterrupt = true;
            _keyQueue.Enqueue(b);
            if (_keyQueue.Count > 100)
                _keyQueue.Dequeue();
            _keyState[b] = true;
        }

        private void SystemKeyboardOnKeyUp(ushort b)
        {
            _keyState[b] = false;
        }

        public int DoInterrupt(Dcpu cpu)
        {
            switch (cpu.A)
            {
                case 0:
                    Array.Clear(_keyState, 0, _keyState.Length);
                    return 3;
                case 1:
                    cpu.C = _keyQueue.Count == 0 ? (ushort)0 : _keyQueue.Dequeue();
                    return 2;
                case 2:
                    cpu.C = cpu.B >= _keyState.Length ? (ushort)0 : _keyState[cpu.B] ? (ushort)1 : (ushort)0;
                    return 2;
                case 3:
                    _interruptMessage = cpu.B;
                    return 1;
                default:
                    return 0;
            }
        }

        public void ExternalCallback(Dcpu cpu)
        {
            if (_interruptMessage == 0 || _doInterrupt == false)
                return;
            _doInterrupt = false;
            cpu.SendInterrupt(new Interrupt(_interruptMessage));
        }
    }
}