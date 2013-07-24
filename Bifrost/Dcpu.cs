using System;
using System.Collections.Generic;

namespace Bifrost
{
    public interface IHardware
    {
        int HardwareId { get; }
        short HardwareVersion { get; }
        int Manufacturer { get; }
        int DoInterrupt(Dcpu cpu);
        void ExternalCallback(Dcpu cpu);
    }

    public struct Interrupt
    {
        private readonly ushort _message;

        public Interrupt(ushort message)
        {
            _message = message;
        }

        public ushort Message
        {
            get { return _message; }
        }
    }

    public class Dcpu
    {
        private readonly List<IHardware> _hardware = new List<IHardware>();
        private readonly Queue<Interrupt> _interrupts = new Queue<Interrupt>();
        private readonly ushort[] _memory = new ushort[0x10000];
        private const uint TicksPerSecond = 100000;
        private ushort _a, _b, _c, _x, _y, _z, _i, _j, _pc, _sp, _ex, _ia;
        private long _cycle;
        private bool _interruptQueueEnabled;
        private DateTime _lastUpdateCallTime = DateTime.UtcNow;

        public ushort A { get { return _a; } set { _a = value; } }
        public ushort B { get { return _b; } set { _b = value; } }
        public ushort C { get { return _c; } set { _c = value; } }
        public ushort X { get { return _x; } set { _x = value; } }
        public ushort Y { get { return _y; } set { _y = value; } }
        public ushort Z { get { return _z; } set { _z = value; } }
        public ushort I { get { return _i; } set { _i = value; } }
        public ushort J { get { return _j; } set { _j = value; } }
        public ushort Pc { get { return _pc; } set { _pc = value; } }
        public ushort Sp { get { return _sp; } set { _sp = value; } }
        public ushort Ex { get { return _ex; } set { _ex = value; } }
        public ushort Ia { get { return _ia; } set { _ia = value; } }
        public ushort[] Memory { get { return _memory; } }
        public uint ClockCyclesPerSecond { get { return TicksPerSecond; } }
        public bool InterruptQueueEnabled { get { return _interruptQueueEnabled; } set { _interruptQueueEnabled = value; } }
        public Queue<Interrupt> Interrupts { get { return _interrupts; } }

        public void Reset()
        {
            _hardware.Clear();
            _interrupts.Clear();
            Array.Clear(_memory, 0, _memory.Length);
            _a = _b = _c = _x = _y = _z = _i = _j = _pc = _sp = _ex = _ia = 0;
            _cycle = 0;
            _interruptQueueEnabled = false;
            _lastUpdateCallTime = DateTime.UtcNow;
        }

        public void SendInterrupt(Interrupt interrupt)
        {
            _interrupts.Enqueue(interrupt);
        }

        public void ClearHardware()
        {
            _hardware.Clear();
        }

        public void AddHardware(IHardware hardware)
        {
            _hardware.Add(hardware);
        }

        public void LoadProgram(ushort[] program)
        {
            Array.Clear(_memory, 0, _memory.Length);
            Array.Copy(program, _memory, program.Length);
        }

        private ushort GetValueForArg(byte arg, bool isInA)
        {
            switch (arg)
            {
                case 0x00: return _a;
                case 0x01: return _b;
                case 0x02: return _c;
                case 0x03: return _x;
                case 0x04: return _y;
                case 0x05: return _z;
                case 0x06: return _i;
                case 0x07: return _j;
                case 0x08: return _memory[_a];
                case 0x09: return _memory[_b];
                case 0x0a: return _memory[_c];
                case 0x0b: return _memory[_x];
                case 0x0c: return _memory[_y];
                case 0x0d: return _memory[_z];
                case 0x0e: return _memory[_i];
                case 0x0f: return _memory[_j];
                case 0x10: _cycle++; return _memory[(ushort)(_a + _memory[_pc++])];
                case 0x11: _cycle++; return _memory[(ushort)(_b + _memory[_pc++])];
                case 0x12: _cycle++; return _memory[(ushort)(_c + _memory[_pc++])];
                case 0x13: _cycle++; return _memory[(ushort)(_x + _memory[_pc++])];
                case 0x14: _cycle++; return _memory[(ushort)(_y + _memory[_pc++])];
                case 0x15: _cycle++; return _memory[(ushort)(_z + _memory[_pc++])];
                case 0x16: _cycle++; return _memory[(ushort)(_i + _memory[_pc++])];
                case 0x17: _cycle++; return _memory[(ushort)(_j + _memory[_pc++])];
                case 0x18: return isInA ? _memory[_sp++] : _memory[--_sp];
                case 0x19: return _memory[_sp];
                case 0x1a: _cycle++; return _memory[_sp + _memory[_pc++]];
                case 0x1b: return _sp;
                case 0x1c: return _pc;
                case 0x1d: return _ex;
                case 0x1e: _cycle++; return _memory[_memory[_pc++]];
                case 0x1f: _cycle++; return _memory[_pc++];
                default:
                    if (isInA && arg <= 0x3f)
                        return (ushort)(arg - 0x21);
                    throw new ArgumentOutOfRangeException("arg", string.Format("Opcode value {0} was out of range", arg));
            }
        }

        private void SetValueForArg(byte arg, bool isInA, ushort value)
        {
            switch (arg)
            {
                case 0x00: _a = value; break;
                case 0x01: _b = value; break;
                case 0x02: _c = value; break;
                case 0x03: _x = value; break;
                case 0x04: _y = value; break;
                case 0x05: _z = value; break;
                case 0x06: _i = value; break;
                case 0x07: _j = value; break;
                case 0x08: _memory[_a] = value; break;
                case 0x09: _memory[_b] = value; break;
                case 0x0a: _memory[_c] = value; break;
                case 0x0b: _memory[_x] = value; break;
                case 0x0c: _memory[_y] = value; break;
                case 0x0d: _memory[_z] = value; break;
                case 0x0e: _memory[_i] = value; break;
                case 0x0f: _memory[_j] = value; break;
                case 0x10: _cycle++; _memory[(ushort)(_a + _memory[_pc++])] = value; break;
                case 0x11: _cycle++; _memory[(ushort)(_b + _memory[_pc++])] = value; break;
                case 0x12: _cycle++; _memory[(ushort)(_c + _memory[_pc++])] = value; break;
                case 0x13: _cycle++; _memory[(ushort)(_x + _memory[_pc++])] = value; break;
                case 0x14: _cycle++; _memory[(ushort)(_y + _memory[_pc++])] = value; break;
                case 0x15: _cycle++; _memory[(ushort)(_z + _memory[_pc++])] = value; break;
                case 0x16: _cycle++; _memory[(ushort)(_i + _memory[_pc++])] = value; break;
                case 0x17: _cycle++; _memory[(ushort)(_j + _memory[_pc++])] = value; break;
                case 0x18: if (isInA) _memory[_sp++] = value; else _memory[--_sp] = value; break;
                case 0x19: _memory[_sp] = value; break;
                case 0x1a: _cycle++; _memory[_sp + _memory[_pc++]] = value; break;
                case 0x1b: _sp = value; break;
                case 0x1c: _pc = value; break;
                case 0x1d: _ex = value; break;
                case 0x1e: _cycle++; _memory[_memory[_pc++]] = value; break;
                case 0x1f: _cycle++; _memory[_pc++] = value; break;
                default:
                    if (isInA && arg <= 0x3f)
                        return;
                    throw new ArgumentOutOfRangeException("arg", string.Format("Opcode value {0} was out of range", arg));
            }
        }

