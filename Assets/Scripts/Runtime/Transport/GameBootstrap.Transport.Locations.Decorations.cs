using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void CreateGasStationDecoration(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        if (TryCreateImportedGasStationModel(owner, parent, center, min, max, anchor))
        {
            return;
        }

        Vector3 ScaleOffset(Vector3 offset) => offset * BuildingDecorScale;
        Vector3 ScaleSize(Vector3 size) => size * BuildingDecorScale;

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + ScaleOffset(new Vector3(0f, 0.72f, -0.18f));
        roof.transform.localScale = ScaleSize(new Vector3(2.15f, 0.12f, 1.08f));
        ApplyColor(roof, new Color(0.95f, 0.3f, 0.22f), VisualSmoothnessRoofMetal);

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
            ApplyColor(post, new Color(0.96f, 0.94f, 0.88f), VisualSmoothnessVehicleMetal);
        }

        GameObject kiosk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kiosk.transform.SetParent(parent, false);
        kiosk.transform.position = center + ScaleOffset(new Vector3(0f, 0.36f, 0.38f));
        kiosk.transform.localScale = ScaleSize(new Vector3(1.25f, 0.52f, 0.5f));
        ApplyColor(kiosk, new Color(0.98f, 0.92f, 0.78f), VisualSmoothnessBuildingWall);

        GameObject pump = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pump.transform.SetParent(parent, false);
        pump.transform.position = center + ScaleOffset(new Vector3(0f, 0.32f, -0.12f));
        pump.transform.localScale = ScaleSize(new Vector3(0.24f, 0.42f, 0.24f));
        ApplyColor(pump, new Color(0.2f, 0.22f, 0.26f), VisualSmoothnessVehicleMetal);

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.58f);
        EnhanceGasStationModel(parent, center, min, max, anchor);
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

        ApplyColor(driveway, new Color(0.2f, 0.21f, 0.23f), VisualSmoothnessAsphalt);
        ConfigureStaticVisual(driveway, VisualSmoothnessAsphalt);

        GameObject drivewayTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        drivewayTop.name = "DrivewayTop";
        drivewayTop.transform.SetParent(driveway.transform, false);
        drivewayTop.transform.localPosition = new Vector3(0f, 0.58f, 0f);
        drivewayTop.transform.localScale = new Vector3(0.72f, 0.18f, 0.88f);
        ApplyColor(drivewayTop, new Color(0.76f, 0.71f, 0.58f), VisualSmoothnessAsphalt);
        ConfigureStaticVisual(drivewayTop, VisualSmoothnessAsphalt);
    }

    private void CreateWarehouseDecoration(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        if (TryCreateImportedWarehouseModel(owner, parent, center, min, max, anchor))
        {
            return;
        }

        Vector3 ScaleOffset(Vector3 offset) => offset * BuildingDecorScale;
        Vector3 ScaleSize(Vector3 size) => size * BuildingDecorScale;

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + ScaleOffset(new Vector3(0f, 0.47f, 0f));
        roof.transform.localScale = ScaleSize(new Vector3(2.05f, 0.12f, 2.05f));
        ApplyColor(roof, new Color(0.88f, 0.24f, 0.2f), VisualSmoothnessRoofMetal);

        EnhanceWarehouseModel(parent, center, min, max, anchor);
    }

    private void CreateSawmillDecoration(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        if (TryCreateImportedSawmillModel(owner, parent, center, min, max, anchor))
        {
            return;
        }

        Vector3 ScaleOffset(Vector3 offset) => offset * BuildingDecorScale;
        Vector3 ScaleSize(Vector3 size) => size * BuildingDecorScale;

        for (int i = 0; i < 2; i++)
        {
            GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.transform.SetParent(parent, false);
            house.transform.position = center + ScaleOffset(new Vector3(-0.3f + i * 0.6f, 0.4f, 0f));
            house.transform.localScale = ScaleSize(new Vector3(0.45f, 0.5f, 0.45f));
            ApplyColor(house, new Color(0.92f, 0.84f, 0.66f), VisualSmoothnessBuildingWall);
        }

        EnhanceSawmillModel(parent, center, min, max, anchor);
    }

    private void CreateCarMarketDecoration(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        if (TryCreateImportedCarMarketModel(owner, parent, center, min, max, anchor))
        {
            return;
        }

        GameObject asphalt = GameObject.CreatePrimitive(PrimitiveType.Cube);
        asphalt.transform.SetParent(parent, false);
        asphalt.transform.position = center + new Vector3(0f, -0.19f, 0f);
        asphalt.transform.localScale = new Vector3(4.8f, 0.04f, 4.8f);
        ApplyColor(asphalt, new Color(0.12f, 0.13f, 0.14f), VisualSmoothnessAsphalt);
        ConfigureStaticVisual(asphalt, VisualSmoothnessAsphalt);

        Vector3 officePos = center + new Vector3(-1.35f, 0.35f, 1.35f);
        GameObject office = GameObject.CreatePrimitive(PrimitiveType.Cube);
        office.transform.SetParent(parent, false);
        office.transform.position = officePos;
        office.transform.localScale = new Vector3(1.4f, 0.9f, 1.2f);
        ApplyColor(office, new Color(0.74f, 0.68f, 0.56f), VisualSmoothnessBuildingWall);
        ConfigureShadowVisual(office, VisualSmoothnessBuildingWall);

        GameObject awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
        awning.transform.SetParent(parent, false);
        awning.transform.position = officePos + new Vector3(0f, 0.5f, -0.66f);
        awning.transform.localScale = new Vector3(1.55f, 0.12f, 0.34f);
        ApplyColor(awning, new Color(0.84f, 0.42f, 0.18f), VisualSmoothnessRoofMetal);
        ConfigureShadowVisual(awning, VisualSmoothnessRoofMetal);

        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.transform.SetParent(parent, false);
        sign.transform.position = officePos + new Vector3(0f, 0.64f, -0.82f);
        sign.transform.localScale = new Vector3(1.05f, 0.22f, 0.04f);
        ApplyColor(sign, new Color(0.95f, 0.78f, 0.28f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(sign, VisualSmoothnessVehicleMetal);

        Vector3[] standOffsets =
        {
            new(0.75f, -0.08f, 1.2f),
            new(1.35f, -0.08f, 0f),
            new(0.75f, -0.08f, -1.2f)
        };

        for (int i = 0; i < standOffsets.Length; i++)
        {
            Vector3 standCenter = center + standOffsets[i];
            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stand.transform.SetParent(parent, false);
            stand.transform.position = standCenter;
            stand.transform.localScale = new Vector3(1.1f, 0.06f, 0.7f);
            ApplyColor(stand, new Color(0.22f, 0.23f, 0.24f), VisualSmoothnessAsphalt);
            ConfigureStaticVisual(stand, VisualSmoothnessAsphalt);

            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.SetParent(parent, false);
            stripe.transform.position = standCenter + new Vector3(0f, 0.04f, 0f);
            stripe.transform.localScale = new Vector3(0.08f, 0.02f, 0.64f);
            ApplyColor(stripe, new Color(0.92f, 0.9f, 0.82f), VisualSmoothnessAsphalt);
            ConfigureStaticVisual(stripe, VisualSmoothnessAsphalt);

            GameObject car = CreateCarModel(i, parent);
            car.transform.position = standCenter + new Vector3(0f, 0.12f, 0f);
            car.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            car.transform.localScale = Vector3.one * 0.7f;
        }

        EnhanceCarMarketModel(parent, center, min, max, anchor);
    }

    private void CreateFurnitureFactoryDecoration(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        if (TryCreateImportedFurnitureFactoryModel(owner, parent, center, min, max, anchor))
        {
            return;
        }

        Vector3 ScaleOffset(Vector3 offset) => offset * BuildingDecorScale;
        Vector3 ScaleSize(Vector3 size) => size * BuildingDecorScale;

        GameObject mainHall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mainHall.transform.SetParent(parent, false);
        mainHall.transform.position = center + ScaleOffset(new Vector3(-0.18f, 0.42f, 0.02f));
        mainHall.transform.localScale = ScaleSize(new Vector3(2.1f, 0.52f, 1.2f));
        ApplyColor(mainHall, new Color(0.86f, 0.8f, 0.68f), VisualSmoothnessBuildingWall);
        ConfigureStaticVisual(mainHall, VisualSmoothnessBuildingWall);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + ScaleOffset(new Vector3(-0.18f, 0.73f, 0.02f));
        roof.transform.localScale = ScaleSize(new Vector3(2.2f, 0.09f, 1.28f));
        ApplyColor(roof, new Color(0.72f, 0.24f, 0.18f), VisualSmoothnessRoofMetal);
        ConfigureStaticVisual(roof, VisualSmoothnessRoofMetal);

        GameObject sideWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sideWing.transform.SetParent(parent, false);
        sideWing.transform.position = center + ScaleOffset(new Vector3(0.94f, 0.28f, -0.04f));
        sideWing.transform.localScale = ScaleSize(new Vector3(0.68f, 0.32f, 0.78f));
        ApplyColor(sideWing, new Color(0.64f, 0.56f, 0.4f), VisualSmoothnessBuildingWall);
        ConfigureStaticVisual(sideWing, VisualSmoothnessBuildingWall);

        GameObject loadingAwning = GameObject.CreatePrimitive(PrimitiveType.Cube);
        loadingAwning.transform.SetParent(parent, false);
        loadingAwning.transform.position = center + ScaleOffset(new Vector3(0.92f, 0.54f, -0.44f));
        loadingAwning.transform.localScale = ScaleSize(new Vector3(0.86f, 0.06f, 0.42f));
        ApplyColor(loadingAwning, new Color(0.24f, 0.28f, 0.33f), VisualSmoothnessRoofMetal);
        ConfigureStaticVisual(loadingAwning, VisualSmoothnessRoofMetal);

        float[] postX = { 0.64f, 1.2f };
        foreach (float px in postX)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.position = center + ScaleOffset(new Vector3(px, 0.24f, -0.44f));
            post.transform.localScale = ScaleSize(new Vector3(0.08f, 0.42f, 0.08f));
            ApplyColor(post, new Color(0.28f, 0.3f, 0.34f), VisualSmoothnessVehicleMetal);
            ConfigureStaticVisual(post, VisualSmoothnessVehicleMetal);
        }

        GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chimney.transform.SetParent(parent, false);
        chimney.transform.position = center + ScaleOffset(new Vector3(-0.78f, 0.92f, 0.26f));
        chimney.transform.localScale = ScaleSize(new Vector3(0.18f, 0.78f, 0.18f));
        ApplyColor(chimney, new Color(0.42f, 0.3f, 0.22f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(chimney, VisualSmoothnessVehicleMetal);

        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.transform.SetParent(parent, false);
        sign.transform.position = center + ScaleOffset(new Vector3(-0.12f, 0.58f, -0.62f));
        sign.transform.localScale = ScaleSize(new Vector3(1.02f, 0.18f, 0.06f));
        ApplyColor(sign, new Color(0.94f, 0.82f, 0.22f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(sign, VisualSmoothnessVehicleMetal);

        for (int i = 0; i < 3; i++)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.transform.SetParent(parent, false);
            crate.transform.position = center + ScaleOffset(new Vector3(0.62f + i * 0.22f, 0.08f + (i % 2) * 0.02f, 0.38f));
            crate.transform.localScale = ScaleSize(new Vector3(0.18f, 0.16f, 0.18f));
            ApplyColor(crate, new Color(0.68f, 0.48f, 0.22f), VisualSmoothnessWood);
            ConfigureStaticVisual(crate, VisualSmoothnessWood);
        }

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.96f);
        EnhanceFurnitureFactoryModel(parent, center, min, max, anchor);
    }

    private void CreateMotelDecoration(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        if (TryCreateImportedMotelModel(owner, parent, center, min, max, anchor))
        {
            return;
        }

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

        // === BUILDING - back half of footprint (local Z < 0 = away from anchor) ===

        // Main body
        GameObject mainBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mainBlock.transform.SetParent(or, false);
        mainBlock.transform.localPosition = new Vector3(0f, 0.36f, -0.4f);
        mainBlock.transform.localScale = new Vector3(1.85f, 0.52f, 0.72f);
        ApplyColor(mainBlock, new Color(0.91f, 0.87f, 0.74f), VisualSmoothnessBuildingWall);

        // Red flat roof
        GameObject roofBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roofBlock.transform.SetParent(or, false);
        roofBlock.transform.localPosition = new Vector3(0f, 0.66f, -0.4f);
        roofBlock.transform.localScale = new Vector3(1.92f, 0.09f, 0.82f);
        ApplyColor(roofBlock, new Color(0.76f, 0.22f, 0.18f), VisualSmoothnessRoofMetal);

        // Facade canopy - on the front face of the building body (toward anchor side)
        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canopy.transform.SetParent(or, false);
        canopy.transform.localPosition = new Vector3(0f, 0.58f, -0.06f);
        canopy.transform.localScale = new Vector3(1.85f, 0.07f, 0.32f);
        ApplyColor(canopy, new Color(0.78f, 0.24f, 0.2f), VisualSmoothnessRoofMetal);

        // Three support posts under the canopy
        float[] postX = { -0.68f, 0f, 0.68f };
        foreach (float px in postX)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(or, false);
            post.transform.localPosition = new Vector3(px, 0.38f, 0.04f);
            post.transform.localScale = new Vector3(0.07f, 0.4f, 0.07f);
            ApplyColor(post, new Color(0.82f, 0.22f, 0.18f), VisualSmoothnessVehicleMetal);
        }

        // MOTEL sign above the roofline
        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.transform.SetParent(or, false);
        sign.transform.localPosition = new Vector3(0f, 0.82f, -0.18f);
        sign.transform.localScale = new Vector3(0.72f, 0.18f, 0.06f);
        ApplyColor(sign, new Color(0.98f, 0.84f, 0.12f), VisualSmoothnessVehicleMetal);

        // === PARKING AREA - front half of footprint (local Z > 0 = toward anchor) ===

        // Two flat parking panels on the ground in front of the building.
        // localY = 0.37 puts them just above the top of the location base block (top = 0.35 local).
        float[] slotX = { -0.27f, 0.27f };
        foreach (float sx in slotX)
        {
            GameObject slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slot.transform.SetParent(or, false);
            slot.transform.localPosition = new Vector3(sx, -0.32f, 0.5f);
            slot.transform.localScale = new Vector3(0.46f, 0.015f, 0.72f);
            ApplyColor(slot, new Color(0.56f, 0.56f, 0.58f), VisualSmoothnessAsphalt);
            ConfigureStaticVisual(slot, VisualSmoothnessAsphalt);
        }

        EnhanceMotelModel(parent, center, min, max, anchor);
    }

    private void CreateLocationNightLights(LocationData owner, LocationType type, Transform parent, Vector3 center, Vector2Int size)
    {
        if (type == LocationType.Forest)
        {
            CreateLocationNightLight(
                owner,
                parent,
                center + new Vector3(0f, 1.15f, -0.95f),
                new Color(0.28f, 0.24f, 0.18f),
                new Color(1f, 0.9f, 0.72f),
                1.15f,
                3.2f);
            CreateLocationPerimeterGlow(owner, type, parent, center, size);
            return;
        }

        if (type == LocationType.CityPark)
        {
            float[] lx = { -2.5f,  2.5f, -2.5f,  2.5f };
            float[] lz = { -2.5f, -2.5f,  2.5f,  2.5f };
            for (int li = 0; li < 4; li++)
            {
                CreateLocationNightLight(
                    owner,
                    parent,
                    center + new Vector3(lx[li], 1.3f, lz[li]),
                    new Color(0.18f, 0.22f, 0.14f),
                    new Color(1f, 0.94f, 0.72f),
                    0.88f,
                    4.0f);
            }
            CreateLocationPerimeterGlow(owner, type, parent, center, size);
            return;
        }

        if (type == LocationType.PersonalHouse)
        {
            // Two porch lanterns flanking the front door
            CreateLocationNightLight(owner, parent, center + new Vector3(-0.4f, 1.0f, 0f),
                new Color(0.24f, 0.20f, 0.14f), new Color(1f, 0.90f, 0.68f), 0.75f, 3.0f);
            CreateLocationNightLight(owner, parent, center + new Vector3(0.4f, 1.0f, 0f),
                new Color(0.24f, 0.20f, 0.14f), new Color(1f, 0.90f, 0.68f), 0.75f, 3.0f);
            CreateLocationPerimeterGlow(owner, type, parent, center, size);
            return;
        }

        float xOffset = Mathf.Max(0.45f, size.x * 0.28f);
        float zOffset = Mathf.Max(0.38f, size.y * 0.28f);

        Color offColor = new Color(0.28f, 0.24f, 0.18f);
        Color onColor = new Color(1f, 0.9f, 0.72f);
        float maxIntensity = 1.15f;
        float lightRange = 3.2f;

        if (type == LocationType.Bar)
        {
            offColor = new Color(0.30f, 0.20f, 0.10f);
            onColor = new Color(1f, 0.74f, 0.30f);
            maxIntensity = 1.28f;
            lightRange = 3.5f;
        }
        else if (type == LocationType.Canteen)
        {
            offColor = new Color(0.22f, 0.26f, 0.24f);
            onColor = new Color(1f, 0.94f, 0.82f);
            maxIntensity = 1.05f;
            lightRange = 3.8f;
        }
        else if (type == LocationType.GamblingHall)
        {
            offColor = new Color(0.24f, 0.14f, 0.30f);
            onColor = new Color(1f, 0.34f, 0.82f);
            maxIntensity = 1.42f;
            lightRange = 4.3f;
        }

        CreateLocationNightLight(owner, parent, center + new Vector3(-xOffset, 0.92f, -zOffset), offColor, onColor, maxIntensity, lightRange);
        CreateLocationNightLight(owner, parent, center + new Vector3(xOffset, 0.92f, -zOffset), offColor, onColor, maxIntensity, lightRange);

        if (type == LocationType.Sawmill || type == LocationType.FurnitureFactory)
        {
            CreateLocationNightLight(owner, parent, center + new Vector3(0f, 0.86f, zOffset), offColor, onColor, maxIntensity, lightRange);
        }

        if (type == LocationType.Bar)
        {
            CreateLocationNightLight(
                owner,
                parent,
                center + new Vector3(0f, 1.06f, -1.02f),
                new Color(0.34f, 0.18f, 0.08f),
                new Color(1f, 0.66f, 0.22f),
                1.46f,
                4.1f);
        }
        else if (type == LocationType.Canteen)
        {
            CreateLocationNightLight(
                owner,
                parent,
                center + new Vector3(0f, 0.82f, -0.96f),
                new Color(0.24f, 0.28f, 0.26f),
                new Color(1f, 0.98f, 0.88f),
                1.08f,
                4.2f);
        }
        else if (type == LocationType.GamblingHall)
        {
            CreateLocationNightLight(
                owner,
                parent,
                center + new Vector3(0f, 1.18f, -1.08f),
                new Color(0.26f, 0.12f, 0.32f),
                new Color(0.30f, 0.92f, 0.90f),
                1.56f,
                4.9f);
        }

        CreateLocationPerimeterGlow(owner, type, parent, center, size);
    }

    private void CreateLocationPerimeterGlow(LocationData owner, LocationType type, Transform parent, Vector3 center, Vector2Int size)
    {
        if (type == LocationType.Stop || type == LocationType.IntercityStop)
        {
            return;
        }

        float xOffset = Mathf.Max(0.62f, size.x * 0.5f - 0.12f);
        float zOffset = Mathf.Max(0.62f, size.y * 0.5f - 0.12f);
        float y = type == LocationType.CityPark ? 0.76f : 0.72f;
        float intensity = type switch
        {
            LocationType.GamblingHall => 0.42f,
            LocationType.Bar          => 0.38f,
            LocationType.Canteen      => 0.32f,
            LocationType.CityPark     => 0.30f,
            LocationType.CarMarket    => 0.34f,
            LocationType.Warehouse    => 0.32f,
            _                         => 0.28f
        };
        float range = type switch
        {
            LocationType.CityPark  => 4.6f,
            LocationType.CarMarket => 4.4f,
            LocationType.Warehouse => 4.1f,
            _                      => 3.6f
        };

        Color offColor = new(0.17f, 0.13f, 0.08f);
        Color onColor = type switch
        {
            LocationType.GamblingHall => new Color(1f, 0.48f, 0.95f),
            LocationType.Bar          => new Color(1f, 0.68f, 0.26f),
            LocationType.Canteen      => new Color(1f, 0.95f, 0.82f),
            _                         => new Color(1f, 0.86f, 0.58f)
        };

        Vector3[] positions =
        {
            center + new Vector3(-xOffset, y, -zOffset),
            center + new Vector3(xOffset, y, -zOffset),
            center + new Vector3(-xOffset, y, zOffset),
            center + new Vector3(xOffset, y, zOffset),
        };

        foreach (Vector3 pos in positions)
        {
            CreateLocationNightLight(owner, parent, pos, offColor, onColor, intensity, range);
        }

        if (size.x >= 4)
        {
            CreateLocationNightLight(owner, parent, center + new Vector3(0f, y, -zOffset), offColor, onColor, intensity * 0.85f, range);
            CreateLocationNightLight(owner, parent, center + new Vector3(0f, y, zOffset), offColor, onColor, intensity * 0.85f, range);
        }

        if (size.y >= 4)
        {
            CreateLocationNightLight(owner, parent, center + new Vector3(-xOffset, y, 0f), offColor, onColor, intensity * 0.85f, range);
            CreateLocationNightLight(owner, parent, center + new Vector3(xOffset, y, 0f), offColor, onColor, intensity * 0.85f, range);
        }
    }

    private void CreateLocationNightLight(LocationData owner, Transform parent, Vector3 localPosition, Color offColor, Color onColor, float maxIntensity, float range)
    {
        if (owner != null)
        {
            range = ExpandLocationNightLightRange(owner.Type, range);
        }

        GameObject lampVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampVisual.name = "NightLampVisual";
        lampVisual.transform.SetParent(parent, false);
        lampVisual.transform.localPosition = localPosition;
        lampVisual.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
        ApplyColor(lampVisual, offColor, VisualSmoothnessGlass);
        ConfigureStaticVisual(lampVisual, VisualSmoothnessGlass);
        Renderer lampRenderer = lampVisual.GetComponent<Renderer>();
        if (lampRenderer != null)
        {
            lampRenderer.enabled = false;
        }

        locationNightLightRenderers.Add(lampRenderer);
        locationNightLightMaterials.Add(lampRenderer != null ? lampRenderer.material : null);
        locationNightLightOffColors.Add(offColor);
        locationNightLightOnColors.Add(onColor);
        locationNightLightMaxIntensities.Add(maxIntensity);
        locationNightLightRanges.Add(range);
        locationNightLightMaterialOwnerInstanceIds.Add(owner?.InstanceId ?? 0);

        GameObject lightObject = new("NightLamp");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition + new Vector3(0f, 0.06f, 0f);

        Light lamp = lightObject.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = onColor;
        lamp.range = range;
        lamp.intensity = 0f;
        lamp.shadows = LightShadows.None;
        lamp.enabled = false;
        locationNightLights.Add(lamp);
        locationNightLightOwnerInstanceIds.Add(owner?.InstanceId ?? 0);
        locationNightPointLightOnColors.Add(onColor);
        locationNightPointLightMaxIntensities.Add(maxIntensity);
        locationNightPointLightRanges.Add(range);
        MarkCellLightingDirty();
    }

    private void CreateLocationWindowLanguage(LocationData owner, LocationType type, Transform parent, Vector3 center, Vector2Int size)
    {
        if (type == LocationType.IntercityStop || type == LocationType.Stop || type == LocationType.CityPark || type == LocationType.PersonalHouse)
        {
            return;
        }

        if (HasImportedBuildingModel(parent))
        {
            return;
        }

        Color offColor = new Color(0.12f, 0.10f, 0.06f, 0f);
        Color onColor  = new Color(1f, 0.88f, 0.55f, 0.92f);

        switch (type)
        {
            case LocationType.Parking:
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0f, 0.84f, -0.56f), new Vector3(size.x * 0.44f, 0.16f, 0.06f), offColor, onColor);
                break;
            case LocationType.GasStation:
                CreateLocationGlowStrip(owner, parent, center + new Vector3(-0.44f, 0.78f, -0.74f), new Vector3(0.38f, 0.14f, 0.06f), offColor, onColor);
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0.44f, 0.78f, -0.74f), new Vector3(0.38f, 0.14f, 0.06f), offColor, onColor);
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0f, 0.96f, 0.14f), new Vector3(0.92f, 0.10f, 0.06f), offColor, onColor);
                break;
            case LocationType.Forest:
                // Window on the shed front face, to the right of the door, upper wall
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0.38f, 0.20f, -0.34f), new Vector3(0.24f, 0.16f, 0.06f), offColor, onColor);
                break;
            case LocationType.Warehouse:
                CreateLocationGlowStrip(owner, parent, center + new Vector3(-0.68f, 0.84f, -0.94f), new Vector3(0.34f, 0.18f, 0.06f), offColor, onColor);
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0f, 0.84f, -0.94f), new Vector3(0.34f, 0.18f, 0.06f), offColor, onColor);
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0.68f, 0.84f, -0.94f), new Vector3(0.34f, 0.18f, 0.06f), offColor, onColor);
                break;
            case LocationType.Motel:
                CreateLocationGlowStrip(owner, parent, center + new Vector3(-0.52f, 0.78f, -0.82f), new Vector3(0.32f, 0.16f, 0.06f), offColor, onColor);
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0f, 0.78f, -0.82f), new Vector3(0.32f, 0.16f, 0.06f), offColor, onColor);
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0.52f, 0.78f, -0.82f), new Vector3(0.32f, 0.16f, 0.06f), offColor, onColor);
                break;
            case LocationType.Sawmill:
                CreateLocationGlowStrip(owner, parent, center + new Vector3(-0.42f, 0.8f, -0.82f), new Vector3(0.28f, 0.22f, 0.06f), offColor, onColor);
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0.42f, 0.8f, -0.82f), new Vector3(0.28f, 0.22f, 0.06f), offColor, onColor);
                break;
            case LocationType.FurnitureFactory:
                CreateLocationGlowStrip(owner, parent, center + new Vector3(-0.72f, 0.84f, -0.9f), new Vector3(0.32f, 0.18f, 0.06f), offColor, onColor);
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0f, 0.84f, -0.9f), new Vector3(0.32f, 0.18f, 0.06f), offColor, onColor);
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0.72f, 0.84f, -0.9f), new Vector3(0.32f, 0.18f, 0.06f), offColor, onColor);
                break;
            case LocationType.Bar:
                CreateLocationGlowStrip(owner, parent, center + new Vector3(-0.55f, 0.85f, -1.22f), new Vector3(0.28f, 0.24f, 0.06f), offColor, onColor);
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0.55f, 0.85f, -1.22f), new Vector3(0.28f, 0.24f, 0.06f), offColor, onColor);
                break;
            case LocationType.Canteen:
                CreateLocationGlowStrip(owner, parent, center + new Vector3(size.x * 0.28f, 0.72f, -1.17f), new Vector3(size.x * 0.58f, 0.38f, 0.06f), offColor, onColor);
                break;
            case LocationType.GamblingHall:
                CreateLocationGlowStrip(owner, parent, center + new Vector3(-0.60f, 0.88f, -1.05f), new Vector3(0.30f, 0.22f, 0.06f), offColor, onColor);
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0f, 0.88f, -1.05f), new Vector3(0.34f, 0.26f, 0.06f), offColor, onColor);
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0.60f, 0.88f, -1.05f), new Vector3(0.30f, 0.22f, 0.06f), offColor, onColor);
                break;
            default:
                CreateLocationGlowStrip(owner, parent, center + new Vector3(0f, 0.82f, -0.82f), new Vector3(Mathf.Max(0.4f, size.x * 0.34f), 0.16f, 0.06f), offColor, onColor);
                break;
        }
    }

    private void CreateLocationGlowStrip(LocationData owner, Transform parent, Vector3 localPosition, Vector3 localScale, Color offColor, Color onColor)
    {
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.transform.SetParent(parent, false);
        strip.transform.localPosition = localPosition;
        strip.transform.localScale = localScale;

        Renderer rendererComponent = strip.GetComponent<Renderer>();
        if (rendererComponent != null)
        {
            Material glowMaterial = CreateTransparentOverlayMaterial(offColor);
            rendererComponent.sharedMaterial = glowMaterial;
            rendererComponent.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rendererComponent.receiveShadows = false;
            locationNightLightRenderers.Add(rendererComponent);
            locationNightLightMaterials.Add(rendererComponent.material);
            locationNightLightOffColors.Add(offColor);
            locationNightLightOnColors.Add(onColor);
            locationNightLightMaxIntensities.Add(0f);
            locationNightLightRanges.Add(0f);
            locationNightLightMaterialOwnerInstanceIds.Add(owner?.InstanceId ?? 0);
        }

        if (strip.TryGetComponent(out Collider stripCollider))
        {
            stripCollider.enabled = false;
        }
    }

}
