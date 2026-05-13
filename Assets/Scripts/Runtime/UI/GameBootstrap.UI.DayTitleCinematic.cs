using UnityEngine;

public partial class GameBootstrap
{
    private const float DayTitleCinematicHoldSeconds = 3f;
    private const float DayTitleCinematicFadeSeconds = 1f;
    private const float GameStartSceneFadeSeconds = 4.8f;
    private const float GameStartTitleHoldAfterSceneSeconds = 0.85f;
    private const float GameStartTitleFadeSeconds = 1.6f;
    private const float GameStartAudioFadeSeconds = 5.4f;
    private const float GameStartDayTitleCinematicTotalSeconds =
        GameStartSceneFadeSeconds + GameStartTitleHoldAfterSceneSeconds + GameStartTitleFadeSeconds;
    private int dayTitleCinematicDay;
    private float dayTitleCinematicStartedAt = -1000f;
    private bool isGameStartDayTitleCinematic;
    private bool isGameStartAudioFadeActive;
    private float gameStartAudioFadeTargetVolume = 1f;

    private void ShowDayTitleCinematic(int day)
    {
        if (day <= 0) return;
        dayTitleCinematicDay = day;
        dayTitleCinematicStartedAt = Time.unscaledTime;
        isGameStartDayTitleCinematic = false;
    }

    private void ShowGameStartDayTitleCinematic(int day)
    {
        if (day <= 0) return;
        dayTitleCinematicDay = day;
        dayTitleCinematicStartedAt = Time.unscaledTime;
        isGameStartDayTitleCinematic = true;
        isGameStartAudioFadeActive = true;
        gameStartAudioFadeTargetVolume = Mathf.Clamp01(AudioListener.volume > 0.001f ? AudioListener.volume : 1f);
        AudioListener.volume = 0f;
    }

    private void UpdateGameStartAudioFade()
    {
        if (!isGameStartAudioFadeActive) return;
        float elapsed = Time.unscaledTime - dayTitleCinematicStartedAt;
        float t = Mathf.Clamp01(elapsed / GameStartAudioFadeSeconds);
        AudioListener.volume = Mathf.Lerp(0f, gameStartAudioFadeTargetVolume, SmootherStep01(t));
        if (t < 1f) return;
        AudioListener.volume = gameStartAudioFadeTargetVolume;
        isGameStartAudioFadeActive = false;
    }

    private void DrawDayTitleCinematic()
    {
        if (dayTitleCinematicDay <= 0) return;
        float elapsed = Time.unscaledTime - dayTitleCinematicStartedAt;
        float totalDuration = isGameStartDayTitleCinematic
            ? GameStartDayTitleCinematicTotalSeconds
            : DayTitleCinematicHoldSeconds + DayTitleCinematicFadeSeconds;
        if (elapsed < 0f) return;
        if (elapsed >= totalDuration)
        {
            dayTitleCinematicDay = 0;
            return;
        }

        Color previousColor = GUI.color;
        Color previousContentColor = GUI.contentColor;
        if (isGameStartDayTitleCinematic)
        {
            float blackAlpha = 1f - SmootherStep01(elapsed / GameStartSceneFadeSeconds);
            if (blackAlpha > 0.001f)
            {
                GUI.color = new Color(0f, 0f, 0f, blackAlpha);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            }
        }

        float alpha = GetDayTitleTextAlpha(elapsed);
        if (alpha > 0f) DrawDayTitleText(alpha);
        GUI.contentColor = previousContentColor;
        GUI.color = previousColor;
    }

    private float GetDayTitleTextAlpha(float elapsed)
    {
        if (!isGameStartDayTitleCinematic)
        {
            return elapsed <= DayTitleCinematicHoldSeconds
                ? 1f
                : 1f - Mathf.Clamp01((elapsed - DayTitleCinematicHoldSeconds) / DayTitleCinematicFadeSeconds);
        }

        float fadeStart = GameStartSceneFadeSeconds + GameStartTitleHoldAfterSceneSeconds;
        return elapsed <= fadeStart
            ? 1f
            : 1f - SmootherStep01((elapsed - fadeStart) / GameStartTitleFadeSeconds);
    }

    private void DrawDayTitleText(float alpha)
    {
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.contentColor = GUI.color;
        int fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.095f), 54, 112);
        GUIStyle titleStyle = new(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = GUI.contentColor;
        titleStyle.hover.textColor = GUI.contentColor;
        titleStyle.active.textColor = GUI.contentColor;
        titleStyle.focused.textColor = GUI.contentColor;

        string title = IsRussianLanguage()
            ? $"\u0414\u0435\u043d\u044c {dayTitleCinematicDay}"
            : $"Day {dayTitleCinematicDay}";
        GUI.Label(new Rect(0f, Screen.height * 0.36f, Screen.width, Mathf.Max(118f, fontSize * 1.5f)), title, titleStyle);
    }

}
