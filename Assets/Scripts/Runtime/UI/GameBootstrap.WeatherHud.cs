using UnityEngine;

public partial class GameBootstrap
{
    private void DrawWeatherHud()
    {
        Rect panelRect = GetWeatherHudRect();

        bool ru = IsRussianLanguage();
        GUI.Box(panelRect, ru ? "Погода" : "Weather");

        GUIStyle iconStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
        };

        string icon = isWeatherTransitioning
            ? $"{GetWeatherStateIcon(currentWeatherState)}  {GetWeatherStateIcon(nextWeatherState)}"
            : GetWeatherStateIcon(currentWeatherState);

        GUI.Label(new Rect(panelRect.x, panelRect.y + 20f, panelRect.width, panelRect.height - 20f), icon, iconStyle);
    }

    private static string GetWeatherStateIcon(WeatherState state) => state switch
    {
        WeatherState.Clear    => "☀",
        WeatherState.Overcast => "☁",
        WeatherState.Rainy    => "☂",
        WeatherState.Foggy    => "≡",
        WeatherState.Windy    => "≈",
        _                     => "?",
    };

    private static string GetWeatherStateLabel(WeatherState state, bool ru) => state switch
    {
        WeatherState.Clear    => ru ? "Ясно"     : "Clear",
        WeatherState.Overcast => ru ? "Пасмурно" : "Overcast",
        WeatherState.Rainy    => ru ? "Дождь"    : "Rain",
        WeatherState.Foggy    => ru ? "Туман"    : "Fog",
        WeatherState.Windy    => ru ? "Ветер"    : "Windy",
        _                     => "?",
    };
}
