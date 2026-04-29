using UnityEngine;

public partial class GameBootstrap
{
    private void DrawWeatherHud()
    {
        Rect panelRect = GetWeatherHudRect();
        bool ru = IsRussianLanguage();

        string title;
        string valueText;
        if (isWeatherTransitioning)
        {
            string curr = GetWeatherStateLabel(currentWeatherState, ru);
            string next = GetWeatherStateLabel(nextWeatherState, ru);
            int secs    = Mathf.CeilToInt(Mathf.Max(0f, weatherTransitionDuration - weatherTransitionTimer));
            title     = $"{curr} > {next}";
            valueText = FormatWeatherCountdown(secs);
        }
        else
        {
            title     = ru ? "Погода" : "Weather";
            int secs  = Mathf.CeilToInt(Mathf.Max(0f, weatherHoldTimer));
            valueText = $"{GetWeatherStateLabel(currentWeatherState, ru)}  {FormatWeatherCountdown(secs)}";
        }

        GUI.Box(panelRect, title);

        GUIStyle valueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
        };
        GUI.Label(new Rect(panelRect.x, panelRect.y + 22f, panelRect.width, 26f), valueText, valueStyle);
    }

    private static string GetWeatherStateLabel(WeatherState state, bool ru)
    {
        return state switch
        {
            WeatherState.Clear    => ru ? "Ясно"     : "Clear",
            WeatherState.Overcast => ru ? "Пасмурно" : "Overcast",
            WeatherState.Rainy    => ru ? "Дождь"    : "Rain",
            WeatherState.Foggy    => ru ? "Туман"    : "Fog",
            WeatherState.Windy    => ru ? "Ветер"    : "Windy",
            _                     => "?",
        };
    }

    private static string FormatWeatherCountdown(int totalSeconds)
    {
        int m = totalSeconds / 60;
        int s = totalSeconds % 60;
        return $"{m}:{s:D2}";
    }
}
