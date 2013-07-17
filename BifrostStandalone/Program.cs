using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Bifrost;

namespace BifrostStandalone
{
    class Program
    {
        static void Main(string[] args)
        {
            var window = new MainWindow();
            var dcpu = new Dcpu();
            dcpu.AddHardware(new GenericKeyboard(window));
            dcpu.AddHardware(new Lem1802(window));
            bool[] running = { true };
            var thread = new Thread(() =>
                                        {
                                            while (running[0])
                                                dcpu.Run();
                                        });
            thread.Start();
            Application.Run(window);
            running[0] = false;
        }
    }

    class MainWindow : Form, ISystemKeyboard, ISystemMonitor
    {
        private Bitmap _backgroundBitmap;

        public MainWindow()
        {
            base.KeyUp += (sender, args) =>
                              {
                                  var b = KeyToDcpuByte(args.KeyCode, args.KeyValue);
                                  if (b != 0 && KeyUp != null)
                                      KeyUp(b);
                              };
            base.KeyDown += (sender, args) =>
                                {
                                    var b = KeyToDcpuByte(args.KeyCode, args.KeyValue);
                                    if (b != 0 && KeyDown != null)
                                        KeyDown(b);
                                };
        }

        private static byte KeyToDcpuByte(Keys keyCode, int keyValue)
        {
            switch (keyCode)
            {
                case Keys.Back: return 0x10;
                case Keys.Return: return 0x11;
                case Keys.Insert: return 0x12;
                case Keys.Delete: return 0x13;
                case Keys.Up: return 0x80;
                case Keys.Down: return 0x81;
                case Keys.Left: return 0x82;
                case Keys.Right: return 0x83;
                case Keys.Shift: return 0x90;
                case Keys.Control: return 0x91;
                default:
                    if (keyValue >= 0x20 && keyValue <= 0x7f)
                        return (byte)keyValue;
                    return 0;
            }
        }

        public new event Action<byte> KeyUp;
        public new event Action<byte> KeyDown;

        public void SetScreen(int[] rgb, int width)
        {
            if (_backgroundBitmap == null)
                _backgroundBitmap = new Bitmap(width, rgb.Length / width);
            lock (_backgroundBitmap)
            {
                var locked = _backgroundBitmap.LockBits(new Rectangle(0, 0, _backgroundBitmap.Width, _backgroundBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                Marshal.Copy(rgb, 0, locked.Scan0, rgb.Length);
                _backgroundBitmap.UnlockBits(locked);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (_backgroundBitmap != null)
                lock (_backgroundBitmap)
                    e.Graphics.DrawImageUnscaled(_backgroundBitmap, 0, 0);
        }
    }
}
