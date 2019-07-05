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
        private const string TestFile = @"..\..\TestData\LongLoadTest.rtf";

        private readonly Timer timer = new Timer();

        public MainForm()
        {
            InitializeComponent();

            StatusLabel.Text = "";
            LoadFileTextBox.Text = TestFile;

            if (!RTBAsync.IsInitialized) RTBAsync.InitRichTextBox();

            timer.Tick += Timer_Tick;
            timer.Interval = 20;
            timer.Start();
        }

        // If this animation pauses, then I know the UI is being blocked
        private void Timer_Tick(object sender, EventArgs e)
        {
            BeginInvoke(new Action(() =>
                {
                    AnimationTestLabel.Text = int.TryParse(AnimationTestLabel.Text, out int result) && result < 9
                        ? (result + 1).ToString()
                        : "0";
                }));
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            StatusLabel.Text = @"Loading...";

            try
            {
                await RTBAsync.LoadFileAsync(LoadFileTextBox.Text);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                StatusLabel.Text = @"Couldn't load file!";
                return;
            }

            StatusLabel.Text = @"Done!";
            Trace.WriteLine("---------done!");
        }
    }
}
