using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void CreateShiftsDriverRows(RectTransform contentRect, Font font)
    {
        for (int i = 0; i < MaxShiftDriverSlots; i++)
        {
            ShiftDriverRowUi row = new();
            GameObject rowObj = CreateUiObject($"ShiftDriverRow{i + 1}", contentRect);
            row.Root = rowObj.GetComponent<RectTransform>();
            row.Root.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
            row.Background = rowObj.AddComponent<Image>();
            row.Background.color = ShiftsCardColor;
            Outline rowOutline = rowObj.AddComponent<Outline>();
            rowOutline.effectColor = new Color(0f, 0f, 0f, 0.2f);
            rowOutline.effectDistance = new Vector2(1f, -1f);
            GameObject selectedBorderObj = CreateUiObject($"VacancySelectedBorder{i + 1}", rowObj.transform);
            row.SelectedBorder = selectedBorderObj.AddComponent<Image>();
            row.SelectedBorder.color = FleetAccentColor;
            row.SelectedBorder.raycastTarget = false;
            RectTransform selectedBorderRt = selectedBorderObj.GetComponent<RectTransform>();
            selectedBorderRt.anchorMin = new Vector2(0f, 0f);
            selectedBorderRt.anchorMax = new Vector2(0f, 1f);
            selectedBorderRt.pivot = new Vector2(0f, 0.5f);
            selectedBorderRt.anchoredPosition = Vector2.zero;
            selectedBorderRt.sizeDelta = new Vector2(4f, 0f);
            selectedBorderObj.AddComponent<LayoutElement>().ignoreLayout = true;
            VerticalLayoutGroup rowLayout = rowObj.AddComponent<VerticalLayoutGroup>();
            rowLayout.padding = new RectOffset(12, 12, 10, 10);
            rowLayout.spacing = 4;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            RectTransform workerHeaderRow = CreateLayoutRow($"ShiftDriverHeaderRow{i + 1}", row.Root, 24f, 8f);
            row.NameText = CreateHeaderText($"ShiftDriverName{i + 1}", workerHeaderRow, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
            row.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            RectTransform badgeRoot = CreateStyledPanel($"ShiftDriverBadge{i + 1}", workerHeaderRow, new Color(0.12f, 0.18f, 0.14f, 0.95f));
            row.BadgeBackground = badgeRoot.GetComponent<Image>();
            LayoutElement professionLayout = badgeRoot.gameObject.AddComponent<LayoutElement>();
            professionLayout.preferredWidth = 92f;
            professionLayout.preferredHeight = 20f;
            professionLayout.flexibleWidth = 0f;
            row.ProfessionText = CreateBodyText($"ShiftDriverProfession{i + 1}", badgeRoot, font, string.Empty, 11, TextAnchor.MiddleCenter, Color.white);
            StretchRect(row.ProfessionText.GetComponent<RectTransform>(), 6f, 2f, 6f, 2f);
            row.StatusText = CreateBodyText($"ShiftDriverStatus{i + 1}", rowObj.transform, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);

            row.SelectButton = rowObj.AddComponent<Button>();
            ColorBlock rowColors = row.SelectButton.colors;
            rowColors.normalColor = Color.white;
            rowColors.highlightedColor = new Color(1.16f, 1.13f, 1.05f, 1f);
            rowColors.pressedColor = new Color(0.88f, 0.82f, 0.68f, 1f);
            rowColors.selectedColor = new Color(1.08f, 1.02f, 0.86f, 1f);
            rowColors.fadeDuration = 0.08f;
            row.SelectButton.colors = rowColors;

            int rowIndex = i;
            row.SelectButton.onClick.AddListener(() =>
            {
                if (rowIndex >= vacancyViewModels.Count)
                {
                    return;
                }

                bool isSelected = selectedVacancyIndex == rowIndex;
                selectedVacancyIndex = isSelected ? -1 : rowIndex;
                selectedVacancyShiftIndex = -1;
                selectedVacancyTruckNumber = 0;
                vacancySuccessMessage = string.Empty;
                vacancySuccessTimer = 0f;
                string vacancyTitle = vacancyViewModels[rowIndex].Title;
                LogUiInput($"Vacancies Canvas: {(isSelected ? $"deselected {vacancyTitle}" : $"selected {vacancyTitle}")}");
                PlayUiSound(uiSelectClip, 0.8f);
                isShiftsScreenDirty = true;
                isDriversScreenDirty = true;
            });

            shiftsScreenUi.DriverRows.Add(row);
        }

        // ── Right panel ─────────────────────────────────────────────────────
    }
}
