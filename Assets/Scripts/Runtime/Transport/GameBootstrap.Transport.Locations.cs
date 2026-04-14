using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void CreateLocation(LocationType type, string label, Vector2Int min, Vector2Int max, Vector2Int anchor, Color baseColor)
    {
        LocationData data = new()
        {
            Label = label,
            Min = min,
            Max = max,
            Anchor = anchor
            ,
            BaseColor = baseColor
        };

        GameObject root = new(label);
        root.transform.SetParent(worldRoot, false);
        data.RootObject = root;

        Vector2Int size = new(max.x - min.x + 1, max.y - min.y + 1);
        Vector3 center = new Vector3((min.x + max.x + 1) * 0.5f, 0.35f, (min.y + max.y + 1) * 0.5f);

        GameObject baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseBlock.transform.SetParent(root.transform, false);
        if (type == LocationType.Forest)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.17f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.98f, 0.12f, size.y * 0.98f);
            ApplyStylizedGrassMaterial(baseBlock, min.x + max.x + 0.5f, min.y + max.y + 0.5f);
        }
        else if (type == LocationType.Motel)
        {
            // Base block covers only the back half of the footprint (under the building).
            // Anchor direction determines which half is "back" (away from anchor).
            Vector3 toAnchorDir;
            if (anchor.y < min.y) toAnchorDir = new Vector3(0f, 0f, -1f);
            else if (anchor.y > max.y) toAnchorDir = new Vector3(0f, 0f, 1f);
            else if (anchor.x < min.x) toAnchorDir = new Vector3(-1f, 0f, 0f);
            else toAnchorDir = new Vector3(1f, 0f, 0f);

            Vector3 backOffset = -toAnchorDir * 0.5f;
            baseBlock.transform.position = center + backOffset;
            float scaleX = toAnchorDir.z != 0f ? size.x * 0.95f : size.x * 0.47f;
            float scaleZ = toAnchorDir.x != 0f ? size.y * 0.95f : size.y * 0.47f;
            baseBlock.transform.localScale = new Vector3(scaleX, 0.7f, scaleZ);
            ApplyColor(baseBlock, baseColor);
        }
        else if (type == LocationType.BusStop)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.22f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.92f, 0.14f, size.y * 0.64f);
            ApplyColor(baseBlock, new Color(0.78f, 0.74f, 0.68f));
        }
        else
        {
            baseBlock.transform.position = center;
            baseBlock.transform.localScale = new Vector3(size.x * 0.95f, 0.7f, size.y * 0.95f);
            ApplyColor(baseBlock, baseColor);
        }

        ConfigureShadowVisual(baseBlock);
        data.BaseRenderer = baseBlock.GetComponent<Renderer>();

        if (type == LocationType.Parking)
        {
            CreateParkingDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.GasStation)
        {
            CreateGasStationDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Forest)
        {
            CreateForestDecoration(root.transform, min, max, anchor);
        }
        else if (type == LocationType.Warehouse)
        {
            CreateWarehouseDecoration(root.transform, center);
        }
        else if (type == LocationType.Motel)
        {
            CreateMotelDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Sawmill)
        {
            CreateSawmillDecoration(root.transform, center);
        }
        else if (type == LocationType.FurnitureFactory)
        {
            CreateFurnitureFactoryDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.BusStop)
        {
            CreateBusStopDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Bar)
        {
            CreateBarDecoration(root.transform, center, min, max, anchor);
        }
        else
        {
            CreateMotelDecoration(root.transform, center, min, max, anchor);
        }

        CreateLocationNightLights(type, root.transform, center, size);

        GameObject anchorMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anchorMarker.transform.SetParent(root.transform, false);
        anchorMarker.transform.position = GetCellCenter(anchor) + new Vector3(0f, 0.05f, 0f);
        anchorMarker.transform.localScale = new Vector3(0.22f, 0.02f, 0.22f);
        ApplyColor(anchorMarker, new Color(1f, 0.9f, 0.35f));

        locations[type] = data;
        root.transform.position = new Vector3(0f, GetLocationBaseHeight(type), 0f);
        if (worldRoot != null && !locationSelectionHighlights.ContainsKey(type))
        {
            locationSelectionHighlights[type] = SelectionVisualService.CreateHighlight(
                worldRoot,
                data.Label,
                ApplyColor,
                ConfigureStaticVisual);
        }
    }

    private void CreateParkingDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Vector3 ScaleOffset(Vector3 offset) => offset * BuildingDecorScale;
        Vector3 ScaleSize(Vector3 size) => size * BuildingDecorScale;

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canopy.transform.SetParent(parent, false);
        canopy.transform.position = center + ScaleOffset(new Vector3(0f, 0.6f, -0.15f));
        canopy.transform.localScale = ScaleSize(new Vector3(2.8f, 0.12f, 1.4f));
        ApplyColor(canopy, new Color(0.18f, 0.2f, 0.24f));

        Vector3[] postOffsets =
        {
            new(-1.15f, 0.28f, -0.55f),
            new(1.15f, 0.28f, -0.55f),
            new(-1.15f, 0.28f, 0.25f),
            new(1.15f, 0.28f, 0.25f)
        };

        foreach (Vector3 offset in postOffsets)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.position = center + ScaleOffset(offset);
            post.transform.localScale = ScaleSize(new Vector3(0.12f, 0.56f, 0.12f));
            ApplyColor(post, new Color(0.3f, 0.32f, 0.36f));
        }

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.62f);
    }

    private void CreateBusStopDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        GameObject shelterRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shelterRoof.transform.SetParent(parent, false);
        shelterRoof.transform.position = center + new Vector3(0f, 0.72f, 0.05f);
        shelterRoof.transform.localScale = new Vector3(1.55f, 0.08f, 0.52f);
        ApplyColor(shelterRoof, new Color(0.86f, 0.22f, 0.18f));
        ConfigureStaticVisual(shelterRoof);

        Vector3[] postOffsets =
        {
            new(-0.58f, 0.33f, -0.1f),
            new(0.58f, 0.33f, -0.1f)
        };

        foreach (Vector3 offset in postOffsets)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.position = center + offset;
            post.transform.localScale = new Vector3(0.08f, 0.62f, 0.08f);
            ApplyColor(post, new Color(0.28f, 0.3f, 0.34f));
            ConfigureStaticVisual(post);
        }

        GameObject backPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backPanel.transform.SetParent(parent, false);
        backPanel.transform.position = center + new Vector3(0f, 0.38f, 0.18f);
        backPanel.transform.localScale = new Vector3(1.4f, 0.5f, 0.06f);
        ApplyColor(backPanel, new Color(0.9f, 0.92f, 0.95f));
        ConfigureStaticVisual(backPanel);

        GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bench.transform.SetParent(parent, false);
        bench.transform.position = center + new Vector3(0f, 0.16f, -0.05f);
        bench.transform.localScale = new Vector3(0.88f, 0.08f, 0.2f);
        ApplyColor(bench, new Color(0.5f, 0.34f, 0.2f));
        ConfigureStaticVisual(bench);

        GameObject stopPole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stopPole.transform.SetParent(parent, false);
        stopPole.transform.position = center + new Vector3(0.92f, 0.5f, 0.16f);
        stopPole.transform.localScale = new Vector3(0.06f, 1f, 0.06f);
        ApplyColor(stopPole, new Color(0.26f, 0.28f, 0.32f));
        ConfigureStaticVisual(stopPole);

        GameObject stopSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stopSign.transform.SetParent(parent, false);
        stopSign.transform.position = center + new Vector3(0.92f, 0.92f, 0.16f);
        stopSign.transform.localScale = new Vector3(0.34f, 0.28f, 0.04f);
        ApplyColor(stopSign, new Color(0.95f, 0.84f, 0.2f));
        ConfigureStaticVisual(stopSign);
    }

    private void CreateForestDecoration(Transform parent, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        forestWorkPoints.Clear();
        forestWorkTargetTrees.Clear();
        forestTreeWobbles.Clear();

        GameObject groundPatch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        groundPatch.transform.SetParent(parent, false);
        groundPatch.transform.position = new Vector3(
            (min.x + max.x + 1) * 0.5f,
            0.02f,
            (min.y + max.y + 1) * 0.5f);
        groundPatch.transform.localScale = new Vector3(
            Mathf.Max(1.35f, (max.x - min.x + 1) * 0.58f) * LargePropScale,
            0.07f,
            Mathf.Max(1.35f, (max.y - min.y + 1) * 0.58f) * LargePropScale);
        ApplyStylizedGrassMaterial(groundPatch, min.x + max.x, min.y + max.y);
        ConfigureStaticVisual(groundPatch);

        List<Vector2Int> forestCells = new();
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                forestCells.Add(new Vector2Int(x, y));
            }
        }

        forestCells.Sort((a, b) =>
        {
            int aDistance = Mathf.Abs(a.x - anchor.x) + Mathf.Abs(a.y - anchor.y);
            int bDistance = Mathf.Abs(b.x - anchor.x) + Mathf.Abs(b.y - anchor.y);
            return bDistance.CompareTo(aDistance);
        });

        int plantedTrees = 0;
        foreach (Vector2Int cell in forestCells)
        {
            int distanceToAnchor = Mathf.Abs(cell.x - anchor.x) + Mathf.Abs(cell.y - anchor.y);
            bool keepApproachClear = distanceToAnchor <= 2;
            int treesInCell = keepApproachClear ? 1 : 2;

            for (int i = 0; i < treesInCell; i++)
            {
                if (keepApproachClear && i > 0)
                {
                    continue;
                }

                float offsetX = keepApproachClear
                    ? Mathf.Sign(cell.x + 0.5f - (anchor.x + 0.5f)) * 0.14f
                    : (i == 0 ? -0.18f : 0.18f);
                float offsetZ = keepApproachClear
                    ? Mathf.Sign(cell.y + 0.5f - (anchor.y + 0.5f)) * 0.14f
                    : (i == 0 ? 0.16f : -0.16f);

                if (Mathf.Approximately(offsetX, 0f))
                {
                    offsetX = i == 0 ? -0.14f : 0.14f;
                }

                if (Mathf.Approximately(offsetZ, 0f))
                {
                    offsetZ = i == 0 ? 0.14f : -0.14f;
                }

                GameObject treeRoot = new($"ForestTree_{cell.x}_{cell.y}_{i}");
                treeRoot.transform.SetParent(parent, false);
                treeRoot.transform.position = GetCellCenter(cell) + new Vector3(offsetX, 0f, offsetZ);
                treeRoot.transform.rotation = Quaternion.Euler(0f, (cell.x * 37 + cell.y * 29 + i * 71) % 360, 0f);
                treeRoot.transform.localScale = Vector3.one * ((0.96f + ((cell.x + cell.y + i) % 3) * 0.12f) * LargePropScale);
                CreateTreeVariant(treeRoot.transform, (cell.x + cell.y + i) % 3);
                if (!keepApproachClear)
                {
                    forestWorkPoints.Add(treeRoot.transform.position + new Vector3(
                        Mathf.Sign(anchor.x + 0.5f - treeRoot.transform.position.x) * 0.22f,
                        0f,
                        Mathf.Sign(anchor.y + 0.5f - treeRoot.transform.position.z) * 0.22f));
                    forestWorkTargetTrees.Add(treeRoot.transform);
                }

                plantedTrees++;
            }
        }

        GameObject lumberMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lumberMarker.transform.SetParent(parent, false);
        lumberMarker.transform.position = GetCellCenter(GetForestDepotCell(min, max, anchor)) + new Vector3(0f, 0.12f, 0f);
        lumberMarker.transform.localScale = new Vector3(0.44f, 0.14f, 0.72f) * LargePropScale;
        ApplyColor(lumberMarker, new Color(0.47f, 0.31f, 0.18f));
        ConfigureStaticVisual(lumberMarker);

        GameObject logTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        logTop.transform.SetParent(parent, false);
        logTop.transform.position = lumberMarker.transform.position + new Vector3(-0.12f, 0.14f, 0f);
        logTop.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        logTop.transform.localScale = new Vector3(0.12f, 0.18f, 0.12f) * LargePropScale;
        ApplyColor(logTop, new Color(0.6f, 0.42f, 0.24f));
        ConfigureStaticVisual(logTop);

        GameObject logBottom = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        logBottom.transform.SetParent(parent, false);
        logBottom.transform.position = lumberMarker.transform.position + new Vector3(0.1f, 0.08f, 0f);
        logBottom.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        logBottom.transform.localScale = new Vector3(0.12f, 0.2f, 0.12f) * LargePropScale;
        ApplyColor(logBottom, new Color(0.56f, 0.38f, 0.22f));
        ConfigureStaticVisual(logBottom);

        CreateForestStoredLogsVisuals(parent, lumberMarker.transform.position + new Vector3(0f, 0.08f, 0.54f));
        RefreshForestStoredLogsVisual();

        SessionDebugLogger.Log("WORLD", $"Built enlarged forest cluster with {plantedTrees} trees.");
    }

    private void CreateForestStoredLogsVisuals(Transform parent, Vector3 basePosition)
    {
        if (!locations.TryGetValue(LocationType.Forest, out LocationData forestLocation))
        {
            return;
        }

        forestLocation.StoredLogVisuals.Clear();
        for (int i = 0; i < ForestMaxLogsStorage; i++)
        {
            int row = i / 5;
            int column = i % 5;
            GameObject storedLog = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            storedLog.name = $"StoredLog_{i + 1}";
            storedLog.transform.SetParent(parent, false);
            storedLog.transform.position = basePosition + new Vector3(-0.36f + column * 0.18f, row * 0.12f, 0f);
            storedLog.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            storedLog.transform.localScale = new Vector3(0.11f, 0.2f, 0.11f);
            ApplyColor(storedLog, new Color(0.58f, 0.4f, 0.22f));
            ConfigureStaticVisual(storedLog);
            forestLocation.StoredLogVisuals.Add(storedLog);
        }
    }

    private void RefreshForestStoredLogsVisual()
    {
        if (!locations.TryGetValue(LocationType.Forest, out LocationData forestLocation))
        {
            return;
        }

        int visibleLogs = Mathf.Clamp(forestLocation.LogsStored, 0, ForestMaxLogsStorage);
        for (int i = 0; i < forestLocation.StoredLogVisuals.Count; i++)
        {
            if (forestLocation.StoredLogVisuals[i] != null)
            {
                forestLocation.StoredLogVisuals[i].SetActive(i < visibleLogs);
            }
        }
    }

    private void TryAddForestLogFromChop()
    {
        if (!locations.TryGetValue(LocationType.Forest, out LocationData forestLocation) ||
            forestLocation.LogsStored >= ForestMaxLogsStorage)
        {
            forestProductionProgress = 0f;
            return;
        }

        forestProductionProgress += ForestLogProgressPerChop;
        if (forestProductionProgress < 1f)
        {
            return;
        }

        forestProductionProgress -= 1f;
        forestLocation.LogsStored = Mathf.Min(ForestMaxLogsStorage, forestLocation.LogsStored + 1);
        RefreshForestStoredLogsVisual();
        SessionDebugLogger.Log("FOREST", $"Forest produced logs. Storage is now {forestLocation.LogsStored}/{ForestMaxLogsStorage}.");
    }

    private Vector2Int GetForestDepotCell(Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        if (anchor.y < min.y)
        {
            return new Vector2Int(Mathf.Clamp(anchor.x, min.x, max.x), min.y);
        }

        if (anchor.y > max.y)
        {
            return new Vector2Int(Mathf.Clamp(anchor.x, min.x, max.x), max.y);
        }

        if (anchor.x < min.x)
        {
            return new Vector2Int(min.x, Mathf.Clamp(anchor.y, min.y, max.y));
        }

        if (anchor.x > max.x)
        {
            return new Vector2Int(max.x, Mathf.Clamp(anchor.y, min.y, max.y));
        }

        return new Vector2Int((min.x + max.x) / 2, (min.y + max.y) / 2);
    }

    private void CreateGasStationDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Vector3 ScaleOffset(Vector3 offset) => offset * BuildingDecorScale;
        Vector3 ScaleSize(Vector3 size) => size * BuildingDecorScale;

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + ScaleOffset(new Vector3(0f, 0.72f, -0.18f));
        roof.transform.localScale = ScaleSize(new Vector3(2.15f, 0.12f, 1.08f));
        ApplyColor(roof, new Color(0.95f, 0.3f, 0.22f));

        Vector3[] postOffsets =
        {
            new(-0.8f, 0.32f, -0.44f),
            new(0.8f, 0.32f, -0.44f),
            new(-0.8f, 0.32f, 0.08f),
            new(0.8f, 0.32f, 0.08f)
        };

        foreach (Vector3 offset in postOffsets)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.position = center + ScaleOffset(offset);
            post.transform.localScale = ScaleSize(new Vector3(0.12f, 0.64f, 0.12f));
            ApplyColor(post, new Color(0.96f, 0.94f, 0.88f));
        }

        GameObject kiosk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kiosk.transform.SetParent(parent, false);
        kiosk.transform.position = center + ScaleOffset(new Vector3(0f, 0.36f, 0.38f));
        kiosk.transform.localScale = ScaleSize(new Vector3(1.25f, 0.52f, 0.5f));
        ApplyColor(kiosk, new Color(0.98f, 0.92f, 0.78f));

        GameObject pump = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pump.transform.SetParent(parent, false);
        pump.transform.position = center + ScaleOffset(new Vector3(0f, 0.32f, -0.12f));
        pump.transform.localScale = ScaleSize(new Vector3(0.24f, 0.42f, 0.24f));
        ApplyColor(pump, new Color(0.2f, 0.22f, 0.26f));

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.58f);
    }

    private void CreateDrivewayToAnchor(Transform parent, Vector2Int min, Vector2Int max, Vector2Int anchor, float width)
    {
        Vector3 end = GetCellCenter(anchor) + new Vector3(0f, 0.11f, 0f);
        Vector3 start = GetDrivewayStartPoint(min, max, anchor) + new Vector3(0f, 0.11f, 0f);
        CreateDriveway(parent, start, end, width);
    }

    private Vector3 GetDrivewayStartPoint(Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        float centerX = (min.x + max.x + 1) * 0.5f;
        float centerZ = (min.y + max.y + 1) * 0.5f;

        if (anchor.y < min.y)
        {
            return new Vector3(Mathf.Clamp(anchor.x + 0.5f, min.x + 0.25f, max.x + 0.75f), GetLocationPadHeight(min, max, anchor), min.y - 0.02f);
        }

        if (anchor.y > max.y)
        {
            return new Vector3(Mathf.Clamp(anchor.x + 0.5f, min.x + 0.25f, max.x + 0.75f), GetLocationPadHeight(min, max, anchor), max.y + 1.02f);
        }

        if (anchor.x < min.x)
        {
            return new Vector3(min.x - 0.02f, GetLocationPadHeight(min, max, anchor), Mathf.Clamp(anchor.y + 0.5f, min.y + 0.25f, max.y + 0.75f));
        }

        if (anchor.x > max.x)
        {
            return new Vector3(max.x + 1.02f, GetLocationPadHeight(min, max, anchor), Mathf.Clamp(anchor.y + 0.5f, min.y + 0.25f, max.y + 0.75f));
        }

        return new Vector3(centerX, GetLocationPadHeight(min, max, anchor), centerZ);
    }

    private float GetLocationPadHeight(Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        float total = terrainHeights[anchor.x, anchor.y];
        int count = 1;
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                total += terrainHeights[x, y];
                count++;
            }
        }

        return total / Mathf.Max(1, count);
    }

    private void CreateDriveway(Transform parent, Vector3 worldStart, Vector3 worldEnd, float width)
    {
        GameObject driveway = GameObject.CreatePrimitive(PrimitiveType.Cube);
        driveway.name = "Driveway";
        driveway.transform.SetParent(parent, false);

        Vector3 delta = worldEnd - worldStart;
        float length = delta.magnitude;
        driveway.transform.position = worldStart + delta * 0.5f;
        driveway.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        driveway.transform.localScale = new Vector3(width, 0.1f, length);

        ApplyColor(driveway, new Color(0.2f, 0.21f, 0.23f));
        ConfigureStaticVisual(driveway);

        GameObject drivewayTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        drivewayTop.name = "DrivewayTop";
        drivewayTop.transform.SetParent(driveway.transform, false);
        drivewayTop.transform.localPosition = new Vector3(0f, 0.58f, 0f);
        drivewayTop.transform.localScale = new Vector3(0.72f, 0.18f, 0.88f);
        ApplyColor(drivewayTop, new Color(0.76f, 0.71f, 0.58f));
        ConfigureStaticVisual(drivewayTop);
    }

    private void CreateWarehouseDecoration(Transform parent, Vector3 center)
    {
        Vector3 ScaleOffset(Vector3 offset) => offset * BuildingDecorScale;
        Vector3 ScaleSize(Vector3 size) => size * BuildingDecorScale;

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + ScaleOffset(new Vector3(0f, 0.47f, 0f));
        roof.transform.localScale = ScaleSize(new Vector3(2.05f, 0.12f, 2.05f));
        ApplyColor(roof, new Color(0.88f, 0.24f, 0.2f));
    }

    private void CreateSawmillDecoration(Transform parent, Vector3 center)
    {
        Vector3 ScaleOffset(Vector3 offset) => offset * BuildingDecorScale;
        Vector3 ScaleSize(Vector3 size) => size * BuildingDecorScale;

        for (int i = 0; i < 2; i++)
        {
            GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.transform.SetParent(parent, false);
            house.transform.position = center + ScaleOffset(new Vector3(-0.3f + i * 0.6f, 0.4f, 0f));
            house.transform.localScale = ScaleSize(new Vector3(0.45f, 0.5f, 0.45f));
            ApplyColor(house, new Color(0.92f, 0.84f, 0.66f));
        }
    }

    private void CreateFurnitureFactoryDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Vector3 ScaleOffset(Vector3 offset) => offset * BuildingDecorScale;
        Vector3 ScaleSize(Vector3 size) => size * BuildingDecorScale;

        GameObject mainHall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mainHall.transform.SetParent(parent, false);
        mainHall.transform.position = center + ScaleOffset(new Vector3(-0.18f, 0.42f, 0.02f));
        mainHall.transform.localScale = ScaleSize(new Vector3(2.1f, 0.52f, 1.2f));
        ApplyColor(mainHall, new Color(0.86f, 0.8f, 0.68f));
        ConfigureStaticVisual(mainHall);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + ScaleOffset(new Vector3(-0.18f, 0.73f, 0.02f));
        roof.transform.localScale = ScaleSize(new Vector3(2.2f, 0.09f, 1.28f));
        ApplyColor(roof, new Color(0.72f, 0.24f, 0.18f));
        ConfigureStaticVisual(roof);

        GameObject sideWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sideWing.transform.SetParent(parent, false);
        sideWing.transform.position = center + ScaleOffset(new Vector3(0.94f, 0.28f, -0.04f));
        sideWing.transform.localScale = ScaleSize(new Vector3(0.68f, 0.32f, 0.78f));
        ApplyColor(sideWing, new Color(0.64f, 0.56f, 0.4f));
        ConfigureStaticVisual(sideWing);

        GameObject loadingAwning = GameObject.CreatePrimitive(PrimitiveType.Cube);
        loadingAwning.transform.SetParent(parent, false);
        loadingAwning.transform.position = center + ScaleOffset(new Vector3(0.92f, 0.54f, -0.44f));
        loadingAwning.transform.localScale = ScaleSize(new Vector3(0.86f, 0.06f, 0.42f));
        ApplyColor(loadingAwning, new Color(0.24f, 0.28f, 0.33f));
        ConfigureStaticVisual(loadingAwning);

        float[] postX = { 0.64f, 1.2f };
        foreach (float px in postX)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.position = center + ScaleOffset(new Vector3(px, 0.24f, -0.44f));
            post.transform.localScale = ScaleSize(new Vector3(0.08f, 0.42f, 0.08f));
            ApplyColor(post, new Color(0.28f, 0.3f, 0.34f));
            ConfigureStaticVisual(post);
        }

        GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chimney.transform.SetParent(parent, false);
        chimney.transform.position = center + ScaleOffset(new Vector3(-0.78f, 0.92f, 0.26f));
        chimney.transform.localScale = ScaleSize(new Vector3(0.18f, 0.78f, 0.18f));
        ApplyColor(chimney, new Color(0.42f, 0.3f, 0.22f));
        ConfigureStaticVisual(chimney);

        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.transform.SetParent(parent, false);
        sign.transform.position = center + ScaleOffset(new Vector3(-0.12f, 0.58f, -0.62f));
        sign.transform.localScale = ScaleSize(new Vector3(1.02f, 0.18f, 0.06f));
        ApplyColor(sign, new Color(0.94f, 0.82f, 0.22f));
        ConfigureStaticVisual(sign);

        for (int i = 0; i < 3; i++)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.transform.SetParent(parent, false);
            crate.transform.position = center + ScaleOffset(new Vector3(0.62f + i * 0.22f, 0.08f + (i % 2) * 0.02f, 0.38f));
            crate.transform.localScale = ScaleSize(new Vector3(0.18f, 0.16f, 0.18f));
            ApplyColor(crate, new Color(0.68f, 0.48f, 0.22f));
            ConfigureStaticVisual(crate);
        }

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.96f);
    }

    private void CreateMotelDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        // Oriented root: local +Z faces anchor, local -Z faces away (back of building).
        // Snap to nearest cardinal axis to avoid diagonal rotations.
        Vector3 anchorWorld = new Vector3(anchor.x + 0.5f, center.y, anchor.y + 0.5f);
        Vector3 toAnchorRaw = anchorWorld - center;
        toAnchorRaw.y = 0f;
        Vector3 toAnchor;
        if (Mathf.Abs(toAnchorRaw.x) >= Mathf.Abs(toAnchorRaw.z))
            toAnchor = new Vector3(Mathf.Sign(toAnchorRaw.x), 0f, 0f);
        else
            toAnchor = new Vector3(0f, 0f, Mathf.Sign(toAnchorRaw.z));

        GameObject orientedRoot = new GameObject("MotelOriented");
        orientedRoot.transform.SetParent(parent, false);
        orientedRoot.transform.position = center;
        orientedRoot.transform.rotation = Quaternion.LookRotation(toAnchor, Vector3.up);
        orientedRoot.transform.localScale = Vector3.one * BuildingDecorScale;
        Transform or = orientedRoot.transform;

        // === BUILDING — back half of footprint (local Z < 0 = away from anchor) ===

        // Main body
        GameObject mainBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mainBlock.transform.SetParent(or, false);
        mainBlock.transform.localPosition = new Vector3(0f, 0.36f, -0.4f);
        mainBlock.transform.localScale = new Vector3(1.85f, 0.52f, 0.72f);
        ApplyColor(mainBlock, new Color(0.91f, 0.87f, 0.74f));

        // Red flat roof
        GameObject roofBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roofBlock.transform.SetParent(or, false);
        roofBlock.transform.localPosition = new Vector3(0f, 0.66f, -0.4f);
        roofBlock.transform.localScale = new Vector3(1.92f, 0.09f, 0.82f);
        ApplyColor(roofBlock, new Color(0.76f, 0.22f, 0.18f));

        // Facade canopy — on the front face of the building body (toward anchor side)
        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canopy.transform.SetParent(or, false);
        canopy.transform.localPosition = new Vector3(0f, 0.58f, -0.06f);
        canopy.transform.localScale = new Vector3(1.85f, 0.07f, 0.32f);
        ApplyColor(canopy, new Color(0.78f, 0.24f, 0.2f));

        // Three support posts under the canopy
        float[] postX = { -0.68f, 0f, 0.68f };
        foreach (float px in postX)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(or, false);
            post.transform.localPosition = new Vector3(px, 0.38f, 0.04f);
            post.transform.localScale = new Vector3(0.07f, 0.4f, 0.07f);
            ApplyColor(post, new Color(0.82f, 0.22f, 0.18f));
        }

        // MOTEL sign above the roofline
        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.transform.SetParent(or, false);
        sign.transform.localPosition = new Vector3(0f, 0.82f, -0.18f);
        sign.transform.localScale = new Vector3(0.72f, 0.18f, 0.06f);
        ApplyColor(sign, new Color(0.98f, 0.84f, 0.12f));

        // === PARKING AREA — front half of footprint (local Z > 0 = toward anchor) ===

        // Two flat parking panels on the ground in front of the building.
        // localY = 0.37 puts them just above the top of the location base block (top = 0.35 local).
        float[] slotX = { -0.27f, 0.27f };
        foreach (float sx in slotX)
        {
            GameObject slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slot.transform.SetParent(or, false);
            slot.transform.localPosition = new Vector3(sx, -0.32f, 0.5f);
            slot.transform.localScale = new Vector3(0.46f, 0.015f, 0.72f);
            ApplyColor(slot, new Color(0.56f, 0.56f, 0.58f));
            ConfigureStaticVisual(slot);
        }

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.88f);
    }

    private void CreateLocationNightLights(LocationType type, Transform parent, Vector3 center, Vector2Int size)
    {
        if (type == LocationType.Forest)
        {
            CreateLocationNightLight(parent, center + new Vector3(0f, 1.15f, -0.95f));
            return;
        }

        float xOffset = Mathf.Max(0.45f, size.x * 0.28f);
        float zOffset = Mathf.Max(0.38f, size.y * 0.28f);
        CreateLocationNightLight(parent, center + new Vector3(-xOffset, 0.92f, -zOffset));
        CreateLocationNightLight(parent, center + new Vector3(xOffset, 0.92f, -zOffset));

        if (type == LocationType.Sawmill || type == LocationType.FurnitureFactory)
        {
            CreateLocationNightLight(parent, center + new Vector3(0f, 0.86f, zOffset));
        }
    }

    private void CreateLocationNightLight(Transform parent, Vector3 localPosition)
    {
        GameObject lampVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampVisual.transform.SetParent(parent, false);
        lampVisual.transform.localPosition = localPosition;
        lampVisual.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
        ApplyColor(lampVisual, new Color(0.28f, 0.24f, 0.18f));
        ConfigureStaticVisual(lampVisual);
        Renderer lampRenderer = lampVisual.GetComponent<Renderer>();
        locationNightLightRenderers.Add(lampRenderer);
        locationNightLightMaterials.Add(lampRenderer != null ? lampRenderer.material : null);

        GameObject lightObject = new("NightLamp");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition + new Vector3(0f, 0.06f, 0f);

        Light lamp = lightObject.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, 0.9f, 0.72f);
        lamp.range = 3.2f;
        lamp.intensity = 0f;
        lamp.shadows = LightShadows.None;
        lamp.enabled = false;
        locationNightLights.Add(lamp);
    }

    private void CreateGridLine(Transform parent, Material lineMaterial, Vector3 start, Vector3 end)
    {
        GameObject lineObject = new($"GridLine_{start.x}_{start.z}_{end.x}_{end.z}");
        lineObject.transform.SetParent(parent, false);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.widthMultiplier = 0.03f;
        lineRenderer.material = lineMaterial;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }
}
