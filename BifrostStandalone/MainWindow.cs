using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Bifrost;

namespace BifrostStandalone
{
    class MainWindow : Form, ISystemMonitor
    {
        private readonly Dictionary<Keys, string> _extraMappings = new Dictionary<Keys, string>
                                                                       {
                                                                           {Keys.Oemcomma, ","},
                                                                           {Keys.OemPeriod, "."},
                                                                           {Keys.OemQuestion, "/"},
                                                                           {Keys.Oem1, ";"},
                                                                           {Keys.Oem7, "'"},
                                                                           {Keys.OemOpenBrackets, "["},
                                                                           {Keys.Oem6, "]"},
                                                                           {Keys.Oem5, "\""},
                                                                           {Keys.OemMinus, "-"},
                                                                           {Keys.Oemplus, "="},
                                                                           {Keys.Oemtilde, "`"},
                                                                           {Keys.Add, "+"},
                                                                           {Keys.Subtract, "-"},
                                                                           {Keys.Multiply, "*"},
                                                                           {Keys.Divide, "/"}
                                                                       };
        private Bitmap _backgroundBitmap;
        private GenericKeyboard _keyboardInst;

        public MainWindow()
        {
            base.KeyUp += (sender, args) =>
                              {
                                  var b = GetKeyValue(args.KeyCode, args.Modifiers);
                                  if (b != 0 && _keyboardInst != null)
                                      _keyboardInst.SystemKeyboardOnKeyUp(b);
                              };
            base.KeyDown += (sender, args) =>
                                {
                                    var b = GetKeyValue(args.KeyCode, args.Modifiers);
                                    if (b != 0 && _keyboardInst != null)
                                        _keyboardInst.SystemKeyboardOnKeyDown(b);
                                };
            new Thread(() =>
                           {
                               while (true)
                               {
                                   Thread.Sleep(100);
                                   try
                                   {
                                       Invoke((Action)Refresh);
                                   }
                                   catch
                                   {
                                       break;
                                   }
                               }
                           }).Start();
        }

        // method from Tomato
        public ushort GetKeyValue(Keys keyCode, Keys modifiers)
        {
            switch (keyCode)
            {
                case Keys.Up: return 0x80;
                case Keys.Down: return 0x81;
                case Keys.Left: return 0x82;
                case Keys.Right: return 0x83;
                case Keys.Back: return 0x10;
                case Keys.Return: return 0x11;
                case Keys.Insert: return 0x12;
                case Keys.Delete: return 0x13;
                case Keys.Control: return 0x91;
                case Keys.ControlKey: return 0x91;
                case Keys.Shift:
                case Keys.ShiftKey: return 0x90;
                default:
                    const string lowercase = "1234567890;\'-=`,./[]\\";
                    const string uppercase = "!@#$%^&*():\"_+~<>?{}|";
                    switch (keyCode)
                    {
                        case Keys.NumPad0: keyCode = Keys.D0; break;
                        case Keys.NumPad1: keyCode = Keys.D1; break;
                        case Keys.NumPad2: keyCode = Keys.D2; break;
                        case Keys.NumPad3: keyCode = Keys.D3; break;
                        case Keys.NumPad4: keyCode = Keys.D4; break;
                        case Keys.NumPad5: keyCode = Keys.D5; break;
                        case Keys.NumPad6: keyCode = Keys.D6; break;
                        case Keys.NumPad7: keyCode = Keys.D7; break;
                        case Keys.NumPad8: keyCode = Keys.D8; break;
                        case Keys.NumPad9: keyCode = Keys.D9; break;
                    }
                    var key = Convert.ToChar(keyCode);
                    if (_extraMappings.ContainsKey(keyCode))
                    {
                        key = _extraMappings[keyCode][0];
                        if (key == '"')
                            key = '\\';
                    }
                    string ascii;
                    if (char.IsLetter(key))
                    {
                        ascii = key.ToString(CultureInfo.InvariantCulture);
                        if ((modifiers & Keys.Shift) == Keys.None)
                            ascii = ascii.ToLower();
                    }
                    else
                    {
                        if (lowercase.Contains(key))
                        {
                            if ((modifiers & Keys.Shift) == Keys.None)
                                ascii = key.ToString(CultureInfo.InvariantCulture);
                            else
                                ascii = uppercase[lowercase.IndexOf(key)].ToString(CultureInfo.InvariantCulture);
                        }
                        else
                            ascii = key.ToString(CultureInfo.InvariantCulture);
                    }
                    ushort code = Encoding.ASCII.GetBytes(ascii)[0];
                    return code;
            }
        }

        public new event Action<ushort> KeyUp;
        public new event Action<ushort> KeyDown;

        public void SetScreen(int[] rgb, int width)
        {
            if (_backgroundBitmap == null)
                _backgroundBitmap = new Bitmap(width, rgb.Length / width);
            lock (_backgroundBitmap)
            {
                var locked = _backgroundBitmap.LockBits(new Rectangle(0, 0, _backgroundBitmap.Width, _backgroundBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                Marshal.Copy(rgb, 0, locked.Scan0, rgb.Length);
                _backgroundBitmap.UnlockBits(locked);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_backgroundBitmap != null)
            {
                var graphics = e.Graphics;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                lock (_backgroundBitmap)
                {
                    if (ClientSize.Width < _backgroundBitmap.Width * 4 || ClientSize.Height < _backgroundBitmap.Height * 4)
                        ClientSize = new Size(_backgroundBitmap.Width * 4, _backgroundBitmap.Height * 4);
                    graphics.DrawImage(_backgroundBitmap, 0, 0, _backgroundBitmap.Width * 4, _backgroundBitmap.Height * 4);
                }
            }
        }
    }
}