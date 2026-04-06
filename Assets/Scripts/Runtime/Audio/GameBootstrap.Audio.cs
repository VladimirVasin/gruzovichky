using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void UpdateAudio()
    {
        if (truckLoopAudioSource == null)
        {
            return;
        }

        AudioClip targetClip = isTruckMoving ? truckRollClip : truckIdleClip;
        if (truckLoopAudioSource.clip != targetClip)
        {
            truckLoopAudioSource.clip = targetClip;
            truckLoopAudioSource.Play();
        }

        float targetVolume = isTruckMoving ? 0.18f : 0.11f;
        if (isTruckInteracting)
        {
            targetVolume = 0.07f;
        }

        truckLoopAudioSource.volume = Mathf.Lerp(truckLoopAudioSource.volume, targetVolume, 2.5f * Time.deltaTime);
        truckLoopAudioSource.pitch = Mathf.Lerp(truckLoopAudioSource.pitch, isTruckMoving ? 1.05f : 0.94f, 2.5f * Time.deltaTime);
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
        source.minDistance = 3f;
        source.maxDistance = 18f;
        source.dopplerLevel = 0f;
        return source;
    }

    private void PlayUiSound(AudioClip clip, float volumeScale)
    {
        if (uiAudioSource == null || clip == null)
        {
            return;
        }

        uiAudioSource.PlayOneShot(clip, volumeScale);
    }

    private void PlayTruckFx(AudioClip clip, float volumeScale)
    {
        if (truckFxAudioSource == null || clip == null)
        {
            return;
        }

        truckFxAudioSource.PlayOneShot(clip, volumeScale);
    }

    private AudioClip CreateUiPulseClip(string clipName, float frequency, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float envelope = Mathf.Exp(-26f * t);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * amplitude;
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

            samples[i] = Mathf.Clamp((bed + hiss) * amplitude, -1f, 1f);
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

            samples[i] = leafy * pulse * amplitude;
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
            samples[i] = low * sway * amplitude;
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
            float engine =
                Mathf.Sin(2f * Mathf.PI * 48f * t) * 0.55f +
                Mathf.Sin(2f * Mathf.PI * 96f * t + 0.3f) * 0.2f +
                Mathf.Sin(2f * Mathf.PI * 144f * t + 0.8f) * 0.12f;

            float wobble = 0.82f + Mathf.Sin(2f * Mathf.PI * 1.25f * t) * 0.08f;
            samples[i] = engine * wobble * amplitude;
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
            float wheel =
                Mathf.Sin(2f * Mathf.PI * 78f * t) * 0.28f +
                Mathf.Sin(2f * Mathf.PI * 118f * t + 0.4f) * 0.18f;
            float road =
                Mathf.Sin(2f * Mathf.PI * 320f * t + Mathf.Sin(2f * Mathf.PI * 4.5f * t)) * 0.06f +
                Mathf.Sin(2f * Mathf.PI * 440f * t + 1.4f) * 0.04f;

            samples[i] = (wheel + road) * amplitude;
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

            samples[i] = impact + tail;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateMoneyRewardClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float envelope = Mathf.Exp(-5.5f * t);
            float sparkle =
                Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.45f +
                Mathf.Sin(2f * Mathf.PI * 880f * t + 0.25f) * 0.3f +
                Mathf.Sin(2f * Mathf.PI * 1110f * t + 0.55f) * 0.16f;
            float glide = 1f + t * 0.8f;
            samples[i] = sparkle * glide * envelope * amplitude;
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

