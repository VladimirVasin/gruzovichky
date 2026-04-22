using UnityEngine;

public partial class GameBootstrap
{
    private enum DebugServiceResourceKind
    {
        Fuel,
        Alcohol,
        Food
    }

    private enum DebugProductionResourceKind
    {
        Logs,
        Boards,
        Textile,
        Furniture
    }

    private const int DebugLooseStorageMax = 99;

    // ── state ───────────────────────────────────────────────────────────────
    private bool    isDebugServicePanelOpen;
    private int     debugSelectedDriverId = -1;
    private Vector2 debugWorkerScrollPos;
    private Vector2 debugResourceScrollPos;
    private Rect    debugPanelRect = new Rect(10f, 10f, 380f, 820f);

    private static readonly (LocationType type, string label)[] DebugServiceBuildings =
    {
        (LocationType.Bar,          "Bar"),
        (LocationType.Canteen,      "Canteen"),
        (LocationType.GamblingHall, "Gambling Hall"),
        (LocationType.Motel,        "Motel (Sleep)"),
    };

    // ── toggle ──────────────────────────────────────────────────────────────
    private void ToggleDebugServicePanel()
    {
        isDebugServicePanelOpen = !isDebugServicePanelOpen;
        if (!isDebugServicePanelOpen) debugSelectedDriverId = -1;
    }

    // ── called from the main OnGUI ──────────────────────────────────────────
    private void DrawDebugServicePanel()
    {
        if (!isDebugServicePanelOpen) return;
        debugPanelRect = GUI.Window(9901, debugPanelRect, DrawDebugServiceWindow, "DEBUG  Send Worker to Service");
    }

    private void DrawDebugServiceWindow(int id)
    {
        GUILayout.Space(4f);
        GUILayout.Label("Select worker:");

        debugWorkerScrollPos = GUILayout.BeginScrollView(debugWorkerScrollPos, GUILayout.Height(220f));
        foreach (DriverAgent d in driverAgents)
        {
            if (d?.DriverObject == null) continue;
            bool selected = debugSelectedDriverId == d.DriverId;
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = selected ? new Color(0.9f, 0.75f, 0.1f) : new Color(0.25f, 0.30f, 0.38f);
            string phase = d.WalkPhase != DriverRescuePhase.None ? $"  [{d.WalkPhase}]" : string.Empty;
            if (GUILayout.Button($"{d.DriverName}  ${d.Money}{phase}"))
                debugSelectedDriverId = d.DriverId;
            GUI.backgroundColor = prev;
        }
        GUILayout.EndScrollView();

        GUILayout.Space(8f);
        GUILayout.Label("Send to:");

        DriverAgent target = driverAgents?.Find(x => x.DriverId == debugSelectedDriverId);
        foreach (var (type, label) in DebugServiceBuildings)
        {
            bool built    = locations != null && locations.ContainsKey(type);
            bool canSend  = built && target != null;
            GUI.enabled   = canSend;
            string btnLabel = built ? label : $"{label}  [not built]";
            if (GUILayout.Button(btnLabel) && canSend)
                ForceWorkerToServiceBuilding(target, type);
        }
        GUI.enabled = true;

        if (selectedGameStartMode == GameStartMode.Debug)
        {
            GUILayout.Space(10f);
            GUILayout.Label("Debug resources:");

            debugResourceScrollPos = GUILayout.BeginScrollView(debugResourceScrollPos, GUILayout.Height(360f));
            GUILayout.Label("Service buildings:");
            DrawDebugServiceResourceRow(LocationType.GasStation, "Gas Station", DebugServiceResourceKind.Fuel);
            DrawDebugServiceResourceRow(LocationType.Bar, "Bar", DebugServiceResourceKind.Alcohol);
            DrawDebugServiceResourceRow(LocationType.Canteen, "Canteen", DebugServiceResourceKind.Food);
            GUILayout.Space(8f);
            GUILayout.Label("Production / storage:");
            DrawDebugProductionResourceRow(LocationType.Forest, "Forest", DebugProductionResourceKind.Logs);
            DrawDebugProductionResourceRow(LocationType.Sawmill, "Sawmill Logs", DebugProductionResourceKind.Logs);
            DrawDebugProductionResourceRow(LocationType.Sawmill, "Sawmill Boards", DebugProductionResourceKind.Boards);
            DrawDebugProductionResourceRow(LocationType.Warehouse, "Warehouse Logs", DebugProductionResourceKind.Logs);
            DrawDebugProductionResourceRow(LocationType.Warehouse, "Warehouse Boards", DebugProductionResourceKind.Boards);
            DrawDebugProductionResourceRow(LocationType.Warehouse, "Warehouse Textile", DebugProductionResourceKind.Textile);
            DrawDebugProductionResourceRow(LocationType.Warehouse, "Warehouse Furniture", DebugProductionResourceKind.Furniture);
            DrawDebugProductionResourceRow(LocationType.FurnitureFactory, "Furniture Factory Boards", DebugProductionResourceKind.Boards);
            DrawDebugProductionResourceRow(LocationType.FurnitureFactory, "Furniture Factory Textile", DebugProductionResourceKind.Textile);
            DrawDebugProductionResourceRow(LocationType.FurnitureFactory, "Furniture Factory Furniture", DebugProductionResourceKind.Furniture);
            GUILayout.EndScrollView();
        }

        GUILayout.Space(8f);
        if (GUILayout.Button("Close  [F9]"))
            ToggleDebugServicePanel();

        GUI.DragWindow();
    }

