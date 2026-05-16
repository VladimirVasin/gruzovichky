using UnityEngine;

public partial class GameBootstrap
{
    private const string GasStationImportedModelResourcePath = "Buildings/gasstation";
    private const string FurnitureFactoryImportedModelResourcePath = "Buildings/furniturefactory";
    private const string KioskImportedModelResourcePath = "Buildings/kiosk";
    private const string KindergartenImportedModelResourcePath = "Buildings/kindergarden";
    private const string PrimarySchoolImportedModelResourcePath = "Buildings/primaryschool";
    private const string SecondarySchoolImportedModelResourcePath = "Buildings/secondaryschool";
    private const string CarMarketImportedModelResourcePath = "Buildings/carmarket";
    private const string LaborExchangeImportedModelResourcePath = "Buildings/laborexchange";
    private const string CleaningDepotImportedModelResourcePath = "Buildings/cleaningdepot";
    private const string DocksImportedModelResourcePath = "Buildings/docks";
    private const string PersonalHouseRanchImportedModelResourcePath = "Buildings/personalhouse_ranch";
    private const string PersonalHouseCapeCodImportedModelResourcePath = "Buildings/personalhouse_capecod";
    private const string PersonalHouseColonialImportedModelResourcePath = "Buildings/personalhouse_colonial";
    private const string PersonalHouseCraftsmanImportedModelResourcePath = "Buildings/personalhouse_craftsman";
    private const string PersonalHouseSplitLevelImportedModelResourcePath = "Buildings/personalhouse_splitlevel";

    private const float GasStationImportedModelFootprintFill = 1.10f;
    private const float FurnitureFactoryImportedModelFootprintFill = 1.08f;
    private const float KioskImportedModelFootprintFill = 1.12f;
    private const float KindergartenImportedModelFootprintFill = 1.04f;
    private const float PrimarySchoolImportedModelFootprintFill = 1.02f;
    private const float SecondarySchoolImportedModelFootprintFill = 1.02f;
    private const float CarMarketImportedModelFootprintFill = 0.98f;
    private const float LaborExchangeImportedModelFootprintFill = 1.05f;
    private const float CleaningDepotImportedModelFootprintFill = 1.08f;
    private const float DocksImportedModelFootprintFill = 1.08f;
    private const float PersonalHouseImportedModelFootprintFill = 0.98f;
    private const float ImportedTownBuildingModelGroundY = -0.35f;

    private static readonly string[] PersonalHouseImportedModelResourcePaths =
    {
        PersonalHouseRanchImportedModelResourcePath,
        PersonalHouseCapeCodImportedModelResourcePath,
        PersonalHouseColonialImportedModelResourcePath,
        PersonalHouseCraftsmanImportedModelResourcePath,
        PersonalHouseSplitLevelImportedModelResourcePath
    };

    private static readonly string[] PersonalHouseImportedModelSuffixes =
    {
        "Ranch",
        "CapeCod",
        "Colonial",
        "Craftsman",
        "SplitLevel"
    };

    private bool TryCreateImportedGasStationModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner, parent, center, min, max, anchor, LocationType.GasStation,
            GasStationImportedModelResourcePath, "GasStationImportedModelRoot", "GasStationImportedModel",
            GasStationImportedModelFootprintFill, new Color(1f, 0.84f, 0.48f, 1f), new Color(1f, 0.66f, 0.28f));
    }

    private bool TryCreateImportedFurnitureFactoryModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner, parent, center, min, max, anchor, LocationType.FurnitureFactory,
            FurnitureFactoryImportedModelResourcePath, "FurnitureFactoryImportedModelRoot", "FurnitureFactoryImportedModel",
            FurnitureFactoryImportedModelFootprintFill, new Color(1f, 0.82f, 0.46f, 1f), new Color(1f, 0.68f, 0.30f));
    }

    private bool TryCreateImportedKioskModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner, parent, center, min, max, anchor, LocationType.Kiosk,
            KioskImportedModelResourcePath, "KioskImportedModelRoot", "KioskImportedModel",
            KioskImportedModelFootprintFill, new Color(1f, 0.86f, 0.50f, 1f), new Color(1f, 0.64f, 0.24f));
    }

    private bool TryCreateImportedKindergartenModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner, parent, center, min, max, anchor, LocationType.Kindergarten,
            KindergartenImportedModelResourcePath, "KindergartenImportedModelRoot", "KindergartenImportedModel",
            KindergartenImportedModelFootprintFill, new Color(1f, 0.88f, 0.58f, 1f), new Color(1f, 0.72f, 0.30f));
    }

    private bool TryCreateImportedSchoolModel(LocationData owner, LocationType schoolType, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        bool secondary = schoolType == LocationType.SecondarySchool;
        return TryCreateImportedServiceModel(
            owner, parent, center, min, max, anchor, schoolType,
            secondary ? SecondarySchoolImportedModelResourcePath : PrimarySchoolImportedModelResourcePath,
            secondary ? "SecondarySchoolImportedModelRoot" : "PrimarySchoolImportedModelRoot",
            secondary ? "SecondarySchoolImportedModel" : "PrimarySchoolImportedModel",
            secondary ? SecondarySchoolImportedModelFootprintFill : PrimarySchoolImportedModelFootprintFill,
            new Color(1f, 0.84f, 0.52f, 1f),
            secondary ? new Color(1f, 0.72f, 0.32f) : new Color(1f, 0.76f, 0.36f));
    }

    private bool TryCreateImportedCarMarketModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner, parent, center, min, max, anchor, LocationType.CarMarket,
            CarMarketImportedModelResourcePath, "CarMarketImportedModelRoot", "CarMarketImportedModel",
            CarMarketImportedModelFootprintFill, new Color(1f, 0.82f, 0.46f, 1f), new Color(1f, 0.70f, 0.28f));
    }

    private bool TryCreateImportedLaborExchangeModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner, parent, center, min, max, anchor, LocationType.LaborExchange,
            LaborExchangeImportedModelResourcePath, "LaborExchangeImportedModelRoot", "LaborExchangeImportedModel",
            LaborExchangeImportedModelFootprintFill, new Color(0.86f, 0.94f, 1f, 1f), new Color(1f, 0.76f, 0.34f));
    }

    private bool TryCreateImportedCleaningDepotModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner, parent, center, min, max, anchor, LocationType.CleaningDepot,
            CleaningDepotImportedModelResourcePath, "CleaningDepotImportedModelRoot", "CleaningDepotImportedModel",
            CleaningDepotImportedModelFootprintFill, new Color(0.82f, 1f, 0.86f, 1f), new Color(0.78f, 1f, 0.62f));
    }

    private bool TryCreateImportedDocksModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner, parent, center, min, max, anchor, LocationType.Docks,
            DocksImportedModelResourcePath, "DocksImportedModelRoot", "DocksImportedModel",
            DocksImportedModelFootprintFill, new Color(1f, 0.76f, 0.42f, 1f), new Color(1f, 0.68f, 0.28f));
    }

    private bool TryCreateImportedPersonalHouseModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        int variant = GetImportedPersonalHouseVariantIndex(owner, min, max, anchor);
        string suffix = PersonalHouseImportedModelSuffixes[variant];
        return TryCreateImportedServiceModel(
            owner, parent, center, min, max, anchor, LocationType.PersonalHouse,
            PersonalHouseImportedModelResourcePaths[variant],
            $"PersonalHouse{suffix}ImportedModelRoot",
            $"PersonalHouse{suffix}ImportedModel",
            PersonalHouseImportedModelFootprintFill,
            new Color(1f, 0.82f, 0.48f, 1f),
            new Color(1f, 0.70f, 0.30f));
    }

    private static int GetImportedPersonalHouseVariantIndex(LocationData owner, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        int seed = owner?.InstanceId ?? 0;
        seed = seed * 397 ^ min.x * 101 ^ min.y * 131 ^ max.x * 151 ^ max.y * 181 ^ anchor.x * 211 ^ anchor.y * 241;
        return (seed & int.MaxValue) % PersonalHouseImportedModelResourcePaths.Length;
    }
}
