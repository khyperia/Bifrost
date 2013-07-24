using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Bifrost;

namespace BifrostStandalone
{
    class Program
    {
        private static string ChooseFile(IWin32Window window)
        {
            var ofd = new OpenFileDialog();
            var result = ofd.ShowDialog(window);
            if (result == DialogResult.OK || result == DialogResult.Yes)
                return ofd.FileName;
            return null;
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (BitConverter.IsLittleEndian == false)
                Console.WriteLine("Note: You have a big-endian computer, things might behave a bit differently. (But report bugs, as always!)");
            var window = new MainWindow();
            var file = args.Length > 0 ? args[0] : ChooseFile(window);
            var bytes = File.ReadAllBytes(file);
            var shorts = Extensions.ConvertToShorts(bytes, true);
            var dcpu = new Dcpu();
            dcpu.LoadProgram(shorts);
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
}
