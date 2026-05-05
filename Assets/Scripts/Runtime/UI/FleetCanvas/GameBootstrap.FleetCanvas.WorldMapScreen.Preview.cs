using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void CreateWorldMapGeography(RectTransform parent)
    {
        // The map surface is now generated directly as pixel art.
    }

    private void CreateWorldMapCompass(RectTransform parent)
    {
        RectTransform root = CreateUiObject("WorldMapCompass", parent).GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.055f, 0.055f);
        root.anchorMax = new Vector2(0.175f, 0.175f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();

        CreateWorldMapMapShape(root, "CompassNorth", new Color(0.72f, 0.48f, 0.08f, 0.72f), new Vector2(0.50f, 0.72f), new Vector2(0.10f, 0.46f), 0f);
        CreateWorldMapMapShape(root, "CompassEast", new Color(0.90f, 0.68f, 0.14f, 0.72f), new Vector2(0.72f, 0.50f), new Vector2(0.46f, 0.10f), 0f);
        CreateWorldMapMapShape(root, "CompassSouth", new Color(0.72f, 0.48f, 0.08f, 0.72f), new Vector2(0.50f, 0.28f), new Vector2(0.10f, 0.46f), 0f);
        CreateWorldMapMapShape(root, "CompassWest", new Color(0.90f, 0.68f, 0.14f, 0.72f), new Vector2(0.28f, 0.50f), new Vector2(0.46f, 0.10f), 0f);
    }

    private Image CreateWorldMapMapShape(RectTransform parent, string name, Color color, Vector2 center, Vector2 size, float rotation)
    {
        GameObject obj = CreateUiObject(name, parent);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(center.x - size.x * 0.5f, center.y - size.y * 0.5f);
        rect.anchorMax = new Vector2(center.x + size.x * 0.5f, center.y + size.y * 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        rect.SetAsFirstSibling();
        return image;
    }

    private static Sprite GetRegionalWorldMapSprite()
    {
        if (s_regionalWorldMapSprite != null)
        {
            return s_regionalWorldMapSprite;
        }

        s_regionalWorldMapSprite = CreateRegionalWorldMapPixelSprite();
        return s_regionalWorldMapSprite;
    }

    private static float DistanceToEllipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / Mathf.Max(0.0001f, rx);
        float dy = (y - cy) / Mathf.Max(0.0001f, ry);
        return dx * dx + dy * dy;
    }

    private static void DrawMapRiverBranch(Color[] pixels, int width, int height, Vector2 a, Vector2 b, Color color)
    {
        for (int y = 0; y < height; y++)
        {
            float ny = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1);
                float t = Mathf.Clamp01(Vector2.Dot(new Vector2(nx, ny) - a, b - a) / Mathf.Max(0.0001f, (b - a).sqrMagnitude));
                Vector2 p = Vector2.Lerp(a, b, t);
                float dist = Vector2.Distance(new Vector2(nx, ny), p);
                if (dist < 0.0065f)
                {
                    pixels[y * width + x] = Color.Lerp(pixels[y * width + x], color, 0.86f);
                }
            }
        }
    }

    private static void DrawMapBorder(Color[] pixels, int width, int height, Color color)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool edge = x < 3 || y < 3 || x >= width - 3 || y >= height - 3;
                bool inner = x == 8 || y == 8 || x == width - 9 || y == height - 9;
                if (edge || inner)
                {
                    pixels[y * width + x] = color;
                }
            }
        }
    }

    private static void DrawMapTreeCluster(Color[] pixels, int width, int height, Vector2 center, int count)
    {
        Color trunk = new(0.28f, 0.18f, 0.07f, 1f);
        Color leaf = new(0.16f, 0.39f, 0.18f, 1f);
        for (int i = 0; i < count; i++)
        {
            float angle = i * 2.399963f;
            float radius = 0.012f + 0.055f * Mathf.Sqrt((i + 1f) / Mathf.Max(1f, count));
            Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            DrawMapDot(pixels, width, height, p, 2, trunk, 0.90f);
            DrawMapDot(pixels, width, height, p + new Vector2(0f, 0.008f), 4, leaf, 0.82f);
        }
    }

    private static void DrawMapMountainCluster(Color[] pixels, int width, int height, Vector2 center, int count)
    {
        Color ridge = new(0.44f, 0.40f, 0.34f, 1f);
        Color light = new(0.75f, 0.70f, 0.62f, 1f);
        for (int i = 0; i < count; i++)
        {
            float x = center.x + (i - count * 0.5f) * 0.026f;
            float y = center.y + 0.018f * Mathf.Sin(i * 1.7f);
            DrawMapDot(pixels, width, height, new Vector2(x, y), 7, ridge, 0.55f);
            DrawMapDot(pixels, width, height, new Vector2(x - 0.006f, y + 0.006f), 3, light, 0.45f);
        }
    }

    private static void DrawMapDot(Color[] pixels, int width, int height, Vector2 normalized, int radius, Color color, float alpha)
    {
        int cx = Mathf.RoundToInt(normalized.x * (width - 1));
        int cy = Mathf.RoundToInt(normalized.y * (height - 1));
        int r2 = radius * radius;
        for (int y = -radius; y <= radius; y++)
        {
            int py = cy + y;
            if (py < 0 || py >= height) continue;
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y > r2) continue;
                int px = cx + x;
                if (px < 0 || px >= width) continue;
                int index = py * width + px;
                pixels[index] = Color.Lerp(pixels[index], color, alpha);
            }
        }
    }

    private Image CreateWorldMapRouteLine(RectTransform parent, int regionIndex)
    {
        GameObject obj = CreateUiObject($"WorldMapRouteLine_{regionIndex}", parent);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        Image image = obj.AddComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;
        obj.SetActive(false);
        return image;
    }

    private WorldMapCellUi CreateWorldMapCityMarker(RectTransform parent, Font font, int regionIndex)
    {
        WorldMapCellUi cell = new();
        GameObject markerObject = CreateUiObject($"WorldMapCityMarker_{regionIndex}", parent);
        RectTransform markerRect = markerObject.GetComponent<RectTransform>();
        markerRect.anchorMin = GetWorldMapRegionPosition(regionIndex);
        markerRect.anchorMax = GetWorldMapRegionPosition(regionIndex);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(70f, 52f);

        Image background = markerObject.AddComponent<Image>();
        background.color = new Color(0.10f, 0.07f, 0.04f, 0f);
        Button button = markerObject.AddComponent<Button>();
        Outline outline = markerObject.AddComponent<Outline>();
        outline.effectColor = Color.clear;
        outline.effectDistance = new Vector2(2f, -2f);

        cell.NameText = CreateHeaderText($"WorldMapCityName_{regionIndex}", markerRect, font, string.Empty, 1, TextAnchor.MiddleCenter, Color.clear);
        cell.NameText.gameObject.SetActive(false);
        cell.TypeText = CreateBodyText($"WorldMapCityType_{regionIndex}", markerRect, font, string.Empty, 1, TextAnchor.MiddleCenter, Color.clear);
        cell.TypeText.gameObject.SetActive(false);

        GameObject dotObj = CreateUiObject($"WorldMapRouteDot_{regionIndex}", markerRect);
        RectTransform dotRect = dotObj.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(1f, 1f);
        dotRect.anchorMax = new Vector2(1f, 1f);
        dotRect.pivot = new Vector2(1f, 1f);
        dotRect.anchoredPosition = new Vector2(-6f, -6f);
        dotRect.sizeDelta = new Vector2(8f, 8f);
        Image dotImage = dotObj.AddComponent<Image>();
        dotImage.color = Color.clear;
        dotImage.raycastTarget = false;

        cell.Button = button;
        cell.Background = background;
        cell.Outline = outline;
        cell.RouteStatusDot = dotImage;
        cell.RegionIndex = regionIndex;
        cell.Button.onClick.AddListener(() => SelectWorldMapRegion(regionIndex));
        return cell;
    }

    private RectTransform CreateWorldMapCityIcon(RectTransform parent, int regionIndex)
    {
        RectTransform root = CreateUiObject($"WorldMapCityIcon_{regionIndex}", parent).GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(30f, 30f);
        Color wall = regionIndex == 4 ? new Color(0.96f, 0.74f, 0.18f, 1f) : new Color(0.88f, 0.74f, 0.48f, 1f);
        Color roof = regionIndex == 6 ? new Color(0.72f, 0.30f, 0.14f, 1f) : new Color(0.72f, 0.47f, 0.17f, 1f);
        Color shadow = new(0.23f, 0.13f, 0.05f, 0.84f);

        CreateWorldMapIconPart(root, "Shadow", shadow, new Vector2(0.16f, 0.10f), new Vector2(0.84f, 0.22f));
        CreateWorldMapIconPart(root, "Base", wall, new Vector2(0.20f, 0.18f), new Vector2(0.80f, 0.58f));
        CreateWorldMapIconPart(root, "Roof", roof, new Vector2(0.14f, 0.56f), new Vector2(0.86f, 0.72f));
        CreateWorldMapIconPart(root, "TowerA", wall, new Vector2(0.24f, 0.42f), new Vector2(0.40f, 0.82f));
        CreateWorldMapIconPart(root, "TowerB", wall, new Vector2(0.60f, 0.38f), new Vector2(0.76f, 0.78f));
        CreateWorldMapIconPart(root, "Door", new Color(0.28f, 0.16f, 0.06f, 1f), new Vector2(0.46f, 0.18f), new Vector2(0.56f, 0.42f));
        return root;
    }

    private Image CreateWorldMapIconPart(RectTransform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        GameObject obj = CreateUiObject(name, parent);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private WorldMapCellUi CreateWorldMapCell(RectTransform parent, Font font, int regionIndex)
    {
        WorldMapCellUi cell = new();
        GameObject cellObject = CreateUiObject($"WorldMapCell_{regionIndex}", parent);
        RectTransform cellRect = cellObject.GetComponent<RectTransform>();
        LayoutElement layoutElement = cellRect.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 170f;
        layoutElement.preferredHeight = 120f;

        Image background = cellObject.AddComponent<Image>();
        background.color = FleetInsetColor;
        Button button = cellObject.AddComponent<Button>();
        Outline outline = cellObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        outline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup layout = cellObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform previewRoot = CreateStyledPanel($"WorldMapCellPreview_{regionIndex}", cellRect, new Color(0.12f, 0.15f, 0.20f, 0.98f));
        LayoutElement previewLayout = previewRoot.gameObject.AddComponent<LayoutElement>();
        previewLayout.preferredHeight = 70f;
        cell.PreviewBackground = previewRoot.GetComponent<Image>();

        cell.PreviewPlaceholderText = CreateBodyText($"WorldMapCellPreviewPlaceholder_{regionIndex}", previewRoot, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetMutedTextColor);
        StretchRect(cell.PreviewPlaceholderText.rectTransform, 10f, 10f, 10f, 10f);

        cell.WaterShape = CreateWorldMapPreviewShape(previewRoot, $"WorldMapWater_{regionIndex}", new Color(0.54f, 0.77f, 0.92f, 0.95f), 0f, 0.76f, 1f, 0.24f);
        cell.HighwayShape = CreateWorldMapPreviewShape(previewRoot, $"WorldMapHighway_{regionIndex}", new Color(0.15f, 0.17f, 0.20f, 1f), 0.04f, 0.08f, 0.92f, 0.16f);
        cell.ForestShape = CreateWorldMapPreviewShape(previewRoot, $"WorldMapForest_{regionIndex}", new Color(0.19f, 0.39f, 0.24f, 0.98f), 0.06f, 0.36f, 0.28f, 0.30f);
        cell.TownBlockA = CreateWorldMapPreviewShape(previewRoot, $"WorldMapTownA_{regionIndex}", new Color(0.83f, 0.72f, 0.46f, 0.96f), 0.40f, 0.30f, 0.16f, 0.16f);
        cell.TownBlockB = CreateWorldMapPreviewShape(previewRoot, $"WorldMapTownB_{regionIndex}", new Color(0.86f, 0.78f, 0.55f, 0.96f), 0.58f, 0.30f, 0.18f, 0.18f);
        cell.TownBlockC = CreateWorldMapPreviewShape(previewRoot, $"WorldMapTownC_{regionIndex}", new Color(0.76f, 0.63f, 0.34f, 0.96f), 0.50f, 0.50f, 0.12f, 0.12f);
        cell.HighwayDashA = CreateWorldMapPreviewShape(previewRoot, $"WorldMapDashA_{regionIndex}", new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.18f, 0.12f, 0.10f, 0.03f);
        cell.HighwayDashB = CreateWorldMapPreviewShape(previewRoot, $"WorldMapDashB_{regionIndex}", new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.45f, 0.12f, 0.10f, 0.03f);
        cell.HighwayDashC = CreateWorldMapPreviewShape(previewRoot, $"WorldMapDashC_{regionIndex}", new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.72f, 0.12f, 0.10f, 0.03f);

        cell.NameText = CreateHeaderText($"WorldMapCellName_{regionIndex}", cellRect, font, string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        cell.TypeText = CreateBodyText($"WorldMapCellType_{regionIndex}", cellRect, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetMutedTextColor);

        // Route status dot — top-right corner of cell
        GameObject dotObj = CreateUiObject($"WorldMapRouteDot_{regionIndex}", cellRect);
        RectTransform dotRect = dotObj.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(1f, 1f);
        dotRect.anchorMax = new Vector2(1f, 1f);
        dotRect.pivot = new Vector2(1f, 1f);
        dotRect.anchoredPosition = new Vector2(-6f, -6f);
        dotRect.sizeDelta = new Vector2(10f, 10f);
        Image dotImage = dotObj.AddComponent<Image>();
        dotImage.color = new Color(0.3f, 0.8f, 0.4f, 0f); // hidden by default
        cell.RouteStatusDot = dotImage;

        cell.Button = button;
        cell.Background = background;
        cell.Outline = outline;
        cell.RegionIndex = regionIndex;
        cell.Button.onClick.AddListener(() => SelectWorldMapRegion(regionIndex));
        return cell;
    }

    private WorldMapDetailPreviewUi CreateWorldMapDetailPreview(RectTransform parent, Font font)
    {
        WorldMapDetailPreviewUi preview = new();
        preview.PreviewBackground = parent.GetComponent<Image>();

        preview.PlaceholderText = CreateBodyText("WorldMapDetailPlaceholder", parent, font,
            "No regional map yet", 14, TextAnchor.MiddleCenter, FleetMutedTextColor);
        StretchRect(preview.PlaceholderText.rectTransform, 10f, 10f, 10f, 10f);

        preview.WaterShape   = CreateWorldMapPreviewShape(parent, "WorldMapDetailWater",    new Color(0.54f, 0.77f, 0.92f, 0.95f), 0f,    0.76f, 1f,    0.24f);
        preview.HighwayShape = CreateWorldMapPreviewShape(parent, "WorldMapDetailHighway",  new Color(0.15f, 0.17f, 0.20f, 1f),    0.04f, 0.08f, 0.92f, 0.16f);
        preview.ForestShape  = CreateWorldMapPreviewShape(parent, "WorldMapDetailForest",   new Color(0.19f, 0.39f, 0.24f, 0.98f), 0.06f, 0.36f, 0.28f, 0.30f);
        preview.TownBlockA   = CreateWorldMapPreviewShape(parent, "WorldMapDetailTownA",    new Color(0.83f, 0.72f, 0.46f, 0.96f), 0.40f, 0.30f, 0.16f, 0.16f);
        preview.TownBlockB   = CreateWorldMapPreviewShape(parent, "WorldMapDetailTownB",    new Color(0.86f, 0.78f, 0.55f, 0.96f), 0.58f, 0.30f, 0.18f, 0.18f);
        preview.TownBlockC   = CreateWorldMapPreviewShape(parent, "WorldMapDetailTownC",    new Color(0.76f, 0.63f, 0.34f, 0.96f), 0.50f, 0.50f, 0.12f, 0.12f);
        preview.HighwayDashA = CreateWorldMapPreviewShape(parent, "WorldMapDetailDashA",    new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.18f, 0.12f, 0.10f, 0.03f);
        preview.HighwayDashB = CreateWorldMapPreviewShape(parent, "WorldMapDetailDashB",    new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.45f, 0.12f, 0.10f, 0.03f);
        preview.HighwayDashC = CreateWorldMapPreviewShape(parent, "WorldMapDetailDashC",    new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.72f, 0.12f, 0.10f, 0.03f);

        return preview;
    }

    private Image CreateWorldMapPreviewShape(RectTransform parent, string name, Color color, float x, float y, float width, float height)
    {
        GameObject obj = CreateUiObject(name, parent);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(x + width, y + height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private void ApplyWorldMapCellPreview(WorldMapCellUi cell, int regionIndex, bool hasPreview)
    {
        ApplyWorldMapPreviewShapes(
            regionIndex,
            cell.PreviewBackground,
            cell.PreviewPlaceholderText,
            cell.WaterShape,
            cell.HighwayShape,
            cell.ForestShape,
            cell.TownBlockA,
            cell.TownBlockB,
            cell.TownBlockC,
            cell.HighwayDashA,
            cell.HighwayDashB,
            cell.HighwayDashC,
            hasPreview);
    }

    private void ApplyWorldMapDetailPreview(WorldMapDetailPreviewUi preview, int regionIndex, bool hasPreview)
    {
        ApplyWorldMapPreviewShapes(
            regionIndex,
            preview.PreviewBackground,
            preview.PlaceholderText,
            preview.WaterShape,
            preview.HighwayShape,
            preview.ForestShape,
            preview.TownBlockA,
            preview.TownBlockB,
            preview.TownBlockC,
            preview.HighwayDashA,
            preview.HighwayDashB,
            preview.HighwayDashC,
            hasPreview);
    }

    private void ApplyWorldMapPreviewShapes(
        int regionIndex,
        Image previewBackground,
        Text placeholderText,
        Image water,
        Image highway,
        Image forest,
        Image townA,
        Image townB,
        Image townC,
        Image dashA,
        Image dashB,
        Image dashC,
        bool hasPreview)
    {
        if (!hasPreview)
        {
            if (previewBackground != null)
                previewBackground.color = new Color(0.15f, 0.17f, 0.21f, 0.98f);
            if (placeholderText != null)
            {
                placeholderText.gameObject.SetActive(true);
                placeholderText.text = IsRussianLanguage() ? "Регион не разведан" : "Region not surveyed";
            }

            SetWorldMapPreviewShapeActive(water, false);
            SetWorldMapPreviewShapeActive(highway, false);
            SetWorldMapPreviewShapeActive(forest, false);
            SetWorldMapPreviewShapeActive(townA, false);
            SetWorldMapPreviewShapeActive(townB, false);
            SetWorldMapPreviewShapeActive(townC, false);
            SetWorldMapPreviewShapeActive(dashA, false);
            SetWorldMapPreviewShapeActive(dashB, false);
            SetWorldMapPreviewShapeActive(dashC, false);
            return;
        }

        if (placeholderText != null)
            placeholderText.gameObject.SetActive(false);

        switch (regionIndex)
        {
            case 2:
                if (previewBackground != null) previewBackground.color = new Color(0.11f, 0.16f, 0.18f, 1f);
                SetWorldMapPreviewShape(water, new Color(0.34f, 0.63f, 0.82f, 0.96f), 0.00f, 0.00f, 0.36f, 1.00f);
                SetWorldMapPreviewShape(highway, new Color(0.14f, 0.16f, 0.18f, 1f), 0.42f, 0.08f, 0.13f, 0.84f);
                SetWorldMapPreviewShape(forest, new Color(0.16f, 0.31f, 0.22f, 0.96f), 0.66f, 0.50f, 0.24f, 0.28f);
                SetWorldMapPreviewShape(townA, new Color(0.74f, 0.63f, 0.40f, 0.96f), 0.56f, 0.18f, 0.28f, 0.14f);
                SetWorldMapPreviewShape(townB, new Color(0.86f, 0.74f, 0.45f, 0.96f), 0.58f, 0.35f, 0.22f, 0.12f);
                SetWorldMapPreviewShape(townC, new Color(0.80f, 0.72f, 0.55f, 0.96f), 0.19f, 0.15f, 0.10f, 0.66f);
                SetWorldMapPreviewShape(dashA, new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.47f, 0.18f, 0.03f, 0.10f);
                SetWorldMapPreviewShape(dashB, new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.47f, 0.43f, 0.03f, 0.10f);
                SetWorldMapPreviewShape(dashC, new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.47f, 0.68f, 0.03f, 0.10f);
                break;

            case 5:
                if (previewBackground != null) previewBackground.color = new Color(0.20f, 0.27f, 0.17f, 1f);
                SetWorldMapPreviewShape(water, new Color(0.42f, 0.62f, 0.34f, 0.92f), 0.04f, 0.08f, 0.26f, 0.34f);
                SetWorldMapPreviewShape(highway, new Color(0.16f, 0.17f, 0.18f, 1f), 0.08f, 0.74f, 0.84f, 0.13f);
                SetWorldMapPreviewShape(forest, new Color(0.77f, 0.82f, 0.63f, 0.96f), 0.36f, 0.10f, 0.26f, 0.30f);
                SetWorldMapPreviewShape(townA, new Color(0.54f, 0.64f, 0.78f, 0.96f), 0.66f, 0.18f, 0.18f, 0.18f);
                SetWorldMapPreviewShape(townB, new Color(0.70f, 0.55f, 0.76f, 0.96f), 0.68f, 0.42f, 0.20f, 0.16f);
                SetWorldMapPreviewShape(townC, new Color(0.90f, 0.90f, 0.78f, 0.96f), 0.12f, 0.48f, 0.46f, 0.16f);
                SetWorldMapPreviewShape(dashA, new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.18f, 0.79f, 0.10f, 0.025f);
                SetWorldMapPreviewShape(dashB, new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.45f, 0.79f, 0.10f, 0.025f);
                SetWorldMapPreviewShape(dashC, new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.72f, 0.79f, 0.10f, 0.025f);
                break;

            case 6:
                if (previewBackground != null) previewBackground.color = new Color(0.36f, 0.28f, 0.16f, 1f);
                SetWorldMapPreviewShape(water, new Color(0.71f, 0.58f, 0.32f, 0.92f), 0.00f, 0.00f, 1.00f, 1.00f);
                SetWorldMapPreviewShape(highway, new Color(0.14f, 0.14f, 0.13f, 1f), 0.12f, 0.14f, 0.76f, 0.14f);
                SetWorldMapPreviewShape(forest, new Color(0.54f, 0.43f, 0.22f, 0.96f), 0.10f, 0.45f, 0.22f, 0.24f);
                SetWorldMapPreviewShape(townA, new Color(0.78f, 0.42f, 0.22f, 0.96f), 0.58f, 0.38f, 0.18f, 0.18f);
                SetWorldMapPreviewShape(townB, new Color(0.62f, 0.28f, 0.16f, 0.96f), 0.76f, 0.48f, 0.12f, 0.22f);
                SetWorldMapPreviewShape(townC, new Color(0.90f, 0.74f, 0.42f, 0.96f), 0.36f, 0.56f, 0.12f, 0.12f);
                SetWorldMapPreviewShape(dashA, new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.20f, 0.195f, 0.10f, 0.025f);
                SetWorldMapPreviewShape(dashB, new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.45f, 0.195f, 0.10f, 0.025f);
                SetWorldMapPreviewShape(dashC, new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.70f, 0.195f, 0.10f, 0.025f);
                break;

            default:
                if (previewBackground != null) previewBackground.color = new Color(0.18f, 0.20f, 0.17f, 1f);
                SetWorldMapPreviewShape(water, new Color(0.54f, 0.77f, 0.92f, 0.95f), 0.00f, 0.76f, 1.00f, 0.24f);
                SetWorldMapPreviewShape(highway, new Color(0.15f, 0.17f, 0.20f, 1f), 0.04f, 0.08f, 0.92f, 0.16f);
                SetWorldMapPreviewShape(forest, new Color(0.19f, 0.39f, 0.24f, 0.98f), 0.06f, 0.36f, 0.28f, 0.30f);
                SetWorldMapPreviewShape(townA, new Color(0.83f, 0.72f, 0.46f, 0.96f), 0.40f, 0.30f, 0.16f, 0.16f);
                SetWorldMapPreviewShape(townB, new Color(0.86f, 0.78f, 0.55f, 0.96f), 0.58f, 0.30f, 0.18f, 0.18f);
                SetWorldMapPreviewShape(townC, new Color(0.76f, 0.63f, 0.34f, 0.96f), 0.50f, 0.50f, 0.12f, 0.12f);
                SetWorldMapPreviewShape(dashA, new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.18f, 0.12f, 0.10f, 0.03f);
                SetWorldMapPreviewShape(dashB, new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.45f, 0.12f, 0.10f, 0.03f);
                SetWorldMapPreviewShape(dashC, new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.72f, 0.12f, 0.10f, 0.03f);
                break;
        }
    }

    private static void SetWorldMapPreviewShape(Image image, Color color, float x, float y, float width, float height)
    {
        if (image == null) return;
        image.gameObject.SetActive(true);
        image.color = color;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(x + width, y + height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetWorldMapPreviewShapeActive(Image image, bool active)
    {
        if (image != null)
            image.gameObject.SetActive(active);
    }

    private static Vector2 GetWorldMapRegionPosition(int regionIndex)
    {
        return regionIndex switch
        {
            0 => new Vector2(0.18f, 0.82f),
            1 => new Vector2(0.42f, 0.76f),
            2 => new Vector2(0.78f, 0.68f),
            3 => new Vector2(0.18f, 0.48f),
            4 => new Vector2(0.50f, 0.47f),
            5 => new Vector2(0.76f, 0.45f),
            6 => new Vector2(0.28f, 0.20f),
            7 => new Vector2(0.52f, 0.18f),
            8 => new Vector2(0.83f, 0.24f),
            _ => new Vector2(0.5f, 0.5f)
        };
    }

    private static void UpdateWorldMapRouteLine(Image line, RectTransform mapRoot, int regionIndex, bool visible, Color color)
    {
        if (line == null || mapRoot == null)
        {
            return;
        }

        line.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        Vector2 size = mapRoot.rect.size;
        if (size.x < 1f || size.y < 1f)
        {
            size = new Vector2(720f, 500f);
        }

        Vector2 from = GetWorldMapRegionPosition(4);
        Vector2 to = GetWorldMapRegionPosition(regionIndex);
        Vector2 a = new(from.x * size.x, from.y * size.y);
        Vector2 b = new(to.x * size.x, to.y * size.y);
        Vector2 delta = b - a;

        RectTransform rect = line.rectTransform;
        rect.anchoredPosition = (a + b) * 0.5f;
        rect.sizeDelta = new Vector2(delta.magnitude, 4f);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        line.color = color;
    }


}
