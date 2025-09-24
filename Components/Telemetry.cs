
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace MarvinsAIRARefactored.Components;

public class Telemetry
{
	private const string MemoryMappedFileName = "Local\\MAIRARefactoredTelemetry";
	private const int MaxStringLengthInBytes = 256;

	[StructLayout( LayoutKind.Sequential, Pack = 4 )]
	public unsafe struct DataBufferStruct
	{
		public int tickCount;

		public float racingWheelStrength;
		public float racingWheelMaxForce;

		public float racingWheelAutoTorque;

		public int racingWheelAlgorithm;
		public fixed byte racingWheelAlgorithmName[ MaxStringLengthInBytes ];

		public fixed float racingWheelAlgorithmSettings[ 5 ];
		public fixed byte racingWheelAlgorithmSettingNames[ 5 * MaxStringLengthInBytes ];
		public fixed byte racingWheelAlgorithmSettingValues[ 5 * MaxStringLengthInBytes ];

		public float racingWheelOutputTorque;
		public bool racingWheelOutputTorqueIsClipping;

		public bool racingWheelCrashProtectionIsActive;
		public bool racingWheelCurbProtectionIsActive;

		public bool racingWheelFadingIsActive;

		public float steeringEffectsUndersteerEffect;
		public float steeringEffectsOversteerEffect;
		public float steeringEffectsSkidSlip;

		public float pedalsClutchFrequency;
		public float pedalsClutchAmplitude;

		public float pedalsBrakeFrequency;
		public float pedalsBrakeAmplitude;

		public float pedalsThrottleFrequency;
		public float pedalsThrottleAmplitude;

		public void SetAlgorithmName( string? value )
		{
			fixed ( byte* bytePtr = racingWheelAlgorithmName )
			{
				WriteString( bytePtr, 0, MaxStringLengthInBytes, value );
			}
		}

		public void SetAlgorithmSettingName( int index, string? value )
		{
			if ( index < 0 || index >= 5 ) return;

			fixed ( byte* bytePtr = racingWheelAlgorithmSettingNames )
			{
				WriteString( bytePtr, index, MaxStringLengthInBytes, value );
			}
		}

		public void SetAlgorithmSettingValue( int index, string? value )
		{
			if ( index < 0 || index >= 5 ) return;

			fixed ( byte* bytePtr = racingWheelAlgorithmSettingValues )
			{
				WriteString( bytePtr, index, MaxStringLengthInBytes, value );
			}
		}

		public static unsafe void WriteString( byte* bytePtr, int index, int capacity, string? value )
		{
			if ( bytePtr == null || capacity <= 0 ) return;

			var offset = index * capacity;

			if ( string.IsNullOrEmpty( value ) )
			{
				bytePtr[ offset ] = 0;
				return;
			}

			var bytes = Encoding.UTF8.GetBytes( value );

			var length = Math.Min( bytes.Length, capacity - 1 );

			Marshal.Copy( bytes, 0, (IntPtr) bytePtr + offset, length );

			bytePtr[ offset + length ] = 0;
		}
	}

	[StructLayout( LayoutKind.Sequential, Pack = 4 )]
	public unsafe struct DataStruct
	{
		public int version;
		public int bufferIndex;

		public DataBufferStruct buffer0;
		public DataBufferStruct buffer1;
		public DataBufferStruct buffer2;

		public static ref DataBufferStruct GetDataBuffer( ref DataStruct dataStruct, int index )
		{
			switch ( index )
			{
				case 0: return ref dataStruct.buffer0;
				case 1: return ref dataStruct.buffer1;
				default: return ref dataStruct.buffer2;
			}
		}
	}

	private DataStruct _data = new();
	private int _currentBufferIndex = 0;

	private MemoryMappedFile? _memoryMappedFile = null;
	private MemoryMappedViewAccessor? _memoryMappedFileViewAccessor = null;

	public void Initialize()
	{
		var app = App.Instance!;

		app.Logger.WriteLine( "[Telemetry] Initialize >>>" );

		var sizeOfTelemetryData = Marshal.SizeOf<DataStruct>();

		_memoryMappedFile = MemoryMappedFile.CreateOrOpen( MemoryMappedFileName, sizeOfTelemetryData );
		_memoryMappedFileViewAccessor = _memoryMappedFile.CreateViewAccessor();

		app.Logger.WriteLine( "[Telemetry] <<< Initialize" );
	}

