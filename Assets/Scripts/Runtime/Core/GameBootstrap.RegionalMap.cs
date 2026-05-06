using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private static readonly bool LegacyIntercityTradeEnabled = false;
    private const int RegionalCitySlotCount = 9;
    private const int CurrentRegionalCityIndex = 4;

    private enum RegionalTradeRouteMode
    {
        None,
        River,
        Land
    }

    private sealed class RegionalCityData
    {
        public int RegionIndex;
        public string NameEn;
        public string NameRu;
        public string TypeEn;
        public string TypeRu;
        public string DescriptionEn;
        public string DescriptionRu;
        public Vector2 Position;
        public bool IsKnown;
        public bool IsCurrentCity;
        public bool TradeRouteBuilt;
        public RegionalTradeRouteMode RouteMode;
        public TradeResourceType[] Sells = System.Array.Empty<TradeResourceType>();
        public TradeResourceType[] Buys = System.Array.Empty<TradeResourceType>();
    }

    private sealed class RegionalMapData
    {
        public int Seed;
        public Vector2[] RiverPath = System.Array.Empty<Vector2>();
        public readonly List<Vector2> Lakes = new();
        public readonly List<Vector2> Forests = new();
        public readonly List<Vector2> Mountains = new();
        public readonly RegionalCityData[] Cities = new RegionalCityData[RegionalCitySlotCount];
    }

    private RegionalMapData regionalMapData;
    private Sprite regionalWorldMapSprite;

    private void GenerateRegionalMapState()
    {
        regionalMapData = new RegionalMapData { Seed = Random.Range(10000, 999999) };
        regionalWorldMapSprite = null;

        float topX = Random.Range(0.40f, 0.58f);
        float midX = Mathf.Clamp(topX + Random.Range(-0.08f, 0.08f), 0.34f, 0.66f);
        float bottomX = Mathf.Clamp(midX + Random.Range(-0.10f, 0.10f), 0.32f, 0.68f);
        regionalMapData.RiverPath = new[]
        {
            new Vector2(topX, 1.05f),
            new Vector2(Mathf.Lerp(topX, midX, 0.55f), 0.76f),
            new Vector2(midX, 0.50f),
            new Vector2(Mathf.Lerp(midX, bottomX, 0.55f), 0.25f),
            new Vector2(bottomX, -0.05f)
        };

        regionalMapData.Lakes.Add(new Vector2(Random.Range(0.16f, 0.30f), Random.Range(0.56f, 0.76f)));
        regionalMapData.Lakes.Add(new Vector2(Random.Range(0.70f, 0.86f), Random.Range(0.18f, 0.36f)));
        regionalMapData.Forests.Add(new Vector2(Random.Range(0.12f, 0.30f), Random.Range(0.62f, 0.84f)));
        regionalMapData.Forests.Add(new Vector2(Random.Range(0.20f, 0.40f), Random.Range(0.14f, 0.34f)));
        regionalMapData.Mountains.Add(new Vector2(Random.Range(0.68f, 0.86f), Random.Range(0.58f, 0.82f)));
        regionalMapData.Mountains.Add(new Vector2(Random.Range(0.62f, 0.82f), Random.Range(0.10f, 0.24f)));

        Vector2 currentPos = PointNearRiver(0.48f, Random.Range(-0.025f, 0.025f));
        RegionalCityData current = CreateRegionalCity(
            CurrentRegionalCityIndex,
            "Your Town",
            "Твой город",
            "Current region",
            "Текущий регион",
            "Your active town sits on a river bend. River routes use Docks, while land routes use the Warehouse.",
            "Текущий город стоит у речного изгиба. Речные маршруты используют Доки, сухопутные - Склад.",
            currentPos,
            RegionalTradeRouteMode.None,
            new[] { TradeResourceType.Logs, TradeResourceType.Boards, TradeResourceType.Furniture },
            new[] { TradeResourceType.Cotton, TradeResourceType.Textile });
        current.IsCurrentCity = true;
        regionalMapData.Cities[CurrentRegionalCityIndex] = current;

        Vector2 riverCityPos = PointNearRiver(Random.Range(0.64f, 0.78f), Random.Range(-0.035f, 0.035f));
        bool riverNameA = Random.value < 0.5f;
        regionalMapData.Cities[5] = CreateRegionalCity(
            5,
            riverNameA ? "Riverspun" : "Threadwater",
            riverNameA ? "Речной Ткач" : "Нитеводье",
            "River trade city",
            "Речной торговый город",
            "A textile town built beside the water. Ships from this route trade through your Docks.",
            "Текстильный город у воды. Корабли этого маршрута торгуют через твои Доки.",
            riverCityPos,
            RegionalTradeRouteMode.River,
            new[] { TradeResourceType.Textile },
            new[] { TradeResourceType.Furniture });

        Vector2 landCityPos = GetLandCityPosition(riverCityPos, currentPos);
        bool landNameA = Random.value < 0.5f;
        regionalMapData.Cities[6] = CreateRegionalCity(
            6,
            landNameA ? "Oakbarrel" : "Drybarrel",
            landNameA ? "Дубовая Бочка" : "Сухая Бочка",
            "Land trade city",
            "Сухопутный торговый город",
            "A dry inland distillery town. Merchant trucks from this route visit your Warehouse.",
            "Сухой внутренний город винокурен. Торговые грузовики этого маршрута приезжают на твой Склад.",
            landCityPos,
            RegionalTradeRouteMode.Land,
            new[] { TradeResourceType.Alcohol },
            new[] { TradeResourceType.Boards, TradeResourceType.Furniture });

        SessionDebugLogger.Log(
            "REGION_MAP",
            $"Generated regional map seed={regionalMapData.Seed}; current=({current.Position.x:0.00},{current.Position.y:0.00}); riverCity={regionalMapData.Cities[5].NameEn} mode=River; landCity={regionalMapData.Cities[6].NameEn} mode=Land.");
    }

    private RegionalCityData CreateRegionalCity(
        int index,
        string nameEn,
        string nameRu,
        string typeEn,
        string typeRu,
        string descriptionEn,
        string descriptionRu,
        Vector2 position,
        RegionalTradeRouteMode routeMode,
        TradeResourceType[] sells,
        TradeResourceType[] buys)
    {
        return new RegionalCityData
        {
            RegionIndex = index,
            NameEn = nameEn,
            NameRu = nameRu,
            TypeEn = typeEn,
            TypeRu = typeRu,
            DescriptionEn = descriptionEn,
            DescriptionRu = descriptionRu,
            Position = ClampRegionalPosition(position),
            IsKnown = true,
            RouteMode = routeMode,
            Sells = sells,
            Buys = buys
        };
    }

    private void EnsureRegionalMapState()
    {
        if (regionalMapData == null)
        {
            GenerateRegionalMapState();
        }
    }

    private RegionalCityData GetRegionalCity(int regionIndex)
    {
        EnsureRegionalMapState();
        return regionIndex >= 0 && regionIndex < regionalMapData.Cities.Length
            ? regionalMapData.Cities[regionIndex]
            : null;
    }

    private static Vector2 ClampRegionalPosition(Vector2 position)
    {
        return new Vector2(Mathf.Clamp(position.x, 0.10f, 0.90f), Mathf.Clamp(position.y, 0.12f, 0.88f));
    }

    private Vector2 PointNearRiver(float targetY, float xOffset)
    {
        EnsureRegionalMapState();
        Vector2 closest = regionalMapData.RiverPath[0];
        float bestDistance = float.MaxValue;
        for (int i = 0; i < regionalMapData.RiverPath.Length - 1; i++)
        {
            Vector2 a = regionalMapData.RiverPath[i];
            Vector2 b = regionalMapData.RiverPath[i + 1];
            float t = Mathf.InverseLerp(a.y, b.y, targetY);
            Vector2 p = Vector2.Lerp(a, b, Mathf.Clamp01(t));
            float distance = Mathf.Abs(p.y - targetY);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                closest = p;
            }
        }

        return ClampRegionalPosition(closest + new Vector2(xOffset, 0f));
    }

    private Vector2 GetLandCityPosition(Vector2 riverCityPos, Vector2 currentPos)
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector2 p = new(Random.Range(0.15f, 0.88f), Random.Range(0.16f, 0.84f));
            if (DistanceToRegionalRiver(p) > 0.12f &&
                Vector2.Distance(p, riverCityPos) > 0.24f &&
                Vector2.Distance(p, currentPos) > 0.20f)
            {
                return p;
            }
        }

        return new Vector2(0.78f, 0.24f);
    }

    private float DistanceToRegionalRiver(Vector2 point)
    {
        EnsureRegionalMapState();
        float best = float.MaxValue;
        for (int i = 0; i < regionalMapData.RiverPath.Length - 1; i++)
        {
            Vector2 a = regionalMapData.RiverPath[i];
            Vector2 b = regionalMapData.RiverPath[i + 1];
            float t = Mathf.Clamp01(Vector2.Dot(point - a, b - a) / Mathf.Max(0.0001f, (b - a).sqrMagnitude));
            best = Mathf.Min(best, Vector2.Distance(point, Vector2.Lerp(a, b, t)));
        }

        return best;
    }

    private string GetRegionalRouteModeLabel(RegionalTradeRouteMode mode)
    {
        bool ru = IsRussianLanguage();
        return mode switch
        {
            RegionalTradeRouteMode.River => ru ? "река / Доки" : "river / Docks",
            RegionalTradeRouteMode.Land => ru ? "суша / Склад" : "land / Warehouse",
            _ => ru ? "местный регион" : "local region"
        };
    }

    private bool TryFindBuiltRegionalTradeRoute(
        TradeResourceType resourceType,
        TradeOrderType orderType,
        RegionalTradeRouteMode? modeFilter,
        out RegionalCityData city)
    {
        EnsureRegionalMapState();
        for (int i = 0; i < regionalMapData.Cities.Length; i++)
        {
            RegionalCityData candidate = regionalMapData.Cities[i];
            if (candidate == null ||
                candidate.IsCurrentCity ||
                !candidate.IsKnown ||
                !candidate.TradeRouteBuilt ||
                modeFilter.HasValue && candidate.RouteMode != modeFilter.Value)
            {
                continue;
            }

            TradeResourceType[] catalog = orderType == TradeOrderType.Buy ? candidate.Sells : candidate.Buys;
            for (int j = 0; j < catalog.Length; j++)
            {
                if (catalog[j] == resourceType)
                {
                    city = candidate;
                    return true;
                }
            }
        }

        city = null;
        return false;
    }

    private bool HasBuiltRegionalTradeRoute(
        TradeResourceType resourceType,
        TradeOrderType orderType,
        RegionalTradeRouteMode? modeFilter = null)
    {
        return TryFindBuiltRegionalTradeRoute(resourceType, orderType, modeFilter, out _);
    }

    private string DescribeRegionalTradeRouteAvailability(
        TradeResourceType resourceType,
        TradeOrderType orderType,
        RegionalTradeRouteMode? modeFilter)
    {
        EnsureRegionalMapState();
        string resourceLabel = GetTradeResourceShortLabel(resourceType);
        string modeLabel = modeFilter.HasValue ? GetRegionalRouteModeDebugLabel(modeFilter.Value) : "any";
        string cityVerb = orderType == TradeOrderType.Buy ? "sells" : "buys";
        string partnerLabel = orderType == TradeOrderType.Buy ? "seller" : "buyer";
        RegionalCityData compatibleUnbuilt = null;
        RegionalCityData compatibleWrongModeBuilt = null;
        RegionalCityData compatibleWrongModeUnbuilt = null;
        RegionalCityData builtModeWithoutResource = null;
        RegionalCityData knownModeWithoutResource = null;

        for (int i = 0; i < regionalMapData.Cities.Length; i++)
        {
            RegionalCityData candidate = regionalMapData.Cities[i];
            if (candidate == null || candidate.IsCurrentCity || !candidate.IsKnown)
            {
                continue;
            }

            bool modeMatches = !modeFilter.HasValue || candidate.RouteMode == modeFilter.Value;
            bool resourceMatches = RegionalCityHandlesTradeResource(candidate, resourceType, orderType);
            if (modeMatches && resourceMatches)
            {
                if (candidate.TradeRouteBuilt)
                {
                    return $"built {modeLabel} route to {candidate.NameEn} exists and that city {cityVerb} {resourceLabel}";
                }

                compatibleUnbuilt ??= candidate;
                continue;
            }

            if (!modeMatches && resourceMatches)
            {
                if (candidate.TradeRouteBuilt)
                {
                    compatibleWrongModeBuilt ??= candidate;
                }
                else
                {
                    compatibleWrongModeUnbuilt ??= candidate;
                }

                continue;
            }

            if (modeMatches && !resourceMatches)
            {
                if (candidate.TradeRouteBuilt)
                {
                    builtModeWithoutResource ??= candidate;
                }
                else
                {
                    knownModeWithoutResource ??= candidate;
                }
            }
        }

        if (compatibleUnbuilt != null)
        {
            return $"{modeLabel} route to {compatibleUnbuilt.NameEn} is not built; that city {cityVerb} {resourceLabel}";
        }

        if (compatibleWrongModeBuilt != null)
        {
            return $"built route to {compatibleWrongModeBuilt.NameEn} is {GetRegionalRouteModeDebugLabel(compatibleWrongModeBuilt.RouteMode)}, not {modeLabel}; that city {cityVerb} {resourceLabel}";
        }

        if (compatibleWrongModeUnbuilt != null)
        {
            return $"known route to {compatibleWrongModeUnbuilt.NameEn} is {GetRegionalRouteModeDebugLabel(compatibleWrongModeUnbuilt.RouteMode)}, not {modeLabel}; that city {cityVerb} {resourceLabel}";
        }

        if (builtModeWithoutResource != null)
        {
            return $"built {modeLabel} route to {builtModeWithoutResource.NameEn} exists, but that city does not {cityVerb} {resourceLabel}";
        }

        if (knownModeWithoutResource != null)
        {
            return $"known {modeLabel} city {knownModeWithoutResource.NameEn} does not {cityVerb} {resourceLabel}";
        }

        return $"no known {modeLabel} regional {partnerLabel} for {resourceLabel}";
    }

    private static bool RegionalCityHandlesTradeResource(
        RegionalCityData city,
        TradeResourceType resourceType,
        TradeOrderType orderType)
    {
        if (city == null)
        {
            return false;
        }

        TradeResourceType[] catalog = orderType == TradeOrderType.Buy ? city.Sells : city.Buys;
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] == resourceType)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetRegionalRouteModeDebugLabel(RegionalTradeRouteMode mode)
    {
        return mode switch
        {
            RegionalTradeRouteMode.River => "river",
            RegionalTradeRouteMode.Land => "land",
            _ => "local"
        };
    }
}
