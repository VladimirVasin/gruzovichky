using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class CellQuickHudRefs
    {
        public GameObject CanvasRoot;
        public RectTransform Root;
        public Text HeaderText;
        public Text StatusText;
        public Text DetailsText;
        public Button CloseButton;
    }

    private CellQuickHudRefs cellQuickHud;

    private void SetupCellQuickHud()
    {
        if (cellQuickHud != null)
        {
            return;
        }

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cellQuickHud = new CellQuickHudRefs();

        GameObject canvasObject = new("CellQuickHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        cellQuickHud.CanvasRoot = canvasObject;

        RectTransform root = CreateStyledPanel("CellQuickHudRoot", canvasObject.transform, FleetPanelColor);
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(0f, 0f);
        root.pivot = new Vector2(0f, 0f);
        root.anchoredPosition = new Vector2(18f, 104f);
        root.sizeDelta = new Vector2(330f, 214f);
        VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(16, 16, 16, 16);
        rootLayout.spacing = 12;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        cellQuickHud.Root = root;

        RectTransform headerRow = CreateLayoutRow("CellQuickHudHeader", root, 30f, 10f);
        cellQuickHud.HeaderText = CreateHeaderText("Header", headerRow, uiFont, "Cell", 21, TextAnchor.MiddleLeft, Color.white);
        cellQuickHud.HeaderText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        cellQuickHud.CloseButton = CreateButton("CloseButton", headerRow, uiFont, out Text closeText, "X", 12, new Color(0.26f, 0.30f, 0.36f, 1f), Color.white);
        LayoutElement closeLayout = cellQuickHud.CloseButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 28f;
        closeLayout.preferredHeight = 28f;
        cellQuickHud.CloseButton.onClick.AddListener(ClearSelectedDebugCell);

        RectTransform summaryCard = CreateSectionCard(root, uiFont, string.Empty, out RectTransform summaryBody, false);
        summaryCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 132f;
        cellQuickHud.StatusText = CreateBodyText("Status", summaryBody, uiFont, string.Empty, 17, TextAnchor.MiddleLeft, Color.white);
        cellQuickHud.StatusText.fontStyle = FontStyle.Bold;
        cellQuickHud.StatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        cellQuickHud.DetailsText = CreateBodyText("Details", summaryBody, uiFont, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        cellQuickHud.DetailsText.supportRichText = false;
        cellQuickHud.DetailsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        cellQuickHud.DetailsText.verticalOverflow = VerticalWrapMode.Overflow;

        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        highlight.name = "SelectedDebugCellHighlight";
        highlight.transform.SetParent(worldRoot, false);
        highlight.transform.localScale = new Vector3(1.02f, 0.035f, 1.02f);
        ApplyColor(highlight, new Color(0.3f, 0.86f, 1f));
        ConfigureStaticVisual(highlight);
        if (highlight.TryGetComponent(out Collider collider))
        {
            collider.enabled = false;
        }

        selectedDebugCellHighlight = highlight;
        selectedDebugCellHighlight.SetActive(false);

        GameObject outlineRoot = new("SelectedDebugCellOutline");
        outlineRoot.transform.SetParent(worldRoot, false);
        selectedDebugCellOutline = outlineRoot;
        selectedDebugCellOutline.SetActive(false);

        CreateDebugCellOutlineSegment(outlineRoot.transform, "North", new Vector3(0f, 0f, 0.49f), new Vector3(1.02f, 0.045f, 0.05f));
        CreateDebugCellOutlineSegment(outlineRoot.transform, "South", new Vector3(0f, 0f, -0.49f), new Vector3(1.02f, 0.045f, 0.05f));
        CreateDebugCellOutlineSegment(outlineRoot.transform, "East", new Vector3(0.49f, 0f, 0f), new Vector3(0.05f, 0.045f, 1.02f));
        CreateDebugCellOutlineSegment(outlineRoot.transform, "West", new Vector3(-0.49f, 0f, 0f), new Vector3(0.05f, 0.045f, 1.02f));

        cellQuickHud.CanvasRoot.SetActive(false);
    }

    private void CreateDebugCellOutlineSegment(Transform parent, string name, Vector3 localPosition, Vector3 localScale)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = name;
        segment.transform.SetParent(parent, false);
        segment.transform.localPosition = localPosition;
        segment.transform.localScale = localScale;
        ApplyColor(segment, new Color(0.95f, 0.96f, 1f));
        ConfigureStaticVisual(segment);
        if (segment.TryGetComponent(out Collider collider))
        {
            collider.enabled = false;
        }
    }

    private void UpdateCellQuickHud()
    {
        if (cellQuickHud == null)
        {
            return;
        }

        bool shouldShow = selectedDebugCell.HasValue && !isMainMenuOpen;
        if (cellQuickHud.CanvasRoot.activeSelf != shouldShow)
        {
            cellQuickHud.CanvasRoot.SetActive(shouldShow);
        }

        if (selectedDebugCellHighlight != null)
        {
            selectedDebugCellHighlight.SetActive(shouldShow);
        }

        if (selectedDebugCellOutline != null)
        {
            selectedDebugCellOutline.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            return;
        }

        Vector2Int cell = selectedDebugCell.Value;
        if (selectedDebugCellHighlight != null)
        {
            float y = (waterCells.Contains(cell) ? GetCurrentVisualWaterHeight(cell) : SampleTerrainHeight(cell.x + 0.5f, cell.y + 0.5f)) + 0.06f;
            selectedDebugCellHighlight.transform.position = new Vector3(cell.x + 0.5f, y, cell.y + 0.5f);
        }

        if (selectedDebugCellOutline != null)
        {
            float outlineY = (waterCells.Contains(cell) ? GetCurrentVisualWaterHeight(cell) : SampleTerrainHeight(cell.x + 0.5f, cell.y + 0.5f)) + 0.085f;
            selectedDebugCellOutline.transform.position = new Vector3(cell.x + 0.5f, outlineY, cell.y + 0.5f);
        }

        cellQuickHud.HeaderText.text = $"Cell {cell.x};{cell.y}";
        cellQuickHud.StatusText.text = GetCellQuickHudStatus(cell);
        cellQuickHud.DetailsText.text = GetCellQuickHudDetails(cell);
        LocalizeCanvas(cellQuickHud.CanvasRoot);
    }

    private void SelectDebugCell(Vector2Int cell)
    {
        if (!IsInsideGrid(cell))
        {
            ClearSelectedDebugCell();
            return;
        }

        selectedDebugCell = cell;
    }

    private void ClearSelectedDebugCell()
    {
        selectedDebugCell = null;
        if (selectedDebugCellHighlight != null)
        {
            selectedDebugCellHighlight.SetActive(false);
        }

        if (selectedDebugCellOutline != null)
        {
            selectedDebugCellOutline.SetActive(false);
        }

        if (cellQuickHud?.CanvasRoot != null)
        {
            cellQuickHud.CanvasRoot.SetActive(false);
        }
    }

    private string GetCellQuickHudStatus(Vector2Int cell)
    {
        if (waterCells.Contains(cell))
        {
            return "Water";
        }

        if (edgeHighwayCells.Contains(cell))
        {
            return "Edge Highway";
        }

        if (roadCells.Contains(cell))
        {
            return "Road";
        }

        if (IsAnchorCell(cell))
        {
            return "Location Anchor";
        }

        LocationType? locationType = GetContainingLocation(cell);
        if (locationType.HasValue)
        {
            return $"{locations[locationType.Value].Label} Footprint";
        }

        return IsGrassGroundCell(cell.x, cell.y) ? "Grass Ground" : "Ground";
    }

    private string GetCellQuickHudDetails(Vector2Int cell)
    {
        int shoreRow = GridHeight - WaterRiverWidth;
        int beachNearRow = shoreRow - 1;
        int beachFarRow = shoreRow - 2;
        string shoreLabel =
            waterCells.Contains(cell) ? "Water" :
            cell.y == beachNearRow ? "Beach Near" :
            cell.y == beachFarRow ? "Beach Far" :
            "No";

        LocationType? containingLocation = GetContainingLocation(cell);

        string truckLabel = "None";
        for (int i = 0; i < truckAgents.Count; i++)
        {
            if (truckAgents[i] != null && truckAgents[i].TruckCell == cell)
            {
                truckLabel = truckAgents[i].DisplayName;
                break;
            }
        }

        string driverLabel = "None";
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver?.DriverObject == null || !driver.DriverObject.activeSelf)
            {
                continue;
            }

            if (WorldToCell(driver.DriverObject.transform.position) == cell)
            {
                driverLabel = driver.DriverName;
                break;
            }
        }

        string movementLabel = roadCells.Contains(cell)
            ? "Road"
            : edgeHighwayCells.Contains(cell)
                ? "Edge highway"
                : waterCells.Contains(cell)
                    ? "Water"
                    : IsGrassGroundCell(cell.x, cell.y)
                        ? "Grass"
                        : "Ground";

        string occupantLabel = truckLabel != "None" && driverLabel != "None"
            ? $"{truckLabel}, {driverLabel}"
            : truckLabel != "None"
                ? truckLabel
                : driverLabel != "None"
                    ? driverLabel
                    : "None";

        string locationLabel = containingLocation.HasValue ? locations[containingLocation.Value].Label : "None";

        string heightLine = waterCells.Contains(cell)
            ? $"Height: {SampleTerrainHeight(cell.x + 0.5f, cell.y + 0.5f):0.00}\nVisual Water Height: {GetCurrentVisualWaterHeight(cell):0.00}"
            : $"Height: {SampleTerrainHeight(cell.x + 0.5f, cell.y + 0.5f):0.00}";

        return
            $"Surface: {movementLabel}\n" +
            $"Shore: {shoreLabel}\n" +
            $"Location: {locationLabel}" + (IsAnchorCell(cell) ? " (Anchor)" : string.Empty) + "\n" +
            $"Occupant: {occupantLabel}\n" +
            $"Misc: {(miscOccupiedCells.Contains(cell) ? "Yes" : "No")}\n" +
            heightLine;
    }
}

