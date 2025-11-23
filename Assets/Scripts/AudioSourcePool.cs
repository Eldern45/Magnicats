using System.Collections.Generic;
using UnityEngine;

public class AudioSourcePool : MonoBehaviour
{
    public static AudioSourcePool Instance;
    public AudioSource AudioSourcePrefab;

    private List<AudioSource> AudioSources;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        AudioSources = new List<AudioSource>();
    }

    public AudioSource GetSource()
    {
        foreach (AudioSource source in AudioSources)
        {
            if (!source.isPlaying) return source;
        }

        AudioSource NewSource = GameObject.Instantiate(AudioSourcePrefab, transform);
        AudioSources.Add(NewSource);
        return NewSource;
    }
}
