
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Brushes = System.Windows.Media.Brushes;

using PInvoke;

using static PInvoke.User32;

using IRSDKSharper;
using MarvinsAIRARefactored.Components;

namespace MarvinsAIRARefactored.Windows;

public partial class GapMonitorWindow : Window
{
	private const int UpdateInterval = 1;

	private int _updateCounter = UpdateInterval + 3;

	private bool _initialized = false;
	private bool _isDraggable = false;

	// New: track previous deltas and times to compute delta rate (sec/sec)
	private const double DeltaRateThreshold = 0.05; // seconds per second

	private double _prevFrontDelta = double.NaN;
	private double _prevFrontDeltaTime = double.NaN;

	private double _prevBackDelta = double.NaN;
	private double _prevBackDeltaTime = double.NaN;

	public GapMonitorWindow()
	{
		var app = App.Instance!;

		app.Logger.WriteLine( "[GapMonitorWindow] Constructor >>>" );

		InitializeComponent();

		app.Logger.WriteLine( "[GapMonitorWindow] <<< Constructor" );
	}

	public void Initialize()
	{
		var app = App.Instance!;

		app.Logger.WriteLine( "[GapMonitorWindow] Initialize >>>" );

		var settings = MarvinsAIRARefactored.DataContext.DataContext.Instance.Settings;

		var rectangle = settings.OverlaysGapMonitorWindowPosition;

		Left = rectangle.Location.X;
		Top = rectangle.Location.Y;

		WindowStartupLocation = WindowStartupLocation.Manual;

		_initialized = true;

		UpdateVisibility();

		app.Logger.WriteLine( "[GapMonitorWindow] <<< Initialize" );
	}

	private void Window_Loaded( object sender, RoutedEventArgs e )
	{
		// Do not set WS_EX_TOOLWINDOW here so external window-pickers (like OpenKneeboard)
		// can discover and select this window. Keep `ShowInTaskbar="False"` in XAML
		// to avoid taskbar presence while still allowing enumeration by other tools.

		/*
		var hwnd = new WindowInteropHelper( this ).Handle;

		var exStyle = User32.GetWindowLong( hwnd, WindowLongIndexFlags.GWL_EXSTYLE );

		_ = User32.SetWindowLong( hwnd, WindowLongIndexFlags.GWL_EXSTYLE, (SetWindowLongFlags) ( (uint) exStyle | (uint) SetWindowLongFlags.WS_EX_TOOLWINDOW ) ); // Prevent Alt+Tab visibility
		*/
	}

