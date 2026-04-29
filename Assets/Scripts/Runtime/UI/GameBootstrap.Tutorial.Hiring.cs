using UnityEngine;

public partial class GameBootstrap
{
    private void NotifyTutorialWorkerHiredByPlayer()
    {
        if (selectedGameStartMode != GameStartMode.User || isTutorialSkipped)
        {
            return;
        }

        if (!isTutorialGoalsActive || tutorialGoalsMode != TutorialGoalsMode.WorkerCard)
        {
            return;
        }

        MarkTutorialGoalComplete(TutorialGoalKind.HireNewWorker);
        isDriversPanelOpen = false;
        if (driversScreenUi?.CanvasRoot != null)
        {
            driversScreenUi.CanvasRoot.SetActive(false);
        }

        isDriversScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.UserWorkerHiringBusInfo, 0.25f);
        SessionDebugLogger.Log("TUTORIAL", "Worker hire goal completed; scheduled hiring bus explanation.");
    }

    private void NotifyTutorialHiringWaveDisembarked()
    {
        if (selectedGameStartMode != GameStartMode.User || isTutorialSkipped)
        {
            return;
        }

        ScheduleTutorial(TutorialTrigger.UserWarehouseLoadersInfo, 0.5f);
        SessionDebugLogger.Log("TUTORIAL", "Tutorial worker wave disembarked; scheduled warehouse loaders explanation.");
    }

    private void ShowUserLocalTransportTutorial()
    {
        if (hasShownUserLocalTransportTutorial)
        {
            return;
        }

        hasShownUserLocalTransportTutorial = true;
        UnlockBuildTool(BuildTool.Stop);
        bool ru = IsRussianLanguage();
        ShowTutorialWindow(
            TutorialTrigger.UserLocalTransportInfo,
            23,
            ru ? "\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u043e\u0439 \u0442\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442" : "Local Transport",
            ru
                ? "\u0413\u043e\u0440\u043e\u0434 \u0440\u0430\u0441\u0442\u0451\u0442, \u0438 \u0445\u043e\u0434\u0438\u0442\u044c \u043f\u0435\u0448\u043a\u043e\u043c \u0432\u0435\u0437\u0434\u0435 \u0441\u0442\u0430\u043d\u0435\u0442 \u043d\u0435\u0443\u0434\u043e\u0431\u043d\u043e.\n\n\u041d\u0430\u0441\u0442\u0440\u043e\u0439 \u043e\u0441\u043d\u043e\u0432\u0443 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u043d\u043e\u0439 \u0441\u0438\u0441\u0442\u0435\u043c\u044b: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0440\u043e\u0432\u043d\u043e 2 \u0433\u043e\u0440\u043e\u0434\u0441\u043a\u0438\u0435 \u043e\u0441\u0442\u0430\u043d\u043e\u0432\u043a\u0438 \u0438 \u043d\u0430\u0437\u043d\u0430\u0447\u044c 3 \u0432\u043e\u0434\u0438\u0442\u0435\u043b\u0435\u0439 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0430 \u0432 \u0412\u0430\u043a\u0430\u043d\u0441\u0438\u044f\u0445."
                : "The town is growing, and walking everywhere will become uncomfortable.\n\nSet up the base of the bus system: place exactly 2 local bus stops and assign 3 bus drivers from Vacancies.");
    }

    private void ShowUserLocalBusRoutesTutorial()
    {
        if (hasShownUserLocalBusRoutesTutorial)
        {
            return;
        }

        hasShownUserLocalBusRoutesTutorial = true;
        bool ru = IsRussianLanguage();
        ShowTutorialWindow(
            TutorialTrigger.UserLocalBusRoutesInfo,
            24,
            ru ? "\u041a\u0430\u043a \u0435\u0437\u0434\u044f\u0442 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u044b" : "How Buses Run",
            ru
                ? "\u041a\u0430\u0436\u0434\u0430\u044f \u0433\u043e\u0440\u043e\u0434\u0441\u043a\u0430\u044f \u043e\u0441\u0442\u0430\u043d\u043e\u0432\u043a\u0430 \u0438\u043c\u0435\u0435\u0442 \u043d\u043e\u043c\u0435\u0440. \u0410\u0432\u0442\u043e\u0431\u0443\u0441\u044b \u0435\u0434\u0443\u0442 \u043f\u043e \u043d\u0438\u043c \u043f\u043e \u043f\u043e\u0440\u044f\u0434\u043a\u0443: \u043e\u0442 \u043f\u0435\u0440\u0432\u043e\u0439 \u043a \u043f\u043e\u0441\u043b\u0435\u0434\u043d\u0435\u0439 \u0438 \u043e\u0431\u0440\u0430\u0442\u043d\u043e.\n\n\u0420\u0430\u0431\u043e\u0447\u0438\u0435 \u043c\u043e\u0433\u0443\u0442 \u0435\u0445\u0430\u0442\u044c \u043d\u0430 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0435, \u0435\u0441\u043b\u0438 \u0446\u0435\u043b\u044c \u0434\u0430\u043b\u0435\u043a\u043e. \u041f\u043e\u0435\u0437\u0434\u043a\u0430 \u0441\u0442\u043e\u0438\u0442 $1, \u0430 \u0432 \u043a\u043e\u043d\u0446\u0435 \u0440\u0435\u0439\u0441\u0430 \u0434\u0435\u043d\u044c\u0433\u0438 \u0438\u0437 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0430 \u043f\u0435\u0440\u0435\u0445\u043e\u0434\u044f\u0442 \u0432 \u043a\u0430\u0441\u0441\u0443 Parking."
                : "Each local bus stop has a number. Buses visit them in order: from the first stop to the last, then back again.\n\nWorkers can ride the bus when their destination is far away. A ride costs $1, and at the end of a route the bus transfers its bank into Parking.");
    }

    private void NotifyTutorialWarehouseLoaderAssigned()
    {
        if (selectedGameStartMode != GameStartMode.User || isTutorialSkipped)
        {
            return;
        }

        CheckTutorialWarehouseLoaderGoal();
    }

    private void NotifyTutorialLocalBusStopBuilt()
    {
        if (selectedGameStartMode != GameStartMode.User || isTutorialSkipped)
        {
            return;
        }

        CheckTutorialLocalTransportGoals();
    }

    private void NotifyTutorialBusDriverAssigned()
    {
        if (selectedGameStartMode != GameStartMode.User || isTutorialSkipped)
        {
            return;
        }

        CheckTutorialLocalTransportGoals();
    }

    private void CheckTutorialWarehouseLoaderGoal()
    {
        if (!isTutorialGoalsActive ||
            isTutorialGoalsComplete ||
            tutorialGoalsMode != TutorialGoalsMode.WarehouseLoaders ||
            !activeTutorialGoals.Contains(TutorialGoalKind.AssignWarehouseLoaders))
        {
            return;
        }

        int assignedLoaders = GetAssignedWarehouseLoaderCount();
        SessionDebugLogger.Log("TUTORIAL", $"Warehouse loader goal progress: {assignedLoaders}/{TutorialWarehouseLoaderGoalCount}.");
        if (assignedLoaders >= TutorialWarehouseLoaderGoalCount)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.AssignWarehouseLoaders);
        }
    }

    private int GetAssignedWarehouseLoaderCount()
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver != null &&
                driver.DutyMode == DriverDutyMode.Logistics &&
                driver.AssignedBuildingType == LocationType.Warehouse)
            {
                count++;
            }
        }

        return count;
    }

    private void CheckTutorialLocalTransportGoals()
    {
        if (!isTutorialGoalsActive ||
            isTutorialGoalsComplete ||
            tutorialGoalsMode != TutorialGoalsMode.LocalTransport)
        {
            return;
        }

        int stopCount = localStops.Count;
        int busDriverCount = GetAssignedBusDriverCount();
        SessionDebugLogger.Log("TUTORIAL", $"Local transport goal progress: stops={stopCount}/{TutorialLocalBusStopGoalCount}, busDrivers={busDriverCount}/{TutorialBusDriverGoalCount}.");

        if (stopCount >= TutorialLocalBusStopGoalCount)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.BuildLocalBusStops);
        }

        if (busDriverCount >= TutorialBusDriverGoalCount)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.AssignBusDrivers);
        }
    }

    private int GetAssignedBusDriverCount()
    {
        int count = 0;
        for (int i = 0; i < busDriverShiftIds.Length; i++)
        {
            if (busDriverShiftIds[i] > 0)
            {
                count++;
            }
        }

        return count;
    }

    private void FocusCameraOnHiringBusForTutorial()
    {
        isFleetPanelOpen = false;
        isDriversPanelOpen = false;
        isShiftsPanelOpen = false;
        isTruckDetailsOpen = false;
        isLocalBusDetailsOpen = false;
        isDriverDetailsOpen = false;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        RefreshSelectionVisuals();

        isTruckCameraFocused = false;
        isCameraReturningToDiorama = false;
        isCameraRotatingToTarget = false;
        tutorialCameraFollowTruck = null;
        tutorialCameraFollowHiringBus = true;
        tutorialCameraFocusTarget = hiringDriverArrival?.BusRootTransform != null
            ? new Vector3(hiringDriverArrival.BusRootTransform.position.x, 0f, hiringDriverArrival.BusRootTransform.position.z)
            : new Vector3(-1f, 0f, GetEdgeHighwayBusLaneWorldZ(isCitySideLane: false));
        tutorialCameraFocusOffset = new Vector3(-12f, 17f, -12f);
        isTutorialCameraFocusActive = true;
        SessionDebugLogger.Log("TUTORIAL", "Started hiring bus camera focus for tutorial worker wave.");
    }
}
