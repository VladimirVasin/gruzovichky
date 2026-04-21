using UnityEngine;

public partial class GameBootstrap
{
    // ── state ───────────────────────────────────────────────────────────────
    private bool    isDebugServicePanelOpen;
    private int     debugSelectedDriverId = -1;
    private Vector2 debugWorkerScrollPos;
    private Rect    debugPanelRect = new Rect(10f, 10f, 270f, 420f);

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

        debugWorkerScrollPos = GUILayout.BeginScrollView(debugWorkerScrollPos, GUILayout.Height(200f));
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

        GUILayout.Space(8f);
        if (GUILayout.Button("Close  [F9]"))
            ToggleDebugServicePanel();

        GUI.DragWindow();
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
