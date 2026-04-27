using UnityEngine;
using UnityEngine.InputSystem;

public partial class GameBootstrap : MonoBehaviour
{
    private void PlayRacingVictorySound()
    {
        // Procedural two-note victory chime: major third (C5 + E5), short attack/decay
        const int sampleRate = 44100;
        const float duration = 0.55f;
        int samples = (int)(sampleRate * duration);

        float[] data = new float[samples * 2]; // stereo
        float[] freqs = { 523.25f, 659.25f, 783.99f }; // C5, E5, G5
        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / sampleRate;
            float env  = Mathf.Clamp01(t / 0.02f) * Mathf.Pow(1f - t / duration, 1.8f);
            float note = Mathf.Sin(2f * Mathf.PI * freqs[0] * t) * 0.45f
                       + Mathf.Sin(2f * Mathf.PI * freqs[1] * t) * 0.35f
                       + Mathf.Sin(2f * Mathf.PI * freqs[2] * t) * 0.25f;
            float sample = note * env;
            data[i * 2]     = sample;
            data[i * 2 + 1] = sample;
        }

        AudioClip chime = AudioClip.Create("VictoryChime", samples, 2, sampleRate, false);
        chime.SetData(data, 0);

        AudioSource src = CreateAudioSource("VictorySound", null, false, 0.75f, 0f, false);
        src.ignoreListenerPause = true;
        src.clip = chime;
        src.Play();
        Object.Destroy(src.gameObject, duration + 0.5f);
    }

    private void UpdateWheelMouseDrag(float dt)
    {
        if (racingCamera == null) return;
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();

        // On click inside wheel zone вЂ” start drag
        if (mouse.leftButton.wasPressedThisFrame)
        {
            racingWheelDragging = false;
            float wheelRadius = Screen.height * 0.32f;
            if (racingSteeringWheelRoot != null &&
                ScreenDist(mousePos, racingCamera, racingSteeringWheelRoot.transform.position) < wheelRadius)
            {
                racingWheelDragging   = true;
                racingWheelAngularVel = 0f;
            }
        }

        if (mouse.leftButton.wasReleasedThisFrame)
            racingWheelDragging = false;

        // в”Ђв”Ђ Wheel drag в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        if (racingWheelDragging && mouse.leftButton.isPressed)
        {
            float mouseDX      = mouse.delta.ReadValue().x;
            float targetAngVel = mouseDX * 0.55f / Mathf.Max(dt, 0.001f);
            racingWheelAngularVel = Mathf.Lerp(racingWheelAngularVel, targetAngVel, 12f * dt);
            racingWheelAngle     += racingWheelAngularVel * dt;
        }
        else if (!racingWheelDragging)
        {
            // Inertia вЂ” decays over ~3 s
            racingWheelAngle     += racingWheelAngularVel * dt;
            racingWheelAngularVel *= Mathf.Pow(0.975f, dt * 60f);
            // Spring return вЂ” proportional to angle (stronger the further from centre)
            racingWheelAngle -= racingWheelAngle * 2.8f * dt;
        }

        racingWheelAngle = Mathf.Clamp(racingWheelAngle, -360f, 360f);   // max В±1 full turn
        racingSteerInput = Mathf.Clamp(racingWheelAngle / 180f, -1f, 1f);
    }

    private bool UpdateGearShiftMouseDrag(float dt)
    {
        if (racingCamera == null || racingGearShift == null) return false;
        var mouse = Mouse.current;
        if (mouse == null) return false;

        Vector2 mousePos = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame)
        {
            racingGearDragging = false;
            if (ScreenDist(mousePos, racingCamera, racingGearShift.position) < Screen.height * 0.16f)
            {
                racingGearDragging = true;
                racingGearDragStart = mousePos;
                racingGearDragAccumY = 0f;
            }
        }

        if (!racingGearDragging) return false;

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            racingGearDragging = false;
            racingGearDragAccumY = 0f;
            return true;
        }

        if (mouse.leftButton.isPressed)
        {
            racingGearDragAccumY += mouse.delta.ReadValue().y;
            if (Mathf.Abs(racingGearDragAccumY) >= GearDragThresholdPx)
            {
                TryShiftRacingGear(racingGearDragAccumY > 0f ? 1 : -1);
                racingGearDragAccumY = 0f;
            }
        }

        return true;
    }

    // Distance in screen pixels from mousePos to a world point projected via camera.
    private static float ScreenDist(Vector2 mousePos, Camera cam, Vector3 worldPos)
        => Vector2.Distance(mousePos, ScreenProject(cam, worldPos));

    private static Vector2 ScreenProject(Camera cam, Vector3 worldPos)
    {
        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        return new Vector2(vp.x * Screen.width, vp.y * Screen.height);
    }

    private void UpdateSteeringWheel(float dt)
    {
        if (racingSteeringWheelRoot == null) return;
        // Apply current wheel angle directly вЂ” driven by mouse drag or keyboard
        racingSteeringWheelRoot.transform.localRotation = Quaternion.Euler(0f, racingWheelAngle, 0f);
    }

    // в”Ђв”Ђ Road buses в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

}
