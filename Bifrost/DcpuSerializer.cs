using System;
using System.IO;

namespace Bifrost
{
    class DcpuSerializer
    {
        // make sure to change MagicNumber if format changes after release
        private const int MagicNumber = 0xdabb1ed;

        public static void Serialize(Dcpu dcpu, BinaryWriter writer)
        {
            writer.Write(MagicNumber);
            writer.Write(dcpu.A);
            writer.Write(dcpu.B);
            writer.Write(dcpu.C);
            writer.Write(dcpu.X);
            writer.Write(dcpu.Y);
            writer.Write(dcpu.Z);
            writer.Write(dcpu.I);
            writer.Write(dcpu.J);
            writer.Write(dcpu.Pc);
            writer.Write(dcpu.Sp);
            writer.Write(dcpu.Ex);
            writer.Write(dcpu.Ia);
            writer.Write(dcpu.InterruptQueueEnabled);
            writer.Write(dcpu.Interrupts.Count);
            foreach (var interrupt in dcpu.Interrupts)
                writer.Write(interrupt.Message);
            var mem = dcpu.Memory;
            var len = mem.Length;
            writer.Write(len);
            for (var i = 0; i < len; i++)
                writer.Write(mem[i]);
        }

        public static void Deserialize(Dcpu dcpu, BinaryReader reader)
        {
            if (reader.ReadInt32() != MagicNumber)
                throw new Exception("Unable to read DCPU file: invalid magic number");
            dcpu.Reset(); // Cilph: Remove if you already do this elsewhere
            dcpu.A = reader.ReadUInt16();
            dcpu.B = reader.ReadUInt16();
            dcpu.C = reader.ReadUInt16();
            dcpu.X = reader.ReadUInt16();
            dcpu.Y = reader.ReadUInt16();
            dcpu.Z = reader.ReadUInt16();
            dcpu.I = reader.ReadUInt16();
            dcpu.J = reader.ReadUInt16();
            dcpu.Pc = reader.ReadUInt16();
            dcpu.Sp = reader.ReadUInt16();
            dcpu.Ex = reader.ReadUInt16();
            dcpu.Ia = reader.ReadUInt16();
            dcpu.InterruptQueueEnabled = reader.ReadBoolean();
            var interruptCount = reader.ReadInt32();
            for (var i = 0; i < interruptCount; i++)
                dcpu.Interrupts.Enqueue(new Interrupt(reader.ReadUInt16()));
            var len = reader.ReadInt32();
            var mem = new ushort[len];
            for (var i = 0; i < len; i++)
                mem[i] = reader.ReadUInt16();
        }
    }
}
