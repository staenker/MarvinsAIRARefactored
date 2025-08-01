
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using PInvoke;

using static PInvoke.User32;

using MarvinsAIRARefactored.Classes;

using Brushes = System.Windows.Media.Brushes;

namespace MarvinsAIRARefactored.Windows;

public partial class GripOMeter : Window
{
	private bool _initialized = false;
	private bool _isDraggable = false;

	public GripOMeter()
	{
		var app = App.Instance!;

		app.Logger.WriteLine( "[GripOMeter] Constructor >>>" );

		InitializeComponent();

		app.Logger.WriteLine( "[GripOMeter] <<< Constructor" );
	}

	public void Initialize()
	{
		var app = App.Instance!;

		app.Logger.WriteLine( "[GripOMeter] Initialize >>>" );

		var settings = MarvinsAIRARefactored.DataContext.DataContext.Instance.Settings;

		var rectangle = settings.SteeringEffectsGripOMeterWindowPosition;

		Left = rectangle.Location.X;
		Top = rectangle.Location.Y;

		WindowStartupLocation = WindowStartupLocation.Manual;

		UpdateVisibility();

		_initialized = true;

		app.Logger.WriteLine( "[GripOMeter] <<< Initialize" );
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

				var rectangle = settings.SteeringEffectsGripOMeterWindowPosition;

				rectangle.Location = new System.Drawing.Point( (int) RestoreBounds.Left, (int) RestoreBounds.Top );

				settings.SteeringEffectsGripOMeterWindowPosition = rectangle;
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

			if ( settings.SteeringEffectsShowGripOMeterWindow )
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

		_isDraggable = settings.SteeringEffectsMakeGripOMeterDraggable;

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

	public void Tick( App app )
	{
		if ( Visibility == Visibility.Visible )
		{
			if ( app.SteeringEffects.MaximumGrip == 0f )
			{
				GripOMeter_Bar_Image.Margin = new Thickness( 0, 0, 0, 324f - 16f );

				GripOMeter_Fill_Rectangle.Fill = Brushes.Transparent;
			}
			else
			{
				GripOMeter_Fill_Rectangle.Height = Math.Clamp( 324f * app.SteeringEffects.CurrentGrip, 0f, 376f );

				if ( app.SteeringEffects.CurrentGrip > app.SteeringEffects.MaximumGrip )
				{
					GripOMeter_Fill_Rectangle.Fill = Brushes.DarkOrange;
				}
				else
				{
					GripOMeter_Fill_Rectangle.Fill = Brushes.SkyBlue;
				}

				GripOMeter_Bar_Image.Margin = new Thickness( 0, 0, 0, Misc.Lerp( 0f, 324f, app.SteeringEffects.MaximumGrip ) - 16f );
			}
		}
	}
}
