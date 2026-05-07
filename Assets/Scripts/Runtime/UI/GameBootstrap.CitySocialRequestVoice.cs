using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float CitySocialVoiceBaseVolume = 0.18f;
    private readonly Dictionary<int, AudioClip[]> citySocialVoiceClipCache = new();
    private AudioSource citySocialVoiceAudioSource;

    private readonly struct CitySocialVoiceProfile
    {
        public readonly int Id;
        public readonly float BaseFrequency;
        public readonly float Brightness;
        public readonly float Formant;
        public readonly float Roughness;

        public CitySocialVoiceProfile(int id, float baseFrequency, float brightness, float formant, float roughness)
        {
            Id = id;
            BaseFrequency = baseFrequency;
            Brightness = brightness;
            Formant = formant;
            Roughness = roughness;
        }
    }

    private void ResetCitySocialVoice()
    {
        EnsureCitySocialVoiceAudioSource();
        if (citySocialVoiceAudioSource != null)
        {
            citySocialVoiceAudioSource.Stop();
        }
    }

    private void PlayCitySocialVoiceWord(int visibleWordIndex)
    {
        EnsureCitySocialVoiceAudioSource();
        if (citySocialVoiceAudioSource == null || visibleWordIndex <= 0)
        {
            return;
        }

        int speakerId = GetCitySocialVoiceSpeakerId();
        bool isNarrator = citySocialSpeakingSide < 0;
        bool failedLine = citySocialConversationOutcome == CitySocialConversationOutcome.Failure && !isNarrator;
        if (failedLine && visibleWordIndex % 9 == 0)
        {
            return;
        }

        CitySocialVoiceProfile profile = GetCitySocialVoiceProfile(speakerId, isNarrator);
        AudioClip[] clips = GetCitySocialVoiceClips(profile);
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        int variant = Mathf.Abs(speakerId * 31 + visibleWordIndex * 17) % clips.Length;
        AudioClip clip = clips[variant];
        if (clip == null)
        {
            return;
        }

        float moodPitch = GetCitySocialVoiceMoodPitch(isNarrator);
        float wordWobble = 1f + Mathf.Sin((speakerId + visibleWordIndex * 13) * 1.37f) * 0.045f;
        citySocialVoiceAudioSource.pitch = Mathf.Clamp(moodPitch * wordWobble, 0.72f, 1.42f);
        float volume = CitySocialVoiceBaseVolume * (isNarrator ? 0.48f : failedLine ? 0.82f : 1f);
        citySocialVoiceAudioSource.PlayOneShot(clip, volume);
    }

    private void EnsureCitySocialVoiceAudioSource()
    {
        if (citySocialVoiceAudioSource != null)
        {
            return;
        }

        citySocialVoiceAudioSource = CreateAudioSource("CitySocialVoice", null, false, 0.8f, 0f, false);
        citySocialVoiceAudioSource.ignoreListenerPause = true;
        citySocialVoiceAudioSource.priority = 120;
    }

    private int GetCitySocialVoiceSpeakerId()
    {
        CitySocialIntroductionRequest request = activeCitySocialIntroductionRequest;
        if (request != null)
        {
            if (citySocialSpeakingSide == 0)
            {
                return request.RequesterId;
            }

            if (citySocialSpeakingSide == 1)
            {
                return request.TargetId;
            }
        }

        return -7001;
    }

    private float GetCitySocialVoiceMoodPitch(bool isNarrator)
    {
        if (isNarrator)
        {
            return citySocialTypewriterTargetText == citySocialRequestSceneHud?.ResultBodyText ? 0.86f : 0.92f;
        }

        return citySocialConversationOutcome == CitySocialConversationOutcome.Success ? 1.08f : 0.88f;
    }

    private static CitySocialVoiceProfile GetCitySocialVoiceProfile(int speakerId, bool isNarrator)
    {
        if (isNarrator)
        {
            return new CitySocialVoiceProfile(-7001, 155f, 0.28f, 540f, 0.12f);
        }

        int seed = Mathf.Abs(speakerId * 1103 + 97);
        float baseFrequency = 135f + seed % 115;
        float brightness = 0.18f + (seed % 37) / 100f;
        float formant = 620f + (seed % 9) * 55f;
        float roughness = 0.08f + (seed % 11) / 100f;
        return new CitySocialVoiceProfile(speakerId, baseFrequency, brightness, formant, roughness);
    }

    private AudioClip[] GetCitySocialVoiceClips(CitySocialVoiceProfile profile)
    {
        if (citySocialVoiceClipCache.TryGetValue(profile.Id, out AudioClip[] cached))
        {
            return cached;
        }

        AudioClip[] clips = new AudioClip[5];
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i] = CreateCitySocialVoiceClip(profile, i);
        }

        citySocialVoiceClipCache[profile.Id] = clips;
        return clips;
    }

    private AudioClip CreateCitySocialVoiceClip(CitySocialVoiceProfile profile, int variant)
    {
        float duration = 0.066f + variant * 0.006f;
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];
        float frequency = profile.BaseFrequency * (1f + (variant - 2) * 0.035f);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float p = t / duration;
            float attack = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.10f, p));
            float release = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.56f, 1f, p));
            float envelope = attack * release;
            float wobble = 1f + Mathf.Sin(2f * Mathf.PI * (7f + variant) * t) * 0.018f;
            float f = frequency * wobble;
            float throat =
                Mathf.Sin(2f * Mathf.PI * f * t) * 0.58f +
                Mathf.Sin(2f * Mathf.PI * f * 2.02f * t + 0.3f) * profile.Brightness +
                Mathf.Sin(2f * Mathf.PI * f * 3.01f * t + 1.1f) * 0.11f;
            float formant =
                Mathf.Sin(2f * Mathf.PI * (profile.Formant + variant * 32f) * t + 0.45f) * 0.13f +
                Mathf.Sin(2f * Mathf.PI * (profile.Formant * 1.45f) * t + 1.2f) * 0.07f;
            float rasp = Mathf.Sin(2f * Mathf.PI * (f * 0.51f) * t + Mathf.Sin(t * 91f)) * profile.Roughness;
            samples[i] = Mathf.Clamp((throat + formant + rasp) * envelope * 0.46f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create($"CitySocialVoice_{profile.Id}_{variant}", samples.Length, 1, AudioSampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
