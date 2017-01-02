using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Papae2D.AudioEngine
{
    /// <summary>
    /// The manager of all things with regards to sound
    /// </summary>
    [RequireComponent(typeof(AudioOptions))]
    public class AudioManager : MonoBehaviour
    {
        #region Single Pattern

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
                            // Humbly create a new gameobject named AudioManager
                            GameObject clone = new GameObject();
                            clone.hideFlags = HideFlags.None;
                            // Make object persist throughout lifetime of application. Persis between scene transitions
                            DontDestroyOnLoad(clone);
                            
                            // Add the AudioManager component to the gameobject and store the instance as a singleton
                            instance = clone.AddComponent<AudioManager>();

                            // Initialise all pertinent properties
                            instance.Initialise(clone);
                        }
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Prevent calling the consructor of the AudioManager
        /// </summary>
        protected AudioManager() { }

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

        void Awake()
        {
            if (instance == null)
            {
                // Make object persist throughout lifetime of application. Persis between scene transitions
                DontDestroyOnLoad(this.gameObject);
                // Store the instance as a singleton 
                instance = this;
                // Perform any other initialisation tasks here
                Initialise(null);
            }
            else if (instance != this)
            {
                // Get rid of any other instances if they exist. Only one instance is permitted or allowed
                Destroy(this.gameObject);
            }
        }

        #endregion


        #region Inspector Variables

        [Tooltip("Background music managed by the AudioManager")]
        [HideInInspector] BackgroundMusic backgroundMusic;
        [Tooltip("List of currently repeating sounds managed by the AudioManager")]
        [SerializeField] List<RepeatSound> repeatingSounds = new List<RepeatSound>();
        [Tooltip("List of all assets attached to the AudioManager")]
        [SerializeField] List<SoundAsset> soundAssets = new List<SoundAsset>();

        #endregion


        #region Static Variables

        /// <summary>
        /// Local instance of attached AudioOptions
        /// </summary>
        public static AudioOptions Options = null;
        // Audio source for both the current music and the next music if crossfade transition is active 
        static AudioSource musicSource = null, crossfadeSource = null;
        // Volume placehlder properties for the current music, sound effect, and the vol limit of the current music
        static float currentMusicVol = 0, currentSfxVol = 0, musicVolCap = 0;
        // Status to detect when the sound effect mute status has changed
        static bool sfxOn = false;
        // Timer countdown used by the Fade transitions
        static float transitionTime;

        // Player Prefabs store keys
        static readonly string BgMusicVolKey = "BGMVol";
        static readonly string SoundFxVolKey = "SFXVol";
        static readonly string BgMusicMuteKey = "BGMMute";
        static readonly string SoundFxMuteKey = "SFxMute";

        /// <summary>
        /// Background music managed by the AudioManager
        /// </summary>
        public static BackgroundMusic BGM
        {
            get { return Instance.backgroundMusic; }
        }

        /// <summary>
        /// Current list or pool of sounds in repeat
        /// </summary>
        public static List<RepeatSound> RepeatSoundPool
        {
            get { return Instance.repeatingSounds; }
        }

        /// <summary>
        /// Pool of sound assests attached to the AudioManager
        /// </summary>
        public static List<SoundAsset> SoundAssetPool
        {
            get { return Instance.soundAssets; }
        }

        #endregion


        #region Initialisation Functions

        /// <summary>
        /// AudioManager initialisation tasks
        /// </summary>
        void Initialise(GameObject clone)
        {
            if (clone == null)
            {
                clone = this.gameObject;
            }

            // Set proper name of the gameobject
            clone.name = "AudioManager";

            // Initialises the sound options used by the AudioManager
            if (Options == null)
            {
                Options = clone.GetComponent<AudioOptions>();
                Options.musicOn = LoadBgMusicMuteStatus();
                Options.musicVolume = Options.useMusicVolOnStart ? Options.musicVolume : LoadBGMVolume();
                Options.soundFxOn = LoadSoundFxMuteStatus();
                Options.soundFxVolume = Options.useSfxVolOnStart ? Options.soundFxVolume : LoadSFXVolume();
            }

            // Initialises the audio source used by the background music
            if (musicSource == null)
            {
                musicSource = clone.GetComponent<AudioSource>();
                // If none exists, create one and attach to AudioManager
                if (musicSource == null)
                {
                    musicSource = Attach2DAudioSource();
                }
            }
        }

        void Start()
        {
            if (Options != null && musicSource != null)
            {
                // this is here because the mixer group float can't be set on awake
                // spent amost 4 hours trying to figure out why... well didn't know until I ran some tests 
                StartCoroutine(OnUpdate());
            }
        }

        void OnDestroy()
        {
            StopAllCoroutines();
            SaveAllPreferences();
        }

        /// <summary>
        /// Creates an audio source with 2D music settings based on some internal properties
        /// </summary>
        /// <returns>An AudioSource with 2D features</returns>
        AudioSource Attach2DAudioSource()
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>() as AudioSource;
            
            audioSource.outputAudioMixerGroup = Options.musicMixerGroup;
            audioSource.playOnAwake = false;
            // Set to 2D AudioSource
            audioSource.spatialBlend = 0;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            // Set the loop setting to true to loop the clip forever
            audioSource.loop = true;
            // Set the volume level of the AudioSource
            audioSource.volume = LoadBGMVolume();
            // Set the mute settings of the AudioSource
            audioSource.mute = !Options.musicOn;

            return audioSource;
        }

        #endregion


        #region Update Functions

        /// <summary>
        /// Manages each repeat sound in the repeating sound pool
        /// Called during Update
        /// </summary>
        internal void ManageRepeatingSounds()
        {
            // Loop through all active repeating sounds
            for (int i = 0; i < repeatingSounds.Count; i++)
            {
                // Update the duration
                RepeatSound rs = repeatingSounds[i];
                rs.duration -= Time.deltaTime;
                repeatingSounds[i] = rs;

                // If time is up
                if (repeatingSounds[i].duration <= 0)
                {
                    // Fire a callback function, if it has one
                    if (repeatingSounds[i].callback != null)
                    {
                        repeatingSounds[i].callback.Invoke();
                    }

                    // Destroy the host
                    Destroy(repeatingSounds[i].audioSource.gameObject);

                    // Delete placeholder to host in the pool
                    repeatingSounds.RemoveAt(i);
                    // Re-order pool :: TODO :: try using a sorted list maybe it might automatically sort itself after deletion
                    repeatingSounds.Sort();
                    break;
                }
            }
        }

        /// <summary>
        /// Returns true, if the music volume or the music mute status been changed
        /// </summary>
        internal bool IsMusicAltered()
        {
            // Get changed status of music mute or the music volume
            bool flag = Options.musicOn != !musicSource.mute || currentMusicVol != Options.musicVolume;

            // If music is using a mixer group
            if (Options.musicMixerGroup != null)
            {
                float vol;
                // Get the music volume from the music mixer 
                Options.musicMixerGroup.audioMixer.GetFloat(Options.volumeOfMusicMixer, out vol);
                // Make it a range of [0 - 1] to suit the music source volume and AudioManager volume
                vol += 80f;
                vol /= 100f;

                return flag || currentMusicVol != vol;
            }

            return flag;
        }

        /// <summary>
        /// Returns true, if the sound effect volume or the sound effect mute status been changed
        /// </summary>
        internal bool IsSoundFxAltered()
        {
            // Get changed status of sound effect mute or the sound effect volume
            bool flag = Options.soundFxOn != sfxOn || currentSfxVol != Options.soundFxVolume;

            // If sound effect is using a mixer group
            if (Options.soundFxMixerGroup != null)
            {
                float vol;
                // Get the sound effect volume from the sound effects mixer 
                Options.soundFxMixerGroup.audioMixer.GetFloat(Options.volumeOfSFXMixer, out vol);
                // Make it a range of [0 - 1] to suit the AudioManager
                vol += 80f;
                vol /= 100f;

                return flag || currentSfxVol != vol;
            }

            return flag;
        }

        /// <summary>
        /// Performs an overlapping play on the current music to produce a smooth transition from one music to another.
        /// As the current music decreases, the next music increases to eventually overlap and overshadow it
        /// In short, it hides any silent gaps that could occur during fading in and fading out
        /// Also known as gapless playback.
        /// </summary>
        internal void CrossFadeBackgroundMusic()
        {
            if (backgroundMusic.transition == MusicTransition.CrossFade)
            {
                // If transition is enroute
                if (musicSource.clip.name != backgroundMusic.nextClip.name)
                {
                    // Decrease the background music volume options 
                    transitionTime -= Time.deltaTime;

                    musicSource.volume = Mathf.Lerp(0, musicVolCap, transitionTime / backgroundMusic.transitionDuration);

                    // Coverting the decrement of the music volume to get the increment of the crossfade
                    crossfadeSource.volume = Mathf.Clamp01(musicVolCap - musicSource.volume);
                    // Also set the mute status to the same as the music source
                    crossfadeSource.mute = musicSource.mute;

                    // When transition is done
                    if (musicSource.volume <= 0.00f)
                    {
                        SetBGMVolume(musicVolCap);
                        PlayBackgroundMusic(backgroundMusic.nextClip, crossfadeSource.time);
                    }
                }
            }
        }

        /// <summary>
        /// Gradually increases or decreases the volume of the background music
        /// Fade Out occurs by gradually reducing the volume of the current music, such that it goes from the original volume to absolute silence
        /// Fade In occurs by gradually increasing the volume of the next music, such that it goes from absolute silence to the original volume
        /// </summary>
        internal void FadeOutFadeInBackgroundMusic()
        {
            if (backgroundMusic.transition == MusicTransition.LinearFade)
            {
                // If fading in
                if (musicSource.clip.name == backgroundMusic.nextClip.name)
                {
                    // Gradually increase volume of clip
                    transitionTime += Time.deltaTime;

                    musicSource.volume = Mathf.Lerp(0, musicVolCap, transitionTime / backgroundMusic.transitionDuration);

                    // When at original volume
                    if (musicSource.volume >= musicVolCap)
                    {
                        SetBGMVolume(musicVolCap);
                        PlayBackgroundMusic(backgroundMusic.nextClip, musicSource.time);
                    }
                }
                // If fading out
                else
                {
                    // Gradually decrease volume of clip
                    transitionTime -= Time.deltaTime;

                    musicSource.volume = Mathf.Lerp(0, musicVolCap, transitionTime/backgroundMusic.transitionDuration);

                    // When volume is silent - fading out is done
                    if (musicSource.volume <= 0.00f)
                    {
                        musicSource.volume = transitionTime = 0;
                        PlayMusicFromSource(ref musicSource, backgroundMusic.nextClip, 0);
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
                ManageRepeatingSounds();

                // Updates value if music volume or music mute status has been changed
                if (IsMusicAltered())
                {
                    musicSource.mute = !Options.musicOn;

                    if (currentMusicVol != Options.musicVolume)
                    {
                        currentMusicVol = Options.musicVolume;
                    }
                    else if (Options.musicMixerGroup != null)
                    {
                        float vol;
                        Options.musicMixerGroup.audioMixer.GetFloat(Options.volumeOfMusicMixer, out vol);
                        vol += 80f;
                        vol /= 100f;
                        currentMusicVol = vol;
                    }

                    SetBGMVolume(currentMusicVol);
                }

                // Updates value if sound effects volume or sound effects mute has been changed
                if (IsSoundFxAltered())
                {
                    sfxOn = Options.soundFxOn;

                    if (currentSfxVol != Options.soundFxVolume)
                    {
                        currentSfxVol = Options.soundFxVolume;
                    }
                    else if (Options.soundFxMixerGroup != null)
                    {
                        float vol;
                        Options.soundFxMixerGroup.audioMixer.GetFloat(Options.volumeOfSFXMixer, out vol);
                        vol += 80f;
                        vol /= 100f;
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
                    if (backgroundMusic.nextClip != null)
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
        /// <param name="audio_source">Audio source / channel</param>
        /// <param name="clip">The audio data to play</param>
        /// <param name="playback_position">Play position of the clip.</param>
        internal static void PlayMusicFromSource(ref AudioSource audio_source, AudioClip clip, float playback_position)
        {
            try
            {
                // Set the current playing clip
                audio_source.clip = clip;
                // Start playing the source clip at the destinated play back position
                audio_source.time = playback_position;
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
        internal static void PlayBackgroundMusic(AudioClip clip, float playback_position)
        {
            // Set the music source to play the current music
            PlayMusicFromSource(ref musicSource, clip, playback_position);
            // Remove the call to next playing clip on queue
            Instance.backgroundMusic.nextClip = null;
            // Set the current playing clip
            Instance.backgroundMusic.currentClip = clip;
            // Get rid of the crossfade source if there is one
            if (crossfadeSource != null)
            {
                Destroy(crossfadeSource);
                crossfadeSource = null;
            }
        }

        /// <summary>
        /// Plays a background music.
        /// Only one background music can be active at a time.
        /// </summary>
        /// <param name="clip">The audio data to play</param>
        /// <param name="transition_mode">How should the music change from the current to the next. Use MusicTransition to specify type </param>
        /// <param name="transition_duration">Time in secs it takes to transition.</param>
        public static void PlayBGM(AudioClip clip, MusicTransition transition, float transition_duration)
        {
            // If it's the first music to be played then switch over immediately - meaning no transition effect
            if (Instance.backgroundMusic.currentClip == null)
            {
                transition = MusicTransition.Swift;
            }
            // Stop if trying to play thesame music
            else if (Instance.backgroundMusic.currentClip == clip)
            {
                return;
            }

            // Save the transition effect to be handled by the internal manager
            Instance.backgroundMusic.transition = transition;

            // Start playing from the beginning if there is no effect mode
            if (Instance.backgroundMusic.transition == MusicTransition.Swift)
            {
                PlayBackgroundMusic(clip, 0);
            }
            else
            {
                // Stop!!! Currenty performing a transition and has not finished
                if (Instance.backgroundMusic.nextClip != null)
                {
                    Debug.LogWarning("Trying to perform a transition on the background music while one is still active");
                    return;
                }

                transitionTime = Instance.backgroundMusic.transitionDuration = transition_duration;
                // Register the music volume limit or cap when transitioning
                musicVolCap = Options.musicVolume;
                // Set the next audio data clip to transition to
                Instance.backgroundMusic.nextClip = clip;
                // Inititalise the crossfade audio source if transition is a cross fade
                if (Instance.backgroundMusic.transition == MusicTransition.CrossFade)
                {
                    // Stop!!! Still performing a crossfade transition
                    if (crossfadeSource != null)
                    {
                        Debug.LogWarning("Trying to perform a transition on the background music while one is still active");
                        return;
                    }

                    // Initialise an AudioSource to the crossfade source
                    crossfadeSource = Instance.Attach2DAudioSource();
                    // The crossfade volume increases as the music volume decreases, so get its relative volume
                    crossfadeSource.volume = Mathf.Clamp01(musicVolCap - currentMusicVol);
                    crossfadeSource.priority = 0;
                    // Start playing the clip from the cross fade source
                    PlayMusicFromSource(ref crossfadeSource, Instance.backgroundMusic.nextClip, 0);
                }
            }
        }

        /// <summary>
        /// Plays a background music.
        /// Only one background music can be active at a time.
        /// </summary>
        /// <param name="clip">The audio data to play</param>
        /// <param name="transition_mode">How should the music change from the current to the next. Use MusicTransition to specify type </param>
        public static void PlayBGM(AudioClip clip, MusicTransition transition)
        {
            PlayBGM(clip, transition, 1f);
        }

        /// <summary>
        /// Plays a background music using the swift the transition mode.
        /// Only one background music can be active at a time.
        /// </summary>
        /// <param name="clip">The audio data to play</param>
        public static void PlayBGM(AudioClip clip)
        {
            PlayBGM(clip, MusicTransition.Swift);
        }

        /// <summary>
        /// Plays a background music from the Resources folder
        /// Only one background music can be active at a time.
        /// </summary>
        /// <param name="path">Path name of the target clip from the Resources folder</param>
        /// <param name="transition">Mode of music Transition.</param>
        /// <param name="transition_duration">Time in secs it takes to transition.</param>
        public static void PlayBGMFromResources(string path, MusicTransition transition, float transition_duration)
        {
            AudioClip musicClip = Resources.Load(path) as AudioClip;
            if (musicClip == null)
            {
                Debug.LogError(string.Format("AudioClip '{0}' not found at location {1}", path, System.IO.Path.Combine(Application.dataPath, "/Resources/")));
            }

            PlayBGM(musicClip, transition, transition_duration);
        }

        /// <summary>
        /// Plays a background music from the Resources folder
        /// Only one background music can be active at a time.
        /// </summary>
        /// <param name="path">Path name of the target clip from the Resources folder</param>
        /// <param name="transition">Mode of music Transition.</param>
        public static void PlayBGMFromResources(string path, MusicTransition transition)
        {
            AudioClip musicClip = Resources.Load(path) as AudioClip;
            if (musicClip == null)
            {
                Debug.LogError(string.Format("AudioClip '{0}' not found at location {1}", path, System.IO.Path.Combine(Application.dataPath, "/Resources/")));
            }

            PlayBGM(musicClip, transition, 1f);
        }

        /// <summary>
        /// Plays a background music from the Resources folder using the swift transition mode.
        /// Only one background music can be active at a time.
        /// </summary>
        /// <param name="path">Path name of the target clip from the Resources folder</param>
        public static void PlayBGMFromResources(string path)
        {
            PlayBGMFromResources(path, MusicTransition.Swift);
        }

        /// <summary>
        /// Stops the playing background music
        /// </summary>
        public static void StopBGM()
        {
            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }
        }

        /// <summary>
        /// Pauses the playing background music
        /// </summary>
        public static void PauseBGM()
        {
            if (musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }

        /// <summary>
        /// Resumes the playing background music
        /// </summary>
        public static void ResumeBGM()
        {
            if (!musicSource.isPlaying)
            {
                musicSource.UnPause();
            }
        }

        #endregion


        #region Sound Effect Functions

        /// <summary>
        /// Inner function used to play all resulting sound effects.
        /// Initialises some particular properties for the sound effect.
        /// </summary>
        /// <param name="sound_clip">The audio data to play</param>
        /// <param name="repeat">Loop or repeat the clip.</param>
        /// <param name="location">World location of the audio clip.</param>
        internal static GameObject InitialiseSoundFx(AudioClip sound_clip, bool loop, Vector2 location)
        {
            // Create a temporary game object to host our audio source
            GameObject host = new GameObject("TempAudio");
            // Set the temp audio's world position
            host.transform.position = location;
            // Parent it to the AudioManager until further notice
            host.transform.SetParent(Instance.transform);
            // Specity a tag for future use
            host.AddComponent<SoundEFfectTag>();

            // Add an audio source to that host
            AudioSource audioSource = host.AddComponent<AudioSource>() as AudioSource;
            // Set the mixer group for the sound effect if one exists
            audioSource.outputAudioMixerGroup = Options.soundFxMixerGroup;
            // Set that audio source clip to the one in paramaters
            audioSource.clip = sound_clip;
            // Set the mute value
            audioSource.mute = !Options.soundFxOn;
            // Set whether to loop the sound
            audioSource.loop = loop;
            // Set the audio source volume to the one in parameters
            audioSource.volume = Options.soundFxVolume;

            return host;
        }

        /// <summary>
        /// Plays a sound effect for a duration of time at a given location in world space and calls the specified callback function after the time is over.
        /// </summary>
        /// <returns>An audiosource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="duration">The length in time the clip should play</param>
        /// <param name="location">World location of the clip</param>
        /// <param name="save_to_repeat_pool">Save to the repeat pool to be managed later</param>
        /// <param name="callback">Action callback to be invoked after the sound has finished.</param>
        public static AudioSource PlaySFX(AudioClip clip, float duration, Vector2 location, bool save_to_repeat_pool, Action callback)
        {
            GameObject host = null;
            AudioSource source = null;

            if (save_to_repeat_pool)
            {
                int index = IndexOfRepeatingPool(clip.name);

                if (index >= 0)
                {
                    // Reset the duration if it exists
                    RepeatSound rs = Instance.repeatingSounds[index];
                    rs.duration = duration;
                    Instance.repeatingSounds[index] = rs;

                    return Instance.repeatingSounds[index].audioSource;
                }

                host = InitialiseSoundFx(clip, duration > clip.length, Vector2.zero);
                source = host.GetComponent<AudioSource>();

                // Create a new repeat sound
                RepeatSound repeatSound;
                repeatSound.name = clip.name;
                repeatSound.audioSource = source;
                repeatSound.duration = duration;
                repeatSound.callback = callback;

                // Add it to the list
                Instance.repeatingSounds.Add(repeatSound);

                // Start playing the sound
                source.Play();

                return source;
            }

            host = InitialiseSoundFx(clip, duration > clip.length, Vector2.zero);
            source = host.GetComponent<AudioSource>();

            source.Play();

            return source;
        }

        /// <summary>
        /// Plays a sound effect for a duration of time at a given location in world space and calls the specified callback function after the time is over
        /// </summary>
        /// <returns>An audiosource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="duration">The length in time the clip should play</param>
        /// <param name="location">World location of the clip</param>
        /// <param name="callback">Action callback to be invoked after the sound has finished.</param>
        public static AudioSource PlaySFX(AudioClip clip, float duration, Vector2 location, Action callback)
        {
            return PlaySFX(clip, duration, location, false, callback);
        }

        /// <summary>
        /// Plays a sound effect for a duration of time at a given location in world space
        /// </summary>
        /// <returns>An audiosource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="duration">The length in time the clip should play</param>
        /// <param name="location">World location of the clip</param>
        /// <param name="save_to_repeat_pool">Save to the repeat pool to be managed later</param>
        public static AudioSource PlaySFX(AudioClip clip, float duration, Vector2 location, bool save_to_repeat_pool)
        {
            return PlaySFX(clip, duration, location, save_to_repeat_pool, null);
        }

        /// <summary>
        /// Plays a sound effect for a duration of time and calls the specified callback function after the time is over
        /// </summary>
        /// <returns>An audiosource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="duration">The length in time the clip should play</param>
        /// <param name="callback">Action callback to be invoked after the sound has finished.</param>
        public static AudioSource PlaySFX(AudioClip clip, float duration, Action callback)
        {
            return PlaySFX(clip, duration, Vector2.zero, false, callback);
        }

        /// <summary>
        /// Plays a sound effect for a duration of time at a given location in world space
        /// </summary>
        /// <returns>An audiosource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="duration">The length in time the clip should play</param>
        /// <param name="location">World location of the clip</param>
        public static AudioSource PlaySFX(AudioClip clip, float duration, Vector2 location)
        {
            return PlaySFX(clip, duration, location, false, null);
        }

        /// <summary>
        /// Plays a sound effect for a duration of time
        /// </summary>
        /// <returns>An audiosource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="duration">The length in time the clip should play</param>
        /// <param name="save_to_repeat_pool">Save to the repeat pool to be managed later</param>
        public static AudioSource PlaySFX(AudioClip clip, float duration, bool save_to_repeat_pool)
        {
            return PlaySFX(clip, duration, Vector2.zero, save_to_repeat_pool, null);
        }

        /// <summary>
        /// Plays a sound effect for a duration of time
        /// </summary>
        /// <returns>An audiosource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="duration">The length in time the clip should play</param>
        public static AudioSource PlaySFX(AudioClip clip, float duration)
        {
            return PlaySFX(clip, duration, Vector2.zero, false, null);
        }

        /// <summary>
        /// Repeats a sound effect for a specified amount of times at a given location in world space 
        /// and calls the specified callback function after the sound is over.
        /// Automatically adds the clip to the repeat sound pool if repeat length is greater than 1
        /// </summary>
        /// <returns>An audiosource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="repeat">How many times in successions you want the clip to play.</param>
        /// <param name="location">World location of the clip</param>
        /// <param name="callback">Action callback to be invoked after the sound has finished.</param>
        public static AudioSource RepeatSFX(AudioClip clip, uint repeat, Vector2 location, Action callback)
        {
            if (repeat > 1)
            {
                int index = IndexOfRepeatingPool(clip.name);

                if (index >= 0)
                {
                    // Reset the duration if it exists in the pool
                    RepeatSound rs = Instance.repeatingSounds[index];
                    rs.duration = clip.length * repeat;
                    Instance.repeatingSounds[index] = rs;

                    return Instance.repeatingSounds[index].audioSource;
                }

                GameObject host = InitialiseSoundFx(clip, repeat > 1, Vector2.zero);
                AudioSource source = host.GetComponent<AudioSource>();

                // Create a new repeat sound
                RepeatSound repeatSound;
                repeatSound.name = clip.name;
                repeatSound.audioSource = source;
                repeatSound.duration = clip.length * repeat;
                repeatSound.callback = callback;

                // Add it to the list
                Instance.repeatingSounds.Add(repeatSound);

                // Start playing the sound
                source.Play();

                return source;
            }

            // Play one shot if repat length is less than or equal to 1
            return PlayOneShot(clip, location, callback);
        }

        /// <summary>
        /// Repeats a sound effect for a specified amount of times at a given location in world space
        /// </summary>
        /// <returns>An audiosource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="repeat">How many times in successions you want the clip to play.</param>
        /// <param name="location">World location of the clip</param>
        public static AudioSource RepeatSFX(AudioClip clip, uint repeat, Vector2 location)
        {
            return RepeatSFX(clip, repeat, location, null);
        }

        /// <summary>
        /// Repeats a sound effect for a specified amount of times
        /// </summary>
        /// <returns>An audiosource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="repeat">How many times in successions you want the clip to play</param>
        public static AudioSource RepeatSFX(AudioClip clip, uint repeat)
        {
            return RepeatSFX(clip, repeat, Vector2.zero, null);
        }

        /// <summary>
        /// Pauses all the sound effects in the game
        /// </summary>
        public static void PauseAllSFX()
        {
            AudioSource source;
            // Loop through all sound effects with the SoundEffectTag and update their properties
            foreach (SoundEFfectTag t in FindObjectsOfType<SoundEFfectTag>())
            {
                source = t.GetComponent<AudioSource>();
                if (source.isPlaying) source.Pause();
            }
        }

        /// <summary>
        /// Resumes all the sound effect in the game
        /// </summary>
        /// <param name="volume">New volume of all sound effects</param>
        public static void ResumeAllSFX()
        {
            AudioSource source;
            /// Loop through all sound effects with the SoundEffectTag and update their properties
            foreach (SoundEFfectTag t in FindObjectsOfType<SoundEFfectTag>())
            {
                source = t.GetComponent<AudioSource>();
                if (!source.isPlaying) source.UnPause();
            }
        }

        /// <summary>
        /// Inner function used to fire a function callback
        /// </summary>
        IEnumerator InvokeFunctionAfter(Action callback, float time)
        {
            yield return new WaitForSeconds(time);

            callback.Invoke();
        }

        /// <summary>
        /// Plays a sound effect once at a location in world space and calls the specified callback function after the sound is over
        /// </summary>
        /// <returns>An AudioSource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="location">World location of the clip</param>
        /// <param name="callback">Action callback to be invoked after clip has finished playing</param>
        public static AudioSource PlayOneShot(AudioClip clip, Vector2 location, Action callback)
        {
            GameObject host = InitialiseSoundFx(clip, false, location);

            AudioSource source = host.GetComponent<AudioSource>();
            source.spatialBlend = 0;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.Play();

            // Destroy the host after the clip has played
            Destroy(host, clip.length);

            if (callback != null)
            {
                Instance.StartCoroutine(Instance.InvokeFunctionAfter(callback, clip.length));
            }

            return source;
        }

        /// <summary>
        /// Plays a sound effect once at a location in world space
        /// </summary>
        /// <returns>An AudioSource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="location">World location of the clip</param>
        public static AudioSource PlayOneShot(AudioClip clip, Vector2 location)
        {
            return PlayOneShot(clip, location, null);
        }


        /// <summary>
        /// Plays a sound effect once and calls the specified callback function after the sound is over
        /// </summary>
        /// <returns>An AudioSource</returns>
        /// <param name="clip">The audio data to play</param>
        /// <param name="callback">Action callback to be invoked after clip has finished playing</param>
        public static AudioSource PlayOneShot(AudioClip clip, Action callback)
        {
            return PlayOneShot(clip, Vector2.zero, callback);
        }

        /// <summary>
        /// Plays a sound effect once
        /// </summary>
        /// <returns>An AudioSource</returns>
        /// <param name="clip">The audio data to play</param>
        public static AudioSource PlayOneShot(AudioClip clip)
        {
            return PlayOneShot(clip, Vector2.zero, null);
        }

        /// <summary>
        /// Plays a sound effect once from the specified resouce path
        /// </summary>
        /// <returns>An Audiosource</returns>
        /// <param name="path">Path name of the target clip from the Resources folder</param>
        /// <param name="callback">Action callback to be invoked after clip has finished playing</param>
        public static AudioSource PlayOneShotFromResources(string path, Action callback)
        {
            AudioClip clip = Resources.Load(path) as AudioClip;
            if (clip == null)
            {
                Debug.LogError(string.Format("AudioClip '{0}' not found at location {1}", path, System.IO.Path.Combine(Application.dataPath, "/Resources/")));
                return null;
            }

            return PlayOneShot(clip, callback);
        }

        /// <summary>
        /// Plays a sound effect once from the Resources folder
        /// </summary>
        /// <returns>An Audiosource</returns>
        /// <param name="path">Path name of the target clip from the Resources folder</param>
        public static AudioSource PlayOneShotFromResources(string path)
        {
            return PlayOneShotFromResources(path, null);
        }

        #endregion


        #region Setter Functions

        /// <summary>
        /// Toggles the mater mute that controls both background music & sound effect mute.
        /// </summary>
        /// <param name="flag">New toggle state of the mute controller.</param>
        public static void ToggleMute(bool flag)
        {
            ToggleBGMMute(flag);
            ToggleSFXMute(flag);
        }

        /// <summary>
        /// Toggles the background music mute.
        /// </summary>
        /// <param name="flag">New toggle state of the background music controller.</param>
        public static void ToggleBGMMute(bool flag)
        {
            Options.musicOn = flag;
            musicSource.mute = !Options.musicOn;
        }

        /// <summary>
        /// Toggles the sound effect mute.
        /// </summary>
        /// <param name="flag">New toggle state of the sound effect controller.</param>
        public static void ToggleSFXMute(bool flag)
        {
            Options.soundFxOn = flag;

            AudioSource source;
            // Loop through all sound effects with the SoundEffectTag and update their properties
            foreach (SoundEFfectTag t in FindObjectsOfType<SoundEFfectTag>())
            {
                source = t.GetComponent<AudioSource>();
                source.mute = !Options.soundFxOn;
            }

            sfxOn = Options.soundFxOn;
        }

        /// <summary>
        /// Sets the background music volume.
        /// </summary>
        /// <param name="volume">New volume of the background music.</param>
        public static void SetBGMVolume(float volume)
        {
            try
            {
                // Restrict the values to a range of [0 - 1] to suit the AudioManager
                volume = Mathf.Clamp01(volume);
                // Assign vol to all music volume variables
                musicSource.volume = currentMusicVol = Options.musicVolume = volume;

                // Is the AudioManager using a master mixer
                if (Options.musicMixerGroup != null)
                {
                    // Get the equivalent mixer volume, always [-80db ... 20db]
                    float mixerVol = -80f + (volume * 100f);
                    // Set the volume of the background music group
                    Options.musicMixerGroup.audioMixer.SetFloat(Options.volumeOfMusicMixer, mixerVol);
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
        public static void SetSFXVolume(float volume)
        {
            try
            {
                // Restrict the values to a range of [0 - 1] to suit the AudioManager
                volume = Mathf.Clamp01(volume);
                // Update the volume controllers of the sound effects
                currentSfxVol = Options.soundFxVolume = volume;

                AudioSource source;
                // Loop through all sound effects with the SoundEffectTag and update their properties
                foreach (SoundEFfectTag t in FindObjectsOfType<SoundEFfectTag>())
                {
                    source = t.GetComponent<AudioSource>();
                    source.volume = currentSfxVol;
                    source.mute = !Options.soundFxOn;
                }

                // Is the AudioManager using a master mixer
                if (Options.soundFxMixerGroup != null)
                {
                    // Get the equivalent mixer volume, always [-80db ... 20db]
                    float mixerVol = -80f + (volume * 100f);
                    // Set the volume of the sound effect group
                    Options.soundFxMixerGroup.audioMixer.SetFloat(Options.volumeOfSFXMixer, mixerVol);
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

        #endregion


        #region Player Prefs Functions

        /// <summary>
        /// Get the volume of the background music from disk
        /// </summary>
        /// <returns></returns>
        internal static float LoadBGMVolume()
        {
            return PlayerPrefs.HasKey(BgMusicVolKey) ? PlayerPrefs.GetFloat(BgMusicVolKey) : Options.DEFAULT_BGM_VOL;
        }

        /// <summary>
        /// Get the volume of the sound effect from disk
        /// </summary>
        /// <returns></returns>
        internal static float LoadSFXVolume()
        {
            return PlayerPrefs.HasKey(SoundFxVolKey) ? PlayerPrefs.GetFloat(SoundFxVolKey) : Options.DEFAULT_SFX_VOL;
        }

        /// <summary>
        /// Converts the integer value to a boolean representative value
        /// </summary>
        static bool ToBool(int integer)
        {
            return integer == 0 ? false : true;
        }

        /// <summary>
        /// Get the mute or disabled status of the background music from disk
        /// </summary>
        /// <returns>Returns the value of the background music mute key from the saved preferences if it exists or the defaut value if it does not</returns>
        internal static bool LoadBgMusicMuteStatus()
        {
            return PlayerPrefs.HasKey(BgMusicMuteKey) ? ToBool(PlayerPrefs.GetInt(BgMusicMuteKey)) : Options.musicOn;
        }

        /// <summary>
        /// Get the mute or disabled status of the sound effect from disk
        /// </summary>
        /// <returns>Returns the value of the sound effect mute key from the saved preferences if it exists or the defaut value if it does not</returns>
        internal static bool LoadSoundFxMuteStatus()
        {
            return PlayerPrefs.HasKey(SoundFxMuteKey) ? ToBool(PlayerPrefs.GetInt(SoundFxMuteKey)) : Options.soundFxOn;
        }

        /// <summary>
        /// Stores the volume and the mute status of the background music to disk.
        /// Note that all preferences would automatically get saved when this script gets destroyed 
        /// </summary>
        public static void SaveBGMPreferences()
        {
            PlayerPrefs.SetInt(BgMusicMuteKey, Options.musicOn ? 1 : 0);
            PlayerPrefs.SetFloat(BgMusicVolKey, Options.musicVolume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Stores the volume and the mute status of the sound effect to disk.
        /// Note that all preferences would automatically get saved when this script gets destroyed
        /// </summary>
        public static void SaveSFXPreferences()
        {
            PlayerPrefs.SetInt(SoundFxMuteKey, Options.soundFxOn ? 1 : 0);
            PlayerPrefs.SetFloat(SoundFxVolKey, Options.soundFxVolume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Removes all key and value pertaining to sound options from disk
        /// </summary>
        public static void ClearAllPreferences()
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
        public static void SaveAllPreferences()
        {
            PlayerPrefs.SetFloat(SoundFxVolKey, Options.soundFxVolume);
            PlayerPrefs.SetFloat(BgMusicVolKey, Options.musicVolume);
            PlayerPrefs.SetInt(SoundFxMuteKey, Options.soundFxOn ? 1 : 0);
            PlayerPrefs.SetInt(BgMusicMuteKey, Options.musicOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        #endregion


        #region Sound Assets Functions

        /// <summary>
        /// Clear the asset list pool of audio assets
        /// </summary>
        public static void EmptyAssetList()
        {
            SoundAssetPool.Clear();
        }

        /// <summary>
        /// Add a sound clip to asset list pool
        /// </summary>
        /// <param name="clip">Sound clip data</param>
        public static void AddToAssetList(AudioClip clip)
        {
            if (clip)
            {
                SoundAsset soundAsset;
                soundAsset.name = clip.name;
                soundAsset.clip = clip;

                SoundAssetPool.Add(soundAsset);
            }
        }

        /// <summary>
        /// Load all sound clips from the Resources folder path into the asset list pool
        /// </summary>
        /// <param name="path">Pathname of the target folder. When using the empty string (i.e, ""), the function will load the entire content of the resource folder</param>
        /// <param name="overwrite">Overwrites the current content(s) of the asset list pool</param>
        public static void LoadSoundsIntoAssetList(string path, bool overwrite)
        {
            // Get all clips from resource path
            AudioClip[] clips = Resources.LoadAll<AudioClip>(path);

            // Overwrite the current pool with the new one
            if (overwrite)
            {
                SoundAssetPool.Clear();
            }

           
            SoundAsset soundAsset;
            // Add every loaded sound resource to the asset list pool
            for (int i = 0; i < clips.Length; i++)
            {
                soundAsset.name = clips[i].name;
                soundAsset.clip = clips[i];
                SoundAssetPool.Add(soundAsset);
            }
        }

        /// <summary>
        /// Gets the AudioClip reference from the name supplied 
        /// </summary>
        /// <param name="clip_name">The name of the clip in the asset list pool </param>
        /// <returns>The AudioClip from the pool or null if no matching name can be found</returns>
        public static AudioClip GetClipFromAssetList(string clip_name)
        {
            // Search for each sound assets in the asset list pool 
            foreach (SoundAsset soundAsset in SoundAssetPool)
            {
                // Check if name is a match
                if (clip_name == soundAsset.name)
                {
                    return soundAsset.clip;
                }
            }

            Debug.LogWarning(clip_name +" does not exist in the asset list pool.");
            return null;
        }

        #endregion


        #region Other Functions
        /// <summary>
        /// Returns the index of a repeating sound in pool if one exists.
        /// </summary>
        /// <returns>Index of repeating sound or -1 is none exists</returns>
        /// <param name="name">The name of the repeating sound.</param>
        public static int IndexOfRepeatingPool(string name)
        {
            int index = 0;
            while (index < Instance.repeatingSounds.Count)
            {
                if (Instance.repeatingSounds[index].name == name)
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        /// <summary>
        /// Loads an AudioClip from the Resources folder
        /// </summary>
        /// <param name="path">Path name of the target clip from the Resources folder</param>
        /// <param name="save_to_pool">Option to save loaded clip into asset list pool for future reference</param>
        /// <returns>The Audioclip from the resource folder</returns>
        public static AudioClip LoadClipFromResources(string path, bool save_to_pool)
        {
            AudioClip clip = Resources.Load(path) as AudioClip;
            if (clip == null)
            {
                Debug.LogError((string.Format("AudioClip '{0}' not found at location {1}", path, System.IO.Path.Combine(Application.dataPath, "/Resources/"))));
                return null;
            }

            if (save_to_pool)
            {
                AddToAssetList(clip);
            }

            return clip;
        }

        /// <summary>
        /// Loads an AudioClip from the Resources folder
        /// </summary>
        /// <param name="path">Path name of the target clip from the Resources folder</param>
        /// /// <returns>The audioclip from the resource folder</returns>
        public static AudioClip LoadClipFromResources(string path)
        {
            return LoadClipFromResources(path, false);
        }

        #endregion
    }
}