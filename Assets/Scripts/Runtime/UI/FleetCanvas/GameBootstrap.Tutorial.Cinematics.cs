using UnityEngine;

public partial class GameBootstrap
{
    private void UpdateTutorialCameraFocus(float dt)
    {
        if (!isTutorialCameraFocusActive || mainCamera == null)
        {
            return;
        }

        bool isTruckFollowMode = tutorialCameraFollowTruck?.TruckObject != null;
        if (isTruckFollowMode)
        {
            Vector3 truckPosition = tutorialCameraFollowTruck.TruckObject.transform.position;
            tutorialCameraFocusTarget = new Vector3(truckPosition.x, 0f, truckPosition.z);
        }
        else if (tutorialCameraFollowHiringBus)
        {
            if (hiringDriverArrival?.BusRootTransform != null)
            {
                Vector3 busPosition = hiringDriverArrival.BusRootTransform.position;
                tutorialCameraFocusTarget = new Vector3(busPosition.x, 0f, busPosition.z);
            }
            else
            {
                tutorialCameraFocusTarget = new Vector3(-1f, 0f, GetEdgeHighwayBusLaneWorldZ(isCitySideLane: false));
            }
        }

        float focusLerp = 1f - Mathf.Exp(-TutorialCameraFocusSpeed * dt);
        float offsetLerp = 1f - Mathf.Exp(-(TutorialCameraFocusSpeed * 1.15f) * dt);
        cameraFocusPoint = Vector3.Lerp(cameraFocusPoint, tutorialCameraFocusTarget, focusLerp);
        cameraOffset = Vector3.Lerp(cameraOffset, tutorialCameraFocusOffset, offsetLerp);
        cameraTargetOffset = cameraOffset;

        mainCamera.transform.position = cameraFocusPoint + cameraOffset;
        mainCamera.transform.rotation = GetDioramaCameraRotation();

        if (!isTruckFollowMode && !tutorialCameraFollowHiringBus)
        {
            bool focusDone = (cameraFocusPoint - tutorialCameraFocusTarget).sqrMagnitude < 0.05f;
            bool offsetDone = (cameraOffset - tutorialCameraFocusOffset).sqrMagnitude < 0.01f;
            if (focusDone && offsetDone)
            {
                cameraFocusPoint = tutorialCameraFocusTarget;
                cameraOffset = tutorialCameraFocusOffset;
                cameraTargetOffset = tutorialCameraFocusOffset;
                mainCamera.transform.position = cameraFocusPoint + cameraOffset;
                mainCamera.transform.rotation = GetDioramaCameraRotation();
                isTutorialCameraFocusActive = false;
                SessionDebugLogger.Log("TUTORIAL", "Completed smooth tutorial camera focus.");
            }
        }
    }

    private void UpdateTutorialUi()
    {
        float dt = Time.unscaledDeltaTime;
        UpdateTutorialCameraFocus(dt);
        if (pendingTutorialTrigger.HasValue)
        {
            pendingTutorialDelay -= dt;
            if (pendingTutorialDelay <= 0f && !isTutorialOpen)
            {
                TutorialTrigger trigger = pendingTutorialTrigger.Value;
                pendingTutorialTrigger = null;
                TryShowTutorial(trigger);
            }
        }

        if (tutorialHud == null || tutorialHud.CanvasRoot == null)
        {
            return;
        }

        if (tutorialHud.WindowRect != null)
        {
            tutorialHud.WindowRect.gameObject.SetActive(isTutorialOpen);
        }

        if (isTutorialOpen && tutorialHud.BodyText != null)
        {
            tutorialWindowTypeTime += dt;
            int visibleChars = Mathf.Clamp(
                Mathf.FloorToInt(tutorialWindowTypeTime * TutorialWindowTypeSpeed),
                0,
                tutorialWindowFullText.Length);
            tutorialHud.BodyText.text = tutorialWindowFullText.Substring(0, visibleChars);
        }

        bool canvasNeeded = isTutorialOpen;
        if (tutorialHud.CanvasRoot.activeSelf != canvasNeeded)
        {
            tutorialHud.CanvasRoot.SetActive(canvasNeeded);
        }

        if (!isTutorialOpen && tutorialHud.OverlayImage != null)
        {
            tutorialHud.OverlayImage.color = OverlayColorTransparent;
        }

        if (isTutorialOpen && isTutorialSideMode && tutorialHud.WindowRect != null)
        {
            tutorialBobTime += Time.unscaledDeltaTime;
            float bobX = Mathf.Sin(tutorialBobTime * 0.65f) * 3.5f;
            float bobY = Mathf.Sin(tutorialBobTime * 1.15f) * 5f;
            tutorialHud.WindowRect.anchoredPosition = new Vector2(TutorialSidePanelBaseX + bobX, bobY);
        }
    }
}
