using UnityEngine;
using System.Collections.Generic;

namespace DarkFort.Audio
{
    /// <summary>
    /// Manages all game audio - persists across scene reloads
    /// Music continues seamlessly, SFX can be triggered from anywhere
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        private static AudioManager _instance;
        public static AudioManager Instance => _instance;
        #endregion

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip missSound;
        [SerializeField] private AudioClip playerHurtSound;
        [SerializeField] private AudioClip monsterDeathSound;
        [SerializeField] private AudioClip playerDeathSound;
        [SerializeField] private AudioClip victorySound;
        [SerializeField] private AudioClip levelUpSound;
        [SerializeField] private AudioClip itemPickupSound;
        [SerializeField] private AudioClip itemUseSound;
        [SerializeField] private AudioClip itemSellSound;
        [SerializeField] private AudioClip itemBuySound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip doorOpenSound;
        [SerializeField] private AudioClip trapSound;
        [SerializeField] private AudioClip coinSound;
        [SerializeField] private AudioClip scrollSound;
        [SerializeField] private AudioClip equipSound;
        [SerializeField] private AudioClip errorSound;

        [Header("Volume Settings")]
        [Range(0f, 1f)][SerializeField] private float musicVolume = 0.5f;
        [Range(0f, 1f)][SerializeField] private float ambientVolume = 0.4f;
        [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.7f;

        // Cache for custom sounds
        private Dictionary<string, AudioClip> customClips = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            // Singleton with persistence
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Create audio sources if not assigned
            EnsureAudioSources();
            ApplyVolumeSettings();
        }

        private void EnsureAudioSources()
        {
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (ambientSource == null)
            {
                GameObject ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.SetParent(transform);
                ambientSource = ambientObj.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        private void ApplyVolumeSettings()
        {
            if (musicSource != null) musicSource.volume = musicVolume;
            if (ambientSource != null) ambientSource.volume = ambientVolume;
            if (sfxSource != null) sfxSource.volume = sfxVolume;
        }

        #region Music & Ambient

        public void PlayMusic(AudioClip clip, bool restart = false)
        {
            if (clip == null) return;

            // Don't restart if same clip is already playing
            if (!restart && musicSource.clip == clip && musicSource.isPlaying)
                return;

            musicSource.clip = clip;
            musicSource.Play();
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void PlayAmbient(AudioClip clip, bool restart = false)
        {
            if (clip == null) return;

            if (!restart && ambientSource.clip == clip && ambientSource.isPlaying)
                return;

            ambientSource.clip = clip;
            ambientSource.Play();
        }

        public void StopAmbient()
        {
            ambientSource.Stop();
        }

        #endregion

        #region Sound Effects - Named Methods

        public void PlayAttack() => PlaySFX(attackSound);
        public void PlayHit() => PlaySFX(hitSound);
        public void PlayMiss() => PlaySFX(missSound);
        public void PlayPlayerHurt() => PlaySFX(playerHurtSound);
        public void PlayMonsterDeath() => PlaySFX(monsterDeathSound);
        public void PlayPlayerDeath() => PlaySFX(playerDeathSound);
        public void PlayVictory() => PlaySFX(victorySound);
        public void PlayLevelUp() => PlaySFX(levelUpSound);
        public void PlayItemPickup() => PlaySFX(itemPickupSound);
        public void PlayItemUse() => PlaySFX(itemUseSound);
        public void PlayItemSell() => PlaySFX(itemSellSound);
        public void PlayItemBuy() => PlaySFX(itemBuySound);
        public void PlayButtonClick() => PlaySFX(buttonClickSound);
        public void PlayDoorOpen() => PlaySFX(doorOpenSound);
        public void PlayTrap() => PlaySFX(trapSound);
        public void PlayCoin() => PlaySFX(coinSound);
        public void PlayScroll() => PlaySFX(scrollSound);
        public void PlayEquip() => PlaySFX(equipSound);
        public void PlayError() => PlaySFX(errorSound);

        #endregion

        #region Core SFX Methods

        /// <summary>
        /// Play a sound effect (allows overlapping)
        /// </summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        /// <summary>
        /// Play a sound effect with custom volume
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, volume * sfxVolume);
        }

        /// <summary>
        /// Register a custom clip that can be played by name
        /// </summary>
        public void RegisterClip(string name, AudioClip clip)
        {
            if (string.IsNullOrEmpty(name) || clip == null) return;
            customClips[name] = clip;
        }

        /// <summary>
        /// Play a registered custom clip by name
        /// </summary>
        public void PlaySFX(string clipName)
        {
            if (customClips.TryGetValue(clipName, out AudioClip clip))
            {
                PlaySFX(clip);
            }
            else
            {
                Debug.LogWarning($"AudioManager: No clip registered with name '{clipName}'");
            }
        }

        #endregion

        #region Volume Control

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null) musicSource.volume = musicVolume;
        }

        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            if (ambientSource != null) ambientSource.volume = ambientVolume;
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            // Note: SFX uses PlayOneShot with volume parameter, so this affects future sounds
        }

        public void MuteAll(bool mute)
        {
            if (musicSource != null) musicSource.mute = mute;
            if (ambientSource != null) ambientSource.mute = mute;
            if (sfxSource != null) sfxSource.mute = mute;
        }

        public float MusicVolume => musicVolume;
        public float AmbientVolume => ambientVolume;
        public float SFXVolume => sfxVolume;

        #endregion
    }
}