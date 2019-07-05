/*
RichTextBoxAsync
An experimental method for making a RichTextBox asynchronous for the purpose of allowing it to load large files
without blocking the UI thread.

Notes:
 -Focus will need some manual work. Tab doesn't automatically select the SetParent()ed window.
 -Hosted windows don't want to stay maximized. Have to figure out a performant way to keep them docked.
 -Casual holy grail: We can even host the rtfbox in our main window, by itself, and then load a file into it AND
  IT DOES IT ASYNCHRONOUSLY EVEN ON THE UI! Hallelujah!
 -In order to support events, we'd have to duplicate them all here and invoke them from the AppContext thread (I
  guess).
*/

using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace RichTextBoxAsync_DemoApp
{
    public partial class MainForm : Form
    {
        private const string TestFile = @"..\..\TestData\LongLoadTest.rtf";

        private readonly Timer AnimationTimer = new Timer();

        public MainForm()
        {
            InitializeComponent();

            StatusLabel.Text = "";
            AnimationTestLabel.Text = "";
            LoadFileTextBox.Text = TestFile;

            AnimationTimer.Tick += AnimationTimerTick;
            AnimationTimer.Interval = 20;
            AnimationTimer.Start();
        }

        // If this animation pauses, then I know the UI is being blocked
        private void AnimationTimerTick(object sender, EventArgs e)
        {
            BeginInvoke(new Action(() =>
                {
                    AnimationTestLabel.Text = int.TryParse(AnimationTestLabel.Text, out int result) && result < 9
                        ? (result + 1).ToString()
                        : "0";
                }));
        }

        private async void LoadFileButton_Click(object sender, EventArgs e)
        {
            LoadFileButton.Enabled = false;
            LoadFileTextBox.Enabled = false;
            StatusLabel.Text = @"Loading...";
            LoadingPictureBox.Location = new Point(
                RTBAsync.Location.X + ((RTBAsync.Width / 2) - (LoadingPictureBox.Width / 2)),
                RTBAsync.Location.Y + ((RTBAsync.Height / 2) - (LoadingPictureBox.Height / 2)));
            LoadingPictureBox.Show();

            try
            {
                await RTBAsync.LoadFileAsync(LoadFileTextBox.Text);
                StatusLabel.Text = @"Done!";
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                StatusLabel.Text = @"Couldn't load file!";
            }
            finally
            {
                LoadingPictureBox.Hide();
                LoadFileButton.Enabled = true;
                LoadFileTextBox.Enabled = true;
            }
        }
    }
}
