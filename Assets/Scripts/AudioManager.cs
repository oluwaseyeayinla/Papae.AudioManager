using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Papae.UnitySDK.Managers
{
	#region External Structures

	/// <summary>
	/// What kind of music transition effect is going to take place
	/// </summary>
	public enum MusicTransition
	{
		/// <summary>
		/// (None) Immediately play the next one
		/// </summary>
		Swift,
		/// <summary>
		/// (In and Out) Fades out the current music then fades in the next one
		/// </summary>
		LinearFade,
		/// <summary>
		/// (No silent gaps) Smooth transition from current music to next
		/// </summary>
		CrossFade
	}

	/// <summary>
	/// Background music properties for the AudioManager
	/// </summary>
	[Serializable]
	public struct BackgroundMusic
	{
		/// <summary>
		/// The current clip of the background music.
		/// </summary>
		public AudioClip CurrentClip;
		/// <summary>
		/// The next clip that is about to be played.
		/// </summary>
		public AudioClip NextClip;
		/// <summary>
		/// The music transition.
		/// </summary>
		public MusicTransition MusicTransition;
		/// <summary>
		/// The duration of the transition.
		/// </summary>
		public float TransitionDuration;
	}

	/// <summary>
	/// Structure and properties for a sound effect
	/// </summary>
	[Serializable]
	public class SoundEffect : MonoBehaviour
	{
		// TODO :: consider making the Sound Effect multifaceted / multifunctional
		// meaning you can add a sound effect as a monobehaviour to do other functions
		// like allow a sound effect play a sound or respond to events
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float originalVolume;
        [SerializeField] private float duration;
        [SerializeField] private float playbackPosition;
        [SerializeField] private float time;
        [SerializeField] private Action callback;
        [SerializeField] private bool singleton;

		/// <summary>
		/// Gets or sets the name of the sound effect.
		/// </summary>
		/// <value>The name.</value>
		public string Name
		{
			get { return audioSource.clip.name; }
		}

		/// <summary>
		/// Gets the length of the sound effect in seconds.
		/// </summary>
		/// <value>The length.</value>
		public float Length
		{
			get { return audioSource.clip.length; }
		}

		/// <summary>
		/// Gets the playback position in seconds.
		/// </summary>
		/// <value>The playback position.</value>
		public float PlaybackPosition
		{
			get { return audioSource.time; }
		}

		/// <summary>
		/// Gets or sets the source of the sound effect.
		/// </summary>
		/// <value>The source.</value>
		public AudioSource Source
		{
			get{ return audioSource; }
			set { audioSource = value; }
		}

		/// <summary>
		/// Gets or sets the original volume of the sound effect.
		/// </summary>
		/// <value>The original volume.</value>
		public float OriginalVolume
		{
			get{ return originalVolume; }
			set { originalVolume = value; }
		}

		/// <summary>
		/// Gets or sets the duration for the sound effect to play in seconds.
		/// </summary>
		/// <value>The duration.</value>
		public float Duration
		{
			get{ return duration; }
			set { duration = value; }
		}

		/// <summary>
		/// Gets or sets the time left or remaining for the sound effect to play in seconds.
		/// </summary>
		/// <value>The duration.</value>
		public float Time
		{
			get{ return time; }
			set { time = value; }
		}

		/// <summary>
		/// Gets the normalised time left for the sound effect to play.
		/// </summary>
		/// <value>The normalised time.</value>
		public float NormalisedTime
		{
			get{ return Time / Duration; }
		}

		/// <summary>
		/// Gets or sets the callback that would fire when the sound effect finishes playing.
		/// </summary>
		/// <value>The callback.</value>
		public Action Callback
		{
			get{ return callback; }
			set { callback = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Papae.UnitySDK.Managers.SoundEffect"/> is a singleton.
		/// Meaning that only one instance of the sound effect is ever allowed to be active.
		/// </summary>
		/// <value><c>true</c> if repeat; otherwise, <c>false</c>.</value>
		public bool Singleton
		{
			get{ return singleton; }
			set { singleton = value; }
		}
	}

	#endregion

	/// <summary>
	/// The manager of all things with regards to sound
	/// </summary>
	[RequireComponent(typeof(AudioSource))]
	public class AudioManager : MonoBehaviour
	{
		#region Inspector Variables

		[Header("Background Music Properties")]

		[Tooltip("Is the background music mute")]
		[SerializeField] bool _musicOn = true;

		[Tooltip("The background music volume")]
		[Range(0, 1)]
		[SerializeField] float _musicVolume = 1f;

		[Tooltip("Use the current music volume settings on initialisation start")]
		[SerializeField] bool _useMusicVolOnStart = false;

		[Tooltip("The target group for the background music to route its their signals. If none is to be used, then leave unassigned or blank")]
		[SerializeField] AudioMixerGroup _musicMixerGroup = null;

		[Tooltip("The exposed volume parameter name of the music mixer group")]
		[SerializeField] string _volumeOfMusicMixer = string.Empty;

		[Space(3)]

		[Header("Sound Effect Properties")]

		[Tooltip("The sound effects volume")]
		[SerializeField] bool _soundFxOn = true;

		[Tooltip("The sound effects volume")]
		[Range(0, 1)]
		[SerializeField] float _soundFxVolume = 1f;

		[Tooltip("Use the current sound effect volume settings on initialisation start")]
		[SerializeField] bool _useSfxVolOnStart = false;

		[Tooltip("The target group for the sound effects to route its their signals. If none is to be used, then leave unassigned or blank")]
		[SerializeField] AudioMixerGroup _soundFxMixerGroup = null;

		[Tooltip("The exposed volume parameter name of the sound effects mixer group")]
		[SerializeField] string _volumeOfSFXMixer = string.Empty;

		[Space(3)]

		[Tooltip("A list of all audio clips attached to the AudioManager")]
		[SerializeField] List<AudioClip> _playlist = new List<AudioClip>();
		// TOGO :: Try a Reorderable list for future implementation of the playlist
		#endregion

		#region Singleton Pattern

		// Singleton placeholder
		private static AudioManager instance;
		// Lock instance object for execution
		private static object key = new object();
		// Is application up and running
		private static bool alive = true;

		/// <summary>
		/// Current and only running instance of the AudioManager
		/// </summary>
		public static AudioManager Instance
		{
			// Singleton design pattern
			get
			{
				// Check if application is quitting and AudioManager is destroyed
				if (!alive)
				{
					Debug.LogWarning(typeof(AudioManager) + "' is already destroyed on application quit.");
					return null;
				}

				// Check if there is a saved instance
				if (instance == null)
				{
					// Find from the list or hierrachy
					instance = FindObjectOfType<AudioManager>();

					// If none exists in scene
					if (instance == null)
					{
						// Lock so can't be used by another thread until release. Useful if two AudioManager instances were created simultaneosly
						lock (key)
						{
							//GameObject clone = new GameObject();
							//clone.SetActive(false);
							//clone.AddComponent<AudioManager>();

							// Create a new gameobject and add the AudioManager component to the gameobject
							instance = new GameObject().AddComponent<AudioManager>();
						}
					}
				}

				return instance;
			}
		}

		/// <summary>
		/// Prevent calling the consructor of the AudioManager
		/// </summary>
		private AudioManager() { }

		#endregion

		#region Public Static Getters

		/// <summary>
		/// Gets the current music clip.
		/// </summary>
		/// <value>The current music clip.</value>
		public AudioClip CurrentMusicClip
		{
			get { return backgroundMusic.CurrentClip; }
		}

		/// <summary>
		/// Current list or pool of the sound effects
		/// </summary>
		public List<SoundEffect> SoundFxPool
		{
			get { return sfxPool; }
		}

		/// <summary>
		/// List of audio clips attached to the AudioManager
		/// </summary>
		public List<AudioClip> Playlist
		{
			get { return _playlist; }
		}

		/// <summary>
		/// Is the AudioManager processing any background music
		/// </summary>
		public bool IsMusicPlaying
		{
			get { return musicSource != null && musicSource.isPlaying; }
		}

		/// <summary>
		/// Gets or sets the music volume.
		/// </summary>
		/// <value>The music volume.</value>
		public float MusicVolume
		{
			get { return _musicVolume; }
			set { SetBGMVolume (value); }
		}

		/// <summary>
		/// Gets or sets the sound volume.
		/// </summary>
		/// <value>The sound volume.</value>
		public float SoundVolume
		{
			get { return _soundFxVolume; }
			set { SetSFXVolume (value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the music is on.
		/// </summary>
		/// <value><c>true</c> if this instance is music on; otherwise, <c>false</c>.</value>
		public bool IsMusicOn
		{
			get { return _musicOn; }
			set { ToggleBGMMute (value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the sound is on.
		/// </summary>
		/// <value><c>true</c> if this instance is sound on; otherwise, <c>false</c>.</value>
		public bool IsSoundOn
		{
			get { return _soundFxOn; }
			set { ToggleSFXMute (value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is master mute.
		/// </summary>
		/// <value><c>true</c> if this instance is master mute; otherwise, <c>false</c>.</value>
		public bool IsMasterMute
		{
			get { return !_musicOn && !_soundFxOn; }
			set { ToggleMute(value); }
		}

		#endregion

		#region Private Static Variables

		// Pool of sound effects managed by the AudioManager
		List<SoundEffect> sfxPool = new List<SoundEffect>();
		// Background music managed by the AudioManager
		static BackgroundMusic backgroundMusic;
		// Audio source for both the current music and the next music if crossfade transition is active 
		static AudioSource musicSource = null, crossfadeSource = null;
		// Volume placehlder properties for the current music, sound effect, and the vol limit of the current music
		static float currentMusicVol = 0, currentSfxVol = 0, musicVolCap = 0, savedPitch = 1f;
		// Status to detect when the sound effect mute status has changed
		static bool musicOn = false, sfxOn = false;
		// Timer countdown used by the Fade transitions
		static float transitionTime;

		// Player Prefabs store keys
		static readonly string BgMusicVolKey = "BGMVol";
		static readonly string SoundFxVolKey = "SFXVol";
		static readonly string BgMusicMuteKey = "BGMMute";
		static readonly string SoundFxMuteKey = "SFXMute";

		#endregion

		#region Initialisation Functions

		/// <summary>
		/// Inherited Monobehavior Function.
		/// </summary>
		void OnDestroy()
		{
			StopAllCoroutines();
			SaveAllPreferences();
		}

		/// <summary>
		/// When your application quits, it destroys objects in a random order.
		/// In principle, you shouldn't get to calll the AudioManager when your application quits or is quitting.
		/// If any script calls Instance after it has been destroyed, it will create a buggy ghost object that will stay on the Editor scene
		/// So, this was made to be sure we're not creating that buggy ghost object.
		/// </summary>
		void OnApplicationExit()
		{
			alive = false;
		}

		/// <summary>
		/// AudioManager initialisation tasks
		/// </summary>
		void Initialise()
		{
			// Set proper name of the gameobject
			gameObject.name = "AudioManager";

			// Initialises the sound options used by the AudioManager
			_musicOn = LoadBGMMuteStatus();
			_musicVolume = _useMusicVolOnStart ? _musicVolume : LoadBGMVolume();
			_soundFxOn = LoadSFXMuteStatus();
			_soundFxVolume = _useSfxVolOnStart ? _soundFxVolume : LoadSFXVolume();

			// Initialises the audio source used by the background music
			if (musicSource == null)
			{
				musicSource = gameObject.GetComponent<AudioSource>();
				// If none exists, create one and attach to AudioManager
				musicSource = musicSource ?? gameObject.AddComponent<AudioSource>();
			}

			musicSource = ConfigureAudioSource(musicSource);

			// Make object persist throughout lifetime of application (including in between scene transitions)
			DontDestroyOnLoad(this.gameObject);
		}

		/// <summary>
		/// Inherited Monobehavior Function.
		/// </summary>
		void Awake()
		{
			if (instance == null)
			{
				// Store the instance as a singleton 
				instance = this;
				// Initialise the AudioManager
				Initialise();
			}
			else if (instance != this)
			{
				// Get rid of any other instances if they exist. Only one instance is permitted or allowed
				Destroy(this.gameObject);
			}
		}

		/// <summary>
		/// Inherited Monobehavior Function.
		/// </summary>
		void Start()
		{
			if (musicSource != null)
			{
				// this is here because the mixer group float can't be set on awake
				// spent amost 4 hours trying to figure out why... well didn't know until I ran some tests 
				StartCoroutine(OnUpdate());
			}
		}

		/// <summary>
		/// Creates an audio source with 2D music settings based on some internal properties
		/// </summary>
		/// <returns>An AudioSource with 2D features</returns>
		AudioSource ConfigureAudioSource(AudioSource audioSource)
		{
			audioSource.outputAudioMixerGroup = _musicMixerGroup;
			audioSource.playOnAwake = false;
			// Set to 2D AudioSource
			audioSource.spatialBlend = 0;
			audioSource.rolloffMode = AudioRolloffMode.Linear;
			// Set the loop setting to true to loop the clip forever
			audioSource.loop = true;
			// Set the volume level of the AudioSource
			audioSource.volume = LoadBGMVolume();
			// Set the mute settings of the AudioSource
			audioSource.mute = !_musicOn;

			return audioSource;
		}

		#endregion

		#region Update Functions

		/// <summary>
		/// Manages each sound effect in the sound effect pool
		/// Called during OnUpdate
		/// </summary>
		private void ManageSoundEffects()
		{
			// Loop through all active sound effects
			for (int i = sfxPool.Count - 1; i >= 0; i--)
			{
				SoundEffect sfx = sfxPool[i];
				// edit as long as sound is playing and duration of sound effect is not set to forever
				// meaning user has to manually stop the sound effect
                if (sfx.Source.isPlaying && !float.IsPositiveInfinity(sfx.Time))
				{
					// Update the duration and return back to pool
					sfx.Time -= Time.deltaTime;
					sfxPool[i] = sfx;
				}

                // If time is up :: also fixed cut issues here: previous threshold value was 0.09f
                if (sfxPool[i].Time <= 0.0001f || HasPossiblyFinished(sfxPool[i]))
				{
					sfxPool[i].Source.Stop();
					// Fire a callback function, if it has one
					if (sfxPool[i].Callback != null)
					{
						sfxPool[i].Callback.Invoke();
					}

					// Destroy the host
					Destroy(sfxPool[i].gameObject);

					// Delete placeholder to host in the pool
					sfxPool.RemoveAt(i);
					break;
				}
			}
		}

        // This extra piece of code simple makes sure that the sound has cbeen ompleted
        bool HasPossiblyFinished(SoundEffect soundEffect)
        {
            return !soundEffect.Source.isPlaying && FloatEquals(soundEffect.PlaybackPosition, 0) && soundEffect.Time <= 0.09f;
        }

        bool FloatEquals(float num1, float num2, float threshold = .0001f)
        {
            return Math.Abs(num1 - num2) < threshold;
        }

		/// <summary>
		/// Returns true, if the music volume or the music mute status been changed
		/// </summary>
		private bool IsMusicAltered()
		{
			// Get changed status of music mute or the music volume
            bool flag = musicOn != _musicOn || musicOn != !musicSource.mute || !FloatEquals(currentMusicVol, _musicVolume);

			// If music is using a mixer group
			if (_musicMixerGroup != null && !string.IsNullOrEmpty(_volumeOfMusicMixer.Trim()))
			{
				float vol;
				// Get the music volume from the music mixer 
				_musicMixerGroup.audioMixer.GetFloat(_volumeOfMusicMixer, out vol);
				// Make it a range of [0 - 1] to suit the music source volume and AudioManager volume
				vol = NormaliseVolume(vol);

                return flag || !FloatEquals(currentMusicVol, vol);
			}

			return flag;
		}

		/// <summary>
		/// Returns true, if the sound effect volume or the sound effect mute status been changed
		/// </summary>
		private bool IsSoundFxAltered()
		{
			// Get changed status of sound effect mute or the sound effect volume
            bool flag = _soundFxOn != sfxOn || !FloatEquals(currentSfxVol, _soundFxVolume);

			// If sound effect is using a mixer group
			if (_soundFxMixerGroup != null && !string.IsNullOrEmpty(_volumeOfSFXMixer.Trim()))
			{
				float vol;
				// Get the sound effect volume from the sound effects mixer 
				_soundFxMixerGroup.audioMixer.GetFloat(_volumeOfSFXMixer, out vol);
				// Make it a range of [0 - 1] to suit the AudioManager
				vol = NormaliseVolume(vol);

                return flag || !FloatEquals(currentSfxVol, vol);
			}

			return flag;
		}

		/// <summary>
		/// Performs an overlapping play on the current music to produce a smooth transition from one music to another.
		/// As the current music decreases, the next music increases to eventually overlap and overshadow it
		/// In short, it hides any silent gaps that could occur during fading in and fading out
		/// Also known as gapless playback.
		/// </summary>
		private void CrossFadeBackgroundMusic()
		{
			if (backgroundMusic.MusicTransition == MusicTransition.CrossFade)
			{
				// If transition is enroute
				if (musicSource.clip.name != backgroundMusic.NextClip.name)
				{
					// Decrease the background music volume options 
					transitionTime -= Time.deltaTime;

					musicSource.volume = Mathf.Lerp(0, musicVolCap, transitionTime / backgroundMusic.TransitionDuration);

					// Coverting the decrement of the music volume to get the increment of the crossfade
					crossfadeSource.volume = Mathf.Clamp01(musicVolCap - musicSource.volume);
					// Also set the mute status to the same as the music source
					crossfadeSource.mute = musicSource.mute;

					// When transition is done
					if (musicSource.volume <= 0.00f)
					{
						SetBGMVolume(musicVolCap);
						PlayBackgroundMusic(backgroundMusic.NextClip, crossfadeSource.time, crossfadeSource.pitch);
					}
				}
			}
		}

		/// <summary>
		/// Gradually increases or decreases the volume of the background music
		/// Fade Out occurs by gradually reducing the volume of the current music, such that it goes from the original volume to absolute silence
		/// Fade In occurs by gradually increasing the volume of the next music, such that it goes from absolute silence to the original volume
		/// </summary>
		private void FadeOutFadeInBackgroundMusic()
		{
			if (backgroundMusic.MusicTransition == MusicTransition.LinearFade)
			{
				// If fading in
				if (musicSource.clip.name == backgroundMusic.NextClip.name)
				{
					// Gradually increase volume of clip
					transitionTime += Time.deltaTime;

					musicSource.volume = Mathf.Lerp(0, musicVolCap, transitionTime / backgroundMusic.TransitionDuration);

					// When at original volume
					if (musicSource.volume >= musicVolCap)
					{
						SetBGMVolume(musicVolCap);
						PlayBackgroundMusic(backgroundMusic.NextClip, musicSource.time, savedPitch);
					}
				}
				// If fading out
				else
				{
					// Gradually decrease volume of clip
					transitionTime -= Time.deltaTime;

					musicSource.volume = Mathf.Lerp(0, musicVolCap, transitionTime/backgroundMusic.TransitionDuration);

					// When volume is silent - fading out is done
					if (musicSource.volume <= 0.00f)
					{
						musicSource.volume = transitionTime = 0;
						PlayMusicFromSource(ref musicSource, backgroundMusic.NextClip, 0, musicSource.pitch);
					}
				}
			}
		}

		/// <summary>
		/// Update function called every frame
		/// </summary>
		IEnumerator OnUpdate()
		{
			while (alive)
			{
				ManageSoundEffects();

				// Updates value if music volume or music mute status has been changed
				if (IsMusicAltered())
				{
					//musicSource.mute = !_musicOn;
					ToggleBGMMute(!musicOn);

                    if (!FloatEquals(currentMusicVol, _musicVolume))
					{
						currentMusicVol = _musicVolume;
					}

					if (_musicMixerGroup != null && !string.IsNullOrEmpty(_volumeOfMusicMixer))
					{
						float vol;
						_musicMixerGroup.audioMixer.GetFloat(_volumeOfMusicMixer, out vol);
						vol = NormaliseVolume(vol);
						currentMusicVol = vol;
					}

					SetBGMVolume(currentMusicVol);
				}

				// Updates value if sound effects volume or sound effects mute has been changed
				if (IsSoundFxAltered())
				{
					//sfxOn = _soundFxOn;
					ToggleSFXMute(!sfxOn);

                    if (!FloatEquals(currentSfxVol,_soundFxVolume))
					{
						currentSfxVol = _soundFxVolume;
					}

					if (_soundFxMixerGroup != null && !string.IsNullOrEmpty(_volumeOfSFXMixer))
					{
						float vol;
						_soundFxMixerGroup.audioMixer.GetFloat(_volumeOfSFXMixer, out vol);
						vol = NormaliseVolume(vol);
						currentSfxVol = vol;
					}

					SetSFXVolume(currentSfxVol);
				}

				// Update the cross fade transition for music
				if (crossfadeSource != null)
				{
					CrossFadeBackgroundMusic();

					yield return null;
				}
				else
				{
					// Update the linear fade (fade out, fade in) transition for music
					if (backgroundMusic.NextClip != null)
					{
						FadeOutFadeInBackgroundMusic();

						yield return null;
					}
				}

				yield return new WaitForEndOfFrame();
			}
		}

		#endregion

		#region Background Music Functions

		/// <summary>
		/// Plays a clip from the specified audio source.
		/// Creates and assigns an audio source component if the refrence is null.
		/// </summary>
		/// <param name="audio_source">Reference to the audio source / channel</param>
		/// <param name="clip">The audio data to play</param>
		/// <param name="playback_position">Play position of the clip.</param>
		/// <param name="pitch">Pitch level of the clip.</param>
		private void PlayMusicFromSource(ref AudioSource audio_source, AudioClip clip, float playback_position, float pitch)
		{
			try
			{
				// Set the current playing clip
				audio_source.clip = clip;
				// Start playing the source clip at the destinated play back position
				audio_source.time = playback_position;
				audio_source.pitch = pitch = Mathf.Clamp (pitch, -3f, 3f);
				audio_source.Play();
			}
			catch (NullReferenceException nre)
			{
				Debug.LogError(nre.Message);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

		/// <summary>
		/// Plays the current audio clip from the music source of the background music
		/// </summary>
		/// <param name="clip">The audio data to play</param>
		/// <param name="playback_position">Play position of the clip</param>
		/// <param name="pitch">Pitch level of the clip.</param>
		private void PlayBackgroundMusic(AudioClip clip, float playback_position, float pitch)
		{
			// Set the music source to play the current music
			PlayMusicFromSource(ref musicSource, clip, playback_position, pitch);
			// Remove the call to next playing clip on queue
			backgroundMusic.NextClip = null;
			// Set the current playing clip
			backgroundMusic.CurrentClip = clip;
			// Get rid of the crossfade source if there is one
			if (crossfadeSource != null)
			{
				Destroy(crossfadeSource);
				crossfadeSource = null;
			}
		}

		#region Public Background Music API 

		/// <summary>
		/// Plays a background music. 
		/// Only one background music can be active at a time.
		/// </summary>
		/// <param name="clip">The audio data to play</param>
		/// <param name="transition">How should the music change from the current to the next </param>
		/// <param name="transition_duration">Time in secs it takes to transition.</param>
		/// <param name="volume">Playback volume.</param>
		/// <param name="pitch">Pitch level of the clip.</param>
		/// <param name="playback_position">Play position of the clip.</param>
		public void PlayBGM(AudioClip clip, MusicTransition transition, float transition_duration, float volume, float pitch, float playback_position = 0)
		{
			// Stop if trying to play thesame music
			if (clip == null || backgroundMusic.CurrentClip == clip)
			{
				return;
			}

			// If it's the first music to be played then switch over immediately - meaning no transition effect
			if (backgroundMusic.CurrentClip == null || transition_duration <= 0)
			{
				transition = MusicTransition.Swift;
			} 

			// Start playing from the beginning if there is no effect mode
			if (transition == MusicTransition.Swift)
			{
				PlayBackgroundMusic(clip, playback_position, pitch);
				SetBGMVolume(volume);
			}
			else
			{
				// Stop!!! Currenty performing a transition and has not finished
				if (backgroundMusic.NextClip != null)
				{
					Debug.LogWarning("Trying to perform a transition on the background music while one is still active");
					return;
				}

				// Save the transition effect to be handled by the internal manager
				backgroundMusic.MusicTransition = transition;
				// set the duration for the tramsition
				transitionTime = backgroundMusic.TransitionDuration = transition_duration;
				// Register the music volume limit or cap when transitioning
				musicVolCap = _musicVolume;
				// Set the next audio data clip to transition to
				backgroundMusic.NextClip = clip;
				// Inititalise the crossfade audio source if transition is a cross fade
				if (backgroundMusic.MusicTransition == MusicTransition.CrossFade)
				{
					// Stop!!! Still performing a crossfade transition
					if (crossfadeSource != null)
					{
						Debug.LogWarning("Trying to perform a transition on the background music while one is still active");
						return;
					}

					// Initialise an AudioSource to the crossfade source
					crossfadeSource = ConfigureAudioSource(gameObject.AddComponent<AudioSource>());
					// The crossfade volume increases as the music volume decreases, so get its relative volume
					crossfadeSource.volume = Mathf.Clamp01(musicVolCap - currentMusicVol);
					crossfadeSource.priority = 0;
					// Start playing the clip from the cross fade source
					PlayMusicFromSource(ref crossfadeSource, backgroundMusic.NextClip, 0, pitch);
				}
			}
		}

		/// <summary>
		/// Plays a background music. 
		/// Only one background music can be active at a time.
		/// </summary>
		/// <param name="clip">The audio data to play</param>
		/// <param name="transition">How should the music change from the current to the next.</param>
		/// <param name="transition_duration">Time in secs it takes to transition.</param>
		/// <param name="volume">Playback volume.</param>
		public void PlayBGM(AudioClip clip, MusicTransition transition, float transition_duration, float volume)
		{
			PlayBGM(clip, transition, transition_duration, volume, 1f);
		}

		/// <summary>
		/// Plays a background music.
		/// Only one background music can be active at a time.
		/// </summary>
		/// <param name="clip">The audio data to play</param>
		/// <param name="transition">How should the music change from the current to the next.</param>
		/// <param name="transition_duration">Time in secs it takes to transition.</param>
		public void PlayBGM(AudioClip clip, MusicTransition transition, float transition_duration)
		{
			PlayBGM(clip, transition, transition_duration, _musicVolume, 1f);
		}

		/// <summary>
		/// Plays a background music.
		/// Only one background music can be active at a time.
		/// </summary>
		/// <param name="clip">The audio data to play</param>
		/// <param name="transition">How should the music change from the current to the next. Use MusicTransition to specify type </param>
		public void PlayBGM(AudioClip clip, MusicTransition transition)
		{
			PlayBGM(clip, transition, 1f, _musicVolume, 1f);
		}

		/// <summary>
		/// Plays a background music using the swift the transition mode.
		/// Only one background music can be active at a time.
		/// </summary>
		/// <param name="clip">The audio data to play</param>
		public void PlayBGM(AudioClip clip)
		{
			PlayBGM(clip, MusicTransition.Swift, 1f, _musicVolume, 1f);
		}

		/// <summary>
		/// Plays a background music. 
		/// Only one background music can be active at a time.
		/// </summary>
		/// <param name="clip_path">Path name of the target clip from the Resources folder</param>
		/// <param name="transition">How should the music change from the current to the next.</param>
		/// <param name="transition_duration">Time in secs it takes to transition.</param>
		/// <param name="volume">Playback volume.</param>
		/// <param name="pitch">Pitch level of the clip.</param>
		/// <param name="playback_position">Play position of the clip.</param>
		public void PlayBGM(string clip_path, MusicTransition transition, float transition_duration, float volume, float pitch, float playback_position = 0)
		{
			PlayBGM (LoadClip(clip_path), transition, transition_duration, volume, pitch, playback_position);
		}

		/// <summary>
		/// Plays a background music. 
		/// Only one background music can be active at a time.
		/// </summary>
		/// <param name="clip_path">Path name of the target clip from the Resources folder</param>
		/// <param name="transition">How should the music change from the current to the next.</param>
		/// <param name="transition_duration">Time in secs it takes to transition.</param>
		/// <param name="volume">Playback volume.</param>
		public void PlayBGM(string clip_path, MusicTransition transition, float transition_duration, float volume)
		{
			PlayBGM (LoadClip(clip_path), transition, transition_duration, volume, 1f);
		}

		/// <summary>
		/// Plays a background music.
		/// Only one background music can be active at a time.
		/// </summary>
		/// <param name="clip_path">Path name of the target clip from the Resources folder</param>
		/// <param name="transition">How should the music change from the current to the next.</param>
		/// <param name="transition_duration">Time in secs it takes to transition.</param>
		public void PlayBGM(string clip_path, MusicTransition transition, float transition_duration)
		{
			PlayBGM(LoadClip(clip_path), transition, transition_duration, _musicVolume, 1f);
		}

		/// <summary>
		/// Plays a background music.
		/// Only one background music can be active at a time.
		/// </summary>
		/// <param name="clip_path">Path name of the target clip from the Resources folder</param>
		/// <param name="transition">How should the music change from the current to the next. Use MusicTransition to specify type </param>
		public void PlayBGM(string clip_path, MusicTransition transition)
		{
			PlayBGM(LoadClip(clip_path), transition, 1f, _musicVolume, 1f);
		}

		/// <summary>
		/// Plays a background music using the swift the transition mode.
		/// Only one background music can be active at a time.
		/// </summary>
		/// <param name="clip_path">Path name of the target clip from the Resources folder</param>
		public void PlayBGM(string clip_path)
		{
			PlayBGM(LoadClip(clip_path), MusicTransition.Swift, 1f, _musicVolume, 1f);
		}

		/// <summary>
		/// Stops the playing background music
		/// </summary>
		public void StopBGM()
		{
			if (musicSource.isPlaying)
			{
				musicSource.Stop();
			}
		}

		/// <summary>
		/// Pauses the playing background music
		/// </summary>
		public void PauseBGM()
		{
			if (musicSource.isPlaying)
			{
				musicSource.Pause();
			}
		}

		/// <summary>
		/// Resumes the playing background music
		/// </summary>
		public void ResumeBGM()
		{
			if (!musicSource.isPlaying)
			{
				musicSource.UnPause();
			}
		}

		#endregion

		#endregion

		#region Sound Effect Functions

		/// <summary>
		/// Inner function used to play all resulting sound effects.
		/// Initialises some particular properties for the sound effect.
		/// </summary>
		/// <param name="audio_clip">The audio data to play</param>
		/// <param name="location">World location of the audio clip.</param>
		/// <returns>Newly created gameobject with sound effect and audio source attached</returns>
		private GameObject CreateSoundFx(AudioClip audio_clip, Vector2 location)
		{
			// Create a temporary game object to host our audio source
			GameObject host = new GameObject("TempAudio");
			// Set the temp audio's world position
			host.transform.position = location;
			// Parent it to the AudioManager until further notice
			host.transform.SetParent(transform);
			// Specity a tag for future use
			host.AddComponent<SoundEffect>();

			// Add an audio source to that host
			AudioSource audioSource = host.AddComponent<AudioSource>() as AudioSource;
			audioSource.playOnAwake = false;
			audioSource.spatialBlend = 0;
			audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
			// Set the mixer group for the sound effect if one exists
			audioSource.outputAudioMixerGroup = _soundFxMixerGroup;
			// Set that audio source clip to the one in paramaters
			audioSource.clip = audio_clip;
			// Set the mute value
			audioSource.mute = !_soundFxOn;

			return host;
		}

		#region Public Sound Effect API 

		/// <summary>
		/// Returns the index of a sound effect in pool if one exists.
		/// </summary>
		/// <param name="name">The name of the sound effect.</param>
		/// <param name="singleton">Is the sound effect a singleton.</param>
		/// <returns>Index of sound effect or -1 is none exists</returns>
		public int IndexOfSoundFxPool(string name, bool singleton = false)
		{
			int index = 0;
			while (index < sfxPool.Count)
			{
				if (sfxPool[index].Name == name && singleton == sfxPool[index].Singleton)
				{
					return index;
				}

				index++;
			}

			return -1;
		}

		/// <summary>
		/// Plays a sound effect for a duration of time at a given location in world space and calls the specified callback function after the time is over.
		/// </summary>
		/// <returns>An audiosource</returns>
		/// <param name="clip">The audio data to play</param>
		/// <param name="location">World location of the clip</param>
		/// <param name="duration">The length in time the clip should play</param>
		/// <param name="volume">Playback volume.</param>
		/// <param name="singleton">Is the sound effect a singleton.</param>
		/// <param name="pitch">Pitch level of the clip.</param>
		/// <param name="callback">Action callback to be invoked after the sound has finished.</param>
		public AudioSource PlaySFX(AudioClip clip, Vector2 location, float duration, float volume, bool singleton = false, float pitch = 1f, Action callback = null)
		{
			if (duration <= 0 || clip == null) 
			{
				return null;
			}

			int index = IndexOfSoundFxPool(clip.name, true);

			if (index >= 0)
			{
				// Reset the duration if it exists in the pool
				SoundEffect singletonSFx = sfxPool[index];
				singletonSFx.Duration = singletonSFx.Time = duration;
				sfxPool[index] = singletonSFx;

				return sfxPool[index].Source;
			}

			GameObject host = null;
			AudioSource source = null;

			host = CreateSoundFx(clip, location);
			source = host.GetComponent<AudioSource>();
			source.loop = duration > clip.length;
			source.volume = _soundFxVolume * volume;
			source.pitch = pitch;

			// Create a new repeat sound
			SoundEffect sfx = host.GetComponent<SoundEffect>();
			sfx.Singleton = singleton;
			sfx.Source = source;
			sfx.OriginalVolume = volume;
			sfx.Duration = sfx.Time = duration;
			sfx.Callback = callback;

			// Add it to the list
			sfxPool.Add(sfx);

			//Destroy (host, duration);
			//FireCallback (callback, duration);

			// Start playing the sound
			source.Play();

			return source;
		}

		/// <summary>
		/// Plays a sound effect for a duration of time at a given location in world space and calls the specified callback function after the time is over
		/// </summary>
		/// <returns>An audiosource</returns>
		/// <param name="clip">The audio data to play</param>
		/// <param name="location">World location of the clip</param>
		/// <param name="duration">The length in time the clip should play</param>
		/// <param name="singleton">Is the sound effect a singleton.</param>
		/// <param name="callback">Action callback to be invoked after the sound has finished.</param>
		public AudioSource PlaySFX(AudioClip clip, Vector2 location, float duration, bool singleton = false, Action callback = null)
		{
			return PlaySFX(clip, location, duration, _soundFxVolume, singleton, 1f, callback);
		}

		/// <summary>
		/// Plays a sound effect for a duration of time and calls the specified callback function after the time is over
		/// </summary>
		/// <returns>An audiosource</returns>
		/// <param name="clip">The audio data to play</param>
		/// <param name="duration">The length in time the clip should play</param>
		/// <param name="singleton">Is the sound effect a singleton.</param>
		/// <param name="callback">Action callback to be invoked after the sound has finished.</param>
		public AudioSource PlaySFX(AudioClip clip, float duration, bool singleton = false, Action callback = null)
		{
			return PlaySFX(clip, Vector2.zero, duration, _soundFxVolume, singleton, 1f, callback);
		}

		/// <summary>
		/// Repeats a sound effect for a specified amount of times at a given location in world space and calls the specified callback function after the sound is over.
		/// </summary>
		/// <returns>An audiosource</returns>
		/// <param name="clip">The audio data to play</param>
		/// <param name="location">World location of the clip</param>
		/// <param name="repeat">How many times in successions you want the clip to play. To loop forever, set as a negative number</param>
		/// <param name="volume">Playback volume.</param>
		/// <param name="singleton">Is the sound effect a singleton.</param>
		/// <param name="pitch">Pitch level of the clip.</param>
		/// <param name="callback">Action callback to be invoked after the sound has finished.</param>
		public AudioSource RepeatSFX(AudioClip clip, Vector2 location, int repeat, float volume, bool singleton = false, float pitch = 1f, Action callback = null)
		{
			if (clip == null) 
			{
				return null;
			}

			if (repeat != 0)
			{
				int index = IndexOfSoundFxPool(clip.name, true);

				if (index >= 0)
				{
					// Reset the duration if it exists in the pool
					SoundEffect singletonSFx = sfxPool[index];
					singletonSFx.Duration = singletonSFx.Time = repeat > 0 ? clip.length * repeat : float.PositiveInfinity;
					sfxPool[index] = singletonSFx;

					return sfxPool[index].Source;
				}

				GameObject host = CreateSoundFx(clip, location);
				AudioSource source = host.GetComponent<AudioSource>();
				source.loop = repeat != 0;
				source.volume = _soundFxVolume * volume;
				source.pitch = pitch;

				// Create a new repeat sound
				SoundEffect sfx = host.GetComponent<SoundEffect>();
				sfx.Singleton = singleton;
				sfx.Source = source;
				sfx.OriginalVolume = volume;
				sfx.Duration = sfx.Time = repeat > 0 ? clip.length * repeat : float.PositiveInfinity;
				sfx.Callback = callback;

				// Add it to the list
				sfxPool.Add(sfx);

				// Start playing the sound
				source.Play();

				return source;
			}

			// Play one shot if repat length is less than or equal to 1
			return PlayOneShot(clip, location, volume, pitch, callback);
		}

		/// <summary>
		/// Repeats a sound effect for a specified amount of times at a given location in world space and calls the specified callback function after the sound is over.
		/// </summary>
		/// <returns>An audiosource</returns>
		/// <param name="clip">The audio data to play</param>
		/// <param name="location">World location of the clip</param>
		/// <param name="repeat">How many times in successions you want the clip to play. To loop forever, set as a negative number</param>
		/// <param name="singleton">Is the sound effect a singleton.</param>
		/// <param name="callback">Action callback to be invoked after the sound has finished.</param>
		public AudioSource RepeatSFX(AudioClip clip, Vector2 location, int repeat, bool singleton = false, Action callback = null)
		{
			return RepeatSFX(clip, location, repeat, _soundFxVolume, singleton, 1f, callback);
		}

		/// <summary>
		/// Repeats a sound effect for a specified amount of times at a given location in world space and calls the specified callback function after the sound is over.
		/// </summary>
		/// <returns>An audiosource</returns>
		/// <param name="clip">The audio data to play</param>
		/// <param name="repeat">How many times in successions you want the clip to play. To loop forever, set as a negative number</param>
		/// <param name="singleton">Is the sound effect a singleton.</param>
		/// <param name="callback">Action callback to be invoked after the sound has finished.</param>
		public AudioSource RepeatSFX(AudioClip clip, int repeat, bool singleton = false, Action callback = null)
		{
			return RepeatSFX(clip, Vector2.zero, repeat, _soundFxVolume, singleton, 1f, callback);
		}

		/// <summary>
		/// Plays a sound effect once at a location in world space and calls the specified callback function after the sound is over
		/// </summary>
		/// <returns>An AudioSource</returns>
		/// <param name="clip">The audio data to play</param>
		/// <param name="location">World location of the clip</param>
		/// <param name="volume">Playback volume.</param>
		/// <param name="pitch">Pitch level of the clip.</param>
		/// <param name="callback">Action callback to be invoked after clip has finished playing</param>
		public AudioSource PlayOneShot(AudioClip clip, Vector2 location, float volume, float pitch = 1f, Action callback = null)
		{
			if (clip == null) 
			{
				return null;
			}

			GameObject host = CreateSoundFx(clip, location);
			AudioSource source = host.GetComponent<AudioSource>();
			source.loop = false;
			source.volume = _soundFxVolume * volume;
			source.pitch = pitch;

			// Create a new repeat sound
			SoundEffect sfx = host.GetComponent<SoundEffect>();
			sfx.Singleton = false;
			sfx.Source = source;
			sfx.OriginalVolume = volume;
			sfx.Duration = sfx.Time = clip.length;
			sfx.Callback = callback;

			// Add it to the list
			sfxPool.Add(sfx);

			source.Play();

			return source;
		}

		/// <summary>
		/// Plays a sound effect once at a location in world space
		/// </summary>
		/// <returns>An AudioSource</returns>
		/// <param name="clip">The audio data to play</param>
		/// <param name="location">World location of the clip</param>
		/// <param name="callback">Action callback to be invoked after clip has finished playing</param>
		public AudioSource PlayOneShot(AudioClip clip, Vector2 location, Action callback = null)
		{
			return PlayOneShot(clip, location, _soundFxVolume, 1f, callback);
		}


		/// <summary>
		/// Plays a sound effect once and calls the specified callback function after the sound is over
		/// </summary>
		/// <returns>An AudioSource</returns>
		/// <param name="clip">The audio data to play</param>
		/// <param name="callback">Action callback to be invoked after clip has finished playing</param>
		public AudioSource PlayOneShot(AudioClip clip, Action callback = null)
		{
			return PlayOneShot(clip, Vector2.zero, _soundFxVolume, 1f, callback);
		}

		/// <summary>
		/// Pauses all the sound effects in the game
		/// </summary>
		public void PauseAllSFX()
		{
			// Loop through all sound effects with the SoundEffectTag and update their properties
			foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect>())
			{
				if (sfx.Source.isPlaying) sfx.Source.Pause();
			}
		}

		/// <summary>
		/// Resumes all the sound effect in the game
		/// </summary>
		public void ResumeAllSFX()
		{
			// Loop through all sound effects with the SoundEffectTag and update their properties
			foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect>())
			{
				if (!sfx.Source.isPlaying) sfx.Source.UnPause();
			}
		}

		/// <summary>
		/// Stops all the sound effects in the game
		/// </summary>
		public void StopAllSFX()
		{
			// Loop through all sound effects with the SoundEffectTag and update their properties
			foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect>())
			{
				if (sfx.Source) 
				{
					sfx.Source.Stop();
					Destroy(sfx.gameObject);
				}
			}

			sfxPool.Clear();
		}

		#endregion

		#endregion

		#region Setter Functions

		/// <summary>
		/// Loads an AudioClip from the Resources folder
		/// </summary>
		/// <param name="path">Path name of the target clip from the Resources folder</param>
		/// <param name="add_to_playlist">Option to add loaded clip into the playlist for future reference</param>
		/// <returns>The Audioclip from the resource folder</returns>
		public AudioClip LoadClip(string path, bool add_to_playlist = false)
		{
			AudioClip clip = Resources.Load(path) as AudioClip;
			if (clip == null)
			{
				Debug.LogError (string.Format ("AudioClip '{0}' not found at location {1}", path, System.IO.Path.Combine (Application.dataPath, "/Resources/"+path)));
				return null;
			}

			if (add_to_playlist)
			{
				AddToPlaylist(clip);
			}

			return clip;
		}

		/// <summary>
		/// Loads an AudioClip from the specified url path.
		/// </summary>
		/// <param name="path">The url path of the audio clip to download. For example: 'http://www.my-server.com/audio.ogg'</param>
		/// <param name="audio_type">The type of audio encoding for the downloaded clip. See AudioType</param>
		/// <param name="add_to_playlist">Option to add loaded clip into the playlist for future reference</param>
		/// <param name="callback">Action callback to be invoked after clip has finished loading</param>
		public void LoadClip(string path, AudioType audio_type, bool add_to_playlist, Action<AudioClip> callback)
		{
			StartCoroutine(LoadAudioClipFromUrl(path, audio_type, (downloadedContent) =>
				{
					if (downloadedContent != null && add_to_playlist)
					{
						AddToPlaylist(downloadedContent);
					}

					callback.Invoke(downloadedContent);
				}));
		}

		/// <summary>
		/// Loads the audio clip from URL.
		/// </summary>
		/// <returns>The audio clip from URL.</returns>
		/// <param name="audio_url">Audio URL.</param>
		/// <param name="audio_type">Audio type.</param>
		/// <param name="callback">Callback.</param>
		IEnumerator LoadAudioClipFromUrl(string audio_url, AudioType audio_type, Action<AudioClip> callback)
		{
			using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(audio_url, audio_type))
			{
                yield return www.SendWebRequest();

				if (www.isNetworkError)
				{
                    Debug.Log(string.Format("Error downloading audio clip at {0} : {1}", audio_url, www.error));
				}

				callback.Invoke(UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www));
			}
		}

		/// <summary>
		/// Toggles the mater mute that controls both background music and sound effect mute.
		/// </summary>
		/// <param name="flag">New toggle state of the mute controller.</param>
		private void ToggleMute(bool flag)
		{
			ToggleBGMMute(flag);
			ToggleSFXMute(flag);
		}

		/// <summary>
		/// Toggles the background music mute.
		/// </summary>
		/// <param name="flag">New toggle state of the background music controller.</param>
		private void ToggleBGMMute(bool flag)
		{
			musicOn = _musicOn = flag;
			musicSource.mute = !musicOn;
		}

		/// <summary>
		/// Toggles the sound effect mute.
		/// </summary>
		/// <param name="flag">New toggle state of the sound effect controller.</param>
		private void ToggleSFXMute(bool flag)
		{
			sfxOn = _soundFxOn = flag;

			// Loop through all sound effects with the SoundEffectTag and update their properties
			foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect>())
			{
				sfx.Source.mute = !sfxOn;
			}

			//sfxOn = _soundFxOn;
		}

		/// <summary>
		/// Sets the background music volume.
		/// </summary>
		/// <param name="volume">New volume of the background music.</param>
		private void SetBGMVolume(float volume)
		{
			try
			{
				// Restrict the values to a range of [0 - 1] to suit the AudioManager
				volume = Mathf.Clamp01(volume);
				// Assign vol to all music volume variables
				musicSource.volume = currentMusicVol = _musicVolume = volume;

				// Is the AudioManager using a master mixer
				if (_musicMixerGroup != null && !string.IsNullOrEmpty(_volumeOfMusicMixer.Trim()))
				{
					// Get the equivalent mixer volume, always [-80db ... 20db]
					float mixerVol = -80f + (volume * 100f);
					// Set the volume of the background music group
					_musicMixerGroup.audioMixer.SetFloat(_volumeOfMusicMixer, mixerVol);
				}
			}
			catch (NullReferenceException nre)
			{
				Debug.LogError(nre.Message);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

		/// <summary>
		/// Sets the volume of the sound effects.
		/// </summary>
		/// <param name="volume">New volume for all the sound effects.</param>
		private void SetSFXVolume(float volume)
		{
			try
			{
				// Restrict the values to a range of [0 - 1] to suit the AudioManager
				volume = Mathf.Clamp01(volume);
				// Update the volume controllers of the sound effects
				currentSfxVol = _soundFxVolume = volume;

				// Loop through all sound effects with the SoundEffectTag and update their properties
				foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect>())
				{
					sfx.Source.volume = _soundFxVolume * sfx.OriginalVolume;
					sfx.Source.mute = !_soundFxOn;
				}

				// Is the AudioManager using a master mixer
				if (_soundFxMixerGroup != null && !string.IsNullOrEmpty(_volumeOfSFXMixer.Trim()))
				{
					// Get the equivalent mixer volume, always [-80db ... 20db]
					float mixerVol = -80f + (volume * 100f);
					// Set the volume of the sound effect group
					_soundFxMixerGroup.audioMixer.SetFloat(_volumeOfSFXMixer, mixerVol);
				}
			}
			catch (NullReferenceException nre)
			{
				Debug.LogError(nre.Message);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

		/// <summary>
		/// Normalises the volume so that it can be in a range of [0 - 1] to suit the music source volume and the AudioManager volume
		/// </summary>
		/// <returns>The normalised volume between the range of zero and one.</returns>
		/// <param name="vol">Vol.</param>
		private float NormaliseVolume(float vol)
		{
			vol += 80f;
			vol /= 100f;
			return vol;
		}

		#endregion

		#region Player Prefs Functions

		/// <summary>
		/// Get the volume of the background music from disk
		/// </summary>
		/// <returns></returns>
		private float LoadBGMVolume()
		{
			return PlayerPrefs.HasKey(BgMusicVolKey) ? PlayerPrefs.GetFloat(BgMusicVolKey) : _musicVolume;
		}

		/// <summary>
		/// Get the volume of the sound effect from disk
		/// </summary>
		/// <returns></returns>
		private float LoadSFXVolume()
		{
			return PlayerPrefs.HasKey(SoundFxVolKey) ? PlayerPrefs.GetFloat(SoundFxVolKey) : _soundFxVolume;
		}

		/// <summary>
		/// Converts the integer value to a boolean representative value
		/// </summary>
		private bool ToBool(int integer)
		{
			return integer == 0 ? false : true;
		}

		/// <summary>
		/// Get the mute or disabled status of the background music from disk
		/// </summary>
		/// <returns>Returns the value of the background music mute key from the saved preferences if it exists or the defaut value if it does not</returns>
		private bool LoadBGMMuteStatus()
		{
			return PlayerPrefs.HasKey(BgMusicMuteKey) ? ToBool(PlayerPrefs.GetInt(BgMusicMuteKey)) : _musicOn;
		}

		/// <summary>
		/// Get the mute or disabled status of the sound effect from disk
		/// </summary>
		/// <returns>Returns the value of the sound effect mute key from the saved preferences if it exists or the defaut value if it does not</returns>
		private bool LoadSFXMuteStatus()
		{
			return PlayerPrefs.HasKey(SoundFxMuteKey) ? ToBool(PlayerPrefs.GetInt(SoundFxMuteKey)) : _soundFxOn;
		}

		#region Public Player Prefs API

		/// <summary>
		/// Stores the volume and the mute status of the background music to disk.
		/// Note that all preferences would automatically get saved when this script gets destroyed 
		/// </summary>
		public void SaveBGMPreferences()
		{
			PlayerPrefs.SetInt(BgMusicMuteKey, _musicOn ? 1 : 0);
			PlayerPrefs.SetFloat(BgMusicVolKey, _musicVolume);
			PlayerPrefs.Save();
		}

		/// <summary>
		/// Stores the volume and the mute status of the sound effect to disk.
		/// Note that all preferences would automatically get saved when this script gets destroyed
		/// </summary>
		public void SaveSFXPreferences()
		{
			PlayerPrefs.SetInt(SoundFxMuteKey, _soundFxOn ? 1 : 0);
			PlayerPrefs.SetFloat(SoundFxVolKey, _soundFxVolume);
			PlayerPrefs.Save();
		}

		/// <summary>
		/// Removes all key and value pertaining to sound options from disk
		/// </summary>
		public void ClearAllPreferences()
		{
			PlayerPrefs.DeleteKey(BgMusicVolKey);
			PlayerPrefs.DeleteKey(SoundFxVolKey);
			PlayerPrefs.DeleteKey(BgMusicMuteKey);
			PlayerPrefs.DeleteKey(SoundFxMuteKey);
			PlayerPrefs.Save();
		}

		/// <summary>
		/// Writes all modified sound options or preferences to disk
		/// </summary>
		public void SaveAllPreferences()
		{
			PlayerPrefs.SetFloat(SoundFxVolKey, _soundFxVolume);
			PlayerPrefs.SetFloat(BgMusicVolKey, _musicVolume);
			PlayerPrefs.SetInt(SoundFxMuteKey, _soundFxOn ? 1 : 0);
			PlayerPrefs.SetInt(BgMusicMuteKey, _musicOn ? 1 : 0);
			PlayerPrefs.Save();
		}

		#endregion

		#endregion

		#region Playlist Functions

		/// <summary>
		/// Clear the list of audio clips
		/// </summary>
		public void EmptyPlaylist()
		{
			_playlist.Clear();
		}

		/// <summary>
		/// Add a sound clip to list of audio clips
		/// </summary>
		/// <param name="clip">Sound clip data</param>
		public void AddToPlaylist(AudioClip clip)
		{
			if (clip != null)
			{
				_playlist.Add(clip);
			}
		}

		/// <summary>
		/// Add a sound clip to asset list pool
		/// </summary>
		/// <param name="clip">Sound clip data</param>
		public void RemoveFromPlaylist(AudioClip clip)
		{
			if (clip != null && GetClipFromPlaylist(clip.name))
			{
				_playlist.Remove (clip);
				_playlist.Sort((x,y)=> x.name.CompareTo(y.name));
			}
		}

		/// <summary>
		/// Gets the AudioClip reference from the name supplied 
		/// </summary>
		/// <param name="clip_name">The name of the clip in the asset list pool </param>
		/// <returns>The AudioClip from the pool or null if no matching name can be found</returns>
		public AudioClip GetClipFromPlaylist(string clip_name)
		{
			// Search for each sound assets in the asset list pool 
			for(int i = 0; i < _playlist.Count; i++)
			{
				// Check if name is a match
				if (clip_name == _playlist[i].name)
				{
					return _playlist[i];
				}
			}

			Debug.LogWarning(clip_name +" does not exist in the playlist.");
			return null;
		}

		/// <summary>
		/// Load all sound clips from the Resources folder path into the asset list pool
		/// </summary>
		/// <param name="path">Pathname of the target folder. When using the empty string (i.e, ""), the function will load the entire audio clip content(s) of the resource folder</param>
		/// <param name="overwrite">Overwrites the current content(s) of the playlist.</param>
		public void LoadPlaylist(string path, bool overwrite)
		{
			// Get all clips from resource path
			AudioClip[] clips = Resources.LoadAll<AudioClip>(path);

			// Overwrite the current pool with the new one
			if (clips != null && clips.Length > 0 && overwrite)
			{
				_playlist.Clear();
			}

			// Add every loaded sound resource to the asset list pool
			for (int i = 0; i < clips.Length; i++)
			{
				_playlist.Add(clips[i]);
			}
		}

		#endregion
	}
}