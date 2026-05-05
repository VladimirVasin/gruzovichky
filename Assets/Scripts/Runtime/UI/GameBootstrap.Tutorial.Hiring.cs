using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private void NotifyTutorialWorkersPanelOpened()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
        {
            return;
        }

        if (!isTutorialGoalsActive || tutorialGoalsMode != TutorialGoalsMode.WorkerCard)
        {
            return;
        }

        if (!activeTutorialGoals.Contains(TutorialGoalKind.HireNewWorker) ||
            completedTutorialGoals.Contains(TutorialGoalKind.HireNewWorker))
        {
            return;
        }

        if (!locations.ContainsKey(LocationType.Motel) ||
            !locations.ContainsKey(LocationType.IntercityStop) ||
            hiringDriverArrival != null)
        {
            SessionDebugLogger.Log("TUTORIAL", "Tutorial worker migration wave is waiting for Motel, Intercity Stop, and a free arrival bus.");
            return;
        }

        List<DriverAgent> tutorialWorkers = new(TutorialHireWorkerWaveCount);
        for (int i = 0; i < TutorialHireWorkerWaveCount; i++)
        {
            tutorialWorkers.Add(CreateAndRegisterDriverAgent(spawnInMotel: false));
        }

        if (!TryStartWorkerArrivalBus(tutorialWorkers, true, "tutorial automatic migration wave"))
        {
            SessionDebugLogger.Log("TUTORIAL", "Tutorial worker migration wave could not start because another arrival is active.");
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
        SessionDebugLogger.Log("TUTORIAL", "Worker migration goal completed; scheduled arrival bus explanation.");
    }

    private void NotifyTutorialHiringWaveDisembarked()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
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

    private void ShowUserEconomyTaxesTutorial()
    {
        if (hasShownUserEconomyTaxesTutorial)
        {
            return;
        }

        hasShownUserEconomyTaxesTutorial = true;
        bool ru = IsRussianLanguage();
        ShowTutorialWindow(
            TutorialTrigger.UserEconomyTaxesInfo,
            25,
            ru ? "\u042d\u043a\u043e\u043d\u043e\u043c\u0438\u043a\u0430 \u0438 \u043d\u0430\u043b\u043e\u0433\u0438" : "Economy and Taxes",
            ru
                ? "\u0421\u0435\u0440\u0432\u0438\u0441\u043d\u044b\u0435 \u0437\u0434\u0430\u043d\u0438\u044f \u043a\u043e\u043f\u044f\u0442 \u0434\u0435\u043d\u044c\u0433\u0438 \u0432 \u0441\u0432\u043e\u0438\u0445 \u043a\u0430\u0441\u0441\u0430\u0445: \u0437\u0430 \u0435\u0434\u0443, \u0434\u043e\u0441\u0443\u0433, \u043f\u043e\u0435\u0437\u0434\u043a\u0438 \u0438 \u0434\u0440\u0443\u0433\u0438\u0435 \u0433\u043e\u0440\u043e\u0434\u0441\u043a\u0438\u0435 \u043f\u0440\u0438\u0432\u044b\u0447\u043a\u0438.\n\n\u041a\u0430\u0436\u0434\u044b\u0439 \u0434\u0435\u043d\u044c \u0432 00:00 \u0433\u043e\u0440\u043e\u0434 \u0441\u043e\u0431\u0438\u0440\u0430\u0435\u0442 \u043d\u0430\u043b\u043e\u0433: \u0432\u044b\u0431\u0440\u0430\u043d\u043d\u044b\u0439 \u043f\u0440\u043e\u0446\u0435\u043d\u0442 \u043f\u0435\u0440\u0435\u0445\u043e\u0434\u0438\u0442 \u0438\u0437 \u043a\u0430\u0441\u0441 \u0437\u0434\u0430\u043d\u0438\u0439 \u0432 \u043e\u0431\u0449\u0443\u044e \u041a\u0430\u0437\u043d\u0443.\n\n\u041e\u0442\u043a\u0440\u043e\u0439 \u042d\u043a\u043e\u043d\u043e\u043c\u0438\u043a\u0443, \u0432\u043a\u043b\u0430\u0434\u043a\u0443 \u041d\u0430\u043b\u043e\u0433\u0438, \u0438 \u043a\u043d\u043e\u043f\u043a\u0430\u043c\u0438 + / - \u0443\u0441\u0442\u0430\u043d\u043e\u0432\u0438 \u0441\u0442\u0430\u0432\u043a\u0443 15%."
                : "Service buildings store money in their own banks: food, leisure, rides, and other small town habits.\n\nEvery day at 00:00 the town collects tax: the selected percent moves from building banks into the Treasury.\n\nOpen Economy, switch to Taxes, and use + / - to set the rate to 15%.");
    }

    private void ShowUserTradeIntroTutorial()
    {
        if (hasShownUserTradeIntroTutorial)
        {
            return;
        }

        hasShownUserTradeIntroTutorial = true;
        EnsureTutorialIntercityTruckAvailable();
        bool ru = IsRussianLanguage();
        ShowTutorialWindow(
            TutorialTrigger.UserTradeIntroInfo,
            26,
            ru ? "\u0422\u043e\u0440\u0433\u043e\u0432\u043b\u044f" : "Trade",
            ru
                ? "\u041d\u0435 \u0432\u0441\u0451 \u043d\u0443\u0436\u043d\u043e\u0435 \u043f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0438\u0442\u0441\u044f \u0432 \u0433\u043e\u0440\u043e\u0434\u0435. \u0427\u0430\u0441\u0442\u044c \u0440\u0435\u0441\u0443\u0440\u0441\u043e\u0432 \u043f\u0440\u0438\u0434\u0451\u0442\u0441\u044f \u043f\u043e\u043a\u0443\u043f\u0430\u0442\u044c \u0437\u0430 \u043f\u0440\u0435\u0434\u0435\u043b\u0430\u043c\u0438 \u043a\u0430\u0440\u0442\u044b.\n\n\u0422\u043e\u0440\u0433\u043e\u0432\u043b\u044f \u0442\u0435\u043f\u0435\u0440\u044c \u0440\u0430\u0431\u043e\u0442\u0430\u0435\u0442 \u0447\u0435\u0440\u0435\u0437 \u043f\u043e\u043b\u0438\u0442\u0438\u043a\u0438 \u0441\u043a\u043b\u0430\u0434\u0430: \u043e\u0442\u043a\u0440\u043e\u0439 \u0422\u043e\u0440\u0433\u043e\u0432\u043b\u044e, \u0432\u044b\u0431\u0435\u0440\u0438 \u0440\u0435\u0441\u0443\u0440\u0441, \u0440\u0435\u0436\u0438\u043c \u0438 \u0446\u0435\u043b\u0435\u0432\u043e\u0439 \u043e\u0441\u0442\u0430\u0442\u043e\u043a. \u0414\u043e\u0441\u0442\u0443\u043f\u043d\u044b\u0439 \u0432\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0430 \u043d\u0430 \u0441\u043c\u0435\u043d\u0435 \u0441\u0430\u043c \u0437\u0430\u0431\u0435\u0440\u0451\u0442 \u0440\u0435\u0439\u0441, \u0432\u043e\u0437\u044c\u043c\u0451\u0442 \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u044b\u0439 Truck \u043d\u0430 Parking \u0438 \u0432\u0435\u0440\u043d\u0451\u0442\u0441\u044f \u0441 \u0442\u043e\u0432\u0430\u0440\u043e\u043c.\n\n\u0414\u043b\u044f \u044d\u0442\u043e\u0433\u043e \u0448\u0430\u0433\u0430 \u043d\u0430 Parking \u0434\u043e\u0431\u0430\u0432\u043b\u0435\u043d \u0443\u0447\u0435\u0431\u043d\u044b\u0439 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a. \u0421\u0435\u0439\u0447\u0430\u0441 \u043d\u0430\u0437\u043d\u0430\u0447\u044c \u0432\u043e\u0434\u0438\u0442\u0435\u043b\u044f \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0430 \u043d\u0430 \u0441\u043c\u0435\u043d\u0443 \u0438 \u0432 \u0422\u043e\u0440\u0433\u043e\u0432\u043b\u0435 \u043f\u043e\u0441\u0442\u0430\u0432\u044c Textile \u0432 \u0440\u0435\u0436\u0438\u043c \u0414\u043e\u043a\u0443\u043f\u0438\u0442\u044c \u0434\u043e \u043d\u043e\u0440\u043c\u044b."
                : "Not everything your town needs is produced locally. Some resources must be bought outside the map.\n\nTrade now works through warehouse policies: open Trade, pick a resource, mode, and target stock. An available truck driver on shift automatically takes a run, grabs a free truck from Parking, and returns with cargo.\n\nFor this step, an extra tutorial truck has been added to Parking. Now assign a truck driver to a shift and set Textile to Buy up to in Trade.");
    }

    private void ShowUserTradeRaceTutorial()
    {
        if (hasShownUserTradeRaceTutorial)
        {
            return;
        }

        hasShownUserTradeRaceTutorial = true;
        TryAutoDispatchNextHudOrder();
        FocusCameraOnActiveTradeTutorialTruck();
        bool ru = IsRussianLanguage();
        ShowTutorialWindow(
            TutorialTrigger.UserTradeRaceInfo,
            27,
            ru ? "\u041b\u0438\u0447\u043d\u044b\u0439 \u0440\u0435\u0439\u0441" : "Join the Run",
            ru
                ? "\u0422\u043e\u0440\u0433\u043e\u0432\u044b\u0439 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a \u0432\u044b\u0435\u0434\u0435\u0442 \u0437\u0430 \u043f\u0440\u0435\u0434\u0435\u043b\u044b \u043a\u0430\u0440\u0442\u044b. \u041a\u043e\u0433\u0434\u0430 \u043e\u043d \u0443\u0435\u0434\u0435\u0442 \u0434\u043e\u0441\u0442\u0430\u0442\u043e\u0447\u043d\u043e \u0434\u0430\u043b\u0435\u043a\u043e, \u043f\u043e\u044f\u0432\u0438\u0442\u0441\u044f \u043a\u043d\u043e\u043f\u043a\u0430 Join the Race.\n\n\u042d\u0442\u043e \u0448\u0430\u043d\u0441 \u043b\u0438\u0447\u043d\u043e \u043f\u043e\u0443\u0447\u0430\u0441\u0442\u0432\u043e\u0432\u0430\u0442\u044c \u0432 \u0440\u0435\u0439\u0441\u0435: \u0442\u044b \u0432\u0440\u0435\u043c\u0435\u043d\u043d\u043e \u043f\u0435\u0440\u0435\u0439\u0434\u0451\u0448\u044c \u0432 \u0433\u043e\u043d\u043e\u0447\u043d\u044b\u0439 \u0440\u0435\u0436\u0438\u043c \u0438 \u043f\u043e\u0432\u0435\u0434\u0451\u0448\u044c \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a \u0441\u0430\u043c. \u0414\u043e\u0436\u0434\u0438\u0441\u044c \u043c\u043e\u043c\u0435\u043d\u0442\u0430 \u0438 \u043d\u0430\u0436\u043c\u0438 Join the Race."
                : "The trade truck will leave the map. Once it is far enough away, the Join the Race button appears.\n\nThat is your chance to take part in the run personally: the game switches into racing mode and you drive the truck yourself. Wait for the moment, then press Join the Race.");
    }

    private void ShowUserDemoCompleteTutorial()
    {
        if (hasShownUserDemoCompleteTutorial)
        {
            return;
        }

        hasShownUserDemoCompleteTutorial = true;
        bool ru = IsRussianLanguage();
        ShowTutorialWindow(
            TutorialTrigger.UserDemoCompleteInfo,
            28,
            ru ? "\u0414\u0435\u043c\u043e \u0437\u0430\u0432\u0435\u0440\u0448\u0435\u043d\u043e" : "Demo Complete",
            ru
                ? "\u041d\u0430 \u044d\u0442\u043e\u043c \u0442\u0435\u043a\u0443\u0449\u0430\u044f \u043e\u0431\u0443\u0447\u0430\u044e\u0449\u0430\u044f \u0434\u0435\u043c\u043e\u043d\u0441\u0442\u0440\u0430\u0446\u0438\u044f \u0437\u0430\u0432\u0435\u0440\u0448\u0435\u043d\u0430.\n\n\u0414\u0430\u043b\u044c\u0448\u0435 \u043c\u043e\u0436\u043d\u043e \u0438\u0433\u0440\u0430\u0442\u044c \u0447\u0435\u0440\u0435\u0437 \u043e\u0431\u044b\u0447\u043d\u0443\u044e \u041d\u043e\u0432\u0443\u044e \u0438\u0433\u0440\u0443: \u0431\u0435\u0437 \u0443\u0447\u0435\u0431\u043d\u044b\u0445 \u043e\u043a\u043e\u043d \u0438 \u0441 \u0441\u0440\u0430\u0437\u0443 \u0434\u043e\u0441\u0442\u0443\u043f\u043d\u044b\u043c\u0438 \u0438\u043d\u0441\u0442\u0440\u0443\u043c\u0435\u043d\u0442\u0430\u043c\u0438.\n\n\u0421\u043f\u0430\u0441\u0438\u0431\u043e, \u0447\u0442\u043e \u0434\u043e\u0448\u0451\u043b \u0434\u043e \u043a\u043e\u043d\u0446\u0430. \u0414\u0430\u043b\u044c\u0448\u0435 \u0433\u043e\u0440\u043e\u0434 \u0431\u0443\u0434\u0435\u0442 \u0440\u0430\u0441\u0442\u0438 \u0443\u0436\u0435 \u0432 \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u043e\u043c \u0442\u0435\u043c\u043f\u0435."
                : "This is the end of the current tutorial demo.\n\nNext, play through New Game: no tutorial windows and the available tools unlocked from the start.\n\nThank you for playing through the demo. From here, the town can grow at your own pace.");
    }

    private void NotifyTutorialRaceFinished()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial ||
            isTutorialSkipped ||
            hasShownUserDemoCompleteTutorial)
        {
            return;
        }

        ScheduleTutorial(TutorialTrigger.UserDemoCompleteInfo, 0.35f);
        SessionDebugLogger.Log("TUTORIAL", "Race completed; scheduled final demo-complete tutorial.");
    }

    private void NotifyTutorialIntercityDriverAssigned()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
        {
            return;
        }

        CheckTutorialTradeSetupGoals();
    }

    private void NotifyTutorialTradeDriverAssigned()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
        {
            return;
        }

        CheckTutorialTradeSetupGoals();
    }

    private void NotifyTutorialTradePolicyChanged(TradeResourceType resourceType)
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
        {
            return;
        }

        CheckTutorialTradeSetupGoals();
    }

    private void NotifyTutorialRaceStarted()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial ||
            isTutorialSkipped ||
            !isTutorialGoalsActive ||
            isTutorialGoalsComplete ||
            tutorialGoalsMode != TutorialGoalsMode.JoinRace)
        {
            return;
        }

        MarkTutorialGoalComplete(TutorialGoalKind.JoinRaceParticipation);
        SessionDebugLogger.Log("TUTORIAL", "Join Race tutorial goal completed by race start.");
    }

    private void CheckTutorialTradeSetupGoals()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial ||
            isTutorialSkipped ||
            !isTutorialGoalsActive ||
            isTutorialGoalsComplete ||
            tutorialGoalsMode != TutorialGoalsMode.TradeSetup)
        {
            return;
        }

        bool hasTradeDriver = HasTutorialTruckDriverShift();
        bool hasBuyTextileOrder = HasTutorialBuyTextileOrder();
        SessionDebugLogger.Log("TUTORIAL", $"Trade setup goal progress: truckDriverShift={hasTradeDriver}, buyTextile={hasBuyTextileOrder}.");

        if (hasTradeDriver)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.AssignIntercityDriver);
        }

        if (hasBuyTextileOrder)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.CreateBuyTextileOrder);
        }

        if (hasTradeDriver && hasBuyTextileOrder)
        {
            TryAutoDispatchNextHudOrder();
        }
    }

    private bool HasTutorialTruckDriverShift()
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver != null &&
                driver.DutyMode == DriverDutyMode.Local &&
                driver.ShiftStartHour >= 0 &&
                !IsDriverBusDriver(driver) &&
                !IsDriverIntercity(driver))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasTutorialBuyTextileOrder()
    {
        return GetTradePolicyMode(TradeResourceType.Textile) == TradePolicyMode.BuyUpTo ||
               (HasActiveTradeRun() &&
               activeTradeRun.OrderType == TradeOrderType.Buy &&
               activeTradeRun.ResourceType == TradeResourceType.Textile);
    }

    private void EnsureTutorialIntercityTruckAvailable()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial ||
            !locations.ContainsKey(LocationType.Parking))
        {
            return;
        }

        for (int i = 0; i < truckAgents.Count; i++)
        {
            TruckAgent truck = truckAgents[i];
            if (truck != null &&
                truck.AssignedDrivers.Count == 0 &&
                !truck.IsTruckMoving &&
                !truck.IsTruckInteracting &&
                !IsTruckOnActiveTradeRun(truck))
            {
                SessionDebugLogger.Log("TUTORIAL", $"Trade intro found free tutorial truck: {truck.DisplayName}.");
                return;
            }
        }

        if (!TryProvisionTruckFromParkingCapacity(out TruckAgent tutorialTruck, "tutorial intercity setup"))
        {
            SessionDebugLogger.Log("TUTORIAL", "Could not add free tutorial intercity truck: Parking truck slots are full.");
            return;
        }

        LoadTruckState(tutorialTruck);
        isFleetScreenDirty = true;
        isShiftsScreenDirty = true;
        SessionDebugLogger.Log("TUTORIAL", $"Added free tutorial intercity truck at Parking: {tutorialTruck.DisplayName}.");
    }

    private void NotifyTutorialWarehouseLoaderAssigned()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
        {
            return;
        }

        CheckTutorialWarehouseLoaderGoal();
    }

    private void NotifyTutorialLocalBusStopBuilt()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
        {
            return;
        }

        CheckTutorialLocalTransportGoals();
    }

    private void NotifyTutorialBusDriverAssigned()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
        {
            return;
        }

        CheckTutorialLocalTransportGoals();
    }

    private void CheckTutorialTaxRateGoal()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial ||
            isTutorialSkipped ||
            !isTutorialGoalsActive ||
            isTutorialGoalsComplete ||
            tutorialGoalsMode != TutorialGoalsMode.EconomyTaxes ||
            !activeTutorialGoals.Contains(TutorialGoalKind.SetTaxRate15))
        {
            return;
        }

        SessionDebugLogger.Log("TUTORIAL", $"Economy tax goal progress: rate={dailyBuildingTaxPercent}%/15%.");
        if (dailyBuildingTaxPercent == 15)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.SetTaxRate15);
        }
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
