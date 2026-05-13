using UnityEngine;
using UnityEngine.InputSystem;

public partial class GameBootstrap
{
    private const float NoosphereVisionResidentHoverRadius = 24f;
    private bool noosphereVisionHasMouseLocal;
    private Vector2 noosphereVisionMouseLocal;
    private DriverAgent noosphereVisionHoveredResident;
    private float noosphereVisionHoveredResidentDistanceSq;

    private string FormatNoosphereVisionSignalTitle(SocialSignalTopicAggregate aggregate, bool ru)
    {
        string label = ru ? aggregate.LabelRu : aggregate.LabelEn;
        string category = GetSocialSignalCategoryLabel(aggregate.Category, ru);
        if (!string.IsNullOrWhiteSpace(label) &&
            !IsNoosphereVisionWorkerName(label) &&
            !string.Equals(label, category, System.StringComparison.OrdinalIgnoreCase))
        {
            return $"{category}: {label}";
        }

        return aggregate.Category switch
        {
            SocialSignalCategory.Work => ru ? "\u0420\u0430\u0431\u043e\u0442\u0430 \u0436\u0438\u0442\u0435\u043b\u0435\u0439" : "Resident work",
            SocialSignalCategory.Money => ru ? "\u0414\u0435\u043d\u044c\u0433\u0438 \u0436\u0438\u0442\u0435\u043b\u0435\u0439" : "Resident money",
            SocialSignalCategory.Need => ru ? "\u041f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u0438 \u0436\u0438\u0442\u0435\u043b\u0435\u0439" : "Resident needs",
            SocialSignalCategory.Family => ru ? "\u0421\u0435\u043c\u044c\u0438 \u0433\u043e\u0440\u043e\u0434\u0430" : "City families",
            SocialSignalCategory.Social => ru ? "\u0421\u0432\u044f\u0437\u0438 \u0436\u0438\u0442\u0435\u043b\u0435\u0439" : "Resident ties",
            SocialSignalCategory.Transport => ru ? "\u0422\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442\u043d\u044b\u0439 \u043e\u043f\u044b\u0442" : "Transport experience",
            _ => ru ? "\u041e\u0431\u0449\u0430\u044f \u0442\u0435\u043c\u0430 \u0433\u043e\u0440\u043e\u0434\u0430" : "Shared city topic"
        };
    }

