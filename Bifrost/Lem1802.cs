using System;

namespace Bifrost
{
    public interface ISystemMonitor
    {
        void SetScreen(uint[] rgb, int width);
    }

    public class Lem1802 : IHardware
    {
        public int HardwareId { get { return 0x7349f615; } }
        public short HardwareVersion { get { return 0x1802; } }
        public int Manufacturer { get { return 0x1c6c8b36; } }

        private readonly ISystemMonitor _systemMonitor;
        private const int Width = 128;
        private const int Height = 96;
        private readonly uint[] _rgb = new uint[Width * Height];
        private ushort _vram;
        private ushort _font;
        private ushort _palette;
        //private byte _bordercolor;

        private static readonly ushort[] DefaultFont = new ushort[]
                                                           {
                                                               0xb79e, 0x388e, 0x722c, 0x75f4, 0x19bb, 0x7f8f, 0x85f9, 0xb158, 0x242e, 0x2400, 0x082a, 0x0800, 0x0008, 0x0000, 0x0808, 0x0808, 0x00ff,
                                                               0x0000, 0x00f8, 0x0808, 0x08f8, 0x0000, 0x080f, 0x0000, 0x000f, 0x0808, 0x00ff, 0x0808, 0x08f8, 0x0808, 0x08ff, 0x0000, 0x080f, 0x0808,
                                                               0x08ff, 0x0808, 0x6633, 0x99cc, 0x9933, 0x66cc, 0xfef8, 0xe080, 0x7f1f, 0x0701, 0x0107, 0x1f7f, 0x80e0, 0xf8fe, 0x5500, 0xaa00, 0x55aa,
                                                               0x55aa, 0xffaa, 0xff55, 0x0f0f, 0x0f0f, 0xf0f0, 0xf0f0, 0x0000, 0xffff, 0xffff, 0x0000, 0xffff, 0xffff, 0x0000, 0x0000, 0x005f, 0x0000,
                                                               0x0300, 0x0300, 0x3e14, 0x3e00, 0x266b, 0x3200, 0x611c, 0x4300, 0x3629, 0x7650, 0x0002, 0x0100, 0x1c22, 0x4100, 0x4122, 0x1c00, 0x1408,
                                                               0x1400, 0x081c, 0x0800, 0x4020, 0x0000, 0x0808, 0x0800, 0x0040, 0x0000, 0x601c, 0x0300, 0x3e49, 0x3e00, 0x427f, 0x4000, 0x6259, 0x4600,
                                                               0x2249, 0x3600, 0x0f08, 0x7f00, 0x2745, 0x3900, 0x3e49, 0x3200, 0x6119, 0x0700, 0x3649, 0x3600, 0x2649, 0x3e00, 0x0024, 0x0000, 0x4024,
                                                               0x0000, 0x0814, 0x2241, 0x1414, 0x1400, 0x4122, 0x1408, 0x0259, 0x0600, 0x3e59, 0x5e00, 0x7e09, 0x7e00, 0x7f49, 0x3600, 0x3e41, 0x2200,
                                                               0x7f41, 0x3e00, 0x7f49, 0x4100, 0x7f09, 0x0100, 0x3e41, 0x7a00, 0x7f08, 0x7f00, 0x417f, 0x4100, 0x2040, 0x3f00, 0x7f08, 0x7700, 0x7f40,
                                                               0x4000, 0x7f06, 0x7f00, 0x7f01, 0x7e00, 0x3e41, 0x3e00, 0x7f09, 0x0600, 0x3e41, 0xbe00, 0x7f09, 0x7600, 0x2649, 0x3200, 0x017f, 0x0100,
                                                               0x3f40, 0x3f00, 0x1f60, 0x1f00, 0x7f30, 0x7f00, 0x7708, 0x7700, 0x0778, 0x0700, 0x7149, 0x4700, 0x007f, 0x4100, 0x031c, 0x6000, 0x0041,
                                                               0x7f00, 0x0201, 0x0200, 0x8080, 0x8000, 0x0001, 0x0200, 0x2454, 0x7800, 0x7f44, 0x3800, 0x3844, 0x2800, 0x3844, 0x7f00, 0x3854, 0x5800,
                                                               0x087e, 0x0900, 0x4854, 0x3c00, 0x7f04, 0x7800, 0x447d, 0x4000, 0x2040, 0x3d00, 0x7f10, 0x6c00, 0x417f, 0x4000, 0x7c18, 0x7c00, 0x7c04,
                                                               0x7800, 0x3844, 0x3800, 0x7c14, 0x0800, 0x0814, 0x7c00, 0x7c04, 0x0800, 0x4854, 0x2400, 0x043e, 0x4400, 0x3c40, 0x7c00, 0x1c60, 0x1c00,
                                                               0x7c30, 0x7c00, 0x6c10, 0x6c00, 0x4c50, 0x3c00, 0x6454, 0x4c00, 0x0836, 0x4100, 0x0077, 0x0000, 0x4136, 0x0800, 0x0201, 0x0201, 0x0205,
                                                               0x0200
                                                           };
        private static readonly ushort[] DefaultPalette = new ushort[] { 0x000, 0x00A, 0x0A0, 0x0AA, 0xA00, 0xA0A, 0xA50, 0xAAA, 0x555, 0x55F, 0x5F5, 0x5FF, 0xF55, 0xF5F, 0xFF5, 0xFFF };

        public Lem1802(ISystemMonitor systemMonitor)
        {
            _systemMonitor = systemMonitor;
        }

        public int DoInterrupt(Dcpu cpu)
        {
            switch (cpu.A)
            {
                case 0:
                    _vram = cpu.B;
                    return 1;
                case 1:
                    _font = cpu.B;
                    return 1;
                case 2:
                    _palette = cpu.B;
                    return 1;
                case 3:
                    //_bordercolor = (byte)(cpu.B & 0xf);
                    return 1;
                case 4:
                    return 256;
                case 5:
                    return 16;
                default:
                    return 0;
            }
        }

        private ushort GetColor(Dcpu cpu, byte colorIndex)
        {
            return _palette == 0 ? DefaultPalette[colorIndex] : cpu.Memory[_palette + colorIndex];
        }

        private ushort GetFont(Dcpu cpu, byte fontIndex)
        {
            return _font == 0 ? DefaultFont[fontIndex] : cpu.Memory[_font + fontIndex];
        }

        private void PrintToRgb(int baseX, int baseY, byte word, uint foreground, uint background)
        {
            for (var dy = 0; dy < 8; dy++)
            {
                var isForeground = (word >> dy & 0x1) != 0;
                var value = isForeground ? foreground : background;
                var y = baseY + dy;
                var x = baseX;
                _rgb[y * Width + x] = value;
            }
        }

        public void ExternalCallback(Dcpu cpu)
        {
            if (_vram == 0)
                return;
            var nowIsBlink = DateTime.UtcNow.Millisecond < 500;
            for (var i = 0; i < Width * Height; i++)
            {
                var word = cpu.Memory[_vram + i];
                var character = word & 0x7f;
                var blinking = (word & 0x8f) != 0;

                var bgcolor = GetColor(cpu, (byte)(word >> 8 & 0xf));
                var fgcolor = GetColor(cpu, (byte)(word >> 12 & 0xf));

                var bg = (uint)((bgcolor & 0xf00 << 20) | (bgcolor & 0xf0 << 12) | (bgcolor & 0xf << 4));
                var fg = (uint)((fgcolor & 0xf00 << 20) | (fgcolor & 0xf0 << 12) | (fgcolor & 0xf << 4));

                if (blinking && nowIsBlink)
                    fg = bg;

                var fontWordZero = GetFont(cpu, (byte)(character * 2));
                var fontWordOne = GetFont(cpu, (byte)(character * 2 + 1));

                var cx = i % Width;
                var cy = i / Width;
                var px = cx * 4;
                var py = cy * 8;
                PrintToRgb(px + 0, py, (byte)fontWordZero, fg, bg);
                PrintToRgb(px + 1, py, (byte)(fontWordZero >> 8), fg, bg);
                PrintToRgb(px + 2, py, (byte)fontWordOne, fg, bg);
                PrintToRgb(px + 3, py, (byte)(fontWordOne >> 8), fg, bg);
            }
            _systemMonitor.SetScreen(_rgb, Width);
        }
    }
}