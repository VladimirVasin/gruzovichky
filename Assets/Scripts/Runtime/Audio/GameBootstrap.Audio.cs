using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private const float PentatonicC4 = 261.63f;
    private const float PentatonicD4 = 293.66f;
    private const float PentatonicE4 = 329.63f;
    private const float PentatonicG4 = 392f;
    private const float PentatonicA4 = 440f;
    private const float PentatonicC5 = 523.25f;
    private const float PentatonicE5 = 659.25f;

    private void UpdateAudio()
    {
        for (int i = 0; i < truckAgents.Count; i++)
        {
            TruckAgent truckAgent = truckAgents[i];
            if (truckAgent?.TruckLoopAudioSource == null)
            {
                continue;
            }

            bool truckHasDriverInside = truckAgent.Driver != null && !IsTruckOutOfMapForTrade(truckAgent);
            if (!truckHasDriverInside)
            {
                if (truckAgent.TruckLoopAudioSource.isPlaying)
                {
                    truckAgent.TruckLoopAudioSource.Stop();
                }

                truckAgent.TruckLoopAudioSource.clip = null;
                truckAgent.TruckLoopAudioSource.volume = 0f;
                continue;
            }

            bool truckIsCurrent = truckAgent == currentLoadedTruckAgent;
            bool truckIsActive = truckIsCurrent && (isTruckMoving || isTruckInteracting || truckAgent.Driver.IsOnActiveShift);
            AudioClip targetClip = truckIsActive && isTruckMoving ? truckRollClip : truckIdleClip;
            if (truckAgent.TruckLoopAudioSource.clip != targetClip)
            {
                truckAgent.TruckLoopAudioSource.clip = targetClip;
            }

            if (!truckAgent.TruckLoopAudioSource.isPlaying)
            {
                truckAgent.TruckLoopAudioSource.Play();
            }

            float targetVolume = truckIsActive ? (isTruckInteracting ? 0.55f : isTruckMoving ? 1.0f : 0.72f) : 0.38f;
            float pitchBias = truckAgent.EngineAudioPitchBias;
            float volumeBias = truckAgent.EngineAudioVolumeBias;
            float wobbleSpeed = truckAgent.EngineAudioWobbleSpeed;
            float phaseOffset = truckAgent.EngineAudioPhaseOffset;
            float engineTime = Time.time * wobbleSpeed + phaseOffset;
            float slowWobble = Mathf.Sin(engineTime * (truckIsActive && isTruckMoving ? 1.75f : 1.1f)) * (truckIsActive && isTruckMoving ? 0.035f : 0.022f);
            float textureWobble = (Mathf.PerlinNoise(engineTime * 0.45f, 7.3f) - 0.5f) * (truckIsActive && isTruckMoving ? 0.06f : 0.03f);
            float targetPitch = (truckIsActive && isTruckMoving ? 1.09f : 0.95f) * pitchBias + slowWobble + textureWobble;
            float targetVolumeWithLife = (targetVolume * volumeBias + Mathf.Abs(slowWobble) * 0.08f + Mathf.Max(0f, textureWobble) * 0.06f)
                * GetAudioClipVolumeMultiplier(targetClip);

            truckAgent.TruckLoopAudioSource.volume = Mathf.Lerp(truckAgent.TruckLoopAudioSource.volume, targetVolumeWithLife, 2.5f * Time.deltaTime);
            truckAgent.TruckLoopAudioSource.pitch = Mathf.Lerp(truckAgent.TruckLoopAudioSource.pitch, targetPitch, 2.5f * Time.deltaTime);
        }

        UpdateNatureAmbience();
        UpdateDayNightMusic();
    }

    private AudioSource CreateAudioSource(string name, Transform parent, bool loop, float volume, float spatialBlend, bool playOnAwake)
    {
        GameObject audioObject = new(name);
        if (parent != null)
        {
            audioObject.transform.SetParent(parent, false);
        }

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.playOnAwake = playOnAwake;
        source.loop = loop;
        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 5f;
        source.maxDistance = 24f;
        source.dopplerLevel = 0f;
        return source;
    }

    private void PlayUiSound(AudioClip clip, float volumeScale)
    {
        if (uiAudioSource == null || clip == null)
        {
            return;
        }

        uiAudioSource.PlayOneShot(clip, volumeScale * 1.7f * GetAudioClipVolumeMultiplier(clip));
    }

    private void UpdateDriverFootsteps(DriverAgent driver, bool isWalking)
    {
        if (driver == null)
        {
            return;
        }

        driver.FootstepCooldown = Mathf.Max(0f, driver.FootstepCooldown - Time.deltaTime);

        if (!isWalking ||
            driver.DriverObject == null ||
            !driver.DriverObject.activeSelf ||
            driver.IsInsideBuilding ||
            driver.WalkPhase == DriverRescuePhase.RidingLocalBus)
        {
            driver.FootstepPhaseIndex = Mathf.FloorToInt(driver.WalkAnimationTime / Mathf.PI);
            return;
        }

        if (workerGrassFootstepClips == null || workerGrassFootstepClips.Length == 0)
        {
            return;
        }

        int stepPhase = Mathf.FloorToInt((driver.WalkAnimationTime + Mathf.PI * 0.18f) / Mathf.PI);
        if (stepPhase == driver.FootstepPhaseIndex || driver.FootstepCooldown > 0f)
        {
            return;
        }

        driver.FootstepPhaseIndex = stepPhase;
        driver.FootstepCooldown = 0.12f;
        PlayDriverFootstep(driver, stepPhase);
    }

    private void PlayDriverFootstep(DriverAgent driver, int stepPhase)
    {
        if (driver == null || workerGrassFootstepClips == null || workerGrassFootstepClips.Length == 0)
        {
            return;
        }

        EnsureDriverFootstepAudioSource(driver);
        if (driver.FootstepAudioSource == null)
        {
            return;
        }

        float volume = 0.42f * GetSoundOptionVolumeById(WorkerGrassFootstepOptionId);
        if (volume <= 0.001f)
        {
            return;
        }

        int clipIndex = Mathf.Abs(driver.DriverId * 31 + stepPhase + driver.FootstepClipCursor) % workerGrassFootstepClips.Length;
        driver.FootstepClipCursor++;
        AudioClip clip = workerGrassFootstepClips[clipIndex];
        if (clip == null)
        {
            return;
        }

        driver.FootstepAudioSource.pitch = Random.Range(0.94f, 1.06f);
        driver.FootstepAudioSource.PlayOneShot(clip, volume);
    }

    private void EnsureDriverFootstepAudioSource(DriverAgent driver)
    {
        if (driver == null || driver.DriverObject == null || driver.FootstepAudioSource != null)
        {
            return;
        }

        AudioSource source = CreateAudioSource($"Footsteps_{driver.DriverId}", driver.DriverObject.transform, false, 0.38f, 1f, false);
        source.minDistance = 1.1f;
        source.maxDistance = 8.5f;
        source.priority = 170;
        driver.FootstepAudioSource = source;
    }

    private void UpdateNatureAmbience()
    {
        EnsureNatureAmbienceSources();

        float dt = Time.deltaTime;
        float daylight = Mathf.Clamp01(currentStylizedDaylight);
        float dayBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.76f, daylight));
        float nightBlend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 0.55f, daylight));
        float cycle01 = Mathf.Repeat(dayNightCycleTimer / DayNightCycleDuration, 1f);
        float eveningBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 0.70f, cycle01))
            * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.82f, 0.93f, cycle01)));
        float rain = Mathf.Clamp01(weatherRainIntensity);
        float strongRain = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.92f, rain));
        float windy = currentWeatherState == WeatherState.Windy || nextWeatherState == WeatherState.Windy ? 1f : 0f;
        float waterBlend = waterSurfaceTiles.Count > 0 ? 1f : 0f;

        UpdateRiverAmbienceSourcePosition();

        FadeNatureLoop(natureForestBirdsAudioSource, natureForestBirdsClip, 0.20f * dayBlend * (1f - rain * 0.70f), dt);
        FadeNatureLoop(natureCicadasAudioSource, natureCicadasClip, 0.12f * eveningBlend * (1f - rain * 0.80f), dt);
        FadeNatureLoop(natureNightAudioSource, natureNightClip, 0.18f * nightBlend * (1f - rain * 0.45f), dt);
        FadeNatureLoop(natureRainCalmAudioSource, natureRainCalmClip, 0.26f * rain * (1f - strongRain * 0.40f), dt);
        FadeNatureLoop(natureRainStrongAudioSource, natureRainStrongClip, 0.24f * strongRain, dt);
        FadeNatureLoop(natureRiverAudioSource, natureRiverClip, 0.13f * waterBlend, dt);
        FadeNatureLoop(natureWindCalmAudioSource, natureWindCalmClip, 0.10f * (0.55f + nightBlend * 0.35f) * (1f - rain * 0.30f), dt);
        FadeNatureLoop(natureWindForestAudioSource, natureWindForestClip, 0.08f * Mathf.Max(dayBlend, windy) * (1f - rain * 0.40f), dt);
    }

    private void EnsureNatureAmbienceSources()
    {
        EnsureNatureAmbienceSource(ref natureForestBirdsAudioSource, "NatureForestBirds", natureForestBirdsClip);
        EnsureNatureAmbienceSource(ref natureCicadasAudioSource, "NatureCicadas", natureCicadasClip);
        EnsureNatureAmbienceSource(ref natureNightAudioSource, "NatureNight", natureNightClip);
        EnsureNatureAmbienceSource(ref natureRainCalmAudioSource, "NatureRainCalm", natureRainCalmClip);
        EnsureNatureAmbienceSource(ref natureRainStrongAudioSource, "NatureRainStrong", natureRainStrongClip);
        EnsureRiverAmbienceSource();
        EnsureNatureAmbienceSource(ref natureWindCalmAudioSource, "NatureWindCalm", natureWindCalmClip);
        EnsureNatureAmbienceSource(ref natureWindForestAudioSource, "NatureWindForest", natureWindForestClip);
    }

    private void EnsureNatureAmbienceSource(ref AudioSource source, string name, AudioClip clip)
    {
        if (source != null || clip == null)
        {
            return;
        }

        source = CreateAudioSource(name, null, true, 0f, 0f, false);
        source.clip = clip;
        source.priority = 210;
        source.Play();
    }

    private void EnsureRiverAmbienceSource()
    {
        if (natureRiverClip == null)
        {
            return;
        }

        if (natureRiverAudioSource == null)
        {
            natureRiverAudioSource = CreateAudioSource("NatureRiver", null, true, 0f, 1f, false);
            natureRiverAudioSource.clip = natureRiverClip;
            natureRiverAudioSource.priority = 210;
            natureRiverAudioSource.Play();
        }

        ConfigureRiverAmbienceSource(natureRiverAudioSource);
        UpdateRiverAmbienceSourcePosition();
    }

    private static void ConfigureRiverAmbienceSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 8f;
        source.maxDistance = 64f;
        source.dopplerLevel = 0f;
    }

    private void UpdateRiverAmbienceSourcePosition()
    {
        if (natureRiverAudioSource == null)
        {
            return;
        }

        Vector3 listenerPosition = mainCamera != null
            ? mainCamera.transform.position
            : cameraFocusPoint + DioramaCameraOffset;
        Vector3 focusPoint = GetRiverAmbienceFocusPoint();
        float riverCenterZ = GridHeight - WaterRiverWidth * 0.5f;
        float riverDistance = riverCenterZ - focusPoint.z;

        // The river spans the whole top edge, so use a nearest-point proxy. This keeps panning along
        // the river loud while moving the camera inland fades the loop by map distance.
        natureRiverAudioSource.transform.position = new Vector3(
            listenerPosition.x,
            listenerPosition.y,
            listenerPosition.z + riverDistance);
    }

    private Vector3 GetRiverAmbienceFocusPoint()
    {
        if (isTruckCameraFocused)
        {
            TruckAgent focusedTruck = GetTruckAgent(selectedTruckNumber);
            if (focusedTruck?.TruckObject != null)
            {
                return focusedTruck.TruckObject.transform.position;
            }
        }

        return cameraFocusPoint;
    }

    private void FadeNatureLoop(AudioSource source, AudioClip clip, float targetBaseVolume, float dt)
    {
        if (source == null || clip == null)
        {
            return;
        }

        if (source.clip != clip)
        {
            source.clip = clip;
            source.Play();
        }

        if (!source.isPlaying)
        {
            source.Play();
        }

        float targetVolume = Mathf.Clamp01(targetBaseVolume) * GetAudioClipVolumeMultiplier(clip);
        source.volume = Mathf.MoveTowards(source.volume, targetVolume, dt * 0.22f);
    }

    private AudioClip CreateUiPulseClip(string clipName, float frequency, float duration, float amplitude)
    {
        duration = Mathf.Max(duration, 0.16f);
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float attack = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.018f, t));
            float release = Mathf.Exp(-11f * Mathf.Max(0f, t - 0.018f));
            float envelope = attack * release;
            float warmFrequency = frequency * 0.72f;
            float lowTap =
                Mathf.Sin(2f * Mathf.PI * warmFrequency * t) * 0.58f +
                Mathf.Sin(2f * Mathf.PI * warmFrequency * 1.5f * t + 0.45f) * 0.18f;
            float softClick = Mathf.Sin(2f * Mathf.PI * 86f * t + 0.2f) * Mathf.Exp(-24f * t) * 0.22f;
            samples[i] = Mathf.Clamp((lowTap + softClick) * envelope * amplitude * 0.95f, -1f, 1f);
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateTruckIdleClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float baseRpm = 42f + Mathf.Sin(2f * Mathf.PI * 0.48f * t + 0.2f) * 1.4f;
            float idlePulse = 0.86f + Mathf.Sin(2f * Mathf.PI * 1.8f * t + Mathf.Sin(2f * Mathf.PI * 0.31f * t) * 0.4f) * 0.12f;
            float engineBody =
                Mathf.Sin(2f * Mathf.PI * baseRpm * t) * 0.36f +
                Mathf.Sin(2f * Mathf.PI * (baseRpm * 1.86f) * t + 0.26f) * 0.13f +
                Mathf.Sin(2f * Mathf.PI * (baseRpm * 2.55f) * t + 0.9f) * 0.035f;
            float mechanicalTick =
                Mathf.Sin(2f * Mathf.PI * 128f * t + Mathf.Sin(2f * Mathf.PI * 1.8f * t) * 0.20f) * 0.012f +
                Mathf.Sin(2f * Mathf.PI * 184f * t + 1.1f) * 0.008f;
            float chassisRattle =
                Mathf.Sin(2f * Mathf.PI * 13.5f * t + 0.6f) * 0.03f +
                Mathf.Sin(2f * Mathf.PI * 27f * t + 1.8f) * 0.018f;

            samples[i] = (engineBody * idlePulse + mechanicalTick + chassisRattle) * amplitude * 0.95f;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateTruckRollClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float rpmSweep = 70f + Mathf.Sin(2f * Mathf.PI * 0.72f * t) * 3.6f;
            float engine =
                Mathf.Sin(2f * Mathf.PI * rpmSweep * t) * 0.20f +
                Mathf.Sin(2f * Mathf.PI * (rpmSweep * 1.52f) * t + 0.34f) * 0.10f +
                Mathf.Sin(2f * Mathf.PI * (rpmSweep * 2.12f) * t + 1.1f) * 0.03f;
            float drivetrain =
                Mathf.Sin(2f * Mathf.PI * 96f * t + Mathf.Sin(2f * Mathf.PI * 1.7f * t) * 0.12f) * 0.055f +
                Mathf.Sin(2f * Mathf.PI * 132f * t + 0.5f) * 0.032f;
            float road =
                Mathf.Sin(2f * Mathf.PI * 210f * t + Mathf.Sin(2f * Mathf.PI * 2.5f * t)) * 0.024f +
                Mathf.Sin(2f * Mathf.PI * 310f * t + 1.4f) * 0.016f +
                Mathf.Sin(2f * Mathf.PI * 420f * t + Mathf.Sin(2f * Mathf.PI * 0.9f * t) * 0.2f) * 0.008f;
            float loadPulse = 0.92f + Mathf.Sin(2f * Mathf.PI * 1.55f * t + 0.2f) * 0.08f;

            samples[i] = ((engine + drivetrain) * loadPulse + road) * amplitude * 0.92f;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateBoatMotorClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            // Low diesel hum: fundamental + 2nd + 3rd harmonics
            float hum =
                Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.46f +
                Mathf.Sin(2f * Mathf.PI * 130f * t + 0.38f) * 0.22f +
                Mathf.Sin(2f * Mathf.PI * 195f * t + 1.2f) * 0.09f;

            // Slow amplitude wobble simulating load variation
            float wobble = 0.85f + Mathf.Sin(2f * Mathf.PI * 0.52f * t) * 0.10f
                                 + Mathf.Sin(2f * Mathf.PI * 1.3f  * t + 0.6f) * 0.04f;

            // Gentle water slosh against hull (300-500 Hz ripple, 0.4 Hz beat)
            float slosh =
                Mathf.Sin(2f * Mathf.PI * 320f * t + Mathf.Sin(2f * Mathf.PI * 0.4f * t) * 0.5f) * 0.026f +
                Mathf.Sin(2f * Mathf.PI * 480f * t + 1.1f) * 0.014f;

            samples[i] = Mathf.Clamp((hum * wobble + slosh) * amplitude, -1f, 1f);
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreatePentatonicMotifClip(string clipName, float duration, float amplitude, float[] frequencies, float[] startTimes)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float sample = 0f;

            for (int eventIndex = 0; eventIndex < frequencies.Length && eventIndex < startTimes.Length; eventIndex++)
            {
                float localTime = t - startTimes[eventIndex];
                if (localTime < 0f)
                {
                    continue;
                }

                float envelope = Mathf.Exp(-8.5f * localTime);
                float shimmer = 1f + Mathf.Sin(2f * Mathf.PI * 5.2f * localTime) * 0.015f;
                float fundamental = Mathf.Sin(2f * Mathf.PI * frequencies[eventIndex] * localTime * shimmer) * 0.62f;
                float overtone = Mathf.Sin(2f * Mathf.PI * frequencies[eventIndex] * 2.01f * localTime + 0.22f) * 0.24f;
                float sparkle = Mathf.Sin(2f * Mathf.PI * frequencies[eventIndex] * 3.98f * localTime + 0.47f) * 0.09f;
                sample += (fundamental + overtone + sparkle) * envelope;
            }

            samples[i] = Mathf.Clamp(sample * amplitude * 1.55f, -1f, 1f);
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateClipFromSamples(string clipName, float[] samples)
    {
        AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, AudioSampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}


