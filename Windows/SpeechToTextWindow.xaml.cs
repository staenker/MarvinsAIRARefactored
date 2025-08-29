
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using PInvoke;

using static PInvoke.User32;

namespace MarvinsAIRARefactored.Windows;

public partial class SpeechToTextWindow : Window
{
	private bool _initialized = false;
	private bool _isDraggable = false;

	private string _finalText = string.Empty;

	private float _finalTextTimer = 0f;
	private float _windowVisibilityTimer = 0f;

	public SpeechToTextWindow()
	{
		var app = App.Instance!;

		app.Logger.WriteLine( "[SpeechToTextWindow] Constructor >>>" );

		InitializeComponent();

		app.Logger.WriteLine( "[SpeechToTextWindow] <<< Constructor" );
	}

	public void Initialize()
	{
		var app = App.Instance!;

		app.Logger.WriteLine( "[SpeechToTextWindow] Initialize >>>" );

		var settings = MarvinsAIRARefactored.DataContext.DataContext.Instance.Settings;

		var rectangle = settings.SpeechToTextOverlayWindowPosition;

		Left = rectangle.Location.X;
		Top = rectangle.Location.Y;

		WindowStartupLocation = WindowStartupLocation.Manual;

		UpdateVisibility();

		_initialized = true;

		app.Logger.WriteLine( "[SpeechToTextWindow] <<< Initialize" );
	}

	private void Window_Loaded( object sender, RoutedEventArgs e )
	{
		var hwnd = new WindowInteropHelper( this ).Handle;

		var exStyle = User32.GetWindowLong( hwnd, WindowLongIndexFlags.GWL_EXSTYLE );

		_ = User32.SetWindowLong( hwnd, WindowLongIndexFlags.GWL_EXSTYLE, (SetWindowLongFlags) ( (uint) exStyle | (uint) SetWindowLongFlags.WS_EX_TOOLWINDOW ) ); // Prevent Alt+Tab visibility
	}

	private void Window_LocationChanged( object sender, EventArgs e )
	{
		if ( _initialized )
		{
			if ( IsVisible && ( WindowState == WindowState.Normal ) )
			{
				var settings = MarvinsAIRARefactored.DataContext.DataContext.Instance.Settings;

				var rectangle = settings.SpeechToTextOverlayWindowPosition;

				rectangle.Location = new System.Drawing.Point( (int) RestoreBounds.Left, (int) RestoreBounds.Top );

				settings.SpeechToTextOverlayWindowPosition = rectangle;
			}
		}
	}

	public void ResetWindow()
	{
		Left = 0;
		Top = 0;
	}

	public void UpdateVisibility()
	{
		if ( _initialized )
		{
			var settings = MarvinsAIRARefactored.DataContext.DataContext.Instance.Settings;

			if ( settings.SpeechToTextShowOverlayWindow )
			{
				Show();
				MakeDraggable();
			}
			else
			{
				Hide();
			}
		}
	}

	public void MakeDraggable()
	{
		var settings = MarvinsAIRARefactored.DataContext.DataContext.Instance.Settings;

		_isDraggable = settings.SpeechToTextMakeOverlayWindowDraggable;

		var hwnd = new WindowInteropHelper( this ).Handle;

		var exStyle = User32.GetWindowLong( hwnd, WindowLongIndexFlags.GWL_EXSTYLE );

		if ( _isDraggable )
		{
			_ = User32.SetWindowLong( hwnd, WindowLongIndexFlags.GWL_EXSTYLE, (SetWindowLongFlags) ( (uint) exStyle & (uint) ~SetWindowLongFlags.WS_EX_TRANSPARENT ) );
		}
		else
		{
			_ = User32.SetWindowLong( hwnd, WindowLongIndexFlags.GWL_EXSTYLE, (SetWindowLongFlags) ( (uint) exStyle | (uint) SetWindowLongFlags.WS_EX_TRANSPARENT ) );
		}
	}

	protected override void OnMouseLeftButtonDown( MouseButtonEventArgs e )
	{
		if ( _isDraggable )
		{
			DragMove();
		}
	}

	public void SetPartialText( string text )
	{
		if ( _initialized )
		{
			Dispatcher.BeginInvoke( () =>
			{
				_finalTextTimer += 1f / 10f;

				if ( ( _finalTextTimer > 10f ) || ( _finalText == string.Empty ) )
				{
					TextBlock.Text = text;
				}
				else
				{
					TextBlock.Text = $"{_finalText}\r\n{text}";
				}

				_windowVisibilityTimer = 0f;

				Show();
			} );
		}
	}

	public void SetFinalText( string text )
	{
		if ( _initialized )
		{
			Dispatcher.BeginInvoke( () =>
			{
				_windowVisibilityTimer = 0f;
				_finalTextTimer = 0f;
				_finalText = text;

				TextBlock.Text = _finalText;

				Show();
			} );
		}
	}

	public void Tick( App app )
	{
		if ( _initialized )
		{
			var settings = MarvinsAIRARefactored.DataContext.DataContext.Instance.Settings;

			if ( ( Visibility == Visibility.Visible ) && !settings.SpeechToTextShowOverlayWindow )
			{
				_windowVisibilityTimer += 1f / 60f;

				if ( _windowVisibilityTimer > 10f )
				{
					Hide();
				}
			}
		}
	}
}
