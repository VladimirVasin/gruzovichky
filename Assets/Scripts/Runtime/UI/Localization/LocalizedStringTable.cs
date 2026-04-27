using System.Collections.Generic;

public sealed class LocalizedStringTable
{
    private readonly IReadOnlyDictionary<string, string> values;

    public LocalizedStringTable(IReadOnlyDictionary<string, string> values)
    {
        this.values = values;
    }

    public bool TryTranslate(string key, out string value)
    {
        value = null;
        return !string.IsNullOrEmpty(key) && values != null && values.TryGetValue(key, out value);
    }

    public string TranslateCommonFragments(string source)
    {
        string translated = source;
        if (string.IsNullOrEmpty(translated) || values == null)
        {
            return translated;
        }

        foreach (KeyValuePair<string, string> pair in values)
        {
            if (pair.Key.Length >= 4)
            {
                translated = translated.Replace(pair.Key, pair.Value);
            }
        }

        return translated;
    }

    public string ToSourceKeyIfKnown(string localizedValue)
    {
        if (string.IsNullOrEmpty(localizedValue) || values == null)
        {
            return localizedValue;
        }

        foreach (KeyValuePair<string, string> pair in values)
        {
            if (pair.Value == localizedValue)
            {
                return pair.Key;
            }
        }

        return localizedValue;
    }
}
