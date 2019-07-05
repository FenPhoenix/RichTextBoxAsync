/*
RichTextBoxAsync
An experimental method for making a RichTextBox asynchronous for the purpose of allowing it to load large files
without blocking the UI thread.

Notes:
 -Focus will need some manual work. Tab doesn't automatically select the SetParent()ed window.
 -Hosted windows don't want to stay maximized. Have to figure out a performant way to keep them docked.
 -Casual holy grail: We can even host the rtfbox in our main window, by itself, and then load a file into it AND
  IT DOES IT ASYNCHRONOUSLY EVEN ON THE UI! Hallelujah!
 -In order to support events, we'd have to have some kind of wrapper that implements its own complete set of
  events that get invoked into by the other thread and then fires them as normal (I guess)
*/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace RichTextBoxAsync_DemoApp
{
    public partial class MainForm : Form
    {
        private const string TestFile = @"C:\Thief Games\ThiefG-ND-1.26-AngelLoader-Test\FMs\TDP20AC_An_Enigmatic_Treasure_\TDP20AC_An_Enigmatic_Treasure_With_A_Recondite_Discovery.rtf";

        private readonly Timer timer = new Timer();

        public MainForm()
        {
            InitializeComponent();

            timer.Tick += Timer_Tick;
            timer.Interval = 20;
            timer.Start();
        }

        // If this animation pauses, then I know the UI is being blocked
        private void Timer_Tick(object sender, EventArgs e)
        {
            BeginInvoke(new Action(() =>
                {
                    TestLabel.Text = int.TryParse(TestLabel.Text, out int result) && result < 9
                        ? (result + 1).ToString()
                        : "0";
                }));
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            // An awkwardly crufty-looking call, but it has essentially the same semantics as a bog-vanilla
            // LoadFile() call, except you can await it and it doesn't block. Heavenly!
            //await Task.Run(() => RTBAsync.Invoke(new Action(() => RTBContext.LoadRTFBoxTestContent())));

            if (!RTBAsync.IsInitialized) RTBAsync.InitRichTextBox();

            await RTBAsync.LoadFile_PerfTest(TestFile, 10);

            Trace.WriteLine("---------done!");
        }
    }
}
