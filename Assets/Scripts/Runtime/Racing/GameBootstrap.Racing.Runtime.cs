using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public partial class GameBootstrap : MonoBehaviour
{
    private void UpdateRacingMinigame()
    {
        if (!isRacingActive && !racingFinishSequenceActive) return;

        // Cinematic finish sequence - runs after crossing finish line
        if (racingFinishSequenceActive)
        {
            UpdateFinishSequence();
            return;
        }

        float dt = Time.unscaledDeltaTime;
        racingElapsedTime += dt;
        if (racingBusImpactCooldown > 0f)
            racingBusImpactCooldown = Mathf.Max(0f, racingBusImpactCooldown - dt);

        // -- Input ----------------------------------------
        float throttle = 0f;
        bool  sBrakeReverse = false;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   throttle += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) sBrakeReverse = true;

            bool gearConsumesMouse = UpdateGearShiftMouseDrag(dt);
            if (!gearConsumesMouse)
            {
                // Steering is intentionally mouse-only: the wheel is the control surface.
                UpdateWheelMouseDrag(dt);
            }

            if (kb.escapeKey.wasPressedThisFrame)
            {
                FinishRace(success: false);
                return;
            }
        }

        // -- Physics --------------------------------------
        float speed = racingVelocity.magnitude;

        // Forward direction (from previous frame angle - used for reverse steer check)
        float rad = racingTruckAngle * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        Vector2 right   = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));

        // When reversing, invert steering so controls feel natural (left = go left)
        float fwdDot    = Vector2.Dot(racingVelocity, forward);
        float steerSign = fwdDot >= 0f ? 1f : -1f;
        racingIsReversing = fwdDot < -0.2f;

        // Steering - uses ramped input, stronger max force
        float steerAmount = racingSteerInput * steerSign * RacingSteerForce * Mathf.Clamp01(speed / 3.5f) * dt;
        racingAngularVel += steerAmount;
        racingAngularVel *= Mathf.Pow(RacingAngularDrag, dt * 60f);

        racingTruckAngle += racingAngularVel * dt;

        // Recompute forward/right after angle update
        rad     = racingTruckAngle * Mathf.Deg2Rad;
        forward = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        right   = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));

        UpdateManualGearState(fwdDot, dt);

        int forwardGear = Mathf.Clamp(racingCurrentGear, 1, 4);
        float gearMaxSpeed = GearMaxSpeed[forwardGear];
        float gearMinGoodSpeed = GearMinGoodSpeed[forwardGear];
        float lowSpeedEfficiency = forwardGear <= 1
            ? 1f
            : Mathf.Lerp(0.24f, 1f, Mathf.InverseLerp(gearMinGoodSpeed * 0.35f, gearMinGoodSpeed, speed));
        float topEndTorque = Mathf.Pow(1f - Mathf.Clamp01(speed / gearMaxSpeed), 0.72f);
        float gearTorque = topEndTorque * lowSpeedEfficiency * GearAccelMult[forwardGear];
        float gearLoad01 = Mathf.Clamp01(speed / Mathf.Max(0.1f, gearMaxSpeed));
        racingGearAccel01 = Mathf.Lerp(racingGearAccel01, throttle > 0.05f ? gearLoad01 : 0f, 8f * dt);
        racingVelocity += forward * throttle * RacingAcceleration * gearTorque * dt;

        // Lateral friction (simulates grip)
        float lateralSpeed = Vector2.Dot(racingVelocity, right);
        racingVelocity -= right * (lateralSpeed * RacingLateralFriction * dt);

        // S key - brake while moving forward, then reverse at half speed
        if (sBrakeReverse)
        {
            float fwdSpeed = Vector2.Dot(racingVelocity, forward); // + = moving forward

            if (fwdSpeed > 0.25f)
            {
                // Brake
                racingVelocity -= racingVelocity.normalized * speed * Mathf.Clamp01(1.4f * dt);
            }
            else
            {
                // Reverse - half max speed, same torque curve
                float revMax    = GearMaxSpeed[0];
                float revSpeed  = Mathf.Max(0f, -fwdSpeed);
                float revTorque = Mathf.Pow(1f - Mathf.Clamp01(revSpeed / revMax), 0.6f);
                racingVelocity -= forward * RacingAcceleration * 0.55f * revTorque * dt;

                // Cap reverse speed
                float newFwdSpeed = Vector2.Dot(racingVelocity, forward);
                if (newFwdSpeed < -revMax)
                    racingVelocity += forward * (-revMax - newFwdSpeed);
            }
        }

        // Drag
        racingVelocity *= Mathf.Pow(RacingDrag, dt * 60f);

        bool visualBraking = sBrakeReverse && Vector2.Dot(racingVelocity, forward) > 0.2f;

        // Speed cap follows the selected manual gear while moving forward.
        float activeSpeedLimit = fwdDot < -0.2f ? GearMaxSpeed[0] : gearMaxSpeed;
        if (racingVelocity.sqrMagnitude > activeSpeedLimit * activeSpeedLimit)
            racingVelocity = racingVelocity.normalized * activeSpeedLimit;

        // Update position
        racingTruckPos.x += racingVelocity.x * dt;
        racingTruckPos.z += racingVelocity.y * dt;
        {
            const float Gravity           = 18f;   // m/s2 - arcade-strong
            const float TerminalVelY      = -25f;  // m/s downward cap
            const float GroundedThreshold = 0.05f; // m above floor в†’ airborne

            Vector3 flatPos = new Vector3(racingTruckPos.x, 0f, racingTruckPos.z);
            bool  onRoad = IsPositionOnRaceRoad(flatPos, 4.8f);
            float floorY = onRoad
                ? SampleRaceRoadY(racingTruckPos.x, racingTruckPos.z)
                : SampleGroundMeshY(racingTruckPos.x, racingTruckPos.z, racingGroundY) + 0.35f;

            if (onRoad && floorY > racingTruckPos.y)
            {
                // Uphill - lerp up to meet the rising surface
                racingTruckPos.y = Mathf.Lerp(racingTruckPos.y, floorY, 18f * dt);
                racingTruckVelY  = 0f;
            }
            else
            {
                // Downhill or off-road - apply gravity, land on floor
                racingTruckVelY   = Mathf.Max(racingTruckVelY - Gravity * dt, TerminalVelY);
                racingTruckPos.y += racingTruckVelY * dt;
                if (racingTruckPos.y <= floorY + GroundedThreshold)
                {
                    racingTruckPos.y = floorY;
                    racingTruckVelY  = 0f;
                }
            }
        }

        // Bus + lantern collision (depenetration + velocity response)
        UpdateRacingBuses(dt);
        UpdateLanternCollisions(dt);
        if (racingFinishSequenceActive)
            return;
        UpdateRacingAtmosphere(dt);
        UpdateRacingTrailParticles(dt, speed, visualBraking);

        // -- Apply truck transform - FWD articulation ---------------------
        if (racingTruckVisual != null)
        {
            // Body/rear lags behind physics angle - gives the "rear follows front" look
            racingBodyAngle = Mathf.LerpAngle(racingBodyAngle, racingTruckAngle, 4.5f * dt);

            // -- Terrain pitch + lateral tilt (4-point finite difference) -----
            const float ProbeD = 0.8f;
            float sinY = Mathf.Sin(racingBodyAngle * Mathf.Deg2Rad);
            float cosY = Mathf.Cos(racingBodyAngle * Mathf.Deg2Rad);
            float yFwd   = SampleSurfaceY(racingTruckPos.x + sinY * ProbeD, racingTruckPos.z + cosY * ProbeD);
            float yBack  = SampleSurfaceY(racingTruckPos.x - sinY * ProbeD, racingTruckPos.z - cosY * ProbeD);
            float yRight = SampleSurfaceY(racingTruckPos.x + cosY * ProbeD, racingTruckPos.z - sinY * ProbeD);
            float yLeft  = SampleSurfaceY(racingTruckPos.x - cosY * ProbeD, racingTruckPos.z + sinY * ProbeD);
            float slopeF = (yFwd - yBack)   / (2f * ProbeD);
            float slopeR = (yRight - yLeft)  / (2f * ProbeD);
            float pitchTarget = Mathf.Clamp(-Mathf.Atan(slopeF) * Mathf.Rad2Deg, -RacingPitchMax, RacingPitchMax);
            float tiltTarget  = Mathf.Clamp(-Mathf.Atan(slopeR) * Mathf.Rad2Deg, -RacingTerrainTiltMax, RacingTerrainTiltMax);
            racingBodyPitch = Mathf.Lerp(racingBodyPitch, pitchTarget, RacingTerrainSmooth * dt);
            racingBodyTiltZ = Mathf.Lerp(racingBodyTiltZ, tiltTarget,  RacingTerrainSmooth * dt);

            racingTruckVisual.transform.position = racingTruckPos;
            racingTruckVisual.transform.rotation = Quaternion.Euler(racingBodyPitch, racingBodyAngle, racingBodyTiltZ);

            // FWD delta - front axle (wheels) + cabin pivot both steer ahead of body
            float frontDelta = Mathf.DeltaAngle(racingBodyAngle, racingTruckAngle);
            if (racingFrontAssembly != null)
                racingFrontAssembly.localRotation = Quaternion.Euler(0f, frontDelta, 0f);
            if (racingCabinGroup != null)
                racingCabinGroup.localRotation = Quaternion.Euler(0f, frontDelta, 0f);

            // Body roll - both red body and yellow cabin lean together (suspension)
            // Terminal angularVel в‰€ 36 deg/s, normalise в†’ В±RacingRollMax degrees
            float rollTarget = Mathf.Clamp(racingAngularVel / 36f, -1f, 1f) * RacingRollMax;
            racingBodyRoll = Mathf.Lerp(racingBodyRoll, rollTarget, RacingRollSmooth * dt);
            if (racingBodyGroup != null)
                racingBodyGroup.localRotation = Quaternion.Euler(0f, 0f, racingBodyRoll);

            // Rotate around the cylinder's local Y (= world X after Euler(0,0,-90)) - rolls forward
            float wheelSpin = speed * dt * 180f;
            if (racingTruckWheelFL != null) racingTruckWheelFL.Rotate(Vector3.up, wheelSpin, Space.Self);
            if (racingTruckWheelFR != null) racingTruckWheelFR.Rotate(Vector3.up, wheelSpin, Space.Self);
            if (racingTruckWheelRL != null) racingTruckWheelRL.Rotate(Vector3.up, wheelSpin, Space.Self);
            if (racingTruckWheelRR != null) racingTruckWheelRR.Rotate(Vector3.up, wheelSpin, Space.Self);

            UpdateRacingBrakeLights(visualBraking, speed);
        }

        // -- Camera follow - lagging yaw + lateral sway -----------------------
        if (racingCamera != null)
        {
            // Camera yaw trails truck yaw - lower value = more lag / heavier feel
            racingCameraAngle = Mathf.LerpAngle(racingCameraAngle, racingTruckAngle, 2.8f * dt);

            // Lateral sway: camera drifts opposite to steering (centrifugal throw)
            float swayTarget  = racingSteerInput * -0.5f;
            racingCameraSwayX = Mathf.Lerp(racingCameraSwayX, swayTarget, 3.5f * dt);

            // Small roll tilt into the corner
            float roll = racingCameraSwayX * -3.0f;

            Quaternion camRot = Quaternion.Euler(26f, racingCameraAngle, roll);
            Vector3 camBack   = camRot * Vector3.back * 5f;
            Vector3 swayWorld = camRot * Vector3.right * racingCameraSwayX;
            Vector3 targetPos = racingTruckPos + Vector3.up * 1f + camBack + swayWorld;
            float speed01 = Mathf.Clamp01(speed / RacingMaxSpeed);
            float shake = Mathf.Lerp(0.004f, 0.055f, speed01) + Mathf.Clamp01(Mathf.Abs(racingAngularVel) / 48f) * 0.035f;
            Vector3 shakeOffset = camRot * new Vector3(
                Mathf.Sin(Time.unscaledTime * 23.1f) * shake,
                Mathf.Sin(Time.unscaledTime * 31.7f) * shake * 0.45f,
                0f);
            targetPos += shakeOffset;

            racingCamera.transform.position = Vector3.Lerp(
                racingCamera.transform.position, targetPos, 5.5f * dt);
            racingCamera.transform.rotation = Quaternion.Slerp(
                racingCamera.transform.rotation, camRot, 5.5f * dt);
            racingCamera.fieldOfView = Mathf.Lerp(racingCamera.fieldOfView, Mathf.Lerp(64f, 74f, speed01), 3.8f * dt);
        }

        // -- Finish check - strip detection ----------------
        // Narrow along road direction (В±2.5 m), very wide across it (В±14 m)
        // so missing the line by driving off-road still counts.
        Vector3 toFinish = new Vector3(racingTruckPos.x - raceFinishPos.x, 0f, racingTruckPos.z - raceFinishPos.z);
        float alongRoad   = Vector3.Dot(toFinish, raceFinishFwd);          // depth through the line
        float acrossRoad  = Vector3.Cross(toFinish, raceFinishFwd).magnitude; // lateral distance
        bool  crossedLine = Mathf.Abs(alongRoad) < 2.5f && acrossRoad < 14f;

        // Keep distToFinish for HUD display (visual distance to centre of line)
        float distToFinish = toFinish.magnitude;

        // -- HUD update ------------------------------------
        float kmh = speed * 3.6f;

        if (racingHudText != null)
        {
            racingHudText.text = IsRussianLanguage()
                ? "МЕЖГОРОДНИЙ РЕЙС\n" +
                  "----------------\n" +
                  $"Финиш: {distToFinish:F0} м\n" +
                  $"Время: {racingElapsedTime:F1} c\n" +
                  $"Грузовик: {racingTruckDamage:F0}%\n" +
                  $"Груз: {racingCargoDamage:F0}%\n" +
                  $"Удары: {racingCollisionCount}\n" +
                  "----------------\n" +
                  "[ESC] Выйти"
                : "INTERCITY DELIVERY\n" +
                  "----------------\n" +
                  $"Finish: {distToFinish:F0} m\n" +
                  $"Time: {racingElapsedTime:F1}s\n" +
                  $"Truck: {racingTruckDamage:F0}%\n" +
                  $"Cargo: {racingCargoDamage:F0}%\n" +
                  $"Impacts: {racingCollisionCount}\n" +
                  "----------------\n" +
                  "[ESC] Exit";
        }

        if (racingSpeedometerNeedle != null)
        {
            // Sweep: 0 km/h = 150 deg CCW from up (7 o'clock), 100 km/h = -150 deg (5 o'clock)
            float needleZ = 150f - Mathf.Clamp01(kmh / 100f) * 300f;
            racingSpeedometerNeedle.localEulerAngles = new Vector3(0f, 0f, needleZ);
        }

        if (racingSpeedometerText != null)
            racingSpeedometerText.text = $"{kmh:F0}";

        if (racingGearText != null)
        {
            racingGearText.text  = racingCurrentGear == 0 ? "R" : racingCurrentGear.ToString();
            Color baseGearColor = racingCurrentGear == 0
                ? new Color(1f, 0.3f, 0.2f)
                : new Color(0.95f, 0.90f, 0.84f);
            racingGearText.color = racingGearFlashTimer > 0f
                ? Color.Lerp(baseGearColor, new Color(1f, 0.82f, 0.20f), Mathf.PingPong(Time.unscaledTime * 12f, 1f))
                : baseGearColor;
        }

        UpdateRacingGearAccelHud();

        UpdateRacingSpeedLines(speed);

        // Headlights - always on, brighter at night
        if (racingHeadlightL != null && racingHeadlightR != null)
        {
            float darkness = 1f - currentStylizedDaylight;
            // Day: 1.2,  dusk/night ramps up to 4.5
            float headlightIntensity = Mathf.Lerp(1.2f, 4.5f, Mathf.Clamp01(darkness * 2f));
            racingHeadlightL.enabled   = true;
            racingHeadlightR.enabled   = true;
            racingHeadlightL.intensity = headlightIntensity;
            racingHeadlightR.intensity = headlightIntensity;
            Color racingHeadlightColor = Color.Lerp(
                new Color(0.42f, 0.20f, 0.08f),
                new Color(1f, 0.66f, 0.32f),
                Mathf.Clamp01(headlightIntensity / 4.5f));
            racingHeadlightL.color = racingHeadlightColor;
            racingHeadlightR.color = racingHeadlightColor;
        }

        // World lanterns along track
        if (racingWorldLights.Count > 0)
        {
            float wDarkness  = 1f - currentStylizedDaylight;
            bool  wOn        = wDarkness > 0.55f;
            float wIntensity = wOn ? Mathf.Lerp(0.3f, 1.2f, Mathf.InverseLerp(0.55f, 1f, wDarkness)) : 0f;
            foreach (Light wl in racingWorldLights)
            {
                if (wl == null) continue;
                wl.enabled    = wOn;
                wl.intensity  = wIntensity;
                wl.color      = Color.Lerp(new Color(0.36f, 0.18f, 0.07f), new Color(1f, 0.62f, 0.28f), Mathf.Clamp01(wIntensity / 1.2f));
            }
        }

        UpdateRacingSkydome();
        UpdateSteeringWheel(dt);
        UpdatePedals(dt, throttle, sBrakeReverse);
        UpdateGearShift(dt);

        if (crossedLine)
        {
            FinishRace(success: true);
        }
    }

}
