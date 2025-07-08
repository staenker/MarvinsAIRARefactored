
using System.IO;

namespace MarvinsAIRARefactored.Components;

public class SteeringEffects
{
	private const float RadiansToDegrees = 180f / MathF.PI;

	private const int SteeringWheelAngleInterval = 30;
	private const int MaxSteeringWheelAngle = 180;
	private const int MaxSpeedInMPS = 90;
	private const int NumSteeringWheelAngles = MaxSteeringWheelAngle * 2 / SteeringWheelAngleInterval + 1;

	private readonly string _recordingsDirectory = Path.Combine( App.DocumentsFolder, "Recordings" );

	private bool _isCalibrating = false;
	private bool _isStopping = false;

	private int _initialSteeringWheelAngle = 0;

	private int _targetSteeringWheelAngle = 0;
	private int _targetSpeedInMPS = 0;
	private float _targetThrottle = 0f;

	private float _lastFrameVelocityX = 0f;
	private float _throttleFade = 0f;
	private float _maxYawRate = 0f;
	private float _settleTimer = 0f;

	private int _numSteeringWheelAngles = 0;

	private readonly float[,] _yawRateData = new float[ NumSteeringWheelAngles, MaxSpeedInMPS + 1 ];

	public void RunCalibration()
	{
		var app = App.Instance!;

		// set initial virtual joystick parameters

		app.VirtualJoystick.SteeringWheelAngle = 0f;
		app.VirtualJoystick.Throttle = 0f;
		app.VirtualJoystick.Brake = 0f;

		// set initial targets

		_initialSteeringWheelAngle = (int) -MathF.Min( MaxSteeringWheelAngle, MathF.Floor( app.Simulator.SteeringWheelAngleMax * RadiansToDegrees * 0.5f / 10 ) * 10 );

		_targetSteeringWheelAngle = _initialSteeringWheelAngle;
		_targetSpeedInMPS = 1;
		_targetThrottle = 0f;

		// clear last frame velocity and throttle fade

		_lastFrameVelocityX = 0f;
		_throttleFade = 0f;
		_maxYawRate = 0f;
		_settleTimer = 0f;

		// reset the number of steering wheel angles we have recorded

		_numSteeringWheelAngles = 0;

		// clear out our old yaw rate data

		Array.Clear( _yawRateData );

		// start the calibration process

		_isCalibrating = true;
	}

	public void StopCalibration()
	{
		// whoa, nelly!

		_isCalibrating = false;
		_isStopping = true;

		// save the data

		SaveRecording();
	}

