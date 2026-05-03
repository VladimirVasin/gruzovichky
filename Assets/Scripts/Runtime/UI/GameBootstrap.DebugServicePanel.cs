using UnityEngine;

using System.Collections.Generic;

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

    // в”Ђв”Ђ state в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    private bool    isDebugServicePanelOpen;
    private int     debugSelectedDriverId = -1;
    private Vector2 debugWorkerScrollPos;
    private Vector2 debugResourceScrollPos;
    private Rect    debugPanelRect;
    private Texture2D _debugOverlayTex;

    private static readonly (LocationType type, string label)[] DebugServiceBuildings =
    {
        (LocationType.Bar,          "Bar"),
        (LocationType.Canteen,      "Canteen"),
        (LocationType.GamblingHall, "Gambling Hall"),
        (LocationType.CityPark,     "City Park"),
        (LocationType.Motel,        "Motel (Sleep)"),
    };

    // в”Ђв”Ђ toggle в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    private void ToggleDebugServicePanel()
    {
        isDebugServicePanelOpen = !isDebugServicePanelOpen;
        if (!isDebugServicePanelOpen) debugSelectedDriverId = -1;
    }

    // в”Ђв”Ђ called from the main OnGUI в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    private void DrawDebugServicePanel()
    {
        if (!isDebugServicePanelOpen) return;

        if (_debugOverlayTex == null)
        {
            _debugOverlayTex = new Texture2D(1, 1);
            _debugOverlayTex.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.10f, 1f));
            _debugOverlayTex.Apply();
        }

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _debugOverlayTex);
        debugPanelRect = new Rect(0, 0, Screen.width, Screen.height);
        debugPanelRect = GUI.Window(9901, debugPanelRect, DrawDebugServiceWindow, "DEBUG  Send Worker to Service");
    }

    private void DrawDebugServiceWindow(int id)
    {
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.9f, 0.85f, 0.4f) },
            margin = new RectOffset(0, 0, 8, 4)
        };
        GUIStyle subHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.6f, 0.8f, 1f) },
            margin = new RectOffset(0, 0, 6, 2)
        };
        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };
        GUIStyle valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        float panelW = Screen.width;
        float panelH = Screen.height;
        float col1W  = panelW * 0.28f;
        float col2W  = panelW * 0.36f;
        float col3W  = panelW * 0.36f;
        float contentH = panelH - 80f;

        using (new GUILayout.HorizontalScope())
        {
            // ── Column 1: Workers ─────────────────────────────────────
            using (new GUILayout.VerticalScope(GUILayout.Width(col1W), GUILayout.Height(contentH)))
            {
                GUILayout.Label("WORKERS", headerStyle);

                DriverAgent target = driverAgents?.Find(x => x.DriverId == debugSelectedDriverId);

                debugWorkerScrollPos = GUILayout.BeginScrollView(debugWorkerScrollPos, GUILayout.ExpandHeight(true));
                foreach (DriverAgent d in driverAgents)
                {
                    if (d?.DriverObject == null) continue;
                    bool selected = debugSelectedDriverId == d.DriverId;
                    Color prev = GUI.backgroundColor;
                    GUI.backgroundColor = selected ? new Color(0.85f, 0.70f, 0.05f) : new Color(0.18f, 0.22f, 0.28f);
                    string phase = d.WalkPhase != DriverRescuePhase.None ? $"\n  [{d.WalkPhase}]" : string.Empty;
                    GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
                    if (GUILayout.Button($"  {d.DriverName}   ${d.Money}{phase}", btnStyle, GUILayout.Height(42f)))
                        debugSelectedDriverId = d.DriverId;
                    GUI.backgroundColor = prev;
                    GUILayout.Space(2f);
                }
                GUILayout.EndScrollView();

                GUILayout.Space(10f);
                GUILayout.Label("SEND TO SERVICE", headerStyle);
                foreach (var (type, label) in DebugServiceBuildings)
                {
                    bool built   = locations != null && locations.ContainsKey(type);
                    bool canSend = built && target != null;
                    GUI.enabled  = canSend;
                    Color prev = GUI.backgroundColor;
                    GUI.backgroundColor = canSend ? new Color(0.15f, 0.35f, 0.55f) : new Color(0.15f, 0.18f, 0.22f);
                    GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 13 };
                    string btnLabel = built ? label : $"{label}  [not built]";
                    if (GUILayout.Button(btnLabel, btnStyle, GUILayout.Height(36f)) && canSend)
                        ForceWorkerToServiceBuilding(target, type);
                    GUI.backgroundColor = prev;
                    GUILayout.Space(2f);
                }
                GUI.enabled = true;
            }

            GUILayout.Space(12f);

            // ── Column 2: Service resources ───────────────────────────
            using (new GUILayout.VerticalScope(GUILayout.Width(col2W), GUILayout.Height(contentH)))
            {
                GUILayout.Label("SERVICE BUILDINGS", headerStyle);
                debugResourceScrollPos = GUILayout.BeginScrollView(debugResourceScrollPos, GUILayout.ExpandHeight(true));
                DrawDebugServiceResourceRow(LocationType.GasStation, "Gas Station",  DebugServiceResourceKind.Fuel,     bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugServiceResourceRow(LocationType.Bar,        "Bar",          DebugServiceResourceKind.Alcohol,  bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugServiceResourceRow(LocationType.Canteen,    "Canteen",      DebugServiceResourceKind.Food,     bodyStyle, valueStyle, subHeaderStyle);
                GUILayout.EndScrollView();
            }

            GUILayout.Space(12f);

            // ── Column 3: Production resources ────────────────────────
            using (new GUILayout.VerticalScope(GUILayout.Width(col3W), GUILayout.Height(contentH)))
            {
                GUILayout.Label("PRODUCTION / STORAGE", headerStyle);
                Vector2 prodScroll = debugResourceScrollPos;
                prodScroll = GUILayout.BeginScrollView(prodScroll, GUILayout.ExpandHeight(true));
                DrawDebugServiceResourceRow(LocationType.Warehouse,        "Warehouse  —  Fuel",        DebugServiceResourceKind.Fuel,     bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugServiceResourceRow(LocationType.Warehouse,        "Warehouse  —  Alcohol",     DebugServiceResourceKind.Alcohol,  bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugServiceResourceRow(LocationType.Warehouse,        "Warehouse  —  Food",        DebugServiceResourceKind.Food,     bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugProductionResourceRow(LocationType.Forest,           "Forest",                    DebugProductionResourceKind.Logs,      bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugProductionResourceRow(LocationType.Sawmill,          "Sawmill  —  Logs",          DebugProductionResourceKind.Logs,      bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugProductionResourceRow(LocationType.Sawmill,          "Sawmill  —  Boards",        DebugProductionResourceKind.Boards,    bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugProductionResourceRow(LocationType.Warehouse,        "Warehouse  —  Logs",        DebugProductionResourceKind.Logs,      bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugProductionResourceRow(LocationType.Warehouse,        "Warehouse  —  Boards",      DebugProductionResourceKind.Boards,    bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugProductionResourceRow(LocationType.Warehouse,        "Warehouse  —  Textile",     DebugProductionResourceKind.Textile,   bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugProductionResourceRow(LocationType.Warehouse,        "Warehouse  —  Furniture",   DebugProductionResourceKind.Furniture, bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugProductionResourceRow(LocationType.FurnitureFactory, "Furn. Factory  —  Boards",  DebugProductionResourceKind.Boards,    bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugProductionResourceRow(LocationType.FurnitureFactory, "Furn. Factory  —  Textile", DebugProductionResourceKind.Textile,   bodyStyle, valueStyle, subHeaderStyle);
                DrawDebugProductionResourceRow(LocationType.FurnitureFactory, "Furn. Factory  —  Furn.",   DebugProductionResourceKind.Furniture, bodyStyle, valueStyle, subHeaderStyle);
                GUILayout.EndScrollView();
            }
        }

        // ── Bottom bar ────────────────────────────────────────────────
        GUILayout.FlexibleSpace();
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.12f, 0.14f, 0.10f);
        using (new GUILayout.HorizontalScope(GUI.skin.box, GUILayout.Height(48f)))
        {
            GUIStyle treasuryLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) },
                alignment = TextAnchor.MiddleLeft
            };
            GUILayout.Label($"  TREASURY:  ${money}", treasuryLabelStyle, GUILayout.ExpandWidth(false));
            GUILayout.Space(16f);
            GUI.backgroundColor = new Color(0.6f, 0.15f, 0.15f);
            GUIStyle adjBtn = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };
            if (GUILayout.Button("  - 50  ", adjBtn, GUILayout.Height(36f), GUILayout.ExpandWidth(false)))
                money = Mathf.Max(0, money - 50);
            GUILayout.Space(4f);
            GUI.backgroundColor = new Color(0.15f, 0.45f, 0.15f);
            if (GUILayout.Button("  + 50  ", adjBtn, GUILayout.Height(36f), GUILayout.ExpandWidth(false)))
                money += 50;
            GUILayout.Space(14f);
            GUI.backgroundColor = new Color(0.18f, 0.34f, 0.58f);
            if (GUILayout.Button("  AUTO ASSIGN ALL  ", adjBtn, GUILayout.Height(36f), GUILayout.ExpandWidth(false)))
                DebugAutoAssignAllAvailableWorkers();
            GUILayout.Space(6f);
            bool canSummonWorkers = hiringDriverArrival == null;
            GUI.enabled = canSummonWorkers;
            GUI.backgroundColor = canSummonWorkers ? new Color(0.22f, 0.42f, 0.64f) : new Color(0.18f, 0.18f, 0.18f);
            if (GUILayout.Button("  SUMMON 10 WORKERS  ", adjBtn, GUILayout.Height(36f), GUILayout.ExpandWidth(false)))
                DebugSummonWorkerWave(DebugHireWorkerWaveCount);
            GUI.enabled = true;
            GUILayout.Space(24f);

            // ── Weather buttons ───────────────────────────────────────
            GUIStyle weatherBtn = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            WeatherState[] weatherStates = { WeatherState.Clear, WeatherState.Overcast, WeatherState.Rainy, WeatherState.Foggy, WeatherState.Windy };
            foreach (WeatherState ws in weatherStates)
            {
                bool isCurrent = !isWeatherTransitioning && currentWeatherState == ws;
                bool isTarget  =  isWeatherTransitioning && nextWeatherState   == ws;
                GUI.backgroundColor = isCurrent ? new Color(0.85f, 0.70f, 0.05f)
                                    : isTarget  ? new Color(0.10f, 0.45f, 0.70f)
                                    :             new Color(0.20f, 0.22f, 0.26f);
                if (GUILayout.Button(GetWeatherStateIcon(ws), weatherBtn, GUILayout.Width(42f), GUILayout.Height(36f)))
                    DebugSetWeather(ws);
                GUILayout.Space(2f);
            }
            GUI.backgroundColor = prevBg;
            GUILayout.Space(24f);

            // ── Join the Race ─────────────────────────────────────────
            GUIStyle raceBtn = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
            GUI.backgroundColor = isRacingActive ? new Color(0.20f, 0.22f, 0.26f) : new Color(0.65f, 0.30f, 0.08f);
            GUI.enabled = !isRacingActive;
            if (GUILayout.Button("  JOIN THE RACE  ", raceBtn, GUILayout.Height(36f), GUILayout.ExpandWidth(false)))
            {
                ToggleDebugServicePanel();
                StartRacingMinigame();
            }
            GUI.enabled = true;
            GUI.backgroundColor = prevBg;
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(0.35f, 0.18f, 0.18f);
            GUIStyle closeBtn = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
            if (GUILayout.Button("  CLOSE  [F9]  ", closeBtn, GUILayout.Height(36f), GUILayout.ExpandWidth(false)))
                ToggleDebugServicePanel();
            GUI.backgroundColor = prevBg;
            GUILayout.Space(8f);
        }
    }

    private void DrawDebugServiceResourceRow(LocationType type, string label, DebugServiceResourceKind resourceKind,
        GUIStyle bodyStyle, GUIStyle valueStyle, GUIStyle subHeaderStyle)
    {
        LocationData location = null;
        bool built = locations != null && locations.TryGetValue(type, out location);
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.14f, 0.17f, 0.22f);
        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            GUI.backgroundColor = prevBg;
            GUILayout.Label(built ? label : $"{label}  [not built]", subHeaderStyle);
            if (!built || location == null) return;

            int cur = GetDebugServiceResourceValue(location, resourceKind);
            int max = GetDebugServiceResourceMax(type, resourceKind);
            GUILayout.Label($"{GetDebugServiceResourceLabel(resourceKind)}: {cur} / {max}", valueStyle);

            using (new GUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(0.5f, 0.12f, 0.12f);
                if (GUILayout.Button("-1", GUILayout.Width(60f), GUILayout.Height(28f)))
                    AdjustDebugServiceResource(type, resourceKind, -1);
                GUILayout.Space(4f);
                GUI.backgroundColor = new Color(0.12f, 0.42f, 0.12f);
                if (GUILayout.Button("+1", GUILayout.Width(60f), GUILayout.Height(28f)))
                    AdjustDebugServiceResource(type, resourceKind, 1);
                GUI.backgroundColor = prevBg;
            }
        }
        GUILayout.Space(4f);
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

    private void DrawDebugProductionResourceRow(LocationType type, string label, DebugProductionResourceKind resourceKind,
        GUIStyle bodyStyle, GUIStyle valueStyle, GUIStyle subHeaderStyle)
    {
        LocationData location = null;
        bool built = locations != null && locations.TryGetValue(type, out location);
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.14f, 0.17f, 0.22f);
        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            GUI.backgroundColor = prevBg;
            GUILayout.Label(built ? label : $"{label}  [not built]", subHeaderStyle);
            if (!built || location == null) return;

            int cur = GetDebugProductionResourceValue(location, resourceKind);
            int max = GetDebugProductionResourceMax(type, resourceKind);
            GUILayout.Label($"{GetDebugProductionResourceLabel(resourceKind)}: {cur} / {max}", valueStyle);

            using (new GUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(0.5f, 0.12f, 0.12f);
                if (GUILayout.Button("-1", GUILayout.Width(60f), GUILayout.Height(28f)))
                    AdjustDebugProductionResource(type, resourceKind, -1);
                GUILayout.Space(4f);
                GUI.backgroundColor = new Color(0.12f, 0.42f, 0.12f);
                if (GUILayout.Button("+1", GUILayout.Width(60f), GUILayout.Height(28f)))
                    AdjustDebugProductionResource(type, resourceKind, 1);
                GUI.backgroundColor = prevBg;
            }
        }
        GUILayout.Space(4f);
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

    private static int GetDebugServiceResourceMax(LocationType type, DebugServiceResourceKind resourceKind)
    {
        if (type == LocationType.Warehouse)
        {
            return resourceKind switch
            {
                DebugServiceResourceKind.Fuel => WarehouseMaxFuelStorage,
                DebugServiceResourceKind.Alcohol => WarehouseMaxAlcoholStorage,
                DebugServiceResourceKind.Food => WarehouseMaxFoodStorage,
                _ => 0
            };
        }

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

        int maxValue = GetDebugServiceResourceMax(type, resourceKind);
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

    // в”Ђв”Ђ force send в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
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
                LocationType.CityPark     => (DriverRescuePhase.IdleWalkToCityPark,     WorkerCityParkDuration,     WorkerLifeGoal.Leisure),
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

        SessionDebugLogger.Log("DEBUG", $"[DBG] Forced {driver.DriverName} в†’ {type}.");
    }


}
