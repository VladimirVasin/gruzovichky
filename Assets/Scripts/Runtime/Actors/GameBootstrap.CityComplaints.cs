using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float CityComplaintScanIntervalSeconds = 4.5f;
    private const float CityComplaintCooldownWorldHours = 8f;
    private const int CityComplaintMaxStoredResolved = 24;

    private enum CityComplaintCategory
    {
        NeedPressure,
        NoJob,
        LowMoney,
        ServiceMissing,
        FamilyStress
    }

    private enum CityComplaintState
    {
        Open,
        Expired,
        Resolved
    }

    private sealed class CityComplaint
    {
        public int Id;
        public int WorkerId;
        public string WorkerName;
        public string GroupKey = string.Empty;
        public CityComplaintCategory Category;
        public CityComplaintState State;
        public int Severity;
        public WorkerNeedKind? LinkedNeed;
        public LocationType? LinkedLocationType;
        public float CreatedWorldHour;
        public int CreatedDay;
        public float DueWorldHour;
        public float ResolvedWorldHour;
        public int ResolvedDay;
        public string ResolveReason = string.Empty;
        public bool ManuallyResolved;
        public readonly List<int> SignerIds = new();
        public readonly List<string> SignerNames = new();
    }

    private sealed class CityComplaintRowViewModel
    {
        public int Id;
        public string WorkerName;
        public CityComplaintCategory Category;
        public CityComplaintState State;
        public int Severity;
        public string Title;
        public string Detail;
        public string Meta;
        public Color AccentColor;
    }

    private readonly List<CityComplaint> cityComplaints = new();
    private readonly Dictionary<string, float> cityComplaintCooldownByKey = new();
    private int nextCityComplaintId = 1;
    private float cityComplaintScanTimer;
    private bool wasCityHallBuiltLastTick;

    private void UpdateCityHallRuntime()
    {
        bool cityHallBuilt = locations.ContainsKey(LocationType.CityHall);
        if (cityHallBuilt != wasCityHallBuiltLastTick)
        {
            wasCityHallBuiltLastTick = cityHallBuilt;
            isCityHallScreenDirty = true;
            if (cityHallBuilt)
            {
                SessionDebugLogger.Log("CITY_HALL", "City Hall runtime enabled.");
            }
        }

        if (!cityHallBuilt)
        {
            return;
        }

        cityComplaintScanTimer -= Time.deltaTime * Mathf.Max(0f, gameSpeedMultiplier);
        if (cityComplaintScanTimer > 0f)
        {
            return;
        }

        cityComplaintScanTimer = CityComplaintScanIntervalSeconds;
        ScanCityComplaints();
        ExpireOverdueCityComplaints();
        ResolveSatisfiedCityComplaints();
        PruneResolvedCityComplaints();
    }

    private void ScanCityComplaints()
    {
        int createdThisScan = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (!CanWorkerFileCityComplaint(worker))
            {
                continue;
            }

            TryCreateNeedComplaint(worker, WorkerNeedKind.Meal, ref createdThisScan);
            TryCreateNeedComplaint(worker, WorkerNeedKind.Sleep, ref createdThisScan);
            TryCreateNeedComplaint(worker, WorkerNeedKind.Leisure, ref createdThisScan);
            TryCreateLowMoneyComplaint(worker, ref createdThisScan);
            TryCreateNoJobComplaint(worker, ref createdThisScan);
            TryCreateServiceMissingComplaint(worker, ref createdThisScan);
            TryCreateFamilyStressComplaint(worker, ref createdThisScan);
        }

        PruneStaleCityComplaintPendingGroups();

        if (createdThisScan > 0)
        {
            isCityHallScreenDirty = true;
        }
    }

    private bool CanWorkerFileCityComplaint(DriverAgent worker)
    {
        return worker != null &&
               worker.DriverId > 0 &&
               !worker.IsArrivingByBus &&
               !worker.IsLeavingTown &&
               !worker.HasDepartedTown;
    }

    private void TryCreateNeedComplaint(DriverAgent worker, WorkerNeedKind need, ref int createdThisScan)
    {
        WorkerNeedStatus status = GetWorkerNeedLastStatus(worker, need);
        if (status != WorkerNeedStatus.Critical)
        {
            return;
        }

        TryCreateCityComplaint(worker, CityComplaintCategory.NeedPressure, 4, need, null, ref createdThisScan);
    }

    private void TryCreateLowMoneyComplaint(DriverAgent worker, ref int createdThisScan)
    {
        if (worker.Money > 4 || !HasCriticalWorkerNeed(worker))
        {
            return;
        }

        TryCreateCityComplaint(worker, CityComplaintCategory.LowMoney, 3, null, null, ref createdThisScan);
    }

    private void TryCreateNoJobComplaint(DriverAgent worker, ref int createdThisScan)
    {
        if (currentDay < 2 || !IsWorkerVacantForVacancyAssignment(worker))
        {
            return;
        }

        TryCreateCityComplaint(worker, CityComplaintCategory.NoJob, 2, null, LocationType.LaborExchange, ref createdThisScan);
    }

    private void TryCreateServiceMissingComplaint(DriverAgent worker, ref int createdThisScan)
    {
        if (worker.LastMealNeedStatus == WorkerNeedStatus.Critical &&
            !locations.ContainsKey(LocationType.Canteen) &&
            !locations.ContainsKey(LocationType.Kiosk))
        {
            TryCreateCityComplaint(worker, CityComplaintCategory.ServiceMissing, 3, WorkerNeedKind.Meal, LocationType.Canteen, ref createdThisScan);
        }

        if (worker.LastSleepNeedStatus == WorkerNeedStatus.Critical && !locations.ContainsKey(LocationType.Motel))
        {
            TryCreateCityComplaint(worker, CityComplaintCategory.ServiceMissing, 3, WorkerNeedKind.Sleep, LocationType.Motel, ref createdThisScan);
        }

        if (worker.LastLeisureNeedStatus == WorkerNeedStatus.Critical &&
            !locations.ContainsKey(LocationType.Bar) &&
            !locations.ContainsKey(LocationType.GamblingHall) &&
            !locations.ContainsKey(LocationType.CityPark))
        {
            TryCreateCityComplaint(worker, CityComplaintCategory.ServiceMissing, 2, WorkerNeedKind.Leisure, LocationType.Bar, ref createdThisScan);
        }
    }

    private void TryCreateFamilyStressComplaint(DriverAgent worker, ref int createdThisScan)
    {
        if (worker.FamilyId <= 0 || worker.Satisfaction >= 35 || !HasCriticalWorkerNeed(worker))
        {
            return;
        }

        TryCreateCityComplaint(worker, CityComplaintCategory.FamilyStress, 3, null, LocationType.Kindergarten, ref createdThisScan);
    }

    private void TryCreateCityComplaint(
        DriverAgent worker,
        CityComplaintCategory category,
        int severity,
        WorkerNeedKind? linkedNeed,
        LocationType? linkedLocationType,
        ref int createdThisScan)
    {
        if (TryRecordCityComplaintSignal(worker, category, Mathf.Clamp(severity, 1, 4), linkedNeed, linkedLocationType))
        {
            createdThisScan++;
        }
    }

    private void ResolveSatisfiedCityComplaints()
    {
        bool changed = false;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (complaint == null || complaint.State != CityComplaintState.Open)
            {
                continue;
            }

            DriverAgent worker = GetDriverAgentById(complaint.WorkerId);
            if (ShouldResolveCityComplaint(complaint, worker, out string reason))
            {
                ResolveCityComplaint(complaint, reason, manually: false);
                changed = true;
            }
        }

        if (changed)
        {
            isCityHallScreenDirty = true;
        }
    }

    private bool ShouldResolveCityComplaint(CityComplaint complaint, DriverAgent worker, out string reason)
    {
        reason = string.Empty;
        if (complaint != null && complaint.SignerIds.Count > 0)
        {
            if (!DoesCityComplaintConditionRemainForAnySigner(complaint, out reason))
            {
                if (string.IsNullOrEmpty(reason))
                {
                    reason = "group issue cleared";
                }

                return true;
            }

            return false;
        }

        if (worker == null || worker.HasDepartedTown || worker.IsLeavingTown)
        {
            reason = "worker unavailable";
            return true;
        }

        switch (complaint.Category)
        {
            case CityComplaintCategory.NeedPressure:
                if (complaint.LinkedNeed.HasValue && GetWorkerNeedLastStatus(worker, complaint.LinkedNeed.Value) != WorkerNeedStatus.Critical)
                {
                    reason = "need no longer critical";
                    return true;
                }
                break;
            case CityComplaintCategory.NoJob:
                if (!IsWorkerVacantForVacancyAssignment(worker))
                {
                    reason = "worker found an assignment";
                    return true;
                }
                break;
            case CityComplaintCategory.LowMoney:
                if (worker.Money >= 15)
                {
                    reason = "worker recovered money";
                    return true;
                }
                break;
            case CityComplaintCategory.ServiceMissing:
                if (complaint.LinkedLocationType.HasValue && locations.ContainsKey(complaint.LinkedLocationType.Value))
                {
                    reason = "requested service exists";
                    return true;
                }
                break;
            case CityComplaintCategory.FamilyStress:
                if (worker.Satisfaction >= 60 || !HasCriticalWorkerNeed(worker))
                {
                    reason = "family pressure eased";
                    return true;
                }
                break;
        }

        return false;
    }

    private bool ResolveCityComplaintManually(int complaintId)
    {
        CityComplaint complaint = GetCityComplaintById(complaintId);
        if (complaint == null || complaint.State != CityComplaintState.Open)
        {
            return false;
        }

        ResolveCityComplaint(complaint, "reviewed by player", manually: true);
        isCityHallScreenDirty = true;
        return true;
    }

    private void ResolveCityComplaint(CityComplaint complaint, string reason, bool manually)
    {
        if (complaint == null || complaint.State != CityComplaintState.Open)
        {
            return;
        }

        complaint.State = CityComplaintState.Resolved;
        complaint.ResolvedWorldHour = GetCurrentWorldHour();
        complaint.ResolvedDay = currentDay;
        complaint.ResolveReason = reason ?? string.Empty;
        complaint.ManuallyResolved = manually;
        cityComplaintCooldownByKey[GetCityComplaintGroupKey(complaint.Category, complaint.LinkedNeed, complaint.LinkedLocationType)] =
            complaint.ResolvedWorldHour + CityComplaintCooldownWorldHours;

        ApplyCityComplaintSatisfactionDelta(complaint, manually ? 1 : Mathf.Clamp(complaint.Severity, 1, 4));

        SessionDebugLogger.Log("CITY_HALL", $"Complaint #{complaint.Id} resolved: manual={manually}, reason={complaint.ResolveReason}.");
    }

    private void PruneResolvedCityComplaints()
    {
        int resolvedCount = 0;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            if (cityComplaints[i] != null && cityComplaints[i].State != CityComplaintState.Open)
            {
                resolvedCount++;
            }
        }

        if (resolvedCount <= CityComplaintMaxStoredResolved)
        {
            return;
        }

        cityComplaints.Sort(CompareCityComplaintsForStorage);
        for (int i = cityComplaints.Count - 1; i >= 0 && resolvedCount > CityComplaintMaxStoredResolved; i--)
        {
            if (cityComplaints[i] == null || cityComplaints[i].State == CityComplaintState.Open)
            {
                continue;
            }

            cityComplaints.RemoveAt(i);
            resolvedCount--;
        }
    }

    private static int CompareCityComplaintsForStorage(CityComplaint a, CityComplaint b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        int stateCompare = GetCityComplaintDisplayStateRank(a.State).CompareTo(GetCityComplaintDisplayStateRank(b.State));
        if (stateCompare != 0) return stateCompare;
        return b.CreatedWorldHour.CompareTo(a.CreatedWorldHour);
    }

    private int CountOpenCityComplaints()
    {
        int count = 0;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            if (cityComplaints[i]?.State == CityComplaintState.Open)
            {
                count++;
            }
        }

        return count;
    }

    private int CountCriticalCityComplaints()
    {
        int count = 0;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (complaint != null && complaint.State == CityComplaintState.Open && complaint.Severity >= 4)
            {
                count++;
            }
        }

        return count;
    }

    private int CountResolvedCityComplaintsToday()
    {
        int count = 0;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (complaint != null && complaint.State == CityComplaintState.Resolved && complaint.ResolvedDay == currentDay)
            {
                count++;
            }
        }

        return count;
    }

    private int CountOpenCityComplaintsForWorker(int workerId)
    {
        int count = 0;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (complaint != null && complaint.State == CityComplaintState.Open && complaint.WorkerId == workerId)
            {
                count++;
            }
        }

        return count;
    }

    private CityComplaint FindOpenCityComplaint(int workerId, CityComplaintCategory category, WorkerNeedKind? need, LocationType? target)
    {
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (complaint != null &&
                complaint.State == CityComplaintState.Open &&
                complaint.WorkerId == workerId &&
                complaint.Category == category &&
                complaint.LinkedNeed == need &&
                complaint.LinkedLocationType == target)
            {
                return complaint;
            }
        }

        return null;
    }

    private CityComplaint GetCityComplaintById(int complaintId)
    {
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            if (cityComplaints[i]?.Id == complaintId)
            {
                return cityComplaints[i];
            }
        }

        return null;
    }

    private CityComplaint GetHighestPriorityOpenCityComplaint()
    {
        CityComplaint best = null;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (complaint == null || complaint.State != CityComplaintState.Open)
            {
                continue;
            }

            if (best == null ||
                complaint.Severity > best.Severity ||
                complaint.Severity == best.Severity && complaint.CreatedWorldHour > best.CreatedWorldHour)
            {
                best = complaint;
            }
        }

        return best;
    }

    private List<CityComplaintRowViewModel> BuildCityHallComplaintRows(bool ru)
    {
        List<CityComplaint> ordered = new(cityComplaints);
        ordered.Sort(CompareCityComplaintsForDisplay);
        List<CityComplaintRowViewModel> rows = new();
        for (int i = 0; i < ordered.Count; i++)
        {
            CityComplaint complaint = ordered[i];
            if (complaint == null)
            {
                continue;
            }

            rows.Add(new CityComplaintRowViewModel
            {
                Id = complaint.Id,
                WorkerName = complaint.WorkerName,
                Category = complaint.Category,
                State = complaint.State,
                Severity = complaint.Severity,
                Title = FormatCityComplaintTitle(complaint, ru),
                Detail = FormatCityComplaintDetail(complaint, ru),
                Meta = FormatCityComplaintMeta(complaint, ru),
                AccentColor = GetCityComplaintAccentColor(complaint)
            });
        }

        return rows;
    }

    private static int CompareCityComplaintsForDisplay(CityComplaint a, CityComplaint b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        int stateCompare = GetCityComplaintDisplayStateRank(a.State).CompareTo(GetCityComplaintDisplayStateRank(b.State));
        if (stateCompare != 0) return stateCompare;
        int severityCompare = b.Severity.CompareTo(a.Severity);
        if (severityCompare != 0) return severityCompare;
        return b.CreatedWorldHour.CompareTo(a.CreatedWorldHour);
    }

    private string GetCityHallQuickResourceText()
    {
        bool ru = IsRussianLanguage();
        CityComplaint top = GetHighestPriorityOpenCityComplaint();
        string topLine = top != null
            ? FormatCityComplaintTitle(top, ru)
            : (ru ? "\u041d\u0435\u0442 \u043e\u0442\u043a\u0440\u044b\u0442\u044b\u0445 \u0436\u0430\u043b\u043e\u0431" : "No open complaints");

        return FormatValueLine(ru ? "\u041e\u0442\u043a\u0440\u044b\u0442\u043e" : "Open", CountOpenCityComplaints().ToString()) + "\n" +
               FormatValueLine(ru ? "\u041a\u0440\u0438\u0442\u0438\u0447\u043d\u044b\u0445" : "Critical", CountCriticalCityComplaints().ToString()) + "\n" +
               FormatValueLine(ru ? "\u041f\u0440\u043e\u0441\u0440\u043e\u0447\u0435\u043d\u043e" : "Expired", CountExpiredCityComplaints().ToString()) + "\n" +
               FormatValueLine(ru ? "\u0420\u0435\u0448\u0435\u043d\u043e \u0441\u0435\u0433\u043e\u0434\u043d\u044f" : "Resolved today", CountResolvedCityComplaintsToday().ToString()) + "\n" +
               FormatValueLine(ru ? "\u0413\u043b\u0430\u0432\u043d\u0430\u044f" : "Top issue", topLine);
    }

    private string FormatCityComplaintTitle(CityComplaint complaint, bool ru)
    {
        if (complaint == null)
        {
            return ru ? "\u0416\u0430\u043b\u043e\u0431\u0430" : "Complaint";
        }

        int signerCount = GetCityComplaintSignerCount(complaint);
        string countSuffix = signerCount > 1 ? $" ({signerCount})" : string.Empty;
        return complaint.Category switch
        {
            CityComplaintCategory.NeedPressure => $"{GetCityComplaintIssueTitle(complaint, ru)}{countSuffix}",
            CityComplaintCategory.NoJob => ru ? $"\u041d\u0443\u0436\u043d\u0430 \u0440\u0430\u0431\u043e\u0442\u0430{countSuffix}" : $"Need jobs{countSuffix}",
            CityComplaintCategory.LowMoney => ru ? $"\u041d\u0435 \u0445\u0432\u0430\u0442\u0430\u0435\u0442 \u0434\u0435\u043d\u0435\u0433{countSuffix}" : $"Low money{countSuffix}",
            CityComplaintCategory.ServiceMissing => ru ? $"\u041d\u0443\u0436\u0435\u043d \u0441\u0435\u0440\u0432\u0438\u0441{countSuffix}" : $"Missing service{countSuffix}",
            CityComplaintCategory.FamilyStress => ru ? $"\u0421\u0435\u043c\u0435\u0439\u043d\u044b\u0439 \u0441\u0442\u0440\u0435\u0441\u0441{countSuffix}" : $"Family stress{countSuffix}",
            _ => ru ? $"\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u0430\u044f \u0436\u0430\u043b\u043e\u0431\u0430{countSuffix}" : $"City complaint{countSuffix}"
        };
    }

    private string FormatCityComplaintDetail(CityComplaint complaint, bool ru)
    {
        if (complaint == null)
        {
            return string.Empty;
        }

        string target = complaint.LinkedLocationType.HasValue ? GetSelectedLocationDisplayName(complaint.LinkedLocationType.Value) : string.Empty;
        string state = GetCityComplaintStateLabel(complaint.State, ru);
        string signers = FormatCityComplaintSignerNames(complaint, ru);
        string timer = complaint.State == CityComplaintState.Open
            ? FormatCityComplaintTimeLeft(complaint, ru)
            : string.Empty;

        string reason = complaint.Category switch
        {
            CityComplaintCategory.NeedPressure => complaint.LinkedNeed.HasValue
                ? (ru
                    ? $"Проблема: {GetCityComplaintNeedLabel(complaint.LinkedNeed.Value, ru)} повторяется у подписавших жителей."
                    : $"Problem: repeated {GetCityComplaintNeedLabel(complaint.LinkedNeed.Value, ru)} pressure among signed citizens.")
                : (ru ? "Повторяющаяся проблема с нуждами." : "Repeated needs pressure."),
            CityComplaintCategory.NoJob => ru ? "Подписавшие жители долго остаются без назначения." : "Signed citizens have remained without assignments.",
            CityComplaintCategory.LowMoney => ru ? "У подписавших жителей мало денег, а нужды уже давят." : "Signed citizens have very low money while needs are pressing.",
            CityComplaintCategory.ServiceMissing => ru ? $"Рекомендуемая постройка: {target}." : $"Suggested building: {target}.",
            CityComplaintCategory.FamilyStress => ru ? "Низкое довольство и критические нужды давят на семьи подписавших жителей." : "Low satisfaction and critical needs are stressing signed families.",
            _ => string.Empty
        };

        if (complaint.State != CityComplaintState.Open && !string.IsNullOrWhiteSpace(complaint.ResolveReason))
        {
            reason += ru ? $"\n\u0418\u0442\u043e\u0433: {complaint.ResolveReason}." : $"\nResult: {complaint.ResolveReason}.";
        }

        string timerLine = string.IsNullOrWhiteSpace(timer)
            ? string.Empty
            : $"\n{timer}";

        return ru
            ? $"\u0421\u0442\u0430\u0442\u0443\u0441: {state}\n\u041f\u043e\u0434\u043f\u0438\u0441\u0430\u043b\u0438: {signers}\n{reason}{timerLine}"
            : $"State: {state}\nSigned: {signers}\n{reason}{timerLine}";
    }

    private string FormatCityComplaintMeta(CityComplaint complaint, bool ru)
    {
        if (complaint == null)
        {
            return string.Empty;
        }

        int hour = Mathf.FloorToInt(Mathf.Repeat(complaint.CreatedWorldHour, 24f));
        string created = ru ? $"\u0414{complaint.CreatedDay} {hour:00}:00" : $"D{complaint.CreatedDay} {hour:00}:00";
        string severity = ru ? $"\u0441\u0440\u043e\u0447\u043d\u043e\u0441\u0442\u044c {complaint.Severity}" : $"severity {complaint.Severity}";
        if (complaint.State == CityComplaintState.Open)
        {
            return $"{created} | {severity} | {FormatCityComplaintTimeLeft(complaint, ru)}";
        }

        return $"{created} | {GetCityComplaintStateLabel(complaint.State, ru)}";
    }

    private static string GetCityComplaintNeedLabel(WorkerNeedKind need, bool ru)
    {
        return need switch
        {
            WorkerNeedKind.Meal => ru ? "\u0433\u043e\u043b\u043e\u0434" : "food",
            WorkerNeedKind.Sleep => ru ? "\u0441\u043e\u043d" : "sleep",
            WorkerNeedKind.Leisure => ru ? "\u0434\u043e\u0441\u0443\u0433" : "leisure",
            _ => ru ? "\u043d\u0443\u0436\u0434\u0430" : "need"
        };
    }

    private static Color GetCityComplaintAccentColor(CityComplaint complaint)
    {
        if (complaint == null)
        {
            return new Color(0.45f, 0.52f, 0.62f, 1f);
        }

        if (complaint.State == CityComplaintState.Resolved)
        {
            return new Color(0.32f, 0.56f, 0.38f, 1f);
        }

        if (complaint.State == CityComplaintState.Expired)
        {
            return new Color(0.76f, 0.22f, 0.18f, 1f);
        }

        return complaint.Severity >= 4
            ? new Color(0.84f, 0.24f, 0.18f, 1f)
            : complaint.Severity >= 3
                ? new Color(0.90f, 0.58f, 0.20f, 1f)
                : new Color(0.46f, 0.58f, 0.78f, 1f);
    }

    private static string GetCityComplaintKey(int workerId, CityComplaintCategory category, WorkerNeedKind? need, LocationType? target)
    {
        return $"{workerId}:{category}:{(need.HasValue ? need.Value.ToString() : "none")}:{(target.HasValue ? target.Value.ToString() : "none")}";
    }
}
