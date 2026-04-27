using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void SelectTruckForFleetTutorial()
    {
        TruckAgent truck = GetTruckAgent(1);
        if (truck == null)
        {
            return;
        }

        FocusTruck(truck.TruckNumber);
        isFleetPanelOpen = true;
        isFleetScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.FleetAssignDriver);
        LogUiInput("Tutorial: selected Truck 1 for Fleet assignment step.");
    }

    private void CompleteFleetTruckSelectionTutorial(int truckNumber)
    {
        if (!isTutorialOpen || activeTutorialTrigger != TutorialTrigger.FleetSelectTruck || truckNumber != 1)
        {
            return;
        }

        isTutorialOpen = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        FocusTruck(truckNumber);
        isFleetPanelOpen = true;
        isFleetScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.FleetAssignDriver);
        LogUiInput("Tutorial: player selected Truck 1 in Fleet.");
        PlayUiSound(uiPanelCloseClip, 0.82f);
    }

    private void OpenFleetDriverPickerForTutorial()
    {
        TruckAgent truck = GetTruckAgent(1);
        if (truck == null)
        {
            return;
        }

        FocusTruck(truck.TruckNumber);
        isFleetPanelOpen = true;
        fleetAssignDriverTargetSlot = 0;
        if (fleetScreenUi?.AssignDriverPickerPanel != null)
        {
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(true);
            if (fleetScreenUi.AssignDriverPickerLayout != null) fleetScreenUi.AssignDriverPickerLayout.preferredHeight = 128f;
            if (fleetScreenUi.InfoCardLayout != null) fleetScreenUi.InfoCardLayout.preferredHeight = 360f;
            UpdateFleetDriverAssignmentPicker(truck);
        }

        isFleetScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.FleetPickDriver);
        LogUiInput("Tutorial: opened Fleet driver picker for Truck 1.");
    }

    private void CompleteFleetAssignDriverTutorial()
    {
        if (!isTutorialOpen || activeTutorialTrigger != TutorialTrigger.FleetAssignDriver)
        {
            return;
        }

        isTutorialOpen = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        OpenFleetDriverPickerForTutorial();
        PlayUiSound(uiPanelCloseClip, 0.82f);
    }

    private void PickFirstFleetDriverForTutorial()
    {
        TruckAgent truck = GetTruckAgent(1);
        if (truck == null)
        {
            return;
        }

        List<DriverAgent> candidates = GetDriverAssignmentCandidates(truck);
        if (candidates.Count == 0)
        {
            return;
        }

        AssignDriverToTruck(truck, candidates[0]);
        if (fleetScreenUi?.AssignDriverPickerPanel != null)
        {
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(false);
            if (fleetScreenUi.AssignDriverPickerLayout != null) fleetScreenUi.AssignDriverPickerLayout.preferredHeight = 0f;
            if (fleetScreenUi.InfoCardLayout != null) fleetScreenUi.InfoCardLayout.preferredHeight = 232f;
        }

        fleetAssignDriverTargetSlot = -1;
        isFleetScreenDirty = true;
        LogUiInput($"Tutorial: assigned {candidates[0].DriverName} to Truck 1.");
        FinishFleetDriverAssignedTutorial();
    }

    private void CompleteFleetPickDriverTutorial()
    {
        if (!hasShownFleetPickDriverTutorial || hasShownAssignSawmillWorkerTutorial)
        {
            return;
        }

        if (isTutorialOpen && activeTutorialTrigger == TutorialTrigger.FleetPickDriver)
        {
            isTutorialOpen = false;
            isTutorialSideMode = false;
            tutorialSideOnLeft = false;
        }

        LogUiInput("Tutorial: Fleet driver pick complete.");
        PlayUiSound(uiPanelCloseClip, 0.82f);
        FinishFleetDriverAssignedTutorial();
    }

    private void FinishFleetDriverAssignedTutorial()
    {
        if (!IsTutorialEnabledForCurrentMode() || hasShownAssignSawmillWorkerTutorial)
        {
            return;
        }

        isTutorialOpen = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        isFleetPanelOpen = false;
        isTruckDetailsOpen = false;
        isFleetScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.AssignSawmillProductionWorker);
        LogUiInput("Tutorial: closed Fleet after truck driver assignment and queued Sawmill assignment prompt.");
    }

    private void CompleteSawmillWorkerAssignedTutorial()
    {
        if (!IsTutorialEnabledForCurrentMode() ||
            !hasShownAssignSawmillWorkerTutorial ||
            hasShownSawmillWorkerAssignedTutorial)
        {
            return;
        }

        isShiftsPanelOpen = false;
        isShiftsScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.SawmillWorkerAssigned);
        LogUiInput("Tutorial: Sawmill worker assigned; closed Shifts and queued step 17.");
    }

    private bool TryShowBeeEasterEggForCell(Vector2Int cell)
    {
        if (isTutorialOpen || !IsBeeEasterEggDaytime() || !IsFlowerBeeCell(cell))
        {
            return false;
        }

        ShowBeeEasterEggHud();
        SessionDebugLogger.Log("TUTORIAL", $"Bee easter egg shown for flower cell {cell.x},{cell.y}.");
        return true;
    }

    private bool IsBeeEasterEggDaytime()
    {
        int hour = GetCurrentHour();
        return hour >= 12 && hour < 18 && AreAmbientBeesActive();
    }

    private bool IsFlowerBeeCell(Vector2Int cell)
    {
        for (int i = 0; i < flowerBeePoints.Count; i++)
        {
            if (WorldToCell(flowerBeePoints[i]) == cell)
            {
                return true;
            }
        }

        return false;
    }

    private static GameObject CreateTutorialDynamicOutline(string name, Transform parent)
    {
        GameObject root = CreateUiObject(name, parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = Vector2.zero;
        CreateTutorialOutlineBar("Top", root.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, 3f), new Vector2(0f, 3f));
        CreateTutorialOutlineBar("Bottom", root.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, -3f), new Vector2(0f, 3f));
        CreateTutorialOutlineBar("Left", root.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(-3f, 0f), new Vector2(3f, 0f));
        CreateTutorialOutlineBar("Right", root.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(3f, 0f), new Vector2(3f, 0f));
        root.SetActive(false);
        return root;
    }

    private void UpdateTutorialOutlineFromTarget(GameObject outlineRoot, RectTransform target, float padding)
    {
        if (outlineRoot == null)
        {
            return;
        }

        if (target == null || tutorialHud?.CanvasRoot == null)
        {
            outlineRoot.SetActive(false);
            return;
        }

        Canvas tutorialCanvas = tutorialHud.CanvasRoot.GetComponent<Canvas>();
        RectTransform canvasRect = tutorialCanvas.GetComponent<RectTransform>();
        Camera uiCamera = tutorialCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : tutorialCanvas.worldCamera;
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out Vector2 local);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        RectTransform outlineRect = outlineRoot.GetComponent<RectTransform>();
        outlineRect.anchoredPosition = (min + max) * 0.5f;
        outlineRect.sizeDelta = (max - min) + new Vector2(padding * 2f, padding * 2f);
        outlineRoot.SetActive(true);
    }

    private GameObject CreateTutorialMenuButtonOutline(string name, Transform parent, float anchorX)
    {
        GameObject root = CreateUiObject(name, parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(anchorX, -17f);
        rootRect.sizeDelta = new Vector2(MenuBtnW, MenuBtnH);

        CreateTutorialOutlineBar("Top", root.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, 3f), new Vector2(0f, 3f));
        CreateTutorialOutlineBar("Bottom", root.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, -3f), new Vector2(0f, 3f));
        CreateTutorialOutlineBar("Left", root.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(-3f, 0f), new Vector2(3f, 0f));
        CreateTutorialOutlineBar("Right", root.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(3f, 0f), new Vector2(3f, 0f));
        root.SetActive(false);
        return root;
    }

    private static GameObject CreateTutorialHireButtonOutline(string name, Transform parent)
    {
        // Positioned over the "Hire New Worker" button inside the Drivers panel (760Г—560, yOffset=-16)
        // Button sits at the bottom of the panel вЂ” approximate canvas-space center: (0, -228)
        GameObject root = CreateUiObject(name, parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = new Vector2(0f, -228f);
        rootRect.sizeDelta = new Vector2(688f, 44f);

        CreateTutorialOutlineBar("Top",    root.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f,  3f), new Vector2(0f, 3f));
        CreateTutorialOutlineBar("Bottom", root.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, -3f), new Vector2(0f, 3f));
        CreateTutorialOutlineBar("Left",   root.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(-3f, 0f), new Vector2(3f, 0f));
        CreateTutorialOutlineBar("Right",  root.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2( 3f, 0f), new Vector2(3f, 0f));
        root.SetActive(false);
        return root;
    }

    private static void CreateTutorialOutlineBar(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject bar = CreateUiObject($"TutorialBuildMenuOutline{name}", parent);
        RectTransform rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        Image image = bar.AddComponent<Image>();
        image.color = new Color(1f, 0.08f, 0.04f, 1f);
        image.raycastTarget = false;
    }
}
