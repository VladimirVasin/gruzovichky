using UnityEngine;

public partial class GameBootstrap
{
    private const int CityTrustMin = -100;
    private const int CityTrustMax = 100;
    private const int CityTrustComplaintExpiredPenalty = -25;
    private const int CityTrustCitizenRequestRejectedPenalty = -25;
    private const int CityTrustCitizenRequestCompletedReward = 25;

    private int cityTrust;

    private void ApplyCityTrustDelta(int delta, string reason)
    {
        if (delta == 0)
        {
            return;
        }

        int oldValue = cityTrust;
        cityTrust = Mathf.Clamp(cityTrust + delta, CityTrustMin, CityTrustMax);
        int appliedDelta = cityTrust - oldValue;
        if (appliedDelta == 0)
        {
            return;
        }

        isCityHallScreenDirty = true;
        SessionDebugLogger.Log(
            "CITY_TRUST",
            $"City trust changed: delta={appliedDelta:+#;-#;0}, value={cityTrust}/{CityTrustMax}, reason={reason ?? "unknown"}.");
    }

    private string FormatCityTrustSummary(bool ru)
    {
        string label = GetCityTrustLabel(ru);
        return ru
            ? $"Доверие: {label} ({FormatCityTrustScore()})"
            : $"Trust: {label} ({FormatCityTrustScore()})";
    }

    private string FormatCityTrustHudValue(bool ru)
    {
        return $"{GetCityTrustShortLabel(ru)} {FormatCityTrustScore()}";
    }

    private string FormatCityTrustScore()
    {
        return cityTrust > 0 ? $"+{cityTrust}" : cityTrust.ToString();
    }

    private string GetCityTrustShortLabel(bool ru)
    {
        if (cityTrust >= 60) return ru ? "высокое" : "high";
        if (cityTrust >= 20) return ru ? "устойч." : "stable";
        if (cityTrust > -20) return ru ? "нейтр." : "neutral";
        if (cityTrust > -60) return ru ? "низкое" : "low";
        return ru ? "кризис" : "crisis";
    }

    private string GetCityTrustLabel(bool ru)
    {
        if (cityTrust >= 60) return ru ? "высокое" : "high";
        if (cityTrust >= 20) return ru ? "устойчивое" : "stable";
        if (cityTrust > -20) return ru ? "нейтральное" : "neutral";
        if (cityTrust > -60) return ru ? "низкое" : "low";
        return ru ? "кризис доверия" : "trust crisis";
    }

    private Color GetCityTrustColor()
    {
        if (cityTrust >= 60) return new Color(0.45f, 0.85f, 0.38f, 1f);
        if (cityTrust >= 20) return new Color(0.66f, 0.86f, 0.52f, 1f);
        if (cityTrust > -20) return new Color(0.78f, 0.84f, 0.92f, 1f);
        if (cityTrust > -60) return new Color(0.96f, 0.66f, 0.22f, 1f);
        return new Color(0.93f, 0.32f, 0.28f, 1f);
    }
}
