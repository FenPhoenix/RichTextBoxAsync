/*
RichTextBoxAsync
An experimental method for making a RichTextBox asynchronous for the purpose of allowing it to load large files
without blocking the UI thread.

Notes:
 -In order to support events, we'd have to duplicate them all here and invoke them from the AppContext thread (I
  guess).

Todos:
 -Support events
 -Support method calls and properties
 -Allow setting of all properties of the RichTextBox from the UI (the RichTextBox won't be able to be displayed
  due to the way its construction has to occur in another thread, so we'll have to duplicate the properties and
  then transfer them over at runtime).
*/

using System;
using System.ComponentModel;
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

        #region Private fields

        private IntPtr _thisHandle;
        private Task _asyncTask;
        private RTB_AppContext _asyncRTBAppContext;
        private readonly AutoResetEvent _waitHandle;
        private RichTextBox_CH _richTextBoxInternal;

        private bool _eventsEnabled;

        private readonly Button _focuser;

        #region Delegates

        private readonly Action RTB_Focus;
        private readonly Action<bool> RTB_SetVisible;
        private readonly Action<bool> RTB_DockToUI;
        private readonly Action<Size> RTB_SetSize;
        private readonly Action<bool> RTB_SetReadOnly;
        private readonly Action<string> RTB_LoadFilePath;
        private readonly Action<string, RichTextBoxStreamType> RTB_LoadFilePathAndType;
        private readonly Action<Stream, RichTextBoxStreamType> RTB_LoadFileStreamAndType;

        #endregion

        #endregion

        public bool IsInitialized { get; private set; }

        public RichTextBoxAsync()
        {
            InitializeComponent();

            _eventsEnabled = true;
            _waitHandle = new AutoResetEvent(false);

            #region Init delegates

            RTB_Focus = () => _richTextBoxInternal.Focus();
            RTB_SetVisible = value => _richTextBoxInternal.Visible = value;
            RTB_DockToUI = value => SetParent(_richTextBoxInternal.Handle, value ? _thisHandle : IntPtr.Zero);
            RTB_SetSize = size => _richTextBoxInternal.Size = size;
            RTB_SetReadOnly = value => _richTextBoxInternal.ReadOnly = value;
            RTB_LoadFilePath = path => _richTextBoxInternal.LoadFile(path);
            RTB_LoadFilePathAndType = (path, fileType) => _richTextBoxInternal.LoadFile(path, fileType);
            RTB_LoadFileStreamAndType = (data, fileType) => _richTextBoxInternal.LoadFile(data, fileType);

            #endregion

            // Repulsive hack to fix freezing behavior on focus - UserControl wants at least one child control to
            // pass focus to, and the thread-hosted RichTextBox doesn't count
            Controls.Add(_focuser = new Button { Location = new Point(-100, -100) });

            // Have to use this check cause DesignMode doesn't return the correct value when used in a constructor
            bool designMode = LicenseManager.UsageMode == LicenseUsageMode.Designtime;

            if (!designMode) InitRichTextBox();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            if (IsInitialized) SetRichTextBoxSizeToFill();

            base.OnSizeChanged(e);
        }

        protected override void OnEnter(EventArgs e)
        {
            if (!_eventsEnabled) return;

            // Because our RichTextBox is being hosted inside our window in a crazy manner and all that, we have
            // to implement tab functionality ourselves.
            // When we get selected, pass the selection on to our RichTextBox, unless it's invisible of course.
            if (IsInitialized && _richTextBoxInternal.Visible)
            {
                _richTextBoxInternal.BeginInvoke(RTB_Focus);
            }

            // If our RichTextBox is hidden, we'll still end up focused. We can't set TabStop to false, because
            // then tabbing to our RichTextBox won't work at all. We could just select the next control here, but
            // the problem is we don't know which direction the user's tab selection came from. So we wouldn't be
            // able to tell if we should select the next or the previous control.

            // Us being a tab stop is not a huge deal really, it'd just be a nice small bit of polish if it could
            // be avoided.

            base.OnEnter(e);
        }

        private void SetRichTextBoxSizeToFill()
        {
            // Make a copy so we don't get cross-thread exceptions
            var size = Size;
            _richTextBoxInternal.BeginInvoke(RTB_SetSize, size);
        }

        private void InitRichTextBox()
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
                    _asyncRTBAppContext = new RTB_AppContext(this, _richTextBoxInternal, _waitHandle);

                    // This starts a second message loop, which is what we want: the RichTextBox will have its
                    // own UI thread
                    Application.Run(_asyncRTBAppContext);
                });

                _asyncTask.Start();

                // We have to use signaling because we're not waiting for the task to finish, but only for the
                // ApplicationContext's constructor to finish. And besides, Application.Run() means the task will
                // never finish anyway.
                _waitHandle.WaitOne();

                // This is why we need to pass our handle and run CreateHandle() on the RichTextBox (see below);
                // this is what puts the RichTextBox inside our main UI (while still keeping it asynchronous)
                _richTextBoxInternal.Invoke(RTB_DockToUI, true);

                // "Set Dock to DockStyle.Fill" as it were
                _richTextBoxInternal.BeginInvoke(new Action(() => _richTextBoxInternal.Location = new Point(0, 0)));
                SetRichTextBoxSizeToFill();
            }

            IsInitialized = true;
        }

        internal void SelectThis()
        {
            if (!_focuser.Focused)
            {
                _eventsEnabled = false;
                Select();
                _eventsEnabled = true;
            }
        }

        #region LoadFile

        private void LoadStart(bool readOnly)
        {
            if (readOnly) _richTextBoxInternal.Invoke(RTB_SetReadOnly, false);
            // To avoid the freeze-up-on-interaction problem, we have to first pop the RichTextBox off the UI and
            // then hide it, in that order.
            _richTextBoxInternal.Invoke(RTB_DockToUI, false);
            _richTextBoxInternal.Invoke(RTB_SetVisible, false);
        }

        private void LoadEnd(bool readOnly)
        {
            if (readOnly) _richTextBoxInternal.Invoke(RTB_SetReadOnly, true);
            _richTextBoxInternal.Invoke(RTB_DockToUI, true);
            _richTextBoxInternal.Invoke(RTB_SetVisible, true);
            SetRichTextBoxSizeToFill();
            if (_focuser.Focused) _richTextBoxInternal.BeginInvoke(RTB_Focus);
        }

        public void LoadFile(string path)
        {
            var readOnly = _richTextBoxInternal.ReadOnly;
            try
            {
                LoadStart(readOnly);
                _richTextBoxInternal.Invoke(RTB_LoadFilePath, path);
            }
            finally
            {
                LoadEnd(readOnly);
            }
        }

        public void LoadFile(string path, RichTextBoxStreamType fileType)
        {
            var readOnly = _richTextBoxInternal.ReadOnly;
            try
            {
                LoadStart(readOnly);
                _richTextBoxInternal.Invoke(RTB_LoadFilePathAndType, path, fileType);
            }
            finally
            {
                LoadEnd(readOnly);
            }
        }

        public void LoadFile(Stream data, RichTextBoxStreamType fileType)
        {
            var readOnly = _richTextBoxInternal.ReadOnly;
            try
            {
                LoadStart(readOnly);
                _richTextBoxInternal.Invoke(RTB_LoadFileStreamAndType, data, fileType);
            }
            finally
            {
                LoadEnd(readOnly);
            }
        }

        public async Task LoadFileAsync(string path)
        {
            var readOnly = _richTextBoxInternal.ReadOnly;
            try
            {
                LoadStart(readOnly);
                await Task.Run(() => _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.LoadFile(path))));
            }
            finally
            {
                LoadEnd(readOnly);
            }
        }

        public async Task LoadFileAsync(string path, RichTextBoxStreamType fileType)
        {
            var readOnly = _richTextBoxInternal.ReadOnly;
            try
            {
                LoadStart(readOnly);
                await Task.Run(() => _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.LoadFile(path, fileType))));
            }
            finally
            {
                LoadEnd(readOnly);
            }
        }

        public async Task LoadFileAsync(Stream data, RichTextBoxStreamType fileType)
        {
            var readOnly = _richTextBoxInternal.ReadOnly;
            try
            {
                LoadStart(readOnly);
                await Task.Run(() => _richTextBoxInternal.Invoke(new Action(() => _richTextBoxInternal.LoadFile(data, fileType))));
            }
            finally
            {
                LoadEnd(readOnly);
            }
        }

        #endregion

        internal sealed class RTB_AppContext : ApplicationContext
        {
            // TODO: Test DPI scaling behavior
            // If the control doesn't inherit its parents' behavior when SetParent()'d, we may have to put it
            // back on a separate form which can in turn have its scaling behavior set.
            private readonly RichTextBoxAsync _owner;
            private readonly RichTextBox_CH _richTextBox;

            internal RTB_AppContext(RichTextBoxAsync owner, RichTextBox_CH richTextBox, AutoResetEvent waitHandle)
            {
                _owner = owner;
                _richTextBox = richTextBox;

                _richTextBox.KeyDown += _richTextBox_KeyDown;
                _richTextBox.MouseDown += _richTextBox_MouseDown;

                // CreateHandle() is not exposed, so to get at it we have to subclass RichTextBox and expose it
                // ourselves. We could call CreateControl() which is exposed, but that will only work if the
                // control is visible, and we don't want to have to set it to visible right off the bat.
                if (!_richTextBox.IsHandleCreated) _richTextBox.CreateHandle();

                // Notify the main thread that we're done initializing
                waitHandle.Set();
            }

            #region Tab selection

            // Because we're on a different ApplicationContext, our selection is separate from the main form's,
            // meaning we can be selected at the same time as another main form control is. To ensure only one
            // selection is active at once, whenever we get selected, focus our parent (which is on the main UI
            // thread).
            // We're using MouseDown instead of Enter because when we're "entered" we may already technically be
            // focused, due to the whole context/thread thing and all that.
            private void _richTextBox_MouseDown(object sender, MouseEventArgs e)
            {
                if (_richTextBox.Visible) _owner.Invoke(new Action(() => _owner.SelectThis()));
            }

            private void _richTextBox_KeyDown(object sender, KeyEventArgs e)
            {
                // Because our RichTextBox is being hosted inside our window in a crazy manner and all that, we
                // have to implement tab functionality ourselves.
                // When the user tabs away from us, pass the selection along to the next control.
                if (_richTextBox.Visible && e.KeyCode == Keys.Tab)
                {
                    // We have to do SelectNextControl on the base form itself, otherwise the next control won't
                    // be found properly
                    var p = _owner.ParentForm;
                    if (p != null) _owner.BeginInvoke(new Action(() => p.SelectNextControl(_owner, !e.Shift, true, true, true)));
                }
            }

            #endregion
        }
    }
}
