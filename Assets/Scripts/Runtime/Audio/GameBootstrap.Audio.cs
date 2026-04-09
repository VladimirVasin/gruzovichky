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
        if (truckLoopAudioSource == null)
        {
            return;
        }

        float dayBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.6f, currentStylizedDaylight));
        float nightBlend = 1f - dayBlend;

        AudioClip targetClip = isTruckMoving ? truckRollClip : truckIdleClip;
        if (truckLoopAudioSource.clip != targetClip)
        {
            truckLoopAudioSource.clip = targetClip;
            truckLoopAudioSource.Play();
        }

        float targetVolume = isTruckMoving ? 0.56f : 0.38f;
        if (isTruckInteracting)
        {
            targetVolume = 0.28f;
        }

        float engineTime = Time.time * truckEngineAudioWobbleSpeed + truckEngineAudioPhaseOffset;
        float slowWobble = Mathf.Sin(engineTime * (isTruckMoving ? 1.75f : 1.1f)) * (isTruckMoving ? 0.035f : 0.022f);
        float textureWobble = (Mathf.PerlinNoise(engineTime * 0.45f, 7.3f) - 0.5f) * (isTruckMoving ? 0.06f : 0.03f);
        float targetPitch = (isTruckMoving ? 1.09f : 0.95f) * truckEngineAudioPitchBias + slowWobble + textureWobble;
        float targetVolumeWithLife = targetVolume * truckEngineAudioVolumeBias + Mathf.Abs(slowWobble) * 0.08f + Mathf.Max(0f, textureWobble) * 0.06f;

        truckLoopAudioSource.volume = Mathf.Lerp(truckLoopAudioSource.volume, targetVolumeWithLife, 2.5f * Time.deltaTime);
        truckLoopAudioSource.pitch = Mathf.Lerp(truckLoopAudioSource.pitch, targetPitch, 2.5f * Time.deltaTime);

        if (ambientAudioSource != null)
        {
            ambientAudioSource.volume = Mathf.Lerp(ambientAudioSource.volume, Mathf.Lerp(0.4f, 0.58f, dayBlend), 1.8f * Time.deltaTime);
        }

        if (dayBirdsAudioSource != null)
        {
            dayBirdsAudioSource.volume = Mathf.Lerp(dayBirdsAudioSource.volume, 0.54f * dayBlend, 1.8f * Time.deltaTime);
        }

        if (forestAudioSource != null)
        {
            forestAudioSource.volume = Mathf.Lerp(forestAudioSource.volume, Mathf.Lerp(0.36f, 0.68f, dayBlend), 1.8f * Time.deltaTime);
        }

        if (townAudioSource != null)
        {
            townAudioSource.volume = Mathf.Lerp(townAudioSource.volume, Mathf.Lerp(0.36f, 0.54f, dayBlend), 1.8f * Time.deltaTime);
        }

        if (nightWindAudioSource != null)
        {
            nightWindAudioSource.volume = Mathf.Lerp(nightWindAudioSource.volume, 0.56f * nightBlend, 1.8f * Time.deltaTime);
        }

        if (nightCricketsAudioSource != null)
        {
            nightCricketsAudioSource.volume = Mathf.Lerp(nightCricketsAudioSource.volume, 0.58f * nightBlend, 1.8f * Time.deltaTime);
        }

        if (gasStationAudioSource != null)
        {
            gasStationAudioSource.volume = Mathf.Lerp(gasStationAudioSource.volume, Mathf.Lerp(0.26f, 0.38f, dayBlend), 1.8f * Time.deltaTime);
        }

        UpdateDayNightAmbientOneShots(dayBlend, nightBlend);
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

        uiAudioSource.PlayOneShot(clip, volumeScale * 1.7f);
    }

    private void PlayTruckFx(AudioClip clip, float volumeScale)
    {
        if (truckFxAudioSource == null || clip == null)
        {
            return;
        }

        truckFxAudioSource.PlayOneShot(clip, volumeScale * 1.62f);
    }

    private void PlayForestWorkerFx(AudioClip clip, Vector3 worldPosition, float volumeScale)
    {
        if (forestWorkerAudioSource == null || clip == null)
        {
            return;
        }

        forestWorkerAudioSource.transform.position = worldPosition;
        forestWorkerAudioSource.PlayOneShot(clip, volumeScale * 1.68f);
    }

    private void PlayAmbientFx(AudioClip clip, Vector3 worldPosition, float volumeScale)
    {
        if (ambienceFxAudioSource == null || clip == null)
        {
            return;
        }

        ambienceFxAudioSource.transform.position = worldPosition;
        ambienceFxAudioSource.PlayOneShot(clip, volumeScale * 1.38f);
    }

    private void UpdateDayNightAmbientOneShots(float dayBlend, float nightBlend)
    {
        dayBirdTimer -= Time.deltaTime;
        warehouseCreakTimer -= Time.deltaTime;
        nightOwlTimer -= Time.deltaTime;
        lanternBuzzTimer -= Time.deltaTime;

        if (dayBlend > 0.25f && dayBirdTimer <= 0f)
        {
            PlayAmbientFx(dayBirdsClip, cameraFocusPoint + new Vector3(0f, 2.5f, 0f), Random.Range(0.36f, 0.5f));
            dayBirdTimer = Random.Range(5.5f, 9.5f);
        }

        if (dayBlend > 0.15f && warehouseCreakTimer <= 0f)
        {
            Vector3 warehousePosition = locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse)
                ? warehouse.RootObject.transform.position + new Vector3(0f, 0.9f, 0f)
                : cameraFocusPoint;
            PlayAmbientFx(warehouseCreakClip, warehousePosition, Random.Range(0.28f, 0.42f));
            warehouseCreakTimer = Random.Range(8f, 14f);
        }

        if (nightBlend > 0.25f && nightOwlTimer <= 0f)
        {
            PlayAmbientFx(owlClip, cameraFocusPoint + new Vector3(Random.Range(-3f, 3f), 3f, Random.Range(-3f, 3f)), Random.Range(0.32f, 0.46f));
            nightOwlTimer = Random.Range(10f, 18f);
        }

        if (nightBlend > 0.2f && lanternBuzzTimer <= 0f && roadLanterns.Count > 0)
        {
            int lanternIndex = Random.Range(0, roadLanterns.Count);
            Light lantern = roadLanterns[lanternIndex].Light;
            Vector3 lanternPosition = lantern != null ? lantern.transform.position : cameraFocusPoint;
            PlayAmbientFx(lanternBuzzClip, lanternPosition, Random.Range(0.26f, 0.38f));
            lanternBuzzTimer = Random.Range(6f, 12f);
        }
    }

    private AudioClip CreateUiPulseClip(string clipName, float frequency, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float envelope = Mathf.Exp(-26f * t);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * amplitude * 1.45f;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateWindClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float bed =
                Mathf.Sin(2f * Mathf.PI * 0.18f * t) * 0.45f +
                Mathf.Sin(2f * Mathf.PI * 0.31f * t + 1.7f) * 0.35f +
                Mathf.Sin(2f * Mathf.PI * 0.54f * t + 0.8f) * 0.2f;

            float hiss =
                Mathf.Sin(2f * Mathf.PI * 180f * t + Mathf.Sin(2f * Mathf.PI * 0.09f * t)) * 0.08f +
                Mathf.Sin(2f * Mathf.PI * 260f * t + 1.1f) * 0.05f;

            samples[i] = Mathf.Clamp((bed + hiss) * amplitude * 1.3f, -1f, 1f);
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateRustleClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float pulse =
                Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 0.41f * t + 0.6f)) * 0.7f +
                Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 0.73f * t + 2.1f)) * 0.3f;

            float leafy =
                Mathf.Sin(2f * Mathf.PI * 510f * t + Mathf.Sin(2f * Mathf.PI * 1.4f * t)) * 0.06f +
                Mathf.Sin(2f * Mathf.PI * 690f * t + 0.8f) * 0.05f;

            samples[i] = leafy * pulse * amplitude * 1.55f;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateDayBirdsClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float chirpGate =
                Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 0.42f * t + 0.4f)), 8f) +
                Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 0.31f * t + 2.1f)), 10f) * 0.7f;
            float chirp =
                Mathf.Sin(2f * Mathf.PI * (980f + Mathf.Sin(2f * Mathf.PI * 5.5f * t) * 120f) * t) * 0.12f +
                Mathf.Sin(2f * Mathf.PI * (1420f + Mathf.Sin(2f * Mathf.PI * 7.2f * t) * 180f) * t) * 0.08f;
            samples[i] = chirp * chirpGate * amplitude;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateForestChopClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float impactEnvelope = Mathf.Exp(-34f * t);
            float woodEnvelope = Mathf.Exp(-12f * t);
            float impact =
                Mathf.Sin(2f * Mathf.PI * 140f * t) * impactEnvelope * 0.95f +
                Mathf.Sin(2f * Mathf.PI * 210f * t + 0.4f) * impactEnvelope * 0.55f;
            float woodTail =
                Mathf.Sin(2f * Mathf.PI * 520f * t + 0.15f) * woodEnvelope * 0.18f +
                Mathf.Sin(2f * Mathf.PI * 760f * t + 0.72f) * woodEnvelope * 0.1f;

            samples[i] = (impact + woodTail) * amplitude * 1.8f;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateNightWindClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float bed =
                Mathf.Sin(2f * Mathf.PI * 0.12f * t) * 0.52f +
                Mathf.Sin(2f * Mathf.PI * 0.24f * t + 1.2f) * 0.28f +
                Mathf.Sin(2f * Mathf.PI * 0.49f * t + 0.5f) * 0.16f;
            float airy =
                Mathf.Sin(2f * Mathf.PI * 150f * t + Mathf.Sin(2f * Mathf.PI * 0.05f * t)) * 0.06f +
                Mathf.Sin(2f * Mathf.PI * 230f * t + 0.8f) * 0.04f;
            samples[i] = Mathf.Clamp((bed + airy) * amplitude, -1f, 1f);
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateNightCricketsClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float chirpPulse =
                Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 3.6f * t)), 12f) * 0.6f +
                Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 4.7f * t + 1.2f)), 14f) * 0.45f;
            float chirp =
                Mathf.Sin(2f * Mathf.PI * 3200f * t) * 0.06f +
                Mathf.Sin(2f * Mathf.PI * 4100f * t + 0.2f) * 0.035f;
            samples[i] = chirp * chirpPulse * amplitude;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateGasStationHumClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float hum =
                Mathf.Sin(2f * Mathf.PI * 82f * t) * 0.45f +
                Mathf.Sin(2f * Mathf.PI * 126f * t + 0.5f) * 0.22f +
                Mathf.Sin(2f * Mathf.PI * 240f * t + 1.3f) * 0.08f;
            float wobble = 0.82f + Mathf.Sin(2f * Mathf.PI * 0.17f * t) * 0.08f;
            samples[i] = hum * wobble * amplitude;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateTownHumClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float low =
                Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.4f +
                Mathf.Sin(2f * Mathf.PI * 142f * t + 0.3f) * 0.25f +
                Mathf.Sin(2f * Mathf.PI * 210f * t + 1.2f) * 0.14f;

            float sway = 0.75f + Mathf.Sin(2f * Mathf.PI * 0.12f * t) * 0.2f;
            samples[i] = low * sway * amplitude * 1.45f;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateWarehouseCreakClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float envelope = Mathf.Exp(-7.5f * t);
            float creak =
                Mathf.Sin(2f * Mathf.PI * (180f + t * 90f) * t) * 0.12f +
                Mathf.Sin(2f * Mathf.PI * (260f + t * 60f) * t + 0.4f) * 0.06f;
            samples[i] = creak * envelope * amplitude;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateOwlClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float envelope = Mathf.Exp(-2.4f * t);
            float hoot =
                Mathf.Sin(2f * Mathf.PI * (360f - t * 40f) * t) * 0.16f +
                Mathf.Sin(2f * Mathf.PI * (420f - t * 30f) * t + 0.18f) * 0.08f;
            samples[i] = hoot * envelope * amplitude;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateLanternBuzzClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float envelope = Mathf.Exp(-8f * t);
            float buzz =
                Mathf.Sin(2f * Mathf.PI * 92f * t) * 0.08f +
                Mathf.Sin(2f * Mathf.PI * 184f * t + 0.35f) * 0.04f +
                Mathf.Sin(2f * Mathf.PI * 276f * t + 1.1f) * 0.02f;
            samples[i] = buzz * envelope * amplitude;
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
                Mathf.Sin(2f * Mathf.PI * baseRpm * t) * 0.46f +
                Mathf.Sin(2f * Mathf.PI * (baseRpm * 1.97f) * t + 0.26f) * 0.22f +
                Mathf.Sin(2f * Mathf.PI * (baseRpm * 3.14f) * t + 0.9f) * 0.09f;
            float mechanicalTick =
                Mathf.Sin(2f * Mathf.PI * 188f * t + Mathf.Sin(2f * Mathf.PI * 2.8f * t) * 0.45f) * 0.035f +
                Mathf.Sin(2f * Mathf.PI * 246f * t + 1.1f) * 0.024f;
            float chassisRattle =
                Mathf.Sin(2f * Mathf.PI * 13.5f * t + 0.6f) * 0.03f +
                Mathf.Sin(2f * Mathf.PI * 27f * t + 1.8f) * 0.018f;

            samples[i] = (engineBody * idlePulse + mechanicalTick + chassisRattle) * amplitude * 1.55f;
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
                Mathf.Sin(2f * Mathf.PI * rpmSweep * t) * 0.24f +
                Mathf.Sin(2f * Mathf.PI * (rpmSweep * 1.63f) * t + 0.34f) * 0.16f +
                Mathf.Sin(2f * Mathf.PI * (rpmSweep * 2.38f) * t + 1.1f) * 0.07f;
            float drivetrain =
                Mathf.Sin(2f * Mathf.PI * 122f * t + Mathf.Sin(2f * Mathf.PI * 2.7f * t) * 0.25f) * 0.11f +
                Mathf.Sin(2f * Mathf.PI * 168f * t + 0.5f) * 0.075f;
            float road =
                Mathf.Sin(2f * Mathf.PI * 320f * t + Mathf.Sin(2f * Mathf.PI * 4.5f * t)) * 0.06f +
                Mathf.Sin(2f * Mathf.PI * 440f * t + 1.4f) * 0.045f +
                Mathf.Sin(2f * Mathf.PI * 560f * t + Mathf.Sin(2f * Mathf.PI * 1.2f * t) * 0.3f) * 0.022f;
            float loadPulse = 0.92f + Mathf.Sin(2f * Mathf.PI * 1.55f * t + 0.2f) * 0.08f;

            samples[i] = ((engine + drivetrain) * loadPulse + road) * amplitude * 1.56f;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateCargoThunkClip(string clipName, float duration, float impactAmplitude, float tailAmplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float impactEnvelope = Mathf.Exp(-40f * t);
            float tailEnvelope = Mathf.Exp(-9f * t);
            float impact =
                Mathf.Sin(2f * Mathf.PI * 170f * t) * impactEnvelope * impactAmplitude +
                Mathf.Sin(2f * Mathf.PI * 240f * t + 0.7f) * impactEnvelope * impactAmplitude * 0.55f;
            float tail =
                Mathf.Sin(2f * Mathf.PI * 620f * t + 0.2f) * tailEnvelope * tailAmplitude +
                Mathf.Sin(2f * Mathf.PI * 820f * t + 1.1f) * tailEnvelope * tailAmplitude * 0.6f;

            samples[i] = (impact + tail) * 1.7f;
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

    private AudioClip CreateMoneyRewardClip(string clipName, float duration, float amplitude)
    {
        return CreatePentatonicMotifClip(
            clipName,
            duration,
            amplitude,
            new[] { PentatonicE4, PentatonicG4, PentatonicA4, PentatonicC5 },
            new[] { 0f, 0.08f, 0.16f, 0.26f });
    }

    private AudioClip CreateBusPassbyClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float norm = t / Mathf.Max(0.001f, duration);
            float attack = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.18f, norm));
            float release = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, norm));
            float envelope = attack * release;

            float engineBed =
                Mathf.Sin(2f * Mathf.PI * 82f * t + 0.3f) * 0.26f +
                Mathf.Sin(2f * Mathf.PI * 124f * t + Mathf.Sin(2f * Mathf.PI * 1.8f * t) * 0.22f) * 0.18f;
            float tireHiss =
                Mathf.Sin(2f * Mathf.PI * 420f * t + 0.9f) * 0.04f +
                Mathf.Sin(2f * Mathf.PI * 560f * t + 1.7f) * 0.03f;
            float whoosh = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 105f, norm) * t + 0.15f) * 0.08f;

            samples[i] = Mathf.Clamp((engineBed + tireHiss + whoosh) * envelope * amplitude * 1.55f, -1f, 1f);
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