	public void Tick( App app )
	{
		if ( _isCalibrating )
		{
			// give us time to settle down before starting the next pass

			_settleTimer += 0.01f;

			if ( _settleTimer > 2f )
			{
				// figure out if we have crashed

				var crashed = app.Simulator.GForce >= 2f;

				// set steering wheel angle

				app.VirtualJoystick.SteeringWheelAngle = _targetSteeringWheelAngle / 450f;

				// fade in the throttle

				_throttleFade = MathF.Min( 1f, _throttleFade + 0.01f );

				// if we aren't gaining speed, increase the throttle a hair

				var speedDelta = app.Simulator.VelocityX - _lastFrameVelocityX;

				_lastFrameVelocityX = app.Simulator.VelocityX;

				if ( ( _throttleFade == 1f ) && ( speedDelta <= 0.005f ) )
				{
					_targetThrottle += 0.0005f;
				}

				// update the virtual joystick throttle

				app.VirtualJoystick.Throttle = _targetThrottle * _throttleFade;

				// shift up if we are in neutral or near shift RPM

				if ( ( app.Simulator.Gear == 0 ) || ( app.Simulator.RPM >= app.Simulator.ShiftLightsShiftRPM * 0.75f ) )
				{
					app.VirtualJoystick.ShiftUp = true;

					_throttleFade = 0f;
				}

				// check if we've reached our target speed

				if ( !crashed && ( app.Simulator.VelocityX >= _targetSpeedInMPS ) )
				{
					// yes - save the data

					_yawRateData[ _numSteeringWheelAngles, _targetSpeedInMPS ] = app.Simulator.YawRate;

					_maxYawRate = MathF.Max( _maxYawRate, MathF.Abs( app.Simulator.YawRate ) );

					// bump up the target speed

					_targetSpeedInMPS++;
				}

				// check if we are done with this pass

				if ( crashed || ( _targetSpeedInMPS > MaxSpeedInMPS ) || ( MathF.Abs( app.Simulator.VelocityY ) > 2f ) || ( MathF.Abs( app.Simulator.YawRate ) < ( _maxYawRate * 0.75f ) ) )
				{
					app.VirtualJoystick.SteeringWheelAngle = 0f;
					app.VirtualJoystick.Throttle = 0f;
					app.VirtualJoystick.Brake = 0f;

					// set next targets

					_targetSteeringWheelAngle += SteeringWheelAngleInterval;
					_targetSpeedInMPS = 1;
					_targetThrottle = 0f;

					// clear last frame velocity and throttle fade

					_lastFrameVelocityX = 0f;
					_throttleFade = 0f;
					_maxYawRate = 0f;
					_settleTimer = 0f;

					// fire active reset

					app.VirtualJoystick.ActiveResetRun = true;

					// increase num steering wheel angles recorded

					_numSteeringWheelAngles++;

					// stop calibrating when we get to the end

					if ( _numSteeringWheelAngles == NumSteeringWheelAngles )
					{
						StopCalibration();
					}
				}
			}
		}
		else if ( _isStopping )
		{
			app.VirtualJoystick.SteeringWheelAngle = 0f;
			app.VirtualJoystick.Throttle = 0f;
			app.VirtualJoystick.Brake = 1f;

			if ( app.Simulator.VelocityX <= 0.001f )
			{
				_isStopping = false;

				app.VirtualJoystick.Brake = 0f;
			}
		}

		app.Debug.Label_1 = $"[SteeringEffects] _isCalibrating = {_isCalibrating}, _isStopping = {_isStopping}";
		app.Debug.Label_2 = $"[SteeringEffects] _targetSteeringWheelAngle = {_targetSteeringWheelAngle}, _targetSpeedInMPS = {_targetSpeedInMPS}, _targetThrottle = {_targetThrottle:F4}";
		app.Debug.Label_3 = $"[SteeringEffects] _numSteeringWheelAngles = {_numSteeringWheelAngles}, _throttleFade = {_throttleFade:F2}, _maxYawRate = {_maxYawRate:F6}";
		app.Debug.Label_4 = $"[VirtualJoystick] SteeringWheelAngle = {app.VirtualJoystick.SteeringWheelAngle:F3}, Throttle = {app.VirtualJoystick.Throttle:F3}, Brake = {app.VirtualJoystick.Brake:F3}";
		app.Debug.Label_5 = $"[Simulator] Gear = {app.Simulator.Gear}, SteeringWheelAngle = {app.Simulator.SteeringWheelAngle * 180f / MathF.PI:F3}, YawRate = {app.Simulator.YawRate:F6}";
		app.Debug.Label_6 = $"[Simulator] VelocityX = {app.Simulator.VelocityX:F3}, VelocityY = {app.Simulator.VelocityY:F3}";
	}

	public void SaveRecording()
	{
		var app = App.Instance!;

		app.Logger.WriteLine( "[SteeringEffects] SaveRecording >>>" );

		// open file

		var filePath = Path.Combine( _recordingsDirectory, $"{app.Simulator.CarScreenName}.csv" );

		using var writer = new StreamWriter( filePath );

		// write car name

		writer.WriteLine( app.Simulator.CarScreenName );

		// write header row

		var steeringWheelAngle = _initialSteeringWheelAngle;

		var headerString = "Speed";

		for ( var i = 0; i < _numSteeringWheelAngles; i++ )
		{
			headerString += $",{steeringWheelAngle + i * SteeringWheelAngleInterval}";
		}

		writer.WriteLine( headerString );

		// write data rows

		for ( var j = 0; j <= MaxSpeedInMPS; j++ )
		{
			var dataString = $"{j}";

			for ( var i = 0; i < _numSteeringWheelAngles; i++ )
			{
				dataString += $",{_yawRateData[ i, j ]:F6}";
			}

			writer.WriteLine( dataString );
		}

		app.Logger.WriteLine( "[SteeringEffects] <<< SaveRecording" );
	}
}
