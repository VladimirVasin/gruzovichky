public partial class GameBootstrap
{
    private bool AreAmbientBeesActive()
    {
        int hour = GetCurrentHour();
        return hour >= 6 && hour < 18;
    }

    private bool AreAmbientLanternMothsActive()
    {
        int hour = GetCurrentHour();
        return hour >= 20 || hour < 6;
    }
}
