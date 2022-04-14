using AppWindowCore;
using AppWindowCore.ViewModel;
using Microsoft.UI.Windowing;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.Graphics;
using Windows.UI;

namespace AppWindowsWPF
{
    public partial class MainWindow : Window
    {
        private NativeWindowHost m_nativeWindowHost;
        private bool m_attached;

        AppWindow _winUIWindow;
        AppWindow _appWindow;
        MainViewModel _viewModel;
        public MainWindow()
        {
            InitializeComponent();

            m_nativeWindowHost = new NativeWindowHost();
            m_nativeWindowHost.CreateControl();
            m_windowsFormsHost.Child = m_nativeWindowHost;

            var success = Bootstrap.TryInitialize(0x00010000, out _);
            if (!success)
            {
                MessageBox.Show("Unable to initialize Windows App SDK - Make sure it's installed");
                return;
            }

            _appWindow = this.GetAppWindowForWPF();

            _viewModel = new MainViewModel { Name = "Name Set from WPF" };
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);

            Bootstrap.Shutdown();
        }

        private void PositionWindowClick(object sender, RoutedEventArgs e)
        {
            var row = Grid.GetRow(sender as Button);
            var col = Grid.GetColumn(sender as Button);


            _appWindow.Reposition(row, col);

            //set winUI window on the oposite
            RepositionWinUIWindow(row, col);
        }

        private void RepositionWinUIWindow(int row, int col)
        {
            var winUIRow = 0;
            var winUICol = 0;
            if (col == 0)
            {
                winUICol = 2;
            }
            if (col == 2)
            {
                winUICol = 0;
            }
            if (col == 1)
            {
                winUICol = 2;
            }
            if (row == 0)
            {
                winUIRow = 1;
            }
            _winUIWindow.Reposition(winUIRow, winUICol);
        }

        private void ChangeIconClick(object sender, RoutedEventArgs e)
        {
            _appWindow.ChangeIcon("icon1.ico");

        }

        private void ChangePresenterClick(object sender, RoutedEventArgs e)
        {
            if (Enum.TryParse<AppWindowPresenterKind>((sender as Button).Content.ToString(), out var presenterKind))
            {
                _appWindow.SetPresenter(presenterKind);
            }
        }


        private void OverlappedPresenterPropertyCheckChanged(object sender, RoutedEventArgs e)
        {
            var presenter = _appWindow.Presenter as OverlappedPresenter;
            if (presenter is null)
            {
                return;
            }

            var check = sender as CheckBox;
            var propertyName = check.Content as string;
            var property = typeof(OverlappedPresenter).GetProperty(propertyName);
            property.SetValue(presenter, check.IsChecked);
        }

        private void OverlappedPresenterTitleBarAndBorderCheckChanged(object sender, RoutedEventArgs e)
        {
            var presenter = _appWindow.Presenter as OverlappedPresenter;
            if (presenter is null)
            {
                return;
            }
            var hasBorder = HasBorderCheckBox.IsChecked ?? true;
            var hasTitleBar = HasTitleBarCheckBox.IsChecked ?? true;
            presenter.SetBorderAndTitleBar(hasBorder, hasTitleBar);
        }

        private Random rnd { get; } = new Random();


        private System.Windows.Media.Color GetRandomColor()
        {
            return System.Windows.Media.Color.FromArgb(
                (byte)rnd.Next(0, 255),
                (byte)rnd.Next(0, 255),
                (byte)rnd.Next(0, 255),
                (byte)rnd.Next(0, 255));
        }

        private Windows.UI.Color GetRandomWindowsColor()
        {
            return Windows.UI.Color.FromArgb(
                (byte)rnd.Next(0, 255),
                (byte)rnd.Next(0, 255),
                (byte)rnd.Next(0, 255),
                (byte)rnd.Next(0, 255));
        }

        private void TitleBarRandomColorClick(object sender, RoutedEventArgs e)
        {
            var property = typeof(AppWindowTitleBar).GetProperty((sender as Button).Content.ToString());
            var clr = GetRandomWindowsColor();
            property.SetValue(_appWindow.TitleBar, clr);
        }

        private void ChangeIconAndMenuClick(object sender, RoutedEventArgs e)
        {

            if (Enum.TryParse<IconShowOptions>((sender as Button).Content.ToString(), out var showOptions))
            {
                _appWindow.TitleBar.IconShowOptions = showOptions;
            }
        }

        private void ToggleClientAreaChanged(object sender, RoutedEventArgs e)
        {
            _appWindow.TitleBar.ExtendsContentIntoTitleBar = (sender as CheckBox).IsChecked ?? false;
        }

        private void SetDragAreaClick(object sender, RoutedEventArgs e)
        {

            var newHeight = (int)rnd.Next(0, 200);
            DragAreaBorder.Height = newHeight;
            DragAreaBorder.Background = new SolidColorBrush(GetRandomColor());

            PresentationSource source = PresentationSource.FromVisual(this);

            double scaleX = 1.0, scaleY = 1.0;
            if (source != null)
            {
                scaleX = source.CompositionTarget.TransformToDevice.M11;
                scaleY = source.CompositionTarget.TransformToDevice.M22;
            }

            _appWindow.TitleBar.SetDragRectangles(new[] {
                new RectInt32(0,0,
                    (int)(DragAreaBorder.ActualWidth * scaleX),
                    (int)((newHeight + (_appWindow.TitleBar.ExtendsContentIntoTitleBar?0:_appWindow.TitleBar.Height))*scaleY)) });
        }


        AppWindowSample.App _app;

        //Try to create a WinUI app, get the CoreWindow and display it 
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenWinUIWindow(false, true);
        }

        private void Button_Click2(object sender, RoutedEventArgs e)
        {
            OpenWinUIWindow(true, false);

        }

        private void OpenWinUIWindow(bool attach, bool active)
        {
            global::WinRT.ComWrappersSupport.InitializeComWrappers();
            global::Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                _app = new AppWindowSample.App() { ViewModel = _viewModel };
                _app.Launched += (s, e) =>
                {
                    _winUIWindow = _app.Window.GetAppWindowForWinUI();

                    if (attach)
                    {
                        TryAttach((long)_app.Window.GetAppWindowHandleForWinUI());
                    }
                    if (active)
                    {
                        _winUIWindow.Show();
                    }
                };

            });
        }

        void TryAttach(long handle)
        {
            try
            {
                if (m_attached)
                {
                    m_nativeWindowHost.Detach();
                    m_attached = false;
                }

                IntPtr windowHandle = new IntPtr(handle);

                NativeWindow nativeWindow = new NativeWindow(windowHandle);
                m_nativeWindowHost.AttachWindow(nativeWindow);
                m_attached = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
