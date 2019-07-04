/*
RichTextBoxAsync
An experimental method for making a RichTextBox asynchronous for the purpose of allowing it to load large files
without blocking the UI thread.

Notes:
 -Focus will need some manual work. Tab doesn't automatically select the SetParent()ed window.
 -Hosted windows don't want to stay maximized. Have to figure out a performant way to keep them docked.
 -Casual holy grail: We can even host the rtfbox in our main window, by itself, and then load a file into it AND
  IT DOES IT ASYNCHRONOUSLY EVEN ON THE UI! Hallelujah!
*/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace RichTextBoxAsync
{
    public partial class MainForm : Form
    {
        private readonly Timer timer = new Timer();

        public MainForm()
        {
            InitializeComponent();

            timer.Tick += Timer_Tick;
            timer.Interval = 20;
            timer.Start();

            RTFBox.Hide();
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

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        private Task RTBTask;
        private AppContext_Test RTBContext;
        private RichTextBox RTBAsync;
        private IntPtr thisHandle;

        private readonly AutoResetEvent RTBThreadWaitHandle = new AutoResetEvent(false);

        private async void Button1_Click(object sender, EventArgs e)
        {
            // We can't pass our actual handle property to another thread, so we make a copy and pass that.
            if (thisHandle == IntPtr.Zero) thisHandle = Handle;

            // Note: a task gets closed automatically on regular shutdown, whereas a thread needs explicit cleanup
            // or Environment.Exit(). Tasks seem to work just as well and are easier to use, so I'm using a task
            // for now.
            if (RTBTask == null)
            {
                RTBTask = new Task(() =>
                {
                    // The RichTextBox is *created* on this thread, which is what allows it to be async, but we
                    // *declared* it on the main thread, which means we can still reference it, as long as we do
                    // it through an Invoke() or BeginInvoke().
                    RTBAsync = new RichTextBox();

                    // And the same sort of setup with the ApplicationContext, just in case we want to call into
                    // it too
                    RTBContext = new AppContext_Test(RTBAsync, RTBThreadWaitHandle);

                    // This starts a second message loop, which is what we want: the RichTextBox will have its
                    // own UI thread
                    Application.Run(RTBContext);
                });

                RTBTask.Start();

                // We have to use signaling because we're not waiting for the task to finish, but only for the
                // ApplicationContext's constructor to finish. And besides, Application.Run() means the task will
                // never finish anyway.
                RTBThreadWaitHandle.WaitOne();

                // This is why we need to pass our handle and run CreateHandle() on the RichTextBox's form and
                // itself (see below) - this is what puts the RichTextBox inside our main UI (while still keeping
                // it asynchronous)
                RTBAsync.Invoke(new Action(() => SetParent(RTBAsync.Handle, thisHandle)));
            }

            // An awkwardly crufty-looking call, but it has essentially the same semantics as a bog-vanilla
            // LoadFile() call, except you can await it and it doesn't block. Heavenly!
            await Task.Run(() => RTBAsync.Invoke(new Action(() => RTBContext.LoadRTFBoxTestContent())));

            Trace.WriteLine("---------done!");
        }
    }

    internal sealed class AppContext_Test : ApplicationContext
    {
        private readonly RichTextBox AC_RTFBox;
        private const string TestFile = @"C:\Thief Games\ThiefG-ND-1.26-AngelLoader-Test\FMs\TDP20AC_An_Enigmatic_Treasure_\TDP20AC_An_Enigmatic_Treasure_With_A_Recondite_Discovery.rtf";

        // The RichTextBox needs to be on a form at first (I think - test this more thoroughly later!), but we
        // can slap it onto our main UI by itself without needing to carry its parent form with it.
        private readonly FormCustom form1 = new FormCustom();

        internal AppContext_Test(RichTextBox rtfBox, AutoResetEvent are)
        {
            AC_RTFBox = rtfBox;

            form1.Controls.Add(AC_RTFBox);
            // CreateHandle() is not exposed, so to get at it we have to subclass Form and expose it ourselves.
            // CreateHandle() also creates handles for all child controls, so as long as we've added the
            // RichTextBox before doing this, it will also have its handle created (which we need). We could call
            // CreateControl() which is exposed, but that will only work if the form is visible. And in order to
            // avoid visual and focus-stealing issues, we don't want to ever set it to visible.
            form1.CreateHandle_();

            // Notify the main thread that we're done initializing
            are.Set();
        }

        public void LoadRTFBoxTestContent()
        {
            // Just in case we hid it or whatever
            AC_RTFBox.Show();

            // Loop so it takes a long enough time for any blocking to become noticeable
            for (int i = 0; i < 10; i++)
            {
                AC_RTFBox.LoadFile(TestFile);
                Trace.WriteLine("boop " + i);
            }
        }
    }
}
