using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Audio/Audio Preload Player")]
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class AudioPreloadPlayer : MonoBehaviour
{
    public static AudioPreloadPlayer Instance { get; private set; }
    [SerializeField] private List<AudioClip> audioList = new List<AudioClip>();
    [SerializeField] private bool preloadOnStart = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>(System.StringComparer.OrdinalIgnoreCase);
    private AudioSource templateSource;
    private List<AudioSource> sourcePool = new List<AudioSource>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
        templateSource = GetComponent<AudioSource>();
        templateSource.loop = false;
        sourcePool.Add(templateSource);

        if (preloadOnStart)
        {
            PreloadAll();
        }
    }

    public void PreloadAll()
    {
        cache.Clear();
        for (int i = 0; i < audioList.Count; i++)
        {
            var clip = audioList[i];
            if (clip == null) continue;
            cache[clip.name] = clip;
            if (!clip.preloadAudioData)
            {
                clip.LoadAudioData();
            }
        }
    }

    private AudioSource GetAvailableSource()
    {
        for (int i = 0; i < sourcePool.Count; i++)
        {
            if (!sourcePool[i].isPlaying)
            {
                return sourcePool[i];
            }
        }

        var newSource = gameObject.AddComponent<AudioSource>();
        newSource.outputAudioMixerGroup = templateSource.outputAudioMixerGroup;
        newSource.spatialBlend = templateSource.spatialBlend;
        newSource.panStereo = templateSource.panStereo;
        newSource.spread = templateSource.spread;
        newSource.dopplerLevel = templateSource.dopplerLevel;
        newSource.rolloffMode = templateSource.rolloffMode;
        newSource.minDistance = templateSource.minDistance;
        newSource.maxDistance = templateSource.maxDistance;
        newSource.bypassEffects = templateSource.bypassEffects;
        newSource.bypassListenerEffects = templateSource.bypassListenerEffects;
        newSource.bypassReverbZones = templateSource.bypassReverbZones;
        newSource.priority = templateSource.priority;
        newSource.mute = templateSource.mute;
        
        newSource.loop = false;
        newSource.playOnAwake = false;
        
        sourcePool.Add(newSource);
        return newSource;
    }

    public void PlayByName(string clipName, float volume = 1f, float pitch = 1f)
    {
        if (string.IsNullOrEmpty(clipName)) return;
        if (!cache.TryGetValue(clipName, out var clip)) return;

        var source = GetAvailableSource();
        source.clip = clip;
        source.volume = templateSource.volume * Mathf.Clamp01(volume);
        source.pitch = pitch;
        source.Play();
    }

    public static void Play(string clipName, float volume = 1f, float pitch = 1f)
    {
        Instance?.PlayByName(clipName, volume, pitch);
    }
}
