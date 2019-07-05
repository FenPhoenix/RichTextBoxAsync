using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
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

        public RichTextBoxAsync()
        {
            InitializeComponent();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            if (IsInitialized) SetRichTextBoxSizeToFill();

            base.OnSizeChanged(e);
        }

        protected override void OnEnter(EventArgs e)
        {
            if (IsInitialized && _richTextBoxInternal.Visible)
            {
                _richTextBoxInternal.BeginInvoke(new Action(() => _richTextBoxInternal.Focus()));
            }
            base.OnEnter(e);
        }

        private void SetRichTextBoxSizeToFill()
        {
            // Make a copy so we don't get cross-thread exceptions
            var size = Size;
            _richTextBoxInternal.BeginInvoke(new Action(() => _richTextBoxInternal.Size = size));
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
                    // TODO: We shouldn't be setting ReadOnly here, but just for testing purposes
                    _richTextBoxInternal = new RichTextBox_CH { ReadOnly = true, BackColor = SystemColors.Window };

                    // And the same sort of setup with the ApplicationContext, just in case we want to call into
                    // it too
                    _asyncAppContext = new AppContext_Test(this, _richTextBoxInternal, _waitHandle);

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

                // "Set Dock to DockStyle.Fill" as it were
                _richTextBoxInternal.BeginInvoke(new Action(() => _richTextBoxInternal.Location = new Point(0, 0)));
                SetRichTextBoxSizeToFill();
            }

            IsInitialized = true;
        }

        #region LoadFile

        // We have to hide the internal RichTextBox while we load, otherwise if we resize this control, the UI
        // freezes up until the load is done (and that's a hard freeze, with even the window resize functionality
        // freezing).
        public async Task LoadFileAsync(string path)
        {
            var oldReadOnly = _richTextBoxInternal.ReadOnly;
            try
            {
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.ReadOnly = false));
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.Hide()));
                await Task.Run(() => _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.LoadFile(path))));
            }
            finally
            {
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.ReadOnly = oldReadOnly));
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.Show()));
                SetRichTextBoxSizeToFill();
            }
        }

        public async Task LoadFileAsync(string path, RichTextBoxStreamType fileType)
        {
            var oldReadOnly = _richTextBoxInternal.ReadOnly;
            try
            {
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.ReadOnly = false));
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.Hide()));
                await Task.Run(() => _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.LoadFile(path, fileType))));
            }
            finally
            {
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.ReadOnly = oldReadOnly));
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.Show()));
                SetRichTextBoxSizeToFill();
            }
        }

        public async Task LoadFileAsync(Stream data, RichTextBoxStreamType fileType)
        {
            var oldReadOnly = _richTextBoxInternal.ReadOnly;
            try
            {
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.ReadOnly = false));
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.Hide()));
                await Task.Run(() => _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.LoadFile(data, fileType))));
            }
            finally
            {
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.ReadOnly = oldReadOnly));
                _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.Show()));
                SetRichTextBoxSizeToFill();
            }
        }

        #endregion

        internal sealed class AppContext_Test : ApplicationContext
        {
            private readonly RichTextBoxAsync Owner;
            private readonly RichTextBox RTFBox;

            internal AppContext_Test(RichTextBoxAsync owner, RichTextBox_CH rtfBox, AutoResetEvent waitHandle)
            {
                Owner = owner;
                RTFBox = rtfBox;

                rtfBox.KeyDown += (sender, e) =>
                {
                    if (rtfBox.Visible && e.KeyCode == Keys.Tab)
                    {
                        var p = owner.ParentForm;
                        if (p != null) owner.BeginInvoke(new Action(() => p.SelectNextControl(owner, true, true, true, true)));
                    }
                };

                rtfBox.Enter += (sender, e) =>
                {
                    Trace.WriteLine("enter");
                };

                // CreateHandle() is not exposed, so to get at it we have to subclass RichTextBox and expose it our-
                // selves. We could call CreateControl() which is exposed, but that will only work if the control is
                // visible. And in order to avoid visual and focus-stealing issues, we don't want to ever set it to
                // visible.
                if (!rtfBox.IsHandleCreated) rtfBox.CreateHandle();

                // Notify the main thread that we're done initializing
                waitHandle.Set();
            }
        }
    }
}
