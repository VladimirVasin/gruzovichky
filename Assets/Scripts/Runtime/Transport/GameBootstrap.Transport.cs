using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private const float BuildingDecorScale = 1.56f;
    private const float LargePropScale = 1.40f;


    private Vector3 GetCellCenter(Vector2Int cell)
    {
        return new Vector3(cell.x + 0.5f, GetTerrainHeight(cell), cell.y + 0.5f);
    }

    private Vector3 GetTruckWorldPosition(Vector2Int cell)
    {
        return GetCellCenter(cell) + new Vector3(0f, TruckSegmentStartLift, 0f);
    }

    private static Vector2Int WorldToCell(Vector3 point)
    {
        return new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.z));
    }

    private float GetTerrainHeight(Vector2Int cell)
    {
        if (!IsInsideGrid(cell))
        {
            return 0f;
        }

        return terrainHeights[cell.x, cell.y];
    }

    private float GetLocationBaseHeight(LocationType locationType)
    {
        if (!locations.TryGetValue(locationType, out LocationData location))
        {
            return 0f;
        }

        float total = 0f;
        int count = 0;
        for (int x = location.Min.x; x <= location.Max.x; x++)
        {
            for (int y = location.Min.y; y <= location.Max.y; y++)
            {
                total += terrainHeights[x, y];
                count++;
            }
        }

        total += terrainHeights[location.Anchor.x, location.Anchor.y];
        count++;
        return total / Mathf.Max(1, count);
    }

    private float SampleTerrainHeight(float worldX, float worldZ)
    {
        float clampedX = Mathf.Clamp(worldX - 0.5f, 0f, GridWidth - 1.001f);
        float clampedZ = Mathf.Clamp(worldZ - 0.5f, 0f, GridHeight - 1.001f);
        int x0 = Mathf.Clamp(Mathf.FloorToInt(clampedX), 0, GridWidth - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(clampedZ), 0, GridHeight - 1);
        int x1 = Mathf.Min(GridWidth - 1, x0 + 1);
        int z1 = Mathf.Min(GridHeight - 1, z0 + 1);
        float tx = clampedX - x0;
        float tz = clampedZ - z0;
        float h00 = terrainHeights[x0, z0];
        float h10 = terrainHeights[x1, z0];
        float h01 = terrainHeights[x0, z1];
        float h11 = terrainHeights[x1, z1];
        float hx0 = Mathf.Lerp(h00, h10, tx);
        float hx1 = Mathf.Lerp(h01, h11, tx);
        return Mathf.Lerp(hx0, hx1, tz);
    }

    private float GetVerticalEdgeHeight(int x, int y)
    {
        if (x <= 0)
        {
            return terrainHeights[0, Mathf.Clamp(y, 0, GridHeight - 1)];
        }

        if (x >= GridWidth)
        {
            return terrainHeights[GridWidth - 1, Mathf.Clamp(y, 0, GridHeight - 1)];
        }

        int clampedY = Mathf.Clamp(y, 0, GridHeight - 1);
        return (terrainHeights[x - 1, clampedY] + terrainHeights[x, clampedY]) * 0.5f;
    }

    private float GetHorizontalEdgeHeight(int x, int y)
    {
        if (y <= 0)
        {
            return terrainHeights[Mathf.Clamp(x, 0, GridWidth - 1), 0];
        }

        if (y >= GridHeight)
        {
            return terrainHeights[Mathf.Clamp(x, 0, GridWidth - 1), GridHeight - 1];
        }

        int clampedX = Mathf.Clamp(x, 0, GridWidth - 1);
        return (terrainHeights[clampedX, y - 1] + terrainHeights[clampedX, y]) * 0.5f;
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Shader urpLit = ShaderRefs.Lit;
        Material material = new(urpLit);
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.14f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        renderer.material = material;
    }

    private static void ApplyUnlitColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Shader shader = ShaderRefs.Unlit ?? ShaderRefs.Sprites;

        Material material = new(shader)
        {
            color = color
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        renderer.material = material;
    }

    private static void ConfigureStaticVisual(GameObject target)
    {
        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    private static void ConfigureShadowVisual(GameObject target)
    {
        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    private bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < GridWidth && cell.y >= 0 && cell.y < GridHeight;
    }
}

