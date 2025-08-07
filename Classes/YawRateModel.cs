
using System.IO;

namespace MarvinsAIRARefactored.Classes;

using MarvinsAIRARefactored.Components;

public class YawRateModel( int[] steeringWheelAnglesInDegrees, float[,] yawRateDataInDegrees, int maxSpeed )
{
	private const int MinStartingMagnitude = 150;
	private const float MaxYawPredictionError = 1.5f;

	private readonly int[] _steeringWheelAnglesInDegrees = steeringWheelAnglesInDegrees;
	private readonly float[,] _yawRateDataInDegrees = yawRateDataInDegrees;
	private readonly int _maxSpeed = maxSpeed;

	public (Func<float, float> yawRatePredictor, Func<float, float> speedPredictor, int shallowestSteeringWheelAngle) FitWithProgressiveRefinement()
	{
		var usedAngles = new List<float>();
		var usedMaxYawRates = new List<float>();
		var usedCorrespondingSpeeds = new List<float>();

		var initialAngles = GetSortedAngles().Where( a => Math.Abs( a ) >= MinStartingMagnitude ).ToList();

		foreach ( var angle in initialAngles )
		{
			var (maxYawRate, correspondingSpeed) = GetMaxYawRateAtAngle( angle );

			if ( correspondingSpeed >= 0 )
			{
				usedAngles.Add( angle );
				usedMaxYawRates.Add( maxYawRate );
				usedCorrespondingSpeeds.Add( correspondingSpeed );
			}
		}

		var remainingAngles = GetSortedAngles().Where( a => Math.Abs( a ) < MinStartingMagnitude ).ToList();

		foreach ( var angle in remainingAngles )
		{
			var yawRatePredictor = FitSpline( [ .. usedAngles ], [ .. usedMaxYawRates ] );
			var expectedYawRate = yawRatePredictor( angle );
			var yawRatePeakCandidates = GetYawRatePeaksAtAngle( angle, out var speeds );

			if ( yawRatePeakCandidates.Count > 0 )
			{
				var bestCandidateIndex = 0;
				var bestCandidateError = float.MaxValue;

				for ( var candidateIndex = 0; candidateIndex < yawRatePeakCandidates.Count; candidateIndex++ )
				{
					var candidateError = MathF.Abs( yawRatePeakCandidates[ candidateIndex ] - expectedYawRate );

					if ( candidateError < bestCandidateError )
					{
						bestCandidateError = candidateError;
						bestCandidateIndex = candidateIndex;
					}
				}

				if ( bestCandidateError <= MaxYawPredictionError )
				{
					usedAngles.Add( angle );
					usedMaxYawRates.Add( yawRatePeakCandidates[ bestCandidateIndex ] );
					usedCorrespondingSpeeds.Add( speeds[ bestCandidateIndex ] );
				}
			}
		}

		var finalYawRatePredictor = FitSpline( [ .. usedAngles ], [ .. usedMaxYawRates ] );
		var finalSpeedPredictor = FitSpline( [ .. usedAngles ], [ .. usedCorrespondingSpeeds ] );

		// write to debug file

		var filePath = Path.Combine( SteeringEffects.CalibrationDirectory, $"debug_sampled_yaw_rates.csv" );

		using var writer = new StreamWriter( filePath );

		writer.WriteLine( "Steering Wheel Angle,Max Yaw Rate,Corresponding Speed" );

		for ( var i = 0; i < usedAngles.Count; i++ )
		{
			writer.WriteLine( $"{usedAngles[ i ]:F0},{usedMaxYawRates[ i ]:F6},{usedCorrespondingSpeeds[ i ]:F0}" );
		}

		return (finalYawRatePredictor, finalSpeedPredictor, (int) usedAngles.Last());
	}

	private List<int> GetSortedAngles()
	{
		return [ .. _steeringWheelAnglesInDegrees.Where( angle => angle < 0 ).OrderBy( angle => Math.Abs( angle ) ).Reverse() ];
	}

	private (float yaw, int speed) GetMaxYawRateAtAngle( int angle )
	{
		var angleIndex = Array.IndexOf( _steeringWheelAnglesInDegrees, angle );

		if ( angleIndex < 0 )
		{
			return (0f, -1);
		}

		var maxYaw = float.MinValue;
		var speedAtMax = -1;

		for ( var speed = 0; speed <= _maxSpeed; speed++ )
		{
			var yaw = _yawRateDataInDegrees[ angleIndex, speed ];

			if ( yaw > maxYaw )
			{
				maxYaw = yaw;
				speedAtMax = speed;
			}
		}

		return (maxYaw, speedAtMax);
	}

	private List<float> GetYawRatePeaksAtAngle( int angle, out List<int> speeds )
	{
		speeds = [];

		var peaks = new List<float>();
		var angleIndex = Array.IndexOf( _steeringWheelAnglesInDegrees, angle );

		if ( angleIndex < 0 )
		{
			return peaks;
		}

		for ( var speed = 1; speed < _maxSpeed - 1; speed++ )
		{
			var prev = _yawRateDataInDegrees[ angleIndex, speed - 1 ];
			var curr = _yawRateDataInDegrees[ angleIndex, speed ];
			var next = _yawRateDataInDegrees[ angleIndex, speed + 1 ];

			if ( ( curr > prev ) && ( curr > next ) )
			{
				peaks.Add( curr );
				speeds.Add( speed );
			}
		}

		return peaks;
	}

	public static Func<float, float> FitSpline( float[] x, float[] y )
	{
		if ( x.Length < 3 )
		{
			throw new ArgumentException( "Need at least three points for cubic spline." );
		}

		var spline = new CubicSpline( x, y );

		return spline.Interpolate;
	}
}
