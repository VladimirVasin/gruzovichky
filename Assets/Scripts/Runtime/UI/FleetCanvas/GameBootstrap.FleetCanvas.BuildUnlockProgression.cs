using System.Collections.Generic;

public partial class GameBootstrap
{
    private void UnlockNewGameStarterBuildTools()
    {
        unlockedBuildTools?.Add(BuildTool.SingleRoad);
        unlockedBuildTools?.Add(BuildTool.Warehouse);
        unlockedBuildTools?.Add(BuildTool.Motel);
        unlockedBuildTools?.Add(BuildTool.CityHall);
    }

    private void NotifyNewGameBuildUnlockProgressionBuilt(LocationType builtType)
    {
        if (!IsNewGameBuildUnlockProgressionActive())
        {
            return;
        }

        if (!AreNewGameCoreUnlockBuildingsBuilt())
        {
            return;
        }

        List<BuildTool> newlyUnlocked = new();
        UnlockNewGameSecondLayerBuildTools(newlyUnlocked);
        if (AreNewGameSecondLayerUnlockPrerequisitesBuilt())
        {
            UnlockNewGameThirdLayerBuildTools(newlyUnlocked);
        }

        ShowNewGameBuildUnlockFeedback(newlyUnlocked);
    }

    private void UnlockNewGameSecondLayerBuildTools(List<BuildTool> newlyUnlocked)
    {
        UnlockNewGameBuildGroup(newlyUnlocked,
            BuildTool.Parking,
            BuildTool.LaborExchange,
            BuildTool.Canteen,
            BuildTool.Forest,
            BuildTool.GasStation,
            BuildTool.Sawmill,
            BuildTool.Bar);
    }

    private void UnlockNewGameThirdLayerBuildTools(List<BuildTool> newlyUnlocked)
    {
        UnlockNewGameBuildGroup(newlyUnlocked,
            BuildTool.Road,
            BuildTool.Stop,
            BuildTool.Docks,
            BuildTool.FurnitureFactory,
            BuildTool.Kiosk,
            BuildTool.GamblingHall,
            BuildTool.CityPark,
            BuildTool.PersonalHouse,
            BuildTool.Kindergarten,
            BuildTool.CarMarket);
    }

    private bool IsNewGameBuildUnlockProgressionActive()
    {
        return selectedGameStartMode == GameStartMode.NewGame && isGameStarted;
    }

    private bool AreNewGameCoreUnlockBuildingsBuilt()
    {
        return locations.ContainsKey(LocationType.Warehouse) &&
               locations.ContainsKey(LocationType.Motel) &&
               locations.ContainsKey(LocationType.CityHall);
    }

    private bool AreNewGameSecondLayerUnlockPrerequisitesBuilt()
    {
        return locations.ContainsKey(LocationType.Parking) &&
               locations.ContainsKey(LocationType.LaborExchange) &&
               locations.ContainsKey(LocationType.Forest) &&
               locations.ContainsKey(LocationType.GasStation) &&
               locations.ContainsKey(LocationType.Sawmill);
    }

    private void UnlockNewGameBuildGroup(List<BuildTool> newlyUnlocked, params BuildTool[] tools)
    {
        if (unlockedBuildTools == null || tools == null)
        {
            return;
        }

        for (int i = 0; i < tools.Length; i++)
        {
            BuildTool tool = tools[i];
            if (tool == BuildTool.None)
            {
                continue;
            }

            if (unlockedBuildTools.Add(tool))
            {
                newlyUnlocked?.Add(tool);
                MarkBuildToolJustUnlocked(tool);
                SessionDebugLogger.Log("BUILD", $"New Game progression unlocked build tool: {tool}.");
            }
        }
    }

    private void ShowNewGameBuildUnlockFeedback(List<BuildTool> newlyUnlocked)
    {
        if (newlyUnlocked == null || newlyUnlocked.Count == 0)
        {
            return;
        }

        isBuildScreenDirty = true;
        string enList = FormatBuildToolUnlockList(newlyUnlocked, false);
        string ruList = FormatBuildToolUnlockList(newlyUnlocked, true);
        PushFeedEvent(
            "New build options unlocked: " + enList,
            "\u041e\u0442\u043a\u0440\u044b\u0442\u044b \u043d\u043e\u0432\u044b\u0435 \u0432\u0430\u0440\u0438\u0430\u043d\u0442\u044b \u0441\u0442\u0440\u043e\u0438\u0442\u0435\u043b\u044c\u0441\u0442\u0432\u0430: " + ruList,
            FeedEventType.Success);
    }

    private string FormatBuildToolUnlockList(List<BuildTool> tools, bool ru)
    {
        if (tools == null || tools.Count == 0)
        {
            return string.Empty;
        }

        string result = string.Empty;
        for (int i = 0; i < tools.Count; i++)
        {
            string title = GetBuildCatalogTitle(tools[i], ru, tools[i].ToString());
            if (string.IsNullOrWhiteSpace(title))
            {
                title = tools[i].ToString();
            }

            result += i == 0 ? title : ", " + title;
        }

        return result;
    }
}
