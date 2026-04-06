using System.Collections.Generic;
using UnityEngine;

public static class SelectionVisualService
{
    public static GameObject CreateHighlight(Transform parent, string label, System.Action<GameObject, Color> applyColor, System.Action<GameObject> configureVisual)
    {
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        highlight.name = $"{label}_SelectionHighlight";
        highlight.transform.SetParent(parent, false);
        highlight.GetComponent<Collider>().enabled = false;
        applyColor(highlight, new Color(1f, 0.86f, 0.28f));
        configureVisual(highlight);
        highlight.SetActive(false);
        return highlight;
    }

    public static GameObject CreateLabelRoot(Transform parent, out TextMesh labelText, List<TextMesh> outlines)
    {
        GameObject labelRoot = new("SelectedLocationLabel");
        labelRoot.transform.SetParent(parent, false);

        labelText = labelRoot.AddComponent<TextMesh>();
        labelText.characterSize = 0.16f;
        labelText.fontSize = 48;
        labelText.anchor = TextAnchor.MiddleCenter;
        labelText.alignment = TextAlignment.Center;
        labelText.color = new Color(0.98f, 0.95f, 0.82f);

        Vector3[] outlineOffsets =
        {
            new(-0.045f, 0f, 0f),
            new(0.045f, 0f, 0f),
            new(0f, 0.045f, 0f),
            new(0f, -0.045f, 0f)
        };

        foreach (Vector3 outlineOffset in outlineOffsets)
        {
            GameObject outlineObject = new("Outline");
            outlineObject.transform.SetParent(labelRoot.transform, false);
            outlineObject.transform.localPosition = outlineOffset;
            TextMesh outlineText = outlineObject.AddComponent<TextMesh>();
            outlineText.characterSize = labelText.characterSize;
            outlineText.fontSize = labelText.fontSize;
            outlineText.anchor = TextAnchor.MiddleCenter;
            outlineText.alignment = TextAlignment.Center;
            outlineText.color = new Color(0.08f, 0.08f, 0.09f);
            outlines.Add(outlineText);
        }

        labelRoot.SetActive(false);
        return labelRoot;
    }

    public static void UpdateLabelVisual(
        GameObject labelRoot,
        TextMesh labelText,
        List<TextMesh> outlines,
        string label,
        Vector3 labelPosition,
        Vector3 cameraPosition,
        float fadeStartDistance,
        float fadeEndDistance)
    {
        labelText.text = label;
        foreach (TextMesh outlineText in outlines)
        {
            outlineText.text = label;
        }

        labelRoot.transform.position = labelPosition;

        Vector3 facingDirection = labelPosition - cameraPosition;
        facingDirection.y = 0f;
        if (facingDirection.sqrMagnitude < 0.0001f)
        {
            facingDirection = Vector3.forward;
        }

        labelRoot.transform.rotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up);

        float distance = Vector3.Distance(cameraPosition, labelPosition);
        float fadeAlpha = 1f - Mathf.Clamp01((distance - fadeStartDistance) / Mathf.Max(0.01f, fadeEndDistance - fadeStartDistance));
        if (fadeAlpha <= 0.01f)
        {
            labelRoot.SetActive(false);
            return;
        }

        labelText.color = new Color(0.98f, 0.95f, 0.82f, fadeAlpha);
        foreach (TextMesh outlineText in outlines)
        {
            outlineText.color = new Color(0.08f, 0.08f, 0.09f, fadeAlpha);
        }

        labelRoot.SetActive(true);
    }
}