    private bool IsNoosphereVisionWorkerName(string label)
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            if (string.Equals(driverAgents[i]?.DriverName, label, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private int ApplyNoosphereVisionResidentDots(int dotIndex, ref int lineIndex, float progress)
    {
        bool dimForFocus = noosphereVisionSelectedInsightIndex >= 0;
        BeginNoosphereVisionResidentHoverScan();

        for (int i = 0; i < driverAgents.Count && dotIndex < noosphereVisionUi.SourceDots.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (!TryGetNoosphereVisionResidentPosition(worker, out Vector3 position))
            {
                continue;
            }

            NoosphereVisionSourceDotUi dot = noosphereVisionUi.SourceDots[dotIndex++];
            dot.Root.gameObject.SetActive(true);
            Vector2 local = WorldToNoosphereVisionLocal(position + Vector3.up * 0.58f);
            dot.Root.anchoredPosition = local;
            bool relatedToFocus = IsNoosphereVisionResidentRelatedToSelectedInsight(worker, local);
            Color color = GetNoosphereVisionResidentColor(worker);
            if (dimForFocus && relatedToFocus)
            {
                color = Color.Lerp(color, new Color(1f, 0.92f, 0.48f, 1f), 0.28f);
            }

            color.a = (dimForFocus ? relatedToFocus ? 0.92f : 0.12f : 0.68f) * progress;
            dot.Image.color = color;
            float pulse = 1f + Mathf.Sin(noosphereVisionAnimationTime * 2.7f + i * 0.53f) * 0.10f;
            float focusScale = dimForFocus ? relatedToFocus ? 1.22f : 0.66f : 1f;
            dot.Root.localScale = Vector3.one * (0.72f + GetNoosphereVisionResidentImportance(worker) * 0.006f) * pulse * focusScale;
            dot.InsightIndex = -1;
            ConsiderNoosphereVisionResidentHover(worker, local);

            bool drawResidentLine = dimForFocus && relatedToFocus;
            if (drawResidentLine && lineIndex < noosphereVisionUi.Lines.Count)
            {
                float alpha = 0.30f;
                ApplyNoosphereVisionLine(
                    noosphereVisionUi.Lines[lineIndex++],
                    local,
                    Vector2.zero,
                    new Color(color.r, color.g, color.b, alpha * progress),
                    1.25f);
            }
        }

        UpdateNoosphereVisionResidentTooltip(progress);
        return dotIndex;
    }

    private void BeginNoosphereVisionResidentHoverScan()
    {
        noosphereVisionHasMouseLocal = false;
        noosphereVisionHoveredResident = null;
        noosphereVisionHoveredResidentDistanceSq = float.MaxValue;

        if (noosphereVisionUi?.FieldRoot == null || Mouse.current == null)
        {
            return;
        }

        Vector2 screen = Mouse.current.position.ReadValue();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(noosphereVisionUi.FieldRoot, screen, null, out Vector2 local))
        {
            noosphereVisionMouseLocal = local;
            noosphereVisionHasMouseLocal = true;
        }
    }

    private void ConsiderNoosphereVisionResidentHover(DriverAgent worker, Vector2 local)
    {
        if (!noosphereVisionHasMouseLocal || worker == null)
        {
            return;
        }

        float distanceSq = (local - noosphereVisionMouseLocal).sqrMagnitude;
        float radius = NoosphereVisionResidentHoverRadius * Mathf.Max(1f, noosphereVisionUi?.CanvasGroup?.alpha ?? 1f);
        if (distanceSq > radius * radius || distanceSq >= noosphereVisionHoveredResidentDistanceSq)
        {
            return;
        }

        noosphereVisionHoveredResident = worker;
        noosphereVisionHoveredResidentDistanceSq = distanceSq;
    }

    private int CountNoosphereVisionResidents()
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            if (TryGetNoosphereVisionResidentPosition(driverAgents[i], out _))
            {
                count++;
            }
        }

