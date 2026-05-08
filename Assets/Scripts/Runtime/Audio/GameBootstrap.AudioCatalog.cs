using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const string SoundOptionPrefsPrefix = "sound_fx_minimal_v1_";
    private const string WorkerGrassFootstepOptionId = "footsteps_worker_grass";
    private const string WorkerGrassFootstepAssetFolder = "Assets/Footsteps - Essentials/Footsteps_Grass/Footsteps_Grass_Walk";
    private const string NatureAmbienceAssetFolder = "Assets/Nature - Essentials";

    private sealed class SoundOptionEntry
    {
        public string Id;
        public string Category;
        public string Label;
        public AudioClip Clip;
        public float DefaultVolume;
        public float Volume;
        public bool IsLoop;
        public float PreviewSeconds;
    }

    private readonly List<SoundOptionEntry> soundOptionEntries = new();
    private readonly Dictionary<string, SoundOptionEntry> soundOptionById = new();
    private readonly Dictionary<AudioClip, SoundOptionEntry> soundOptionByClip = new();
    private bool generatedAudioClipsCreated;
    private Coroutine soundLoopPreviewCoroutine;
    private AudioClip mainMenuMusicClip;
    private AudioClip legacyMenuMusicClip;
    private AudioClip cityMusicClip;
    private AudioClip musicMorningClip;
    private AudioClip musicDayClip;
    private AudioClip musicEveningClip;
    private AudioClip musicNightClip;

    private void EnsureGeneratedAudioClipsCreated()
    {
        if (generatedAudioClipsCreated)
        {
            return;
        }

        uiSelectClip = LoadGeneratedAudioClip("ui_select", CreateUiPulseClip("UI_Select", 280f, 0.09f, 0.024f));
        menuHoverClip = LoadGeneratedAudioClip("menu_hover", CreateUiPulseClip("Menu_Hover", 356f, 0.1f, 0.028f));
        uiPanelOpenClip = LoadGeneratedAudioClip("ui_open", CreateUiPulseClip("UI_Open", 420f, 0.12f, 0.032f));
        uiPanelCloseClip = LoadGeneratedAudioClip("ui_close", CreateUiPulseClip("UI_Close", 220f, 0.1f, 0.026f));
        truckIdleClip = LoadGeneratedAudioClip("truck_idle", CreateTruckIdleClip("Truck_Idle", 2.6f, 0.042f));
        truckRollClip = LoadGeneratedAudioClip("truck_roll", CreateTruckRollClip("Truck_Roll", 1.6f, 0.041f));
        boatMotorClip = LoadGeneratedAudioClip("boat_motor", CreateBoatMotorClip("Boat_Motor", 3.8f, 0.038f));
        slotReelTickClip = LoadGeneratedAudioClip("slot_reel_tick", CreateUiPulseClip("Slot_Tick", 820f, 0.038f, 0.048f));
        slotWinClip = LoadGeneratedAudioClip("slot_win", CreatePentatonicMotifClip("Slot_Win", 1.1f, 0.10f,
            new[] { PentatonicE4, PentatonicG4, PentatonicA4, PentatonicC5, PentatonicE5 },
            new[] { 0f, 0.13f, 0.27f, 0.43f, 0.61f }));
        slotLoseClip = LoadGeneratedAudioClip("slot_lose", CreatePentatonicMotifClip("Slot_Lose", 0.85f, 0.09f,
            new[] { PentatonicC5, 466.16f, PentatonicG4, 311.13f },
            new[] { 0f, 0.16f, 0.34f, 0.54f }));
        tutorialGoalSuccessClip = LoadGeneratedAudioClip("tutorial_goal_success", CreatePentatonicMotifClip("Tutorial_Goal_Success", 1.1f, 0.10f,
            new[] { PentatonicE4, PentatonicG4, PentatonicA4, PentatonicC5, PentatonicE5 },
            new[] { 0f, 0.13f, 0.27f, 0.43f, 0.61f }));
        buildingCompleteClip = LoadGeneratedAudioClip("building_complete", CreatePentatonicMotifClip("Building_Complete", 0.65f, 0.08f,
            new[] { PentatonicD4, PentatonicG4, PentatonicC5 },
            new[] { 0f, 0.08f, 0.18f }));
        roadDragClip = LoadGeneratedAudioClip("road_drag", CreatePentatonicMotifClip("Road_Drag", 0.24f, 0.055f,
            new[] { PentatonicC4, PentatonicE4 },
            new[] { 0f, 0.06f }));
        buildingDemolishClip = LoadGeneratedAudioClip("building_demolish", CreateUiPulseClip("Building_Demolish", 180f, 0.18f, 0.034f));
        workerGrassFootstepClips = LoadWorkerGrassFootstepClips();
        natureCicadasClip = LoadNatureAudioClip("Ambiance_Cicadas_Loop_Stereo");
        natureForestBirdsClip = LoadNatureAudioClip("Ambiance_Forest_Birds_Loop_Stereo");
        natureNightClip = LoadNatureAudioClip("Ambiance_Night_Loop_Stereo");
        natureRainCalmClip = LoadNatureAudioClip("Ambiance_Rain_Calm_Loop_Stereo");
        natureRainStrongClip = LoadNatureAudioClip("Ambiance_Rain_Strong_Loop_Stereo");
        natureRiverClip = LoadNatureAudioClip("Ambiance_River_Moderate_Loop_Stereo");
        natureWindCalmClip = LoadNatureAudioClip("Ambiance_Wind_Calm_Loop_Stereo");
        natureWindForestClip = LoadNatureAudioClip("Ambiance_Wind_Forest_Loop_Stereo");
        mainMenuMusicClip = Resources.Load<AudioClip>("MainMenu1");
#if UNITY_EDITOR
        legacyMenuMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/MenuMusic.mp3");
#endif
        cityMusicClip = Resources.Load<AudioClip>("City1");
        musicMorningClip = Resources.Load<AudioClip>("MusicMorning");
        musicDayClip = Resources.Load<AudioClip>("MusicDay");
        musicEveningClip = Resources.Load<AudioClip>("MusicEvening");
        musicNightClip = Resources.Load<AudioClip>("MusicNight");

        generatedAudioClipsCreated = true;
        RebuildSoundOptionsCatalog();
    }

    private static AudioClip LoadGeneratedAudioClip(string id, AudioClip fallback)
    {
        AudioClip generated = Resources.Load<AudioClip>($"GeneratedAudio/Relaxed/{id}");
#if UNITY_EDITOR
        if (generated == null)
        {
            generated = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Resources/GeneratedAudio/Relaxed/{id}.wav");
        }
#endif

        return generated != null ? generated : fallback;
    }

    private static AudioClip[] LoadWorkerGrassFootstepClips()
    {
        List<AudioClip> clips = new();
        for (int i = 1; i <= 5; i++)
        {
            string clipName = $"Footsteps_Walk_Grass_Mono_{i:00}";
            AudioClip clip = Resources.Load<AudioClip>($"Audio/Footsteps/GrassWalk/{clipName}");
#if UNITY_EDITOR
            if (clip == null)
            {
                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{WorkerGrassFootstepAssetFolder}/{clipName}.wav");
            }
#endif
            if (clip != null)
            {
                clips.Add(clip);
            }
        }

        return clips.ToArray();
    }

    private static AudioClip LoadNatureAudioClip(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audio/Nature/{clipName}");
#if UNITY_EDITOR
        if (clip == null)
        {
            clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{NatureAmbienceAssetFolder}/{clipName}.wav");
        }
#endif

        return clip;
    }

    private void RebuildSoundOptionsCatalog()
    {
        soundOptionEntries.Clear();
        soundOptionById.Clear();
        soundOptionByClip.Clear();

        RegisterSoundOption("ui_select", "UI", "UI Select", uiSelectClip, 0.58f);
        RegisterSoundOption("menu_hover", "UI", "Menu Hover", menuHoverClip, 0.42f);
        RegisterSoundOption("ui_open", "UI", "Panel Open", uiPanelOpenClip, 0.52f);
        RegisterSoundOption("ui_close", "UI", "Panel Close", uiPanelCloseClip, 0.48f);

        RegisterSoundOption("truck_idle", "Transport", "Truck Idle", truckIdleClip, 0.48f, true, 1.4f);
        RegisterSoundOption("truck_roll", "Transport", "Truck Roll", truckRollClip, 0.50f, true, 1.4f);
        RegisterSoundOption("boat_motor", "Transport", "Boat Motor", boatMotorClip, 0.42f, true, 1.5f);

        RegisterSoundOption("slot_reel_tick", "Gambling", "Slot Reel Tick", slotReelTickClip, 0.34f);
        RegisterSoundOption("slot_win", "Gambling", "Slot Win", slotWinClip, 0.42f);
        RegisterSoundOption("slot_lose", "Gambling", "Slot Lose", slotLoseClip, 0.36f);

        RegisterSoundOption("tutorial_goal_success", "Tutorial", "Goal Success", tutorialGoalSuccessClip, 0.42f);

        RegisterSoundOption("building_complete", "Build", "Building Complete", buildingCompleteClip, 0.42f);
        RegisterSoundOption("road_drag", "Build", "Road Drag Complete", roadDragClip, 0.36f);
        RegisterSoundOption("building_demolish", "Build", "Building Demolish", buildingDemolishClip, 0.42f);

        AudioClip footstepPreviewClip = workerGrassFootstepClips != null && workerGrassFootstepClips.Length > 0
            ? workerGrassFootstepClips[0]
            : null;
        RegisterSoundOption(WorkerGrassFootstepOptionId, "Footsteps", "Worker Grass Footsteps", footstepPreviewClip, 0.32f);

        RegisterSoundOption("nature_forest_birds", "Nature", "Forest Birds", natureForestBirdsClip, 0.34f, true, 2.2f);
        RegisterSoundOption("nature_cicadas", "Nature", "Cicadas", natureCicadasClip, 0.22f, true, 2.2f);
        RegisterSoundOption("nature_night", "Nature", "Night Ambience", natureNightClip, 0.26f, true, 2.2f);
        RegisterSoundOption("nature_rain_calm", "Nature", "Calm Rain", natureRainCalmClip, 0.36f, true, 2.2f);
        RegisterSoundOption("nature_rain_strong", "Nature", "Strong Rain", natureRainStrongClip, 0.32f, true, 2.2f);
        RegisterSoundOption("nature_river", "Nature", "River", natureRiverClip, 0.30f, true, 2.2f);
        RegisterSoundOption("nature_wind_calm", "Nature", "Calm Wind", natureWindCalmClip, 0.24f, true, 2.2f);
        RegisterSoundOption("nature_wind_forest", "Nature", "Forest Wind", natureWindForestClip, 0.22f, true, 2.2f);

        RegisterSoundOption("music_main_menu", "Music", "Main Menu Theme (active)", mainMenuMusicClip, 1f, true, 2.2f);
        RegisterSoundOption("music_menu_legacy", "Music", "MenuMusic.mp3", legacyMenuMusicClip, 1f, true, 2.2f);
        RegisterSoundOption("music_city", "Music", "City Theme", cityMusicClip, 1f, true, 2.2f);
        RegisterSoundOption("music_morning", "Music", "Morning Theme", musicMorningClip, 1f, true, 2.2f);
        RegisterSoundOption("music_day", "Music", "Day Theme", musicDayClip, 1f, true, 2.2f);
        RegisterSoundOption("music_evening", "Music", "Evening Theme", musicEveningClip, 1f, true, 2.2f);
        RegisterSoundOption("music_night", "Music", "Night Theme", musicNightClip, 1f, true, 2.2f);
    }

    private void RegisterSoundOption(string id, string category, string label, AudioClip clip, float defaultVolume, bool isLoop = false, float previewSeconds = 1.1f)
    {
        if (clip == null || string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        SoundOptionEntry entry = new()
        {
            Id = id,
            Category = category,
            Label = label,
            Clip = clip,
            DefaultVolume = Mathf.Clamp01(defaultVolume),
            IsLoop = isLoop,
            PreviewSeconds = Mathf.Max(0.25f, previewSeconds)
        };
        entry.Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(GetSoundOptionPrefsKey(id), entry.DefaultVolume));
        soundOptionEntries.Add(entry);
        soundOptionById[id] = entry;
        soundOptionByClip[clip] = entry;
    }

    private static string GetSoundOptionPrefsKey(string id) => SoundOptionPrefsPrefix + id;

    private float GetAudioClipVolumeMultiplier(AudioClip clip)
    {
        return clip != null && soundOptionByClip.TryGetValue(clip, out SoundOptionEntry entry)
            ? Mathf.Clamp01(entry.Volume)
            : 1f;
    }

    private void SetSoundOptionVolume(string id, float volume)
    {
        if (!soundOptionById.TryGetValue(id, out SoundOptionEntry entry))
        {
            return;
        }

        entry.Volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(GetSoundOptionPrefsKey(entry.Id), entry.Volume);
        PlayerPrefs.Save();
        ApplyMusicOptionVolumes();
        RefreshSoundOptionsPanelUI();
    }

    private void ResetSoundOptionVolume(string id)
    {
        if (soundOptionById.TryGetValue(id, out SoundOptionEntry entry))
        {
            SetSoundOptionVolume(id, entry.DefaultVolume);
        }
    }

    private void ResetAllSoundOptionVolumes()
    {
        foreach (SoundOptionEntry entry in soundOptionEntries)
        {
            entry.Volume = entry.DefaultVolume;
            PlayerPrefs.SetFloat(GetSoundOptionPrefsKey(entry.Id), entry.Volume);
        }

        PlayerPrefs.Save();
        ApplyMusicOptionVolumes();
        RefreshSoundOptionsPanelUI();
        SessionDebugLogger.Log("UI", "Sound effect volumes reset to defaults.");
        PlayUiSound(uiSelectClip, 0.76f);
    }

    private float GetSoundOptionVolumeById(string id)
    {
        return soundOptionById.TryGetValue(id, out SoundOptionEntry entry)
            ? Mathf.Clamp01(entry.Volume)
            : 1f;
    }

    private void ApplyMusicOptionVolumes()
    {
        if (mainMenuMusicSource != null)
        {
            mainMenuMusicSource.volume = 0.20f * GetSoundOptionVolumeById("music_main_menu");
        }

        if (cityMusicSource != null)
        {
            cityMusicSource.volume = 0f * GetSoundOptionVolumeById("music_city");
        }
    }

    private void EnsureUiAudioSource()
    {
        if (uiAudioSource != null)
        {
            return;
        }

        uiAudioSource = CreateAudioSource("UIAudio", null, false, 0.96f, 0f, false);
        uiAudioSource.ignoreListenerPause = true;
    }

    private void PreviewSoundOption(string id)
    {
        if (!soundOptionById.TryGetValue(id, out SoundOptionEntry entry) || entry.Clip == null)
        {
            return;
        }

        EnsureUiAudioSource();
        StopSoundPreviewLoop();

        float volume = Mathf.Clamp01(entry.Volume);
        if (volume <= 0.001f)
        {
            return;
        }

        if (entry.IsLoop)
        {
            uiAudioSource.clip = entry.Clip;
            uiAudioSource.loop = true;
            uiAudioSource.volume = volume;
            uiAudioSource.Play();
            soundLoopPreviewCoroutine = StartCoroutine(StopSoundPreviewLoopAfter(entry.PreviewSeconds));
        }
        else
        {
            uiAudioSource.PlayOneShot(entry.Clip, volume * 1.7f);
        }

        SessionDebugLogger.Log("UI", $"Previewed sound option '{entry.Id}' at {Mathf.RoundToInt(volume * 100f)}%.");
    }

    private IEnumerator StopSoundPreviewLoopAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        StopSoundPreviewLoop();
    }

    private void StopSoundPreviewLoop()
    {
        if (soundLoopPreviewCoroutine != null)
        {
            StopCoroutine(soundLoopPreviewCoroutine);
            soundLoopPreviewCoroutine = null;
        }

        if (uiAudioSource != null && uiAudioSource.loop)
        {
            uiAudioSource.Stop();
            uiAudioSource.loop = false;
            uiAudioSource.clip = null;
        }
    }
}