	public void Shutdown()
	{
		var app = App.Instance!;

		app.Logger.WriteLine( "[Telemetry] Shutdown >>>" );

		_memoryMappedFileViewAccessor = null;
		_memoryMappedFile = null;

		app.Logger.WriteLine( "[Telemetry] <<< Shutdown" );
	}

	public void Tick( App app )
	{
		var localization = DataContext.DataContext.Instance.Localization;
		var settings = DataContext.DataContext.Instance.Settings;

		// get the buffer to write to

		_currentBufferIndex = ( _currentBufferIndex + 1 ) % 3;

		ref var dataBuffer = ref DataStruct.GetDataBuffer( ref _data, _currentBufferIndex );

		// update the buffer

		dataBuffer.tickCount++;

		// racing wheel

		dataBuffer.racingWheelStrength = settings.RacingWheelStrength;
		dataBuffer.racingWheelMaxForce = settings.RacingWheelMaxForce;

		dataBuffer.racingWheelAutoTorque = app.RacingWheel.AutoTorque;

		dataBuffer.racingWheelAlgorithm = (int) settings.RacingWheelAlgorithm;
		dataBuffer.SetAlgorithmName( localization[ settings.RacingWheelAlgorithm.ToString() ] );

		unsafe
		{
			for ( var index = 0; index < 5; index++ )
			{
				dataBuffer.racingWheelAlgorithmSettings[ index ] = 0f;

				dataBuffer.SetAlgorithmSettingName( index, null );
				dataBuffer.SetAlgorithmSettingValue( index, null );
			}

			switch ( settings.RacingWheelAlgorithm )
			{
				case RacingWheel.Algorithm.DetailBooster:
				case RacingWheel.Algorithm.DetailBoosterOn60Hz:

					dataBuffer.racingWheelAlgorithmSettings[ 0 ] = settings.RacingWheelDetailBoost;
					dataBuffer.racingWheelAlgorithmSettings[ 1 ] = settings.RacingWheelDetailBoostBias;

					dataBuffer.SetAlgorithmSettingName( 0, localization[ "DetailBoost" ] );
					dataBuffer.SetAlgorithmSettingName( 1, localization[ "DetailBoostBias" ] );

					dataBuffer.SetAlgorithmSettingValue( 0, settings.RacingWheelDetailBoostString );
					dataBuffer.SetAlgorithmSettingValue( 1, settings.RacingWheelDetailBoostBiasString );

					break;

				case RacingWheel.Algorithm.DeltaLimiter:
				case RacingWheel.Algorithm.DeltaLimiterOn60Hz:

					dataBuffer.racingWheelAlgorithmSettings[ 0 ] = settings.RacingWheelDeltaLimit;
					dataBuffer.racingWheelAlgorithmSettings[ 1 ] = settings.RacingWheelDeltaLimiterBias;

					dataBuffer.SetAlgorithmSettingName( 0, localization[ "DeltaLimit" ] );
					dataBuffer.SetAlgorithmSettingName( 1, localization[ "DeltaLimiterBias" ] );

					dataBuffer.SetAlgorithmSettingValue( 0, settings.RacingWheelDeltaLimitString );
					dataBuffer.SetAlgorithmSettingValue( 1, settings.RacingWheelDeltaLimiterBiasString );

					break;

				case RacingWheel.Algorithm.SlewAndTotalCompression:

					dataBuffer.racingWheelAlgorithmSettings[ 0 ] = settings.RacingWheelEnableSoftLimiter ? 1f : 0f;
					dataBuffer.racingWheelAlgorithmSettings[ 1 ] = settings.RacingWheelSlewCompressionThreshold;
					dataBuffer.racingWheelAlgorithmSettings[ 2 ] = settings.RacingWheelSlewCompressionRate;
					dataBuffer.racingWheelAlgorithmSettings[ 3 ] = settings.RacingWheelTotalCompressionThreshold;
					dataBuffer.racingWheelAlgorithmSettings[ 4 ] = settings.RacingWheelTotalCompressionRate;

					dataBuffer.SetAlgorithmSettingName( 0, localization[ "SoftClipping" ] );
					dataBuffer.SetAlgorithmSettingName( 1, localization[ "SlewCompressionThreshold" ] );
					dataBuffer.SetAlgorithmSettingName( 2, localization[ "SlewCompressionRate" ] );
					dataBuffer.SetAlgorithmSettingName( 3, localization[ "TotalCompressionThreshold" ] );
					dataBuffer.SetAlgorithmSettingName( 4, localization[ "TotalCompressionRate" ] );

					dataBuffer.SetAlgorithmSettingValue( 0, settings.RacingWheelEnableSoftLimiter ? localization[ "ON" ] : localization[ "OFF" ] );
					dataBuffer.SetAlgorithmSettingValue( 1, settings.RacingWheelSlewCompressionThresholdString );
					dataBuffer.SetAlgorithmSettingValue( 2, settings.RacingWheelSlewCompressionRateString );
					dataBuffer.SetAlgorithmSettingValue( 3, settings.RacingWheelTotalCompressionThresholdString );
					dataBuffer.SetAlgorithmSettingValue( 4, settings.RacingWheelTotalCompressionRateString );

					break;

				case RacingWheel.Algorithm.MultiAdjustmentToolkit:

					dataBuffer.racingWheelAlgorithmSettings[ 0 ] = settings.RacingWheelEnableMultiSoftLimiter ? 1f : 0f;
					dataBuffer.racingWheelAlgorithmSettings[ 1 ] = settings.RacingWheelMultiTorqueCompression;
					dataBuffer.racingWheelAlgorithmSettings[ 2 ] = settings.RacingWheelMultiSlewRateReduction;
					dataBuffer.racingWheelAlgorithmSettings[ 3 ] = settings.RacingWheelMultiDetailGain;
					dataBuffer.racingWheelAlgorithmSettings[ 4 ] = settings.RacingWheelMultiOutputSmoothing;

					dataBuffer.SetAlgorithmSettingName( 0, localization[ "SoftClipping" ] );
					dataBuffer.SetAlgorithmSettingName( 1, localization[ "TorqueCompression" ] );
					dataBuffer.SetAlgorithmSettingName( 2, localization[ "SlewRateReduction" ] );
					dataBuffer.SetAlgorithmSettingName( 3, localization[ "DetailGain" ] );
					dataBuffer.SetAlgorithmSettingName( 4, localization[ "OutputSmoothing" ] );

					dataBuffer.SetAlgorithmSettingValue( 0, settings.RacingWheelEnableMultiSoftLimiter ? localization[ "ON" ] : localization[ "OFF" ] );
					dataBuffer.SetAlgorithmSettingValue( 1, settings.RacingWheelMultiTorqueCompressionString );
					dataBuffer.SetAlgorithmSettingValue( 2, settings.RacingWheelMultiSlewRateReductionString );
					dataBuffer.SetAlgorithmSettingValue( 3, settings.RacingWheelMultiDetailGainString );
					dataBuffer.SetAlgorithmSettingValue( 4, settings.RacingWheelMultiOutputSmoothingString );

					break;
			}
		}

		dataBuffer.racingWheelOutputTorque = app.RacingWheel.OutputTorque;
		dataBuffer.racingWheelOutputTorqueIsClipping = ( app.RacingWheel.OutputTorque < -1f ) || ( app.RacingWheel.OutputTorque > 1f );

		dataBuffer.racingWheelCrashProtectionIsActive = app.RacingWheel.CrashProtectionIsActive;
		dataBuffer.racingWheelCurbProtectionIsActive = app.RacingWheel.CurbProtectionIsActive;

		dataBuffer.racingWheelFadingIsActive = app.RacingWheel.FadingIsActive;

		// pedals

		dataBuffer.pedalsClutchFrequency = app.Pedals.ClutchFrequency;
		dataBuffer.pedalsClutchAmplitude = app.Pedals.ClutchAmplitude;

		dataBuffer.pedalsBrakeFrequency = app.Pedals.BrakeFrequency;
		dataBuffer.pedalsBrakeAmplitude = app.Pedals.BrakeAmplitude;

		dataBuffer.pedalsThrottleFrequency = app.Pedals.ThrottleFrequency;
		dataBuffer.pedalsThrottleAmplitude = app.Pedals.ThrottleAmplitude;

		// steering effects

		dataBuffer.steeringEffectsUndersteerEffect = app.SteeringEffects.UndersteerEffect;
		dataBuffer.steeringEffectsOversteerEffect = app.SteeringEffects.OversteerEffect;
		dataBuffer.steeringEffectsSkidSlip = app.SteeringEffects.SkidSlip;

		// let SimHub know this buffer is ready for reading

		_data.version = 2;
		_data.bufferIndex = _currentBufferIndex;

		_memoryMappedFileViewAccessor?.Write( 0, ref _data );
	}
}
