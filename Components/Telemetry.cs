
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace MarvinsAIRARefactored.Components;

public class Telemetry
{
	[StructLayout( LayoutKind.Sequential, Pack = 4 )]
	public struct DataStruct
	{
		public int version;
		public int tickCount;

		public float racingWheelStrength;
		public float racingWheelMaxForce;

		public float racingWheelOutputTorque;
		public bool racingWheelOutputTorqueIsClipping;
		public bool racingWheelCrashProtectionIsActive;
		public bool racingWheelCurbProtectionIsActive;
		public bool racingWheelIsFading;

		public float steeringEffectsUndersteerEffect;
		public float steeringEffectsOversteerEffect;
		public float steeringEffectsSkidSlip;

		public float pedalsClutchFrequency;
		public float pedalsClutchAmplitude;

		public float pedalsBrakeFrequency;
		public float pedalsBrakeAmplitude;

		public float pedalsThrottleFrequency;
		public float pedalsThrottleAmplitude;
	}

	public DataStruct Data = new();

	private const string MemoryMappedFileName = "Local\\MAIRARefactoredTelemetry";

	private MemoryMappedFile? _memoryMappedFile = null;
	private MemoryMappedViewAccessor? _memoryMappedFileViewAccessor = null;

	public void Initialize()
	{
		var app = App.Instance!;

		app.Logger.WriteLine( "[Telemetry] Initialize >>>" );

		var sizeOfTelemetryData = Marshal.SizeOf( typeof( DataStruct ) );

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
		var settings = DataContext.DataContext.Instance.Settings;

		Data.version = 1;
		Data.tickCount++;

		Data.racingWheelStrength = settings.RacingWheelStrength;
		Data.racingWheelMaxForce = settings.RacingWheelMaxForce;

		_memoryMappedFileViewAccessor?.Write( 0, ref Data );
	}
}
