using UnityEngine;

public partial class GameBootstrap
{
    private AudioSource musicMorningSource;
    private AudioSource musicDaySource;
    private AudioSource musicEveningSource;
    private AudioSource musicNightSource;

    private const float MusicLayerFadeSpeed = 0.037f; // 0.22 / 6s = full crossfade in 6 seconds
    private const float MusicLayerMaxVolume = 0.22f;

    private int musicActiveBand = -1; // 0=night 1=morning 2=day 3=evening

    private void SetupDayNightMusic()
    {
        EnsureGeneratedAudioClipsCreated();
        AudioClip morningClip = musicMorningClip;
        AudioClip dayClip     = musicDayClip;
        AudioClip eveningClip = musicEveningClip;
        AudioClip nightClip   = musicNightClip;

        if (morningClip == null || dayClip == null || eveningClip == null || nightClip == null)
            return;

        musicMorningSource = CreateAudioSource("MusicMorning", null, true, 0f, 0f, false);
        musicDaySource     = CreateAudioSource("MusicDay",     null, true, 0f, 0f, false);
        musicEveningSource = CreateAudioSource("MusicEvening", null, true, 0f, 0f, false);
        musicNightSource   = CreateAudioSource("MusicNight",   null, true, 0f, 0f, false);

        musicMorningSource.clip = morningClip;
        musicDaySource.clip     = dayClip;
        musicEveningSource.clip = eveningClip;
        musicNightSource.clip   = nightClip;

        // Only the active track starts playing; others are ready but paused at 0
        int band = GetDayNightBand();
        musicActiveBand = band;

        musicMorningSource.volume = band == 1 ? MusicLayerMaxVolume * GetSoundOptionVolumeById("music_morning") : 0f;
        musicDaySource.volume     = band == 2 ? MusicLayerMaxVolume * GetSoundOptionVolumeById("music_day") : 0f;
        musicEveningSource.volume = band == 3 ? MusicLayerMaxVolume * GetSoundOptionVolumeById("music_evening") : 0f;
        musicNightSource.volume   = band == 0 ? MusicLayerMaxVolume * GetSoundOptionVolumeById("music_night") : 0f;

        musicMorningSource.Play();
        musicDaySource.Play();
        musicEveningSource.Play();
        musicNightSource.Play();

        // Seek inactive tracks to 0 so they're ready from the top when their turn comes
        if (band != 1) { musicMorningSource.time = 0f; musicMorningSource.Pause(); }
        if (band != 2) { musicDaySource.time     = 0f; musicDaySource.Pause(); }
        if (band != 3) { musicEveningSource.time = 0f; musicEveningSource.Pause(); }
        if (band != 0) { musicNightSource.time   = 0f; musicNightSource.Pause(); }

        // Unpause the active one
        GetActiveMusicSource(band)?.UnPause();
    }

    private void ResumeDayNightMusic()
    {
        // Only resume the currently active track; others stay paused at 0
        GetActiveMusicSource(musicActiveBand)?.UnPause();
    }

    private void PauseDayNightMusic()
    {
        musicMorningSource?.Pause();
        musicDaySource?.Pause();
        musicEveningSource?.Pause();
        musicNightSource?.Pause();
    }

    private void UpdateDayNightMusic()
    {
        if (musicMorningSource == null) return;

        int band = GetDayNightBand();

        if (band != musicActiveBand)
        {
            // Crossfade: reset incoming track to beginning, then unpause it
            AudioSource incoming = GetActiveMusicSource(band);
            if (incoming != null)
            {
                incoming.time = 0f;
                incoming.UnPause();
            }
            musicActiveBand = band;
        }

        AudioSource activeSource = GetActiveMusicSource(band);
        if (activeSource != null && !activeSource.isPlaying && !AudioListener.pause)
        {
            activeSource.UnPause();
        }

        float vm = band == 1 ? MusicLayerMaxVolume * GetSoundOptionVolumeById("music_morning") : 0f;
        float vd = band == 2 ? MusicLayerMaxVolume * GetSoundOptionVolumeById("music_day") : 0f;
        float ve = band == 3 ? MusicLayerMaxVolume * GetSoundOptionVolumeById("music_evening") : 0f;
        float vn = band == 0 ? MusicLayerMaxVolume * GetSoundOptionVolumeById("music_night") : 0f;

        float dt = Time.unscaledDeltaTime * MusicLayerFadeSpeed;
        musicMorningSource.volume = Mathf.MoveTowards(musicMorningSource.volume, vm, dt);
        musicDaySource.volume     = Mathf.MoveTowards(musicDaySource.volume,     vd, dt);
        musicEveningSource.volume = Mathf.MoveTowards(musicEveningSource.volume, ve, dt);
        musicNightSource.volume   = Mathf.MoveTowards(musicNightSource.volume,   vn, dt);

        // Pause outgoing tracks once fully faded out
        if (band != 1 && musicMorningSource.volume == 0f && musicMorningSource.isPlaying) musicMorningSource.Pause();
        if (band != 2 && musicDaySource.volume     == 0f && musicDaySource.isPlaying)     musicDaySource.Pause();
        if (band != 3 && musicEveningSource.volume == 0f && musicEveningSource.isPlaying) musicEveningSource.Pause();
        if (band != 0 && musicNightSource.volume   == 0f && musicNightSource.isPlaying)   musicNightSource.Pause();
    }

    // 0=night, 1=morning, 2=day, 3=evening; mirrors GetTimeOfDayLabel() thresholds
    private int GetDayNightBand()
    {
        float norm = dayNightCycleTimer / DayNightCycleDuration;
        if      (norm < 0.25f) return 0;
        else if (norm < 0.50f) return 1;
        else if (norm < 0.75f) return 2;
        else                   return 3;
    }

    private AudioSource GetActiveMusicSource(int band) => band switch
    {
        0 => musicNightSource,
        1 => musicMorningSource,
        2 => musicDaySource,
        3 => musicEveningSource,
        _ => null
    };
}

