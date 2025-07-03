
using MarvinsAIRARefactored.Classes;

namespace MarvinsAIRARefactored.Components;

public class Sounds
{
	public enum SoundEffectType : int
	{
		ABSEngaged,
		WheelLock,
		WheelSpin
	}

	public class SoundEffect( string SoundKey, Func<float> volumeProvider, Func<float> frequencyRatioProvider )
	{
		public string SoundKey { get; set; } = SoundKey;
		public bool IsPlaying { get; set; } = false;
		public bool ShouldBePlaying { get; set; } = false;
		public Func<float> GetVolume { get; set; } = volumeProvider;
		public Func<float> GetFrequencyRatio { get; set; } = frequencyRatioProvider;
		public float Volume { get; set; } = 0f;
	}

	private readonly Dictionary<SoundEffectType, SoundEffect> _soundEffects = new() {
		{ SoundEffectType.ABSEngaged, new SoundEffect( "abs_engaged", () => DataContext.DataContext.Instance.Settings.SoundsABSEngagedVolume, () => DataContext.DataContext.Instance.Settings.SoundsABSEngagedFrequencyRatio ) },
		{ SoundEffectType.WheelLock, new SoundEffect( "wheel_lock", () => DataContext.DataContext.Instance.Settings.SoundsWheelLockVolume, () => DataContext.DataContext.Instance.Settings.SoundsWheelLockFrequencyRatio ) },
		{ SoundEffectType.WheelSpin, new SoundEffect( "wheel_spin", () => DataContext.DataContext.Instance.Settings.SoundsWheelSpinVolume, () => DataContext.DataContext.Instance.Settings.SoundsWheelSpinFrequencyRatio ) }
	};

	private SoundEffectType? _testSoundEffectType = null;
	private int _testSoundCounter = 0;

	public void Initialize()
	{
		var app = App.Instance!;

		foreach ( var keyValuePair in _soundEffects )
		{
			app.AudioManager.LoadSound( "Effects", keyValuePair.Value.SoundKey );
		}
	}

	public void Test( SoundEffectType soundEffectType )
	{
		_testSoundEffectType = soundEffectType;
		_testSoundCounter = 60;
	}

	public void Tick( App app )
	{
		// shortcut to settings

		var settings = DataContext.DataContext.Instance.Settings;

		// reset sound effects

		foreach ( var keyValuePair in _soundEffects )
		{
			if ( keyValuePair.Key == _testSoundEffectType )
			{
				keyValuePair.Value.ShouldBePlaying = true;
				keyValuePair.Value.Volume = 1f;
			}
			else
			{
				keyValuePair.Value.ShouldBePlaying = false;
				keyValuePair.Value.Volume = 0f;
			}
		}

		// abs engaged

		if ( settings.SoundsABSEngagedEnabled )
		{
			if ( app.Simulator.BrakeABSactive )
			{
				_soundEffects[ SoundEffectType.ABSEngaged ].ShouldBePlaying = true;
				_soundEffects[ SoundEffectType.ABSEngaged ].Volume = ( settings.SoundsABSEngagedFadeWithBrake ) ? ( app.Simulator.Brake * 0.75f + 0.25f ) : 1f;
			}
		}

		// wheel lock

		if ( settings.SoundsWheelLockEnabled )
		{
			if ( ( app.Simulator.CurrentRpmSpeedRatio > 0f ) && ( app.Simulator.Gear > 0 ) )
			{
				var adjustedRpmSpeedRatio = app.Simulator.RPMSpeedRatios[ app.Simulator.Gear ] * settings.SoundsWheelLockSensitivity;
				var difference = app.Simulator.CurrentRpmSpeedRatio - adjustedRpmSpeedRatio;
				var differencePct = difference / adjustedRpmSpeedRatio;

				if ( differencePct > 0.05f )
				{
					_soundEffects[ SoundEffectType.WheelLock ].ShouldBePlaying = true;
					_soundEffects[ SoundEffectType.WheelLock ].Volume = ( settings.SoundsWheelLockFadeWithBrake ) ? ( app.Simulator.Brake * 0.75f + 0.25f ) : 1f;
				}
			}
		}

		// wheel spin

		if ( settings.SoundsWheelSpinEnabled )
		{
			if ( ( app.Simulator.CurrentRpmSpeedRatio > 0f ) && ( app.Simulator.Gear > 0 ) )
			{
				var adjustedRpmSpeedRatio = app.Simulator.RPMSpeedRatios[ app.Simulator.Gear ] * settings.SoundsWheelSpinSensitivity;
				var difference = adjustedRpmSpeedRatio - app.Simulator.CurrentRpmSpeedRatio;
				var differencePct = difference / adjustedRpmSpeedRatio;

				if ( differencePct > 0.05f )
				{
					_soundEffects[ SoundEffectType.WheelSpin ].ShouldBePlaying = true;
					_soundEffects[ SoundEffectType.WheelSpin ].Volume = ( settings.SoundsWheelSpinFadeWithThrottle ) ? ( app.Simulator.Throttle * 0.75f + 0.25f ) : 1f;
				}
			}
		}

		// update test sound counter and effect type

		if ( _testSoundCounter > 0 )
		{
			_testSoundCounter--;

			if ( _testSoundCounter == 0 )
			{
				_testSoundEffectType = null;
			}
		}

		// play sounds that should be playing and stop sounds that should not be playing

		foreach ( var keyValuePair in _soundEffects )
		{
			var soundEffect = keyValuePair.Value;

			if ( soundEffect.ShouldBePlaying )
			{
				float finalVolume = soundEffect.Volume * soundEffect.GetVolume();

				if ( !soundEffect.IsPlaying )
				{
					soundEffect.IsPlaying = true;

					app.AudioManager.Play( soundEffect.SoundKey, finalVolume, soundEffect.GetFrequencyRatio(), true );
				}
				else
				{
					app.AudioManager.Update( soundEffect.SoundKey, finalVolume, soundEffect.GetFrequencyRatio() );
				}
			}
			else
			{
				if ( soundEffect.IsPlaying )
				{
					soundEffect.IsPlaying = false;

					app.AudioManager.Stop( soundEffect.SoundKey );
				}
			}
		}
	}
}
