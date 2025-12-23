using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop = false;
        [Tooltip("If false, this sound must finish playing before another sound can start")]
        public bool canBeInterrupted = true;
    }

    [SerializeField] private Sound[] sounds;
    private AudioSource audioSource;
    private Dictionary<string, Sound> soundDictionary;
    private Sound currentSound; // Track what's currently playing

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        soundDictionary = new Dictionary<string, Sound>();
        foreach (Sound sound in sounds)
        {
            soundDictionary[sound.name] = sound;
        }
    }

    public void Play(string soundName)
    {
        if (!soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            Debug.LogWarning($"Sound '{soundName}' not found!");
            return;
        }

        // Check if we're currently playing an uninterruptible sound
        if (audioSource.isPlaying && currentSound != null && !currentSound.canBeInterrupted)
        {
            //Debug.Log($"Cannot play '{soundName}' - '{currentSound.name}' must finish first");
            return; // Don't interrupt
        }

        // Don't restart if already playing the same looping sound
        if (sound.loop && audioSource.isPlaying && audioSource.clip == sound.clip)
        {
            return; // Already playing this looped sound
        }

        // Play the new sound
        currentSound = sound;
        audioSource.clip = sound.clip;
        audioSource.volume = sound.volume;
        audioSource.pitch = sound.pitch;
        audioSource.loop = sound.loop;
        audioSource.Play();
        
        //Debug.Log($"Playing sound: {soundName}");
    }

    public void Stop()
    {
        audioSource.Stop();
        currentSound = null;
    }

    public void Pause()
    {
        audioSource.Pause();
    }
    
    // Check if a specific sound is currently playing
    public bool IsPlaying(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            return audioSource.isPlaying && audioSource.clip == sound.clip;
        }
        return false;
    }

    // Check if any uninterruptible sound is playing
    public bool IsPlayingUninterruptibleSound()
    {
        return audioSource.isPlaying && currentSound != null && !currentSound.canBeInterrupted;
    }
}
