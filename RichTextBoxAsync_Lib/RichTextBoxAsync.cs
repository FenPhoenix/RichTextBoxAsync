using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RichTextBoxAsync_Lib
{
    public partial class RichTextBoxAsync : UserControl
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        private Task _asyncTask;
        private AppContext_Test _asyncAppContext;
        private RichTextBox_CH _richTextBoxInternal;
        private IntPtr _thisHandle;

        private readonly AutoResetEvent _waitHandle = new AutoResetEvent(false);

        public bool IsInitialized { get; private set; }

        public RichTextBoxAsync() => InitializeComponent();

        protected override void OnSizeChanged(EventArgs e)
        {
            if (IsInitialized) SetRichTextBoxSizeToFill();

            base.OnSizeChanged(e);
        }

        private void SetRichTextBoxSizeToFill()
        {
            // Make a copy so we don't get cross-thread exceptions
            var size = Size;
            _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.Size = size));
        }

        public void InitRichTextBox()
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("The underlying RichTextBox has already been initialized.");
            }

            if (!IsHandleCreated) base.CreateHandle();

            // We can't pass our actual handle property to another thread, so we make a copy and pass that.
            if (_thisHandle == IntPtr.Zero) _thisHandle = Handle;

            // Note: a task gets closed automatically on regular shutdown, whereas a thread needs explicit cleanup
            // or Environment.Exit(). Tasks seem to work just as well and are easier to use, so I'm using a task
            // for now.
            if (_asyncTask == null)
            {
                _asyncTask = new Task(() =>
                {
                    // The RichTextBox is *created* on this thread, which is what allows it to be async, but we
                    // *declared* it on the main thread, which means we can still reference it, as long as we do
                    // it through an Invoke() or BeginInvoke().
                    _richTextBoxInternal = new RichTextBox_CH();

                    // And the same sort of setup with the ApplicationContext, just in case we want to call into
                    // it too
                    _asyncAppContext = new AppContext_Test(_richTextBoxInternal, _waitHandle);

                    // This starts a second message loop, which is what we want: the RichTextBox will have its
                    // own UI thread
                    Application.Run(_asyncAppContext);
                });

                _asyncTask.Start();

                // We have to use signaling because we're not waiting for the task to finish, but only for the
                // ApplicationContext's constructor to finish. And besides, Application.Run() means the task will
                // never finish anyway.
                _waitHandle.WaitOne();

                // This is why we need to pass our handle and run CreateHandle() on the RichTextBox (see below);
                // this is what puts the RichTextBox inside our main UI (while still keeping it asynchronous)
                _richTextBoxInternal.Invoke(new Action(() => SetParent(_richTextBoxInternal.Handle, _thisHandle)));
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.Location = new Point(0, 0)));
                SetRichTextBoxSizeToFill();
            }

            IsInitialized = true;
        }

        public async Task LoadFileAsync(string path)
        {
            await Task.Run(() => _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.LoadFile(path))));
        }

        public async Task LoadFileAsync(string path, RichTextBoxStreamType fileType)
        {
            await Task.Run(() => _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.LoadFile(path, fileType))));
        }

        public async Task LoadFileAsync(Stream data, RichTextBoxStreamType fileType)
        {
            await Task.Run(() => _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.LoadFile(data, fileType))));
        }

        public async Task LoadFile_PerfTest(string path, int repetitions)
        {
            await Task.Run(() => _richTextBoxInternal.Invoke(new Action(() => _asyncAppContext.LoadFile_PerfTest(path, repetitions))));
        }

        internal sealed class AppContext_Test : ApplicationContext
        {
            private readonly RichTextBox RTFBox;

            internal AppContext_Test(RichTextBox_CH rtfBox, AutoResetEvent waitHandle)
            {
                RTFBox = rtfBox;

                // CreateHandle() is not exposed, so to get at it we have to subclass RichTextBox and expose it our-
                // selves. We could call CreateControl() which is exposed, but that will only work if the control is
                // visible. And in order to avoid visual and focus-stealing issues, we don't want to ever set it to
                // visible.
                if (!rtfBox.IsHandleCreated) rtfBox.CreateHandle();

                // Notify the main thread that we're done initializing
                waitHandle.Set();
            }

            public void LoadFile_PerfTest(string path, int repetitions)
            {
                // Just in case we hid it or whatever
                RTFBox.Show();

                // Loop so it takes a long enough time for any blocking to become noticeable
                for (int i = 0; i < repetitions; i++)
                {
                    RTFBox.LoadFile(path);
                    Trace.WriteLine("boop " + i);
                }
            }
        }
    }
}
