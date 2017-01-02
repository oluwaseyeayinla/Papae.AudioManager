using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Papae2D.AudioEngine
{
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
    /// Background music properties for the Sound Manager
    /// </summary>
    [Serializable]
    public struct BackgroundMusic
    {
        public AudioClip currentClip;
        public AudioClip nextClip;
        public MusicTransition transition;
        public float transitionDuration;
    }

    /// <summary>
    /// Structure and properties for a repeating sound
    /// </summary>
    [Serializable]
    public struct RepeatSound
    {
        public string name;
        public AudioSource audioSource;
        public float duration;
        public Action callback;
    }

    /// <summary>
    /// Cache information of sound asset attacted to the Sound Manager
    /// </summary>
    [Serializable]
    public struct SoundAsset
    {
        /// <summary>
        /// Key information of sound asset
        /// </summary>
        public string name;
        /// <summary>
        /// Value information of sound asset
        /// </summary>
        public AudioClip clip;
    }

    /// <summary>
    /// The regulator of all things with regards to sound
    /// </summary>
    [Serializable]
    public class AudioOptions : MonoBehaviour
    {
        /// <summary>
        /// Default volume of the background music
        /// </summary>
        public readonly float DEFAULT_BGM_VOL = 0.35f;
        /// <summary>
        /// Default volume of the sound effects
        /// </summary>
        public readonly float DEFAULT_SFX_VOL = 0.80f;

        #region Inspector Variables

        [Header("Background Music Properties")]

        /// <summary>
        /// Is the background music mute
        /// </summary>	
        [Tooltip("Is the background music mute")]
        public bool musicOn = true;

        /// <summary>
        /// The background music volume
        /// </summary>
        [Tooltip("The background music volume")]
        [Range(0, 1)]
        public float musicVolume = 0f;

        /// <summary>
        /// Use the current music volume settings on application start
        /// </summary>
        [Tooltip("Use the current music volume settings on application start")]
        public bool useMusicVolOnStart = false;

        /// <summary>
        /// The target group for the background music to route its their signals
        /// </summary>
        [Tooltip("The target group for the background music to route its their signals. If none is to be used, then leave unassigned or blank")]
        public AudioMixerGroup musicMixerGroup;

        /// <summary>
        /// The exposed volume parameter name of the music mixer group 
        /// </summary>
        [Tooltip("The exposed volume parameter name of the music mixer group")]
        public string volumeOfMusicMixer = string.Empty;

        
        [Space(3)]


        [Header("Sound Effect Properties")]

        /// <summary>
        /// Is the sound effects mute
        /// </summary>
        [Tooltip("The sound effects volume")]
        public bool soundFxOn = true;

        /// <summary>
        /// The sound effects volume
        /// </summary>
        [Tooltip("The sound effects volume")]
        [Range(0, 1)]
        public float soundFxVolume = 0f;

        /// <summary>
        /// Use the current sound effect volume settings on application start
        /// </summary>
        [Tooltip("Use the current sound effect volume settings on application start")]
        public bool useSfxVolOnStart = false;

        /// <summary>
        /// The target group for the sound effects to route its their signals
        /// </summary>
        [Tooltip("The target group for the sound effects to route its their signals. If none is to be used, then leave unassigned or blank")]
        public AudioMixerGroup soundFxMixerGroup;

        /// <summary>
        /// The exposed volume parameter name of the sound effects mixer group 
        /// </summary>
        [Tooltip("The exposed volume parameter name of the sound effects mixer group")]
        public string volumeOfSFXMixer = string.Empty;

        #endregion
    }

    /// <summary>
    /// Tag attached to all sound effects. This is my simply way of not using tags. 
    /// I find that I forget to add the required tag in the Tags and Layers Editor 
    /// </summary>
    public class SoundEFfectTag : MonoBehaviour
    {

    }
}

