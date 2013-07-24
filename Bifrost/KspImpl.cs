using System;
using UnityEngine;

namespace Bifrost
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class KspImpl : MonoBehaviour, ISystemMonitor
    {
        private bool _enabled;
        private Rect _windowRect = new Rect(100, 100, 200, 300);
        private Dcpu _dcpu;
        private bool _dcpuRunning;
        private bool _bigEndian;
        private FileBrowser _fileBrowser;
        private Texture2D _screen;

        private bool _keyboard = true;
        private bool _monitor = true;
        private bool _clock = true;
        private GenericKeyboard _keyboardInst;

        private Dcpu Dcpu
        {
            get { return _dcpu ?? (_dcpu = new Dcpu()); }
        }

        public void Awake()
        {
            DontDestroyOnLoad(this);
        }

        public void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            if (_enabled)
                _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawGui, "Bifrost DCPU window");
            if (_fileBrowser != null)
                _fileBrowser.OnGUI();
            if (Event.current.isKey)
            {
                var translated = GetKeyValue(Event.current.character, Event.current.keyCode);
                if (_keyboardInst != null)
                    _keyboardInst.SystemKeyboardOnKeyDown(translated);
            }
        }

        private void DrawGui(int id)
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            if (_dcpuRunning)
                GUI.enabled = false;
            _keyboard = GUILayout.Toggle(_keyboard, "Keyboard plugged in");
            _monitor = GUILayout.Toggle(_monitor, "Monitor plugged in");
            _clock = GUILayout.Toggle(_clock, "Clock plugged in");
            _bigEndian = GUILayout.Toggle(_bigEndian, "Is big endian binary");
            if (_dcpuRunning)
                GUI.enabled = true;

            if (GUILayout.Button("Load program binary"))
            {
                _fileBrowser = new FileBrowser(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 200, 400, 400), "Select DCPU program", s => FileBrowserSelect(s))
                                   {BrowserType = FileBrowserType.File, CurrentDirectory = KSP.IO.IOUtils.GetFilePathFor(typeof (KspImpl), ""), disallowDirectoryChange = true};
            }
            
            if (GUILayout.Button("Reset"))
            {
                _keyboardInst = null;
                Dcpu.Reset();
            }

            if (_dcpuRunning ? GUILayout.Button("Stop") : GUILayout.Button("Start"))
            {
                _dcpu.ClearHardware();
                if (_keyboard)
                    _dcpu.AddHardware(_keyboardInst = new GenericKeyboard());
                if (_monitor)
                    _dcpu.AddHardware(new Lem1802(this));
                if (_clock)
                    _dcpu.AddHardware(new GenericClock());
                _dcpuRunning = !_dcpuRunning;
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void FileBrowserSelect(string path)
        {
            _fileBrowser = null;
            var binary = KSP.IO.File.ReadAllBytes<KspImpl>(path);
            Dcpu.LoadProgram(Extensions.ConvertToShorts(binary, _bigEndian));
        }

        public void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.D))
                _enabled = true;
        }

        public void FixedUpdate()
        {
            if (_dcpu != null && _dcpuRunning)
            {
                try
                {
                    _dcpu.Run();
                }
                catch (Exception e)
                {
                    PopupDialog.SpawnPopupDialog("Error in the DCPU", e.Message, "Ok", false, HighLogic.Skin);
                    _dcpuRunning = false;
                }
            }
        }

        public void SetScreen(int[] rgb, int width)
        {
            if (_screen == null)
                _screen = new Texture2D(width, rgb.Length / width);
            var pixels = _screen.GetPixels();
            for (var i = 0; i < rgb.Length; i++)
            {
                var color = rgb[i];
                var r = color >> 16 & 0xff;
                var g = color >> 8 & 0xff;
                var b = color & 0xff;
                pixels[i] = new Color(r / 255.0f, g / 255.0f, b / 255.0f);
            }
            _screen.SetPixels(pixels);
        }

        public ushort GetKeyValue(char chr, KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.UpArrow: return 0x80;
                case KeyCode.DownArrow: return 0x81;
                case KeyCode.LeftArrow: return 0x82;
                case KeyCode.RightArrow: return 0x83;
                case KeyCode.Backspace: return 0x10;
                case KeyCode.Return: return 0x11;
                case KeyCode.Insert: return 0x12;
                case KeyCode.Delete: return 0x13;
                case KeyCode.LeftControl:
                case KeyCode.RightControl: return 0x91;
                case KeyCode.LeftShift:
                case KeyCode.RightShift: return 0x90;
                default:
                    if (chr >= 0x20 && chr <= 0x7f)
                        return chr;
                    return 0;
            }
        }
    }
}