	private void Window_LocationChanged( object sender, EventArgs e )
	{
		if ( _initialized )
		{
			if ( IsVisible && ( WindowState == WindowState.Normal ) )
			{
				var settings = MarvinsAIRARefactored.DataContext.DataContext.Instance.Settings;

				var rectangle = settings.OverlaysGapMonitorWindowPosition;

				rectangle.Location = new System.Drawing.Point( (int) RestoreBounds.Left, (int) RestoreBounds.Top );

				settings.OverlaysGapMonitorWindowPosition = rectangle;
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
			Dispatcher.BeginInvoke( () =>
			{
				var app = App.Instance!;

				var settings = MarvinsAIRARefactored.DataContext.DataContext.Instance.Settings;

				if ( settings.OverlaysShowGapMonitorWindow && app.Simulator.IsOnTrack )
				{
					Show();
					MakeDraggable();
				}
				else
				{
					Hide();
				}
			} );
		}
	}

	public void MakeDraggable()
	{
		var settings = MarvinsAIRARefactored.DataContext.DataContext.Instance.Settings;

		_isDraggable = settings.OverlaysMakeGapMonitorDraggable;

		var hwnd = new WindowInteropHelper( this ).Handle;

		var exStyle = User32.GetWindowLong( hwnd, WindowLongIndexFlags.GWL_EXSTYLE );

		if ( _isDraggable )
		{
			_ = SetWindowLong( hwnd, WindowLongIndexFlags.GWL_EXSTYLE, (SetWindowLongFlags) ( (uint) exStyle & (uint) ~SetWindowLongFlags.WS_EX_TRANSPARENT ) );
		}
		else
		{
			_ = SetWindowLong( hwnd, WindowLongIndexFlags.GWL_EXSTYLE, (SetWindowLongFlags) ( (uint) exStyle | (uint) SetWindowLongFlags.WS_EX_TRANSPARENT ) );
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
			_updateCounter--;

			if ( _updateCounter == 0 )
			{
				_updateCounter = UpdateInterval;

				// protect against invalid player car index
				if ( ( app.Simulator.PlayerCarIdx < 0 ) || ( app.Simulator.PlayerCarIdx >= IRacingSdkConst.MaxNumCars ) )
				{
					return;
				}

				// find the closest car ahead and behind based on relative lap distance percentage
				var closestAheadCarIdx = -1;
				var closestBehindCarIdx = -1;

				var closestAheadLapDistPct = 0f;
				var closestBehindLapDistPct = 0f;

				Drivers.DriverInfo? closestAheadDriver = null;
				Drivers.DriverInfo? closestBehindDriver = null;

				for ( var carIdx = 0; carIdx < IRacingSdkConst.MaxNumCars; carIdx++ )
				{
					// exclude player's own car
					if ( carIdx == app.Simulator.PlayerCarIdx )
					{
						continue;
					}

					// get the driver for this car
					if ( !app.Drivers.TryGetDriverByCarIdx( carIdx, out var driver ) || ( driver is null ) )
					{
						continue;
					}

					// ignore the pace car and spectators and cars on pit road
					if ( driver.IsSpectator || driver.CarIsPaceCar || app.Simulator.CarIdxOnPitRoad[ carIdx ] )
					{
						continue;
					}

					// make lapDistPct relative to the player's lap distance percentage
					var lapDistPct = app.Simulator.CarIdxLapDistPct[ carIdx ] - app.Simulator.LapDistPct;

					if ( lapDistPct < -0.5f )
					{
						lapDistPct += 1f;
					}
					else if ( lapDistPct > 0.5f )
					{
						lapDistPct -= 1f;
					}

					if ( lapDistPct > 0f ) // this car is ahead
					{
						if ( ( closestAheadCarIdx == -1 ) || ( lapDistPct < closestAheadLapDistPct ) )
						{
							closestAheadCarIdx = carIdx;
							closestAheadLapDistPct = lapDistPct;
							closestAheadDriver = driver;
						}
					}
					else if ( lapDistPct < 0f ) // this car is behind
					{
						if ( ( closestBehindCarIdx == -1 ) || ( lapDistPct > closestBehindLapDistPct ) )
						{
							closestBehindCarIdx = carIdx;
							closestBehindLapDistPct = lapDistPct;
							closestBehindDriver = driver;
						}
					}
				}

				// fill out car and driver info
				if ( ( closestAheadCarIdx != -1 ) && ( closestAheadDriver is not null ) )
				{
					FrontCarNumberText.Text = $"#{closestAheadDriver.CarNumber}";
					FrontIRatingText.Text = closestAheadDriver.IRating.ToString();
					FrontDriverText.Text = closestAheadDriver.UserName;

					if ( app.Simulator.CarIdxPosition[ closestAheadCarIdx ] > 0 )
					{
						FrontPositionText.Text = $"P{app.Simulator.CarIdxPosition[ closestAheadCarIdx ]}";
					}
					else
					{
						FrontPositionText.Text = string.Empty;
					}

					// color code the position text
					var playerLap = app.Simulator.Lap;
					var otherLap = app.Simulator.CarIdxLap[ closestAheadCarIdx ];

					if ( ( playerLap == 0 ) || ( otherLap == 0 ) )
					{
						FrontPositionText.Foreground = Brushes.White;
					}
					else
					{
						var playerCombined = playerLap + app.Simulator.LapDistPct;
						var otherCombined = otherLap + app.Simulator.CarIdxLapDistPct[ closestAheadCarIdx ];
						var diff = otherCombined - playerCombined;

						if ( ( diff >= -0.5f ) && ( diff <= 0.5f ) )
						{
							FrontPositionText.Foreground = Brushes.White;
						}
						else if ( diff > 0.5f )
						{
							FrontPositionText.Foreground = Brushes.Red;
						}
						else
						{
							FrontPositionText.Foreground = Brushes.LightBlue;
						}
					}
				}
				else
				{
					FrontCarNumberText.Text = string.Empty;
					FrontPositionText.Text = string.Empty;
					FrontIRatingText.Text = string.Empty;
					FrontDriverText.Text = string.Empty;
				}

				if ( ( closestBehindCarIdx != -1 ) && ( closestBehindDriver is not null ) )
				{
					BackCarNumberText.Text = $"#{closestBehindDriver.CarNumber}";
					BackIRatingText.Text = closestBehindDriver.IRating.ToString();
					BackDriverText.Text = closestBehindDriver.UserName;

					if ( app.Simulator.CarIdxPosition[ closestBehindCarIdx ] > 0 )
					{
						BackPositionText.Text = $"P{app.Simulator.CarIdxPosition[ closestBehindCarIdx ]}";
					}
					else
					{
						BackPositionText.Text = string.Empty;
					}

					// color code the position text
					var playerLap = app.Simulator.Lap;
					var otherLap = app.Simulator.CarIdxLap[ closestBehindCarIdx ];

					if ( ( playerLap == 0 ) || ( otherLap == 0 ) )
					{
						BackPositionText.Foreground = Brushes.White;
					}
					else
					{
						var playerCombined = playerLap + app.Simulator.LapDistPct;
						var otherCombined = otherLap + app.Simulator.CarIdxLapDistPct[ closestBehindCarIdx ];
						var diff = otherCombined - playerCombined;

						if ( ( diff >= -0.5f ) && ( diff <= 0.5f ) )
						{
							BackPositionText.Foreground = Brushes.White;
						}
						else if ( diff > 0.5f )
						{
							BackPositionText.Foreground = Brushes.Red;
						}
						else
						{
							BackPositionText.Foreground = Brushes.LightBlue;
						}
					}
				}
				else
				{
					BackCarNumberText.Text = string.Empty;
					BackPositionText.Text = string.Empty;
					BackIRatingText.Text = string.Empty;
					BackDriverText.Text = string.Empty;
				}

				// get the closest ahead car's times at the player's most recently passed marker and calculate the time difference
				var aheadIsInvalid = true;

				if ( closestAheadCarIdx != -1 )
				{
					if ( app.TimingMarkers.TryGetMarkerTimeAtLapPct( closestAheadCarIdx, app.Simulator.LapDistPct, out var aheadTime ) )
					{
						if ( app.TimingMarkers.TryGetMarkerTimeAtLapPct( app.Simulator.PlayerCarIdx, app.Simulator.LapDistPct, out var playerTime ) )
						{
							var timeDelta = playerTime - aheadTime;

							if ( timeDelta > 0 )
							{
								aheadIsInvalid = false;

								// display delta
								FrontDeltaText.Text = $"{timeDelta:F2}s";

								// compute trend (sec/sec) and color accordingly
								var deltaTextForeground = Brushes.White;

								if ( !double.IsNaN( _prevFrontDeltaTime ) )
								{
									var dt = app.Simulator.SessionTime - _prevFrontDeltaTime;

									if ( dt > 0 )
									{
										var rate = ( timeDelta - _prevFrontDelta ) / dt;

										// For the car ahead: closing (delta decreasing) => GREEN, opening => RED
										if ( Math.Abs( rate ) >= DeltaRateThreshold )
										{
											deltaTextForeground = ( rate < 0 ) ? Brushes.Green : Brushes.Red;
										}
									}
								}

								FrontDeltaText.Foreground = deltaTextForeground;

								_prevFrontDelta = timeDelta;
								_prevFrontDeltaTime = app.Simulator.SessionTime;
							}
						}
					}
				}

				if ( aheadIsInvalid )
				{
					FrontDeltaText.Text = string.Empty;

					_prevFrontDelta = double.NaN;
					_prevFrontDeltaTime = double.NaN;
				}

				// get the closest behind car's times at that car's most recently passed marker and calculate the time difference
				var behindIsInvalid = true;

				if ( closestBehindCarIdx != -1 )
				{
					if ( app.TimingMarkers.TryGetMarkerTimeAtLapPct( closestBehindCarIdx, app.Simulator.CarIdxLapDistPct[ closestBehindCarIdx ], out var behindTime ) )
					{
						if ( app.TimingMarkers.TryGetMarkerTimeAtLapPct( app.Simulator.PlayerCarIdx, app.Simulator.CarIdxLapDistPct[ closestBehindCarIdx ], out var playerTime ) )
						{
							var timeDelta = behindTime - playerTime;

							if ( timeDelta > 0 )
							{
								behindIsInvalid = false;

								// display delta
								BackDeltaText.Text = $"{timeDelta:F2}s";

								// compute trend (sec/sec) and color accordingly
								var deltaTextForeground = Brushes.White;

								if ( !double.IsNaN( _prevBackDeltaTime ) )
								{
									var dt = app.Simulator.SessionTime - _prevBackDeltaTime;

									if ( dt > 0 )
									{
										var rate = ( timeDelta - _prevBackDelta ) / dt;

										// For the car behind: closing (delta decreasing) => RED, opening => GREEN
										if ( Math.Abs( rate ) >= DeltaRateThreshold )
										{
											deltaTextForeground = ( rate < 0 ) ? Brushes.Red : Brushes.Green;
										}
									}
								}

								BackDeltaText.Foreground = deltaTextForeground;

								_prevBackDelta = timeDelta;
								_prevBackDeltaTime = app.Simulator.SessionTime;
							}
						}
					}
				}

				if ( behindIsInvalid )
				{
					BackDeltaText.Text = string.Empty;

					_prevBackDelta = double.NaN;
					_prevBackDeltaTime = double.NaN;
				}
			}
		}
	}
}
