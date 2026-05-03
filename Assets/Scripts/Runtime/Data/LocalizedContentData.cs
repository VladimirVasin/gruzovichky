using System;

[Serializable]
public sealed class LocalizedContentData
{
    public string en;
    public string ru;

    public string Get(bool useRussian)
    {
        string localized = useRussian ? ru : en;
        if (!string.IsNullOrWhiteSpace(localized))
        {
            return localized;
        }

        return en ?? string.Empty;
    }
}
