using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const float SocialGraphPanelAnimationDuration = 0.16f;
    private const float SocialGraphElementSpawnDuration = 0.28f;

    private readonly List<SocialGraphAnimatedNodeView> socialGraphAnimatedNodes = new();
    private readonly List<SocialGraphAnimatedEdgeView> socialGraphAnimatedEdges = new();
    private readonly Dictionary<int, SocialGraphAnimatedNodeView> socialGraphAnimatedNodeById = new();
    private DriverAgent socialGraphCurrentSelectedWorker;
    private List<SocialRelationViewModel> socialGraphCurrentVisibleRelations = new();
    private SocialGraphStats socialGraphCurrentStats = new();
    private bool socialGraphCurrentRu;
    private bool isSocialGraphPanelVisibilityAnimating;
    private bool isSocialGraphPanelClosingAnimation;
    private float socialGraphPanelAnimationStartedAt;
    private float socialGraphPanelAnimationFromAlpha;
    private float socialGraphPanelAnimationToAlpha = 1f;
    private float socialGraphRebuildAnimationStartedAt;

    private sealed class SocialGraphAnimatedNodeView
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public int WorkerId;
        public Vector2 BasePosition;
        public Vector2 AnimatedPosition;
        public bool IsSelected;
        public float Phase;
        public float SpawnDelay;
    }

    private sealed class SocialGraphAnimatedEdgeView
    {
        public RectTransform Rect;
        public Image Image;
        public long EdgeKey;
        public int FocusWorkerId;
        public int OtherWorkerId;
        public Color BaseColor;
        public float BaseWidth;
        public float SpawnDelay;
    }

    private void BeginSocialGraphPanelVisibilityAnimation(bool opening)
    {
        if (socialGraphScreenUi?.CanvasGroup == null)
        {
            return;
        }

        isSocialGraphPanelVisibilityAnimating = true;
        isSocialGraphPanelClosingAnimation = !opening;
        socialGraphPanelAnimationStartedAt = Time.unscaledTime;
        socialGraphPanelAnimationFromAlpha = socialGraphScreenUi.CanvasGroup.alpha;
        socialGraphPanelAnimationToAlpha = opening ? 1f : 0f;
        socialGraphScreenUi.CanvasGroup.blocksRaycasts = opening;
        socialGraphScreenUi.CanvasGroup.interactable = opening;
        if (opening && socialGraphPanelAnimationFromAlpha <= 0.01f)
        {
            socialGraphScreenUi.WindowRoot.localScale = Vector3.one * 0.985f;
        }
    }

    private bool IsSocialGraphPanelClosingAnimationActive()
    {
        return isSocialGraphPanelVisibilityAnimating && isSocialGraphPanelClosingAnimation;
    }

    private void PrepareSocialGraphAnimatedRebuild()
    {
        socialGraphAnimatedNodes.Clear();
        socialGraphAnimatedEdges.Clear();
        socialGraphAnimatedNodeById.Clear();
        socialGraphRebuildAnimationStartedAt = Time.unscaledTime;
    }

    private void RegisterSocialGraphAnimatedNode(DriverAgent worker, RectTransform rect, bool selected, SocialRelationViewModel relation)
    {
        if (worker == null || rect == null)
        {
            return;
        }

        CanvasGroup group = rect.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;
        group.interactable = true;

        SocialGraphAnimatedNodeView view = new()
        {
            Rect = rect,
            Group = group,
            WorkerId = worker.DriverId,
            BasePosition = rect.anchoredPosition,
            AnimatedPosition = rect.anchoredPosition,
            IsSelected = selected,
            Phase = worker.DriverId * 0.71f + (relation?.Importance ?? 0.35f) * 2.3f,
            SpawnDelay = Mathf.Min(0.16f, socialGraphAnimatedNodes.Count * 0.018f)
        };

        socialGraphAnimatedNodes.Add(view);
        socialGraphAnimatedNodeById[worker.DriverId] = view;
    }

    private void RegisterSocialGraphAnimatedEdge(SocialRelationViewModel relation, RectTransform rect, Image image)
    {
        if (relation == null || rect == null || image == null)
        {
            return;
        }

        Color color = image.color;
        SocialGraphAnimatedEdgeView view = new()
        {
            Rect = rect,
            Image = image,
            EdgeKey = relation.EdgeKey,
            FocusWorkerId = relation.FocusWorkerId,
            OtherWorkerId = relation.OtherWorkerId,
            BaseColor = color,
            BaseWidth = rect.sizeDelta.y,
            SpawnDelay = Mathf.Min(0.18f, socialGraphAnimatedEdges.Count * 0.012f)
        };

        color.a = 0f;
        image.color = color;
        socialGraphAnimatedEdges.Add(view);
    }

    private void RememberSocialGraphCurrentView(
        DriverAgent selected,
        List<SocialRelationViewModel> visibleRelations,
        SocialGraphStats stats,
        bool ru)
    {
        socialGraphCurrentSelectedWorker = selected;
        socialGraphCurrentVisibleRelations = visibleRelations ?? new List<SocialRelationViewModel>();
        socialGraphCurrentStats = stats ?? new SocialGraphStats();
        socialGraphCurrentRu = ru;
    }

    private void RefreshSocialGraphHoverState()
    {
        if (socialGraphScreenUi == null || !socialGraphScreenUi.CanvasRoot.activeSelf)
        {
            return;
        }

        UpdateSocialGraphInspector(
            socialGraphCurrentSelectedWorker,
            socialGraphCurrentVisibleRelations,
            socialGraphCurrentStats,
            socialGraphCurrentRu);
    }

    private void UpdateSocialGraphAnimations()
    {
        UpdateSocialGraphPanelVisibilityAnimation();
        UpdateSocialGraphElementAnimations();
    }

    private void UpdateSocialGraphPanelVisibilityAnimation()
    {
        if (socialGraphScreenUi?.CanvasGroup == null || !socialGraphScreenUi.CanvasRoot.activeSelf)
        {
            return;
        }

        if (!isSocialGraphPanelVisibilityAnimating)
        {
            socialGraphScreenUi.CanvasGroup.alpha = isSocialGraphPanelOpen ? 1f : 0f;
            socialGraphScreenUi.WindowRoot.localScale = Vector3.one;
            return;
        }

        float progress = Mathf.Clamp01((Time.unscaledTime - socialGraphPanelAnimationStartedAt) / SocialGraphPanelAnimationDuration);
        float eased = EaseSocialGraphOutCubic(progress);
        float alpha = Mathf.Lerp(socialGraphPanelAnimationFromAlpha, socialGraphPanelAnimationToAlpha, eased);
        socialGraphScreenUi.CanvasGroup.alpha = alpha;
        socialGraphScreenUi.WindowRoot.localScale = Vector3.one * Mathf.Lerp(0.985f, 1f, alpha);

        if (progress < 1f)
        {
            return;
        }

        isSocialGraphPanelVisibilityAnimating = false;
        if (isSocialGraphPanelClosingAnimation)
        {
            socialGraphScreenUi.CanvasRoot.SetActive(false);
            socialGraphScreenUi.WindowRoot.localScale = Vector3.one;
            PrepareSocialGraphAnimatedRebuild();
        }
        else
        {
            socialGraphScreenUi.CanvasGroup.alpha = 1f;
            socialGraphScreenUi.CanvasGroup.blocksRaycasts = true;
            socialGraphScreenUi.CanvasGroup.interactable = true;
            socialGraphScreenUi.WindowRoot.localScale = Vector3.one;
        }
    }

    private void UpdateSocialGraphElementAnimations()
    {
        if (socialGraphScreenUi == null || !socialGraphScreenUi.CanvasRoot.activeSelf)
        {
            return;
        }

        float now = Time.unscaledTime;
        for (int i = 0; i < socialGraphAnimatedNodes.Count; i++)
        {
            SocialGraphAnimatedNodeView node = socialGraphAnimatedNodes[i];
            if (node.Rect == null || node.Group == null)
            {
                continue;
            }

            float spawn = GetSocialGraphSpawnProgress(now, node.SpawnDelay);
            bool hovered = hoveredSocialGraphWorkerId == node.WorkerId;
            bool highlighted = IsSocialGraphNodeHighlighted(node.WorkerId);
            bool dimmed = HasSocialGraphHover() && !highlighted;
            node.AnimatedPosition = CalculateSocialGraphAnimatedNodePosition(node, now);
            node.Rect.anchoredPosition = node.AnimatedPosition;
            node.Group.alpha = spawn * (dimmed ? 0.42f : 1f);

            float hoverScale = hovered ? 1.075f : 1f;
            float pulseScale = node.IsSelected ? 1f + Mathf.Sin(now * 2.5f + node.Phase) * 0.014f : 1f;
            float wobbleScale = 1f + Mathf.Sin(now * 1.35f + node.Phase) * 0.01f;
            node.Rect.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, spawn) * hoverScale * pulseScale * wobbleScale;
            float rotation = Mathf.Sin(now * 1.05f + node.Phase) * (node.IsSelected ? 0.55f : 1.35f);
            node.Rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        for (int i = 0; i < socialGraphAnimatedEdges.Count; i++)
        {
            UpdateSocialGraphAnimatedEdge(socialGraphAnimatedEdges[i], now);
        }
    }

    private void UpdateSocialGraphAnimatedEdge(SocialGraphAnimatedEdgeView edge, float now)
    {
        if (edge.Rect == null || edge.Image == null ||
            !socialGraphAnimatedNodeById.TryGetValue(edge.FocusWorkerId, out SocialGraphAnimatedNodeView a) ||
            !socialGraphAnimatedNodeById.TryGetValue(edge.OtherWorkerId, out SocialGraphAnimatedNodeView b))
        {
            return;
        }

        Vector2 delta = b.AnimatedPosition - a.AnimatedPosition;
        edge.Rect.anchoredPosition = (a.AnimatedPosition + b.AnimatedPosition) * 0.5f;
        bool highlighted = IsSocialGraphEdgeHighlighted(edge);
        float width = edge.BaseWidth * (highlighted && HasSocialGraphHover() ? 1.18f : 1f);
        edge.Rect.sizeDelta = new Vector2(delta.magnitude, width);
        edge.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        float spawn = GetSocialGraphSpawnProgress(now, edge.SpawnDelay);
        bool dimmed = HasSocialGraphHover() && !highlighted;
        Color color = edge.BaseColor;
        color.a = Mathf.Clamp01(edge.BaseColor.a * spawn * (dimmed ? 0.26f : highlighted && HasSocialGraphHover() ? 1.22f : 1f));
        edge.Image.color = color;
    }

    private Vector2 CalculateSocialGraphAnimatedNodePosition(SocialGraphAnimatedNodeView node, float now)
    {
        Vector2 position = node.BasePosition;
        if (selectedSocialGraphWorkerId > 0 && !node.IsSelected && position.sqrMagnitude > 16f)
        {
            float orbitAngle = Mathf.Sin(now * 0.42f + node.Phase) * 3.1f;
            position = RotateSocialGraphVector(position, orbitAngle);
        }

        float wobble = node.IsSelected ? 1.25f : 2.6f;
        Vector2 offset = new(
            Mathf.Sin(now * 0.95f + node.Phase) * wobble,
            Mathf.Cos(now * 1.12f + node.Phase * 0.67f) * wobble * 0.72f);
        return position + offset;
    }

    private bool HasSocialGraphHover()
    {
        return hoveredSocialGraphWorkerId > 0 || hoveredSocialGraphEdgeKey != 0;
    }

    private bool IsSocialGraphNodeHighlighted(int workerId)
    {
        if (!HasSocialGraphHover())
        {
            return true;
        }

        if (selectedSocialGraphWorkerId > 0 && workerId == selectedSocialGraphWorkerId)
        {
            return true;
        }

        if (hoveredSocialGraphWorkerId > 0)
        {
            if (workerId == hoveredSocialGraphWorkerId)
            {
                return true;
            }

            for (int i = 0; i < socialGraphAnimatedEdges.Count; i++)
            {
                SocialGraphAnimatedEdgeView edge = socialGraphAnimatedEdges[i];
                bool touchesHovered = edge.FocusWorkerId == hoveredSocialGraphWorkerId || edge.OtherWorkerId == hoveredSocialGraphWorkerId;
                bool touchesWorker = edge.FocusWorkerId == workerId || edge.OtherWorkerId == workerId;
                if (touchesHovered && touchesWorker)
                {
                    return true;
                }
            }
        }

        if (hoveredSocialGraphEdgeKey != 0)
        {
            for (int i = 0; i < socialGraphAnimatedEdges.Count; i++)
            {
                SocialGraphAnimatedEdgeView edge = socialGraphAnimatedEdges[i];
                if (edge.EdgeKey == hoveredSocialGraphEdgeKey &&
                    (edge.FocusWorkerId == workerId || edge.OtherWorkerId == workerId))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsSocialGraphEdgeHighlighted(SocialGraphAnimatedEdgeView edge)
    {
        if (!HasSocialGraphHover())
        {
            return true;
        }

        if (hoveredSocialGraphEdgeKey != 0)
        {
            return edge.EdgeKey == hoveredSocialGraphEdgeKey;
        }

        return hoveredSocialGraphWorkerId > 0 &&
               (edge.FocusWorkerId == hoveredSocialGraphWorkerId || edge.OtherWorkerId == hoveredSocialGraphWorkerId);
    }

    private float GetSocialGraphSpawnProgress(float now, float delay)
    {
        float elapsed = now - socialGraphRebuildAnimationStartedAt - delay;
        return EaseSocialGraphOutCubic(Mathf.Clamp01(elapsed / SocialGraphElementSpawnDuration));
    }

    private static float EaseSocialGraphOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private static Vector2 RotateSocialGraphVector(Vector2 value, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }
}
