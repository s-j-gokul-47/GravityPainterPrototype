using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene object that plays gameplay background music only while the player is actively in a level.
/// Place one "GameplayMusic" object in each scene so it stays visible in the Hierarchy.
/// </summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class GameplayMusicController : MonoBehaviour
{
    public const string ObjectName = "GameplayMusic";
    private const string MusicResourcePath = "Audio/BeyondTheHighPass";

    private static GameplayMusicController _instance;

    private static readonly HashSet<string> NonGameplayScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MainMenu",
        "LoadingScene",
        "DeveloperProceduralLevelSelect",
    };

    [SerializeField] [Range(0f, 1f)] private float volume = 0.45f;
    [SerializeField] private AudioClip musicClip;

    private AudioSource _source;
    private bool _inGameplayScene;
    private bool _levelCompleteOverlayVisible;

    public static GameplayMusicController Instance => _instance;

    private void Reset()
    {
        ConfigureAudioSource(GetComponent<AudioSource>());
        if (musicClip == null)
            musicClip = Resources.Load<AudioClip>(MusicResourcePath);
    }

    private void Awake()
    {
        _instance = this;

        _source = GetComponent<AudioSource>();
        ConfigureAudioSource(_source);

        if (musicClip == null)
            musicClip = Resources.Load<AudioClip>(MusicResourcePath);

        _source.clip = musicClip;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        _inGameplayScene = IsGameplayScene(SceneManager.GetActiveScene());
        RefreshPlayback();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_instance == this)
            _instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _inGameplayScene = IsGameplayScene(scene);
        _levelCompleteOverlayVisible = false;
        RefreshPlayback();
    }

    public static void NotifyLevelCompleteOverlayVisible(bool visible)
    {
        if (_instance == null)
            return;

        _instance._levelCompleteOverlayVisible = visible;
        _instance.RefreshPlayback();
    }

    public static bool IsGameplayScene(Scene scene)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.name))
            return false;

        return !NonGameplayScenes.Contains(scene.name);
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.volume = volume;
    }

    private void RefreshPlayback()
    {
        if (_source == null || _source.clip == null)
            return;

        _source.volume = volume;

        bool shouldPlay = _inGameplayScene && !_levelCompleteOverlayVisible;
        if (shouldPlay)
        {
            if (!_source.isPlaying)
                _source.Play();
        }
        else if (_source.isPlaying)
        {
            _source.Stop();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_source == null)
            _source = GetComponent<AudioSource>();

        if (_source != null)
            ConfigureAudioSource(_source);
    }
#endif
}
