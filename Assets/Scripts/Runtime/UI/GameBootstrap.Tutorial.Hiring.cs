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

        MarkTutorialGoalComplete(TutorialGoalKind.OpenWorkersCard);

        if (!activeTutorialGoals.Contains(TutorialGoalKind.WaitForWorkerArrival) ||
            completedTutorialGoals.Contains(TutorialGoalKind.WaitForWorkerArrival))
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

        CheckTutorialWorkerArrivalGoal();
        SessionDebugLogger.Log("TUTORIAL", "Tutorial worker wave disembarked; worker arrival goal checked.");
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
            26,
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
            27,
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
            28,
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
        bool ru = IsRussianLanguage();
        ShowTutorialWindow(
            TutorialTrigger.UserTradeIntroInfo,
            29,
            ru ? "\u041a\u0430\u0440\u0442\u0430 \u0440\u0435\u0433\u0438\u043e\u043d\u043e\u0432" : "Regional Map",
            ru
                ? "\u041d\u0435 \u0432\u0441\u0451 \u043d\u0443\u0436\u043d\u043e\u0435 \u043f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0438\u0442\u0441\u044f \u0432 \u0433\u043e\u0440\u043e\u0434\u0435. \u0412\u043d\u0435\u0448\u043d\u0438\u0435 \u0433\u043e\u0440\u043e\u0434\u0430 \u0432\u0438\u0434\u043d\u044b \u043d\u0430 \u041a\u0430\u0440\u0442\u0435.\n\n\u0423 \u043a\u0430\u0436\u0434\u043e\u0433\u043e \u0433\u043e\u0440\u043e\u0434\u0430 \u0435\u0441\u0442\u044c \u0441\u0442\u0430\u0442\u0443\u0441 \u043c\u0430\u0440\u0448\u0440\u0443\u0442\u0430, \u0441\u043f\u0438\u0441\u043a\u0438 \u041f\u0440\u043e\u0434\u0430\u0451\u0442 / \u041f\u043e\u043a\u0443\u043f\u0430\u0435\u0442 \u0438 \u0442\u0438\u043f \u043c\u0430\u0440\u0448\u0440\u0443\u0442\u0430. \u0421\u0435\u0439\u0447\u0430\u0441 \u043e\u0442\u043a\u0440\u043e\u0439 \u043a\u0430\u0440\u0442\u0443 \u0438 \u043f\u0440\u043e\u043b\u043e\u0436\u0438 \u043c\u0430\u0440\u0448\u0440\u0443\u0442 \u043a \u0440\u0435\u0447\u043d\u043e\u043c\u0443 \u0433\u043e\u0440\u043e\u0434\u0443, \u043a\u043e\u0442\u043e\u0440\u044b\u0439 \u043f\u0440\u043e\u0434\u0430\u0451\u0442 \u0422\u0435\u043a\u0441\u0442\u0438\u043b\u044c."
                : "Not everything your town needs is produced locally. Outside cities are shown on the Map.\n\nEach city has a route status, Sells / Buys tables, and a route type. Now open the map and build a route to the river city that sells Textile.");
    }

    private void ShowUserTradeRouteTutorial()
    {
        if (hasShownUserTradeRouteTutorial)
        {
            return;
        }

        hasShownUserTradeRouteTutorial = true;
        bool ru = IsRussianLanguage();
        ShowTutorialWindow(
            TutorialTrigger.UserTradeRouteInfo,
            30,
            ru ? "\u041c\u0430\u0440\u0448\u0440\u0443\u0442 \u043e\u0442\u043a\u0440\u044b\u0442" : "Route Opened",
            ru
                ? "\u0422\u0435\u043f\u0435\u0440\u044c \u0433\u043e\u0440\u043e\u0434 \u0437\u043d\u0430\u0435\u0442 \u0440\u0435\u0447\u043d\u043e\u0439 \u0442\u043e\u0440\u0433\u043e\u0432\u044b\u0439 \u043c\u0430\u0440\u0448\u0440\u0443\u0442.\n\n\u0420\u0435\u0447\u043d\u0430\u044f \u0442\u043e\u0440\u0433\u043e\u0432\u043b\u044f \u0438\u0434\u0451\u0442 \u0447\u0435\u0440\u0435\u0437 \u0414\u043e\u043a\u0438: \u0442\u0443\u0434\u0430 \u043f\u0440\u0438\u043f\u043b\u044b\u0432\u0430\u0435\u0442 \u043a\u043e\u0440\u0430\u0431\u043b\u044c, \u043f\u043e\u043a\u0443\u043f\u0430\u0435\u0442 \u0438 \u043f\u0440\u043e\u0434\u0430\u0451\u0442 \u0442\u043e\u0432\u0430\u0440\u044b, \u0430 \u043c\u0435\u0441\u0442\u043d\u044b\u0435 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438 \u0440\u0430\u0437\u0432\u043e\u0437\u044f\u0442 \u0433\u0440\u0443\u0437 \u043c\u0435\u0436\u0434\u0443 \u0414\u043e\u043a\u0430\u043c\u0438 \u0438 \u0421\u043a\u043b\u0430\u0434\u043e\u043c."
                : "The town now knows a river trade route.\n\nRiver trade uses Docks: ships arrive there to buy and sell goods, while local trucks move cargo between Docks and Warehouse.");
    }

    private void ShowUserDocksTutorial()
    {
        if (hasShownUserDocksTutorial)
        {
            return;
        }

        hasShownUserDocksTutorial = true;
        UnlockBuildTool(BuildTool.Docks);
        bool ru = IsRussianLanguage();
        ShowTutorialWindow(
            TutorialTrigger.UserDocksPrompt,
            31,
            ru ? "\u0414\u043e\u043a\u0438" : "Docks",
            ru
                ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0414\u043e\u043a\u0438 \u043d\u0430 \u0431\u0435\u0440\u0435\u0433\u0443 \u0440\u0435\u043a\u0438: \u0447\u0430\u0441\u0442\u044c \u0437\u0434\u0430\u043d\u0438\u044f \u0434\u043e\u043b\u0436\u043d\u0430 \u0441\u0442\u043e\u044f\u0442\u044c \u043d\u0430 \u0432\u043e\u0434\u0435, \u0447\u0430\u0441\u0442\u044c \u043d\u0430 \u0441\u0443\u0448\u0435.\n\n\u041f\u043e\u0441\u043b\u0435 \u044d\u0442\u043e\u0433\u043e \u043e\u0442\u043a\u0440\u043e\u0439 \u0412\u0430\u043a\u0430\u043d\u0441\u0438\u0438 \u0438 \u043d\u0430\u0437\u043d\u0430\u0447\u044c \u043e\u0434\u043d\u043e\u0433\u043e \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e \u0432 \u0414\u043e\u043a\u0438."
                : "Build Docks on the river bank: part of the building must sit on water, part on land.\n\nThen open Vacancies and assign one worker to Docks.");
    }

    private void ShowUserDocksBuiltTutorial()
    {
        if (hasShownUserDocksBuiltTutorial)
        {
            return;
        }

        hasShownUserDocksBuiltTutorial = true;
        bool ru = IsRussianLanguage();
        ShowTutorialWindow(
            TutorialTrigger.UserDocksBuiltInfo,
            32,
            ru ? "\u0414\u043e\u043a\u0438 \u043f\u043e\u0441\u0442\u0440\u043e\u0435\u043d\u044b" : "Docks Built",
            ru
                ? "\u0414\u043e\u043a\u0438 \u043f\u0440\u0438\u043d\u0438\u043c\u0430\u044e\u0442 \u043a\u0443\u043f\u043b\u0435\u043d\u043d\u044b\u0435 \u0442\u043e\u0432\u0430\u0440\u044b \u0441 \u043a\u043e\u0440\u0430\u0431\u043b\u0435\u0439 \u0438 \u0433\u0440\u0443\u0437\u044b \u043d\u0430 \u043f\u0440\u043e\u0434\u0430\u0436\u0443 \u0441\u043e \u0421\u043a\u043b\u0430\u0434\u0430.\n\n\u041a\u0443\u043f\u043b\u0435\u043d\u043d\u043e\u0435 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438 \u0432\u0435\u0437\u0443\u0442 \u0438\u0437 \u0414\u043e\u043a\u043e\u0432 \u043d\u0430 \u0421\u043a\u043b\u0430\u0434, \u0430 \u0442\u043e\u0432\u0430\u0440\u044b \u043d\u0430 \u043f\u0440\u043e\u0434\u0430\u0436\u0443 - \u0441\u043e \u0421\u043a\u043b\u0430\u0434\u0430 \u0432 \u0414\u043e\u043a\u0438."
                : "Docks receive imported goods from ships and export cargo from Warehouse.\n\nTrucks move bought goods from Docks to Warehouse, and sale goods from Warehouse to Docks.");
    }

    private void ShowUserTradePolicyTutorial()
    {
        if (hasShownUserTradePolicyTutorial)
        {
            return;
        }

        hasShownUserTradePolicyTutorial = true;
        bool ru = IsRussianLanguage();
        ShowTutorialWindow(
            TutorialTrigger.UserTradePolicyInfo,
            33,
            ru ? "\u041f\u043e\u043b\u0438\u0442\u0438\u043a\u0430 \u0442\u043e\u0440\u0433\u043e\u0432\u043b\u0438" : "Trade Policy",
            ru
                ? "\u0412 \u043c\u0435\u043d\u044e \u0422\u043e\u0440\u0433\u043e\u0432\u043b\u044f \u0432\u044b\u0431\u0435\u0440\u0438 \u0422\u0435\u043a\u0441\u0442\u0438\u043b\u044c \u0438 \u043f\u0435\u0440\u0435\u043a\u043b\u044e\u0447\u0438 \u0440\u0435\u0436\u0438\u043c \u043d\u0430 \"\u0414\u043e\u043a\u0443\u043f\u0438\u0442\u044c \u0434\u043e \u043d\u043e\u0440\u043c\u044b\".\n\n\u041f\u043e\u0441\u043b\u0435 \u044d\u0442\u043e\u0433\u043e \u0440\u0435\u0447\u043d\u043e\u0439 \u043a\u043e\u0440\u0430\u0431\u043b\u044c \u0441\u043c\u043e\u0436\u0435\u0442 \u043f\u0440\u0438\u0432\u0435\u0437\u0442\u0438 \u0442\u043a\u0430\u043d\u044c, \u0430 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438 \u0434\u043e\u0432\u0435\u0437\u0443\u0442 \u0435\u0451 \u0434\u043e \u0421\u043a\u043b\u0430\u0434\u0430."
                : "In Trade, select Textile and switch the mode to Buy up to.\n\nAfter that, river ships can bring fabric and trucks will move it to Warehouse.");
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
            34,
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

        SessionDebugLogger.Log("TUTORIAL", "Race finished outside current tutorial goals.");
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
            tutorialGoalsMode != TutorialGoalsMode.TradeSetup)
        {
            return;
        }

        SessionDebugLogger.Log("TUTORIAL", "Race started outside current tutorial goals.");
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

        bool hasBuyTextileOrder = HasTutorialBuyTextileOrder();
        SessionDebugLogger.Log("TUTORIAL", $"Trade setup goal progress: buyTextile={hasBuyTextileOrder}.");

        if (hasBuyTextileOrder)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.CreateBuyTextileOrder);
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

    private int GetTutorialRiverTradeRegionIndex()
    {
        for (int i = 0; i < 9; i++)
        {
            if (i == 4)
            {
                continue;
            }

            RegionalCityData city = GetRegionalCity(i);
            if (city != null &&
                city.IsKnown &&
                city.RouteMode == RegionalTradeRouteMode.River &&
                System.Array.IndexOf(city.Sells, TradeResourceType.Textile) >= 0)
            {
                return i;
            }
        }

        return 5;
    }

    private void NotifyTutorialBuildingWorkerAssigned(LocationType buildingType)
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
        {
            return;
        }

        if (buildingType == LocationType.LaborExchange)
        {
            CheckTutorialLaborExchangeGoal();
        }
        else if (buildingType == LocationType.Docks)
        {
            CheckTutorialDocksGoal();
        }
    }

    private void CheckTutorialLaborExchangeGoal()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial ||
            isTutorialSkipped ||
            !isTutorialGoalsActive ||
            isTutorialGoalsComplete ||
            tutorialGoalsMode != TutorialGoalsMode.LaborExchange)
        {
            return;
        }

        bool hasLaborExchange = locations.ContainsKey(LocationType.LaborExchange);
        int assignedWorkers = CountLogisticsWorkers(LocationType.LaborExchange);
        SessionDebugLogger.Log("TUTORIAL", $"Labor Exchange goal progress: built={hasLaborExchange}, workers={assignedWorkers}/1.");

        if (hasLaborExchange)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.BuildLaborExchange);
        }

        if (assignedWorkers >= 1)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.StaffLaborExchange);
        }
    }

    private void CheckTutorialWorkerArrivalGoal()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial ||
            isTutorialSkipped ||
            !isTutorialGoalsActive ||
            isTutorialGoalsComplete ||
            tutorialGoalsMode != TutorialGoalsMode.WorkerCard)
        {
            return;
        }

        MarkTutorialGoalComplete(TutorialGoalKind.WaitForWorkerArrival);
    }

    private void NotifyTutorialWorldMapOpened()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
        {
            return;
        }

        CheckTutorialRegionalMapGoal();
    }

    private void NotifyTutorialTradeRouteBuilt(int regionIndex)
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
        {
            return;
        }

        CheckTutorialRegionalMapGoal();

        int riverRegionIndex = GetTutorialRiverTradeRegionIndex();
        if (regionIndex == riverRegionIndex)
        {
            SessionDebugLogger.Log("TUTORIAL", $"Tutorial river trade route built at region {regionIndex}.");
        }
    }

    private void CheckTutorialRegionalMapGoal()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial ||
            isTutorialSkipped ||
            !isTutorialGoalsActive ||
            isTutorialGoalsComplete ||
            tutorialGoalsMode != TutorialGoalsMode.RegionalMap)
        {
            return;
        }

        int riverRegionIndex = GetTutorialRiverTradeRegionIndex();
        bool routeBuilt = IsWorldMapTradeRouteBuilt(riverRegionIndex);
        SessionDebugLogger.Log("TUTORIAL", $"Regional map goal progress: open={isWorldMapPanelOpen}, riverRoute={routeBuilt} region={riverRegionIndex}.");

        if (isWorldMapPanelOpen)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.OpenRegionalMap);
        }

        if (routeBuilt)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.BuildTradeRoute);
        }
    }

    private void NotifyTutorialDocksBuilt()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
        {
            return;
        }

        CheckTutorialDocksGoal();
        if (!hasShownUserDocksBuiltTutorial)
        {
            ScheduleTutorial(TutorialTrigger.UserDocksBuiltInfo, 0.35f);
        }
    }

    private void CheckTutorialDocksGoal()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial ||
            isTutorialSkipped ||
            !isTutorialGoalsActive ||
            isTutorialGoalsComplete ||
            tutorialGoalsMode != TutorialGoalsMode.Docks)
        {
            return;
        }

        bool hasDocks = locations.ContainsKey(LocationType.Docks);
        int assignedWorkers = CountLogisticsWorkers(LocationType.Docks);
        SessionDebugLogger.Log("TUTORIAL", $"Docks goal progress: built={hasDocks}, workers={assignedWorkers}/1.");

        if (hasDocks)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.BuildDocks);
        }

        if (assignedWorkers >= 1)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.AssignDocksWorker);
        }
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