        return count;
    }

    private bool TryGetNoosphereVisionResidentPosition(DriverAgent worker, out Vector3 position)
    {
        position = Vector3.zero;
        if (worker == null || worker.HasDepartedTown || worker.IsLeavingTown)
        {
            return false;
        }

        if (worker.DriverObject != null)
        {
            position = worker.DriverObject.transform.position;
            return true;
        }

        if (worker.IsInsideBuilding && worker.InsideBuildingInstanceId > 0)
        {
            LocationData location = FindLocationByInstanceId(worker.InsideBuildingInstanceId);
            if (location != null)
            {
                position = GetLocationCenter(location);
                return true;
            }
        }

        if (worker.AssignedPersonalHouseIndex >= 0 && worker.AssignedPersonalHouseIndex < personalHouses.Count)
        {
            position = GetLocationCenter(personalHouses[worker.AssignedPersonalHouseIndex]);
            return true;
        }

        return false;
    }

    private Color GetNoosphereVisionResidentColor(DriverAgent worker)
    {
        WorkerThought thought = GetMostImportantWorkerThought(worker);
        if (thought != null)
        {
            return thought.Tone switch
            {
                WorkerThoughtTone.Positive => new Color(0.36f, 0.95f, 0.58f, 1f),
                WorkerThoughtTone.Negative => new Color(1f, 0.42f, 0.25f, 1f),
                _ => new Color(0.62f, 0.82f, 1f, 1f)
            };
        }

        return worker.Satisfaction >= 75
            ? new Color(0.45f, 0.88f, 0.62f, 1f)
            : worker.Satisfaction < 45
                ? new Color(1f, 0.55f, 0.35f, 1f)
                : new Color(0.72f, 0.86f, 1f);
    }

    private int GetNoosphereVisionResidentImportance(DriverAgent worker)
    {
        WorkerThought thought = GetMostImportantWorkerThought(worker);
        if (thought != null)
        {
            return Mathf.Clamp(thought.Intensity, 30, 100);
        }

        return Mathf.Clamp(Mathf.Abs(worker.Satisfaction - 60), 10, 60);
    }

    private bool IsNoosphereVisionResidentRelatedToSelectedInsight(DriverAgent worker, Vector2 residentLocal)
    {
        if (worker == null ||
            noosphereVisionSelectedInsightIndex < 0 ||
            noosphereVisionSelectedInsightIndex >= noosphereVisionInsights.Count)
        {
            return true;
        }

        NoosphereVisionInsight insight = noosphereVisionInsights[noosphereVisionSelectedInsightIndex];
        if (insight == null)
        {
            return false;
        }

        if (insight.Key.StartsWith("city_experience_", System.StringComparison.Ordinal) ||
            string.Equals(insight.Key, "city_canon", System.StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(insight.Key, "family_readiness", System.StringComparison.Ordinal))
        {
            return worker.FamilyId > 0 && IsNoosphereVisionWorkerFamilyInReadinessFocus(worker.FamilyId);
        }

        if (insight.Key.StartsWith("education_", System.StringComparison.Ordinal))
        {
            string typeName = insight.Key.Substring("education_".Length);
            return System.Enum.TryParse(typeName, out LocationType educationType) &&
                   IsNoosphereVisionWorkerFamilyInEducationFocus(worker.FamilyId, educationType);
        }

        if (insight.Key.StartsWith("signal_", System.StringComparison.Ordinal))
        {
            string topicKey = insight.Key.Substring("signal_".Length);
            return IsNoosphereVisionWorkerInSignalFocus(worker, topicKey);
        }

        return IsNoosphereVisionResidentNearInsightSource(residentLocal, insight);
    }

    private bool IsNoosphereVisionWorkerFamilyInReadinessFocus(int familyId)
    {
        if (familyId <= 0)
        {
            return false;
        }

        WorkerFamily family = GetWorkerFamilyById(familyId);
        if (family == null || !IsWorkerFamilyLivingInHouse(family))
        {
            return false;
        }

        int childCount = CountWorkerFamilyChildren(family.Id);
        if (childCount >= MaxWorkerFamilyChildren)
        {
            return false;
        }

        int readiness = CalculateWorkerFamilyNextChildReadiness(family, out _);
        return readiness < WorkerFamilyNextChildReadinessThreshold;
    }

    private bool IsNoosphereVisionWorkerFamilyInEducationFocus(int familyId, LocationType educationType)
    {
        if (familyId <= 0)
        {
            return false;
        }

        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child != null &&
                child.FamilyId == familyId &&
                IsWorkerChildNeedingEducation(child, educationType))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsNoosphereVisionWorkerInSignalFocus(DriverAgent worker, string topicKey)
    {
        if (worker == null || string.IsNullOrWhiteSpace(topicKey))
        {
            return false;
        }

        int latestDay = GetLatestSocialSignalDay();
        if (latestDay <= 0)
        {
            return false;
        }

        for (int i = 0; i < socialSignals.Count; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null || signal.Day < latestDay)
            {
                break;
            }

            if (signal.Day == latestDay &&
                signal.PublicForNoosphere &&
                signal.WorkerId == worker.DriverId &&
                string.Equals(signal.TopicKey, topicKey, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsNoosphereVisionResidentNearInsightSource(Vector2 residentLocal, NoosphereVisionInsight insight)
    {
        if (insight?.SourceWorldPositions == null)
        {
            return false;
        }

        const float relatedRadiusSq = 34f * 34f;
        for (int i = 0; i < insight.SourceWorldPositions.Count; i++)
        {
            Vector2 sourceLocal = WorldToNoosphereVisionLocal(insight.SourceWorldPositions[i]);
            if ((sourceLocal - residentLocal).sqrMagnitude <= relatedRadiusSq)
            {
                return true;
            }
        }

        return false;
    }
}