        private void SkipIfChain()
        {
            byte opcode;
            do
            {
                _cycle++;
                var instruction = Memory[_pc++];
                opcode = (byte)(instruction & 0x1F);
                var a = (byte)(instruction >> 10 & 0x3f);
                var b = (byte)(instruction >> 5 & 0x1f);
                var spOld = _sp;
                GetValueForArg(a, true);
                GetValueForArg(b, false);
                _sp = spOld;
            } while (opcode >= 0x10 && opcode <= 0x17);
        }

        private void RunOpcode(bool debug)
        {
            var instruction = _memory[_pc++];
            if (debug)
                Console.Write("{0} 0x{1:x4} ", _cycle, _pc);
            var opcode = (byte)(instruction & 0x1f);
            var a = (byte)(instruction >> 10 & 0x3f);
            var b = (byte)(instruction >> 5 & 0x1f);
            var va = opcode == 0x00 || opcode == 0x1e || opcode == 0x1f ? (ushort)0 : GetValueForArg(a, true);
            var vb = opcode == 0x00 || opcode == 0x01 ? (ushort)0 : GetValueForArg(b, false);

            switch (opcode)
            {
                case 0x00:
                    switch (b)
                    {
                        case 0x01: // JSR
                            _cycle += 3;
                            if (debug) Console.WriteLine("JSR");
                            var newPc = GetValueForArg(a, true);
                            _memory[--_sp] = _pc;
                            _pc = newPc;
                            break;
                        case 0x08: // INT
                            if (debug) Console.WriteLine("INT");
                            SendInterrupt(new Interrupt(GetValueForArg(a, true)));
                            _cycle += 4;
                            break;
                        case 0x09: // IAG
                            if (debug) Console.WriteLine("IAG");
                            _cycle += 1;
                            SetValueForArg(a, true, _ia);
                            break;
                        case 0x0a: // IAS
                            if (debug) Console.WriteLine("IAS");
                            _cycle += 1;
                            _ia = GetValueForArg(a, true);
                            break;
                        case 0x0b: // RFI
                            if (debug) Console.WriteLine("RFI");
                            _cycle += 3;
                            _a = _memory[_sp++];
                            _pc = _memory[_sp++];
                            _interruptQueueEnabled = false;
                            break;
                        case 0x0c: // IAQ
                            if (debug) Console.WriteLine("IAQ");
                            _cycle += 2;
                            _interruptQueueEnabled = GetValueForArg(a, true) != 0;
                            break;
                        case 0x10: // HWN
                            if (debug) Console.WriteLine("HWN");
                            _cycle += 2;
                            SetValueForArg(a, true, (ushort)_hardware.Count);
                            break;
                        case 0x11: // HWQ
                            if (debug) Console.WriteLine("HWQ");
                            _cycle += 4;
                            va = GetValueForArg(a, true);
                            if (va < _hardware.Count)
                            {
                                var hardware = _hardware[va];
                                _a = (ushort)hardware.HardwareId;
                                _b = (ushort)(hardware.HardwareId >> 16);
                                _c = (ushort)hardware.HardwareVersion;
                                _x = (ushort)hardware.Manufacturer;
                                _y = (ushort)(hardware.Manufacturer >> 16);
                            }
                            else
                                _a = _b = _c = _x = _y = 0;
                            break;
                        case 0x12: // HWI
                            if (debug) Console.WriteLine("HWI");
                            va = GetValueForArg(a, true);
                            if (va < _hardware.Count)
                                _cycle += _hardware[va].DoInterrupt(this);
                            _cycle += 4;
                            break;
                        default:
                            throw new Exception(string.Format("Invalid special opcode {0:x}", b));
                    }
                    break;
                case 0x01: // SET
                    if (debug) Console.WriteLine("SET");
                    _cycle += 1;
                    SetValueForArg(b, false, va);
                    break;
                case 0x02: // ADD
                    if (debug) Console.WriteLine("ADD");
                    _cycle += 2;
                    try
                    {
                        SetValueForArg(b, false, (ushort)checked(va + vb));
                        _ex = 0x0000;
                    }
                    catch (OverflowException)
                    {
                        SetValueForArg(b, false, (ushort)(va + vb));
                        _ex = 0x0001;
                    }
                    break;
                case 0x03: // SUB
                    if (debug) Console.WriteLine("SUB");
                    _cycle += 2;
                    try
                    {
                        SetValueForArg(b, false, (ushort)checked(vb - va));
                        _ex = 0x0000;
                    }
                    catch (OverflowException)
                    {
                        SetValueForArg(b, false, (ushort)(vb - va));
                        _ex = 0xffff;
                    }
                    break;
                case 0x04: // MUL
                    if (debug) Console.WriteLine("MUL");
                    _cycle += 2;
                    SetValueForArg(b, false, (ushort)(vb * va));
                    _ex = (ushort)(((vb * va) >> 16) & 0xffff);
                    break;
                case 0x05: // MLI
                    if (debug) Console.WriteLine("MLI");
                    _cycle += 2;
                    SetValueForArg(b, false, (ushort)((short)vb * (short)va));
                    _ex = (ushort)(((ushort)((short)vb * (short)va) >> 16) & 0xffff);
                    break;
                case 0x06: // DIV
                    if (debug) Console.WriteLine("DIV");
                    _cycle += 3;
                    if (va == 0)
                    {
                        SetValueForArg(b, false, 0);
                        _ex = 0;
                    }
                    else
                    {
                        SetValueForArg(b, false, (ushort)(vb / va));
                        _ex = (ushort)(((vb << 16) / va) & 0xffff);
                    }
                    break;
                case 0x07: // DVI
                    if (debug) Console.WriteLine("DVI");
                    _cycle += 3;
                    if (va == 0)
                    {
                        SetValueForArg(b, false, 0);
                        _ex = 0;
                    }
                    else
                    {
                        SetValueForArg(b, false, (ushort)((short)vb / (short)va));
                        _ex = (ushort)((((short)vb << 16) / (short)va) & 0xffff);
                    }
                    break;
                case 0x08: // MOD
                    if (debug) Console.WriteLine("MOD");
                    _cycle += 3;
                    SetValueForArg(b, false, va == 0 ? (ushort)0 : (ushort)(vb % va));
                    break;
                case 0x09: // MDI
                    if (debug) Console.WriteLine("MDI");
                    _cycle += 3;
                    SetValueForArg(b, false, va == 0 ? (ushort)0 : (ushort)((short)vb % (short)va));
                    break;
                case 0x0a: // AND
                    if (debug) Console.WriteLine("AND");
                    _cycle += 1;
                    SetValueForArg(b, false, (ushort)(va & vb));
                    break;
                case 0x0b: // BOR
                    if (debug) Console.WriteLine("BOR");
                    _cycle += 1;
                    SetValueForArg(b, false, (ushort)(va | vb));
                    break;
                case 0x0c: // XOR
                    if (debug) Console.WriteLine("XOR");
                    _cycle += 1;
                    SetValueForArg(b, false, (ushort)(va ^ vb));
                    break;
                case 0x0d: // SHR
                    if (debug) Console.WriteLine("SHR");
                    _cycle += 1;
                    SetValueForArg(b, false, (ushort)(vb >> va));
                    _ex = (ushort)(((vb << 16) >> va) & 0xffff);
                    break;
                case 0x0e: // ASR
                    if (debug) Console.WriteLine("ASR");
                    _cycle += 1;
                    SetValueForArg(b, false, (ushort)((short)vb >> va));
                    _ex = (ushort)((((short)vb << 16) >> va) & 0xffff);
                    break;
                case 0x0f: // SHL
                    if (debug) Console.WriteLine("SHL");
                    _cycle += 1;
                    SetValueForArg(b, false, (ushort)(vb << va));
                    _ex = (ushort)(((vb << va) >> 16) & 0xffff);
                    break;
                case 0x10: // IFB
                    if (debug) Console.WriteLine("IFB");
                    _cycle += 2;
                    if ((va & vb) == 0)
                        SkipIfChain();
                    break;
                case 0x11: // IFC
                    if (debug) Console.WriteLine("IFC");
                    _cycle += 2;
                    if ((va & vb) != 0)
                        SkipIfChain();
                    break;
                case 0x12: // IFE
                    if (debug) Console.WriteLine("IFE");
                    _cycle += 2;
                    if (va != vb)
                        SkipIfChain();
                    break;
                case 0x13: // IFN
                    if (debug) Console.WriteLine("IFN");
                    _cycle += 2;
                    if (va == vb)
                        SkipIfChain();
                    break;
                case 0x14: // IFG
                    if (debug) Console.WriteLine("IFG");
                    _cycle += 2;
                    if (!(va > vb))
                        SkipIfChain();
                    break;
                case 0x15: // IFA
                    if (debug) Console.WriteLine("IFA");
                    _cycle += 2;
                    if (!((short)va > (short)vb))
                        SkipIfChain();
                    break;
                case 0x16: // IFL
                    if (debug) Console.WriteLine("IFL");
                    _cycle += 2;
                    if (!(va < vb))
                        SkipIfChain();
                    break;
                case 0x17: // IFU
                    if (debug) Console.WriteLine("IFU");
                    _cycle += 2;
                    if (!((short)va < (short)vb))
                        SkipIfChain();
                    break;
                case 0x1a: // ADX
                    if (debug) Console.WriteLine("ADX");
                    _cycle += 3;
                    var vaAdx = va;
                    var vbAdx = vb;
                    try
                    {
                        SetValueForArg(b, false, (ushort)checked(vaAdx + vbAdx + _ex));
                        _ex = 0x0000;
                    }
                    catch (OverflowException)
                    {
                        SetValueForArg(b, false, (ushort)(vaAdx + vbAdx + _ex));
                        _ex = 0x0001;
                    }
                    break;
                case 0x1b: // SBX
                    if (debug) Console.WriteLine("SBX");
                    _cycle += 3;
                    var vaSbx = va;
                    var vbSbx = vb;
                    try
                    {
                        SetValueForArg(b, false, (ushort)checked(vbSbx - vaSbx + _ex));
                        _ex = 0x0000;
                    }
                    catch (OverflowException)
                    {
                        SetValueForArg(b, false, (ushort)(vbSbx - vaSbx + _ex));
                        _ex = 0x0001;
                    }
                    break;
                case 0x1e: // STI
                    if (debug) Console.WriteLine("STI");
                    _cycle += 2;
                    SetValueForArg(a, true, vb);
                    _i++;
                    _j++;
                    break;
                case 0x1f: // STD
                    if (debug) Console.WriteLine("STD");
                    _cycle += 2;
                    SetValueForArg(a, true, vb);
                    _i--;
                    _j--;
                    break;
                default:
                    throw new Exception(string.Format("Invalid opcode {0:x}", opcode));
            }
        }

        private void RunInterrupt()
        {
            if (_interruptQueueEnabled || _interrupts.Count == 0)
                return;
            var interrupt = _interrupts.Dequeue();
            _memory[--_sp] = _pc;
            _memory[--_sp] = _a;
            _pc = _ia;
            _a = interrupt.Message;
            _interruptQueueEnabled = true;
        }

        public void Run()
        {
            var newTime = DateTime.UtcNow;
            var timeSinceLastUpdate = newTime - _lastUpdateCallTime;
            _lastUpdateCallTime = newTime;
            var numCycles = (uint)Math.Ceiling(TicksPerSecond * timeSinceLastUpdate.TotalSeconds);
            if (numCycles == 0)
                return;

            foreach (var hardware in _hardware)
                hardware.ExternalCallback(this);
            if (numCycles > TicksPerSecond)
                numCycles = TicksPerSecond;
            while (_cycle < numCycles)
            {
                RunOpcode(false);
                RunInterrupt();
            }
            _cycle -= numCycles;
        }
    }
}
