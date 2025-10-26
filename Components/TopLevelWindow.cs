
using System.Windows;
using System.Windows.Interop;

namespace MarvinsAIRARefactored.Components;

public class TopLevelWindow
{
	private Window? _window = null;
	public IntPtr WindowHandle { get; private set; } = 0;

	public void Initialize()
	{
		var app = App.Instance!;

		app.Logger.WriteLine( "[TopLevelWindow] Initialize >>>" );

		_window = new Window
		{
			Width = 0,
			Height = 0,
			WindowStyle = WindowStyle.None,
			ResizeMode = ResizeMode.NoResize,
			ShowInTaskbar = false,
			ShowActivated = false,
			AllowsTransparency = true,
			Opacity = 0
		};

		_window.Show();

		var windowInteropHelper = new WindowInteropHelper( _window );

		WindowHandle = windowInteropHelper.Handle;

		app.Logger.WriteLine( "[TopLevelWindow] <<< Initialize" );
	}
}
