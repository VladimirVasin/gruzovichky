using UnityEngine;

public partial class GameBootstrap
{
    private const float TopBarY = 12f;
    private const float TopBarH = 50f;
    private const float TopBarGap = 4f;
    private const float WeatherHudW = 120f;
    private const float SpeedHudW = 90f;
    private const float TimeHudW = 140f;
    private const float PopulationHudW = 100f;
    private const float CityTrustHudW = 116f;
    private const float MoneyHudW = 150f;
    private const float RightColY = TopBarY + TopBarH + 8f;

    private int GetParkingHudHeight()
    {
        return 286 + 36 + truckAgents.Count * 38 + 66;
    }

    private Rect GetParkingHudRect()
    {
        return new Rect(Screen.width - 290, RightColY, 278, GetParkingHudHeight());
    }

    private Rect GetTruckDetailsHudRect()
    {
        return new Rect(Screen.width - 290, RightColY, 278, 420f);
    }

    private Rect GetAvailableTripsHudRect()
    {
        return new Rect(Screen.width - 290, RightColY + GetParkingHudHeight() + 8, 278, 170);
    }

    private Rect GetMoneyHudRect()
    {
        return new Rect(Screen.width - 12f - MoneyHudW, TopBarY, MoneyHudW, TopBarH);
    }

    private Rect GetCityTrustHudRect()
    {
        return new Rect(GetMoneyHudRect().x - TopBarGap - CityTrustHudW, TopBarY, CityTrustHudW, TopBarH);
    }

    private Rect GetPopulationHudRect()
    {
        return new Rect(GetCityTrustHudRect().x - TopBarGap - PopulationHudW, TopBarY, PopulationHudW, TopBarH);
    }

    private Rect GetTimeHudRect()
    {
        return new Rect(GetPopulationHudRect().x - TopBarGap - TimeHudW, TopBarY, TimeHudW, TopBarH);
    }

    private Rect GetSpeedHudRect()
    {
        return new Rect(GetTimeHudRect().x - TopBarGap - SpeedHudW, TopBarY, SpeedHudW, TopBarH);
    }

    private Rect GetWeatherHudRect()
    {
        return new Rect(GetSpeedHudRect().x - TopBarGap - WeatherHudW, TopBarY, WeatherHudW, TopBarH);
    }

    private Rect GetSelectedBuildingHudRect()
    {
        return new Rect(Screen.width - 290, RightColY, 278, 96);
    }
}
