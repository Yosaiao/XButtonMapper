using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace XButtonMapper
{
    internal sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly EventWaitHandle _activationEvent;
        private readonly AppSettings _settings;
        private readonly Dictionary<ButtonAction, ToolStripMenuItem> _backActionItems;
        private readonly Dictionary<ButtonAction, ToolStripMenuItem> _forwardActionItems;

        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _menu;
        private ToolStripMenuItem _enabledItem;
        private ToolStripMenuItem _startupItem;
        private MouseHook _mouseHook;
        private MessageWindow _messageWindow;
        private RegisteredWaitHandle _activationWait;
        private System.Windows.Forms.Timer _smokeTestTimer;
        private volatile bool _exiting;

        internal TrayApplicationContext(EventWaitHandle activationEvent, bool smokeTest)
        {
            _activationEvent = activationEvent;
            _settings = AppSettings.Load();
            _backActionItems = new Dictionary<ButtonAction, ToolStripMenuItem>();
            _forwardActionItems = new Dictionary<ButtonAction, ToolStripMenuItem>();

            try
            {
                _messageWindow = new MessageWindow(this);
                BuildMenu();

                _notifyIcon = new NotifyIcon();
                _notifyIcon.Icon = SystemIcons.Application;
                _notifyIcon.Text = "XButtonMapper - 鼠标侧键复制粘贴";
                _notifyIcon.ContextMenuStrip = _menu;
                _notifyIcon.Visible = true;

                _mouseHook = new MouseHook();
                _mouseHook.BackAction = _settings.BackAction;
                _mouseHook.ForwardAction = _settings.ForwardAction;
                _mouseHook.Enabled = true;

                _activationWait = ThreadPool.RegisterWaitForSingleObject(
                    _activationEvent,
                    OnExistingInstanceStarted,
                    null,
                    Timeout.Infinite,
                    false);

                UpdateMenuState();

                if (smokeTest)
                {
                    _smokeTestTimer = new System.Windows.Forms.Timer();
                    _smokeTestTimer.Interval = 300;
                    _smokeTestTimer.Tick += delegate
                    {
                        _smokeTestTimer.Stop();
                        ExitThread();
                    };
                    _smokeTestTimer.Start();
                }
                else
                {
                    _notifyIcon.ShowBalloonTip(
                        2500,
                        "XButtonMapper 已启动",
                        "侧键映射已启用。右键托盘图标可查看或修改当前设置。",
                        ToolTipIcon.Info);
                }
            }
            catch
            {
                DisposeResources();
                throw;
            }
        }

        protected override void ExitThreadCore()
        {
            DisposeResources();
            base.ExitThreadCore();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeResources();
            }

            base.Dispose(disposing);
        }

        internal void ShowAlreadyRunningNotification()
        {
            if (_exiting || _notifyIcon == null)
            {
                return;
            }

            _notifyIcon.ShowBalloonTip(
                2500,
                "XButtonMapper 正在运行",
                "请右键系统托盘中的图标进行设置或退出。",
                ToolTipIcon.Info);
        }

        private void BuildMenu()
        {
            _menu = new ContextMenuStrip();
            _menu.Opening += delegate { UpdateMenuState(); };

            _enabledItem = new ToolStripMenuItem("启用映射");
            _enabledItem.Click += delegate
            {
                _mouseHook.Enabled = !_mouseHook.Enabled;
                UpdateMenuState();
            };
            _menu.Items.Add(_enabledItem);

            ToolStripMenuItem backMenu = new ToolStripMenuItem("后退键");
            AddActionItems(backMenu, _backActionItems, true);
            _menu.Items.Add(backMenu);

            ToolStripMenuItem forwardMenu = new ToolStripMenuItem("前进键");
            AddActionItems(forwardMenu, _forwardActionItems, false);
            _menu.Items.Add(forwardMenu);

            _menu.Items.Add(new ToolStripSeparator());

            _startupItem = new ToolStripMenuItem("开机自动启动");
            _startupItem.Click += delegate
            {
                bool desiredState = !StartupManager.IsEnabled();
                if (!StartupManager.TrySetEnabled(desiredState))
                {
                    ShowError("无法修改当前用户的开机启动项。");
                }

                UpdateMenuState();
            };
            _menu.Items.Add(_startupItem);

            _menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += delegate { ExitThread(); };
            _menu.Items.Add(exitItem);
        }

        private void AddActionItems(
            ToolStripMenuItem parent,
            Dictionary<ButtonAction, ToolStripMenuItem> itemMap,
            bool isBackButton)
        {
            AddActionItem(parent, itemMap, ButtonAction.Copy, "复制", isBackButton);
            AddActionItem(parent, itemMap, ButtonAction.Paste, "粘贴", isBackButton);
            AddActionItem(parent, itemMap, ButtonAction.PassThrough, "保持原功能", isBackButton);
        }

        private void AddActionItem(
            ToolStripMenuItem parent,
            Dictionary<ButtonAction, ToolStripMenuItem> itemMap,
            ButtonAction action,
            string text,
            bool isBackButton)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Tag = action;
            item.Click += delegate
            {
                SetButtonAction(isBackButton, action);
            };
            itemMap.Add(action, item);
            parent.DropDownItems.Add(item);
        }

        private void SetButtonAction(bool isBackButton, ButtonAction action)
        {
            if (isBackButton)
            {
                _settings.BackAction = action;
                _mouseHook.BackAction = action;
            }
            else
            {
                _settings.ForwardAction = action;
                _mouseHook.ForwardAction = action;
            }

            if (!_settings.TrySave())
            {
                ShowError("设置已经生效，但无法保存到：\r\n" + AppSettings.SettingsPath);
            }

            UpdateMenuState();
        }

        private void UpdateMenuState()
        {
            if (_mouseHook == null)
            {
                return;
            }

            _enabledItem.Checked = _mouseHook.Enabled;
            _startupItem.Checked = StartupManager.IsEnabled();

            foreach (KeyValuePair<ButtonAction, ToolStripMenuItem> pair in _backActionItems)
            {
                pair.Value.Checked = pair.Key == _settings.BackAction;
            }

            foreach (KeyValuePair<ButtonAction, ToolStripMenuItem> pair in _forwardActionItems)
            {
                pair.Value.Checked = pair.Key == _settings.ForwardAction;
            }

            string enabledLabel = _mouseHook.Enabled ? "已启用" : "已暂停";
            _notifyIcon.Text = "XButtonMapper - " + enabledLabel;
        }

        private void OnExistingInstanceStarted(object state, bool timedOut)
        {
            try
            {
                if (timedOut || _exiting)
                {
                    return;
                }

                MessageWindow window = _messageWindow;
                if (window == null || window.Handle == IntPtr.Zero)
                {
                    return;
                }

                NativeMethods.PostMessage(
                    window.Handle,
                    NativeMethods.WM_APP_ACTIVATE,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
            catch
            {
                // A second-instance notification must never destabilize the resident process.
            }
        }

        private void ShowError(string message)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(3500, "XButtonMapper", message, ToolTipIcon.Error);
            }
        }

        private void DisposeResources()
        {
            if (_exiting)
            {
                return;
            }

            _exiting = true;

            if (_smokeTestTimer != null)
            {
                _smokeTestTimer.Stop();
                _smokeTestTimer.Dispose();
                _smokeTestTimer = null;
            }

            if (_mouseHook != null)
            {
                _mouseHook.Dispose();
                _mouseHook = null;
            }

            if (_activationWait != null)
            {
                using (ManualResetEvent callbacksFinished = new ManualResetEvent(false))
                {
                    if (_activationWait.Unregister(callbacksFinished))
                    {
                        callbacksFinished.WaitOne(2000);
                    }
                }

                _activationWait = null;
            }

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            if (_menu != null)
            {
                _menu.Dispose();
                _menu = null;
            }

            if (_messageWindow != null)
            {
                _messageWindow.Dispose();
                _messageWindow = null;
            }
        }

        private sealed class MessageWindow : NativeWindow, IDisposable
        {
            private readonly TrayApplicationContext _owner;

            internal MessageWindow(TrayApplicationContext owner)
            {
                _owner = owner;

                CreateParams createParams = new CreateParams();
                createParams.Caption = "XButtonMapperMessageWindow";
                CreateHandle(createParams);
            }

            public void Dispose()
            {
                if (Handle != IntPtr.Zero)
                {
                    DestroyHandle();
                }
            }

            protected override void WndProc(ref Message message)
            {
                if (message.Msg == NativeMethods.WM_APP_ACTIVATE)
                {
                    _owner.ShowAlreadyRunningNotification();
                    return;
                }

                base.WndProc(ref message);
            }
        }
    }
}