    private void DrawDebugServiceResourceRow(LocationType type, string label, DebugServiceResourceKind resourceKind)
    {
        LocationData location = null;
        bool built = locations != null && locations.TryGetValue(type, out location);
        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            GUILayout.Label(built ? label : $"{label}  [not built]");
            if (!built || location == null)
            {
                return;
            }

            int currentValue = GetDebugServiceResourceValue(location, resourceKind);
            int maxValue = GetDebugServiceResourceMax(resourceKind);
            GUILayout.Label($"{GetDebugServiceResourceLabel(resourceKind)}: {currentValue} / {maxValue}");

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-1", GUILayout.Width(52f)))
                {
                    AdjustDebugServiceResource(type, resourceKind, -1);
                }

                if (GUILayout.Button("+1", GUILayout.Width(52f)))
                {
                    AdjustDebugServiceResource(type, resourceKind, 1);
                }
            }
        }
    }

    private static string GetDebugServiceResourceLabel(DebugServiceResourceKind resourceKind)
    {
        return resourceKind switch
        {
            DebugServiceResourceKind.Fuel => "Fuel",
            DebugServiceResourceKind.Alcohol => "Alcohol",
            DebugServiceResourceKind.Food => "Food",
            _ => "Resource"
        };
    }

    private void DrawDebugProductionResourceRow(LocationType type, string label, DebugProductionResourceKind resourceKind)
    {
        LocationData location = null;
        bool built = locations != null && locations.TryGetValue(type, out location);
        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            GUILayout.Label(built ? label : $"{label}  [not built]");
            if (!built || location == null)
            {
                return;
            }

            int currentValue = GetDebugProductionResourceValue(location, resourceKind);
            int maxValue = GetDebugProductionResourceMax(type, resourceKind);
            GUILayout.Label($"{GetDebugProductionResourceLabel(resourceKind)}: {currentValue} / {maxValue}");

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-1", GUILayout.Width(52f)))
                {
                    AdjustDebugProductionResource(type, resourceKind, -1);
                }

                if (GUILayout.Button("+1", GUILayout.Width(52f)))
                {
                    AdjustDebugProductionResource(type, resourceKind, 1);
                }
            }
        }
    }

    private static string GetDebugProductionResourceLabel(DebugProductionResourceKind resourceKind)
    {
        return resourceKind switch
        {
            DebugProductionResourceKind.Logs => "Logs",
            DebugProductionResourceKind.Boards => "Boards",
            DebugProductionResourceKind.Textile => "Textile",
            DebugProductionResourceKind.Furniture => "Furniture",
            _ => "Resource"
        };
    }

    private static int GetDebugServiceResourceMax(DebugServiceResourceKind resourceKind)
    {
        return resourceKind switch
        {
            DebugServiceResourceKind.Fuel => GasStationMaxFuelStorage,
            DebugServiceResourceKind.Alcohol => BarMaxAlcoholStorage,
            DebugServiceResourceKind.Food => CanteenMaxFoodStorage,
            _ => 0
        };
    }

    private static int GetDebugProductionResourceMax(LocationType type, DebugProductionResourceKind resourceKind)
    {
        return (type, resourceKind) switch
        {
            (LocationType.Forest, DebugProductionResourceKind.Logs) => ForestMaxLogsStorage,
            (LocationType.FurnitureFactory, DebugProductionResourceKind.Boards) => FurnitureFactoryMaxBoardsStorage,
            (LocationType.FurnitureFactory, DebugProductionResourceKind.Textile) => FurnitureFactoryMaxTextileStorage,
            (LocationType.FurnitureFactory, DebugProductionResourceKind.Furniture) => FurnitureFactoryMaxFurnitureStorage,
            _ => DebugLooseStorageMax
        };
    }

    private static int GetDebugServiceResourceValue(LocationData location, DebugServiceResourceKind resourceKind)
    {
        return resourceKind switch
        {
            DebugServiceResourceKind.Fuel => location.FuelStored,
            DebugServiceResourceKind.Alcohol => location.AlcoholStored,
            DebugServiceResourceKind.Food => location.FoodStored,
            _ => 0
        };
    }

    private static int GetDebugProductionResourceValue(LocationData location, DebugProductionResourceKind resourceKind)
    {
        return resourceKind switch
        {
            DebugProductionResourceKind.Logs => location.LogsStored,
            DebugProductionResourceKind.Boards => location.BoardsStored,
            DebugProductionResourceKind.Textile => location.TextileStored,
            DebugProductionResourceKind.Furniture => location.FurnitureStored,
            _ => 0
        };
    }

    private void AdjustDebugServiceResource(LocationType type, DebugServiceResourceKind resourceKind, int delta)
    {
        if (!locations.TryGetValue(type, out LocationData location))
        {
            return;
        }

        int maxValue = GetDebugServiceResourceMax(resourceKind);
        switch (resourceKind)
        {
            case DebugServiceResourceKind.Fuel:
                location.FuelStored = Mathf.Clamp(location.FuelStored + delta, 0, maxValue);
                break;
            case DebugServiceResourceKind.Alcohol:
                location.AlcoholStored = Mathf.Clamp(location.AlcoholStored + delta, 0, maxValue);
                break;
            case DebugServiceResourceKind.Food:
                location.FoodStored = Mathf.Clamp(location.FoodStored + delta, 0, maxValue);
                break;
        }

        SessionDebugLogger.Log("DEBUG", $"[DBG] {type} {GetDebugServiceResourceLabel(resourceKind)} adjusted by {delta}; now {GetDebugServiceResourceValue(location, resourceKind)}/{maxValue}.");
    }

    private void AdjustDebugProductionResource(LocationType type, DebugProductionResourceKind resourceKind, int delta)
    {
        if (!locations.TryGetValue(type, out LocationData location))
        {
            return;
        }

        int maxValue = GetDebugProductionResourceMax(type, resourceKind);
        switch (resourceKind)
        {
            case DebugProductionResourceKind.Logs:
                location.LogsStored = Mathf.Clamp(location.LogsStored + delta, 0, maxValue);
                break;
            case DebugProductionResourceKind.Boards:
                location.BoardsStored = Mathf.Clamp(location.BoardsStored + delta, 0, maxValue);
                break;
            case DebugProductionResourceKind.Textile:
                location.TextileStored = Mathf.Clamp(location.TextileStored + delta, 0, maxValue);
                break;
            case DebugProductionResourceKind.Furniture:
                location.FurnitureStored = Mathf.Clamp(location.FurnitureStored + delta, 0, maxValue);
                break;
        }

        SessionDebugLogger.Log("DEBUG", $"[DBG] {type} {GetDebugProductionResourceLabel(resourceKind)} adjusted by {delta}; now {GetDebugProductionResourceValue(location, resourceKind)}/{maxValue}.");
    }

    // ── force send ──────────────────────────────────────────────────────────
    private void ForceWorkerToServiceBuilding(DriverAgent driver, LocationType type)
    {
        if (driver?.DriverObject == null) return;
        if (locations == null || !locations.ContainsKey(type)) return;

        Vector3 pos = driver.DriverObject.transform.position;

        // Interrupt current activity
        if (driver.WalkPhase == DriverRescuePhase.IdleSittingOnBench)
            ReleaseBench(driver);
        driver.WalkPath?.Clear();
        driver.WalkWaypointIndex  = 0;
        driver.WalkAnimationTime  = 0f;
        driver.WalkPhase          = DriverRescuePhase.None;
        driver.RestPhase          = DriverRestPhase.None;
        driver.IdleActivityTimer  = 0f;
        driver.GamblingBet        = 0;
        driver.GamblingPayout     = 0;
        driver.GamblingMultiplier = 0;

        if (type == LocationType.Motel)
        {
            // Temporarily guarantee the driver can afford it
            LocationData motel = locations[LocationType.Motel];
            int saved = driver.Money;
            if (driver.Money < motel.ServiceFee) driver.Money = motel.ServiceFee;
            TryStartWorkerSleep(driver, pos);
            if (saved < motel.ServiceFee) driver.Money = saved;
        }
        else
        {
            (DriverRescuePhase walkPhase, float duration, WorkerLifeGoal goal) = type switch
            {
                LocationType.Bar          => (DriverRescuePhase.IdleWalkToBar,          WorkerLeisureDuration,      WorkerLifeGoal.Leisure),
                LocationType.Canteen      => (DriverRescuePhase.IdleWalkToCanteen,      WorkerCanteenDuration,      WorkerLifeGoal.Eat),
                LocationType.GamblingHall => (DriverRescuePhase.IdleWalkToGamblingHall, WorkerGamblingHallDuration, WorkerLifeGoal.Leisure),
                _                         => (DriverRescuePhase.None, 0f,               WorkerLifeGoal.None)
            };
            if (walkPhase == DriverRescuePhase.None) return;

            // Guarantee min balance for gambling
            if (type == LocationType.GamblingHall && driver.Money < WorkerGamblingMinBalance)
                driver.Money = WorkerGamblingMinBalance;

            LocationData service = locations[type];
            float x = (service.Min.x + service.Max.x + 1) * 0.5f;
            float z = (service.Min.y + service.Max.y + 1) * 0.5f;
            Vector3 target = new(x + Random.Range(-0.2f, 0.2f), 0f, z + Random.Range(-0.2f, 0.2f));

            driver.LifeGoal          = goal;
            driver.IdleActivityTimer = duration;
            driver.WalkTargetWorld   = target;
            driver.WalkPhase         = walkPhase;
            driver.WalkAnimationTime = 0f;
            BuildDriverWalkPath(driver, pos, target);
        }

        SessionDebugLogger.Log("DEBUG", $"[DBG] Forced {driver.DriverName} → {type}.");
    }
}
