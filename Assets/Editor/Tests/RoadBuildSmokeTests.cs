using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class RoadBuildSmokeTests
{
    private const int GridSize = 32;

    [Test]
    public void RoadSegmentBuildService_BuildsExpectedLShapeWhenNotAxisLocked()
    {
        List<Vector2Int> path = RoadSegmentBuildService.BuildManhattanSegment(
            new Vector2Int(2, 2),
            new Vector2Int(5, 4),
            constrainToDominantAxis: false);

        Assert.That(path, Is.EqualTo(new[]
        {
            new Vector2Int(2, 2),
            new Vector2Int(3, 2),
            new Vector2Int(4, 2),
            new Vector2Int(5, 2),
            new Vector2Int(5, 3),
            new Vector2Int(5, 4)
        }));
    }

    [Test]
    public void RoadSegmentBuildService_DominantAxisModeDoesNotTurnCorner()
    {
        List<Vector2Int> path = RoadSegmentBuildService.BuildManhattanSegment(
            new Vector2Int(2, 2),
            new Vector2Int(5, 4),
            constrainToDominantAxis: true);

        Assert.That(path, Is.EqualTo(new[]
        {
            new Vector2Int(2, 2),
            new Vector2Int(3, 2),
            new Vector2Int(4, 2),
            new Vector2Int(5, 2)
        }));
    }

    [Test]
    public void RoadBuildPlacementService_TurnPreviewMaintainsTwoLaneContinuity()
    {
        List<Vector2Int> path = RoadSegmentBuildService.BuildManhattanSegment(
            new Vector2Int(10, 10),
            new Vector2Int(12, 12),
            constrainToDominantAxis: false);

        HashSet<Vector2Int> previewCells = new();
        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int direction = i < path.Count - 1
                ? path[i + 1] - path[i]
                : path[i] - path[i - 1];

            RoadFootprintResolveResult result = RoadBuildPlacementService.ResolveFootprintOffset(
                path[i],
                direction,
                requireNewRoadCell: true,
                GridSize,
                GridSize,
                previewCells,
                edgeHighwayCells: null,
                miscOccupiedCells: null,
                isBlockedLocationCell: _ => false);

            Assert.That(result.CanPlace, Is.True, $"path index {i}");
            previewCells.Add(path[i]);
            previewCells.Add(path[i] + result.WidthOffset);
        }

        Assert.That(previewCells.Contains(new Vector2Int(10, 9)), Is.True);
        Assert.That(previewCells.Contains(new Vector2Int(11, 9)), Is.True);
        Assert.That(previewCells.Contains(new Vector2Int(12, 10)), Is.True);
        Assert.That(previewCells.Contains(new Vector2Int(13, 11)), Is.True);
        Assert.That(previewCells.Contains(new Vector2Int(13, 12)), Is.True);
    }

    [Test]
    public void RoadBuildPlacementService_BlocksEdgeHighwayAndLocationFootprints()
    {
        HashSet<Vector2Int> highway = new() { new Vector2Int(4, 4) };

        RoadFootprintResolveResult blockedByHighway = RoadBuildPlacementService.ResolveFootprintOffset(
            new Vector2Int(4, 4),
            Vector2Int.right,
            requireNewRoadCell: true,
            GridSize,
            GridSize,
            roadCells: null,
            edgeHighwayCells: highway,
            miscOccupiedCells: null,
            isBlockedLocationCell: _ => false);

        Assert.That(blockedByHighway.CanPlace, Is.False);

        RoadFootprintResolveResult blockedByLocationSideCell = RoadBuildPlacementService.ResolveFootprintOffset(
            new Vector2Int(5, 5),
            Vector2Int.right,
            requireNewRoadCell: true,
            GridSize,
            GridSize,
            roadCells: null,
            edgeHighwayCells: null,
            miscOccupiedCells: null,
            isBlockedLocationCell: cell => cell == new Vector2Int(5, 4));

        Assert.That(blockedByLocationSideCell.CanPlace, Is.True);
        Assert.That(blockedByLocationSideCell.WidthOffset, Is.EqualTo(Vector2Int.up));
    }

    [Test]
    public void RoadBuildPlacementService_JunctionTurnDirectionsUseOnlyConnectedPerpendicularRoads()
    {
        HashSet<Vector2Int> roads = new()
        {
            new Vector2Int(62, 18),
            new Vector2Int(61, 18),
            new Vector2Int(62, 17),
            new Vector2Int(61, 17)
        };

        List<Vector2Int> directions = RoadBuildPlacementService.GetConnectedPerpendicularDirections(
            new Vector2Int(62, 18),
            Vector2Int.up,
            roads);

        Assert.That(directions, Is.EqualTo(new[] { Vector2Int.left }));
    }

    [Test]
    public void RoadBuildPlacementService_JunctionTurnDirectionsKeepRealTJunctions()
    {
        HashSet<Vector2Int> roads = new()
        {
            new Vector2Int(62, 18),
            new Vector2Int(61, 18),
            new Vector2Int(63, 18)
        };

        List<Vector2Int> directions = RoadBuildPlacementService.GetConnectedPerpendicularDirections(
            new Vector2Int(62, 18),
            Vector2Int.up,
            roads);

        Assert.That(directions, Is.EqualTo(new[] { Vector2Int.left, Vector2Int.right }));
    }

    [Test]
    public void RoadBuildPlacementService_TurnFillBoundsUseCapturedLaneOffsets()
    {
        RoadBuildPlacementService.GetTurnFillBoundsFromOffsets(
            new Vector2Int(31, 25),
            Vector2Int.up,
            new Vector2Int(31, 24),
            Vector2Int.left,
            out int minX,
            out int maxX,
            out int minY,
            out int maxY);

        Assert.That(minX, Is.EqualTo(30));
        Assert.That(maxX, Is.EqualTo(31));
        Assert.That(minY, Is.EqualTo(24));
        Assert.That(maxY, Is.EqualTo(26));
    }

    [Test]
    public void GridPathService_DoesNotRouteThroughBlockedWaterCells()
    {
        HashSet<Vector2Int> water = new()
        {
            new Vector2Int(1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(1, 2)
        };

        List<Vector2Int> path = GridPathService.FindPath(
            new Vector2Int(0, 1),
            new Vector2Int(2, 1),
            GridPathService.GetCardinalNeighbors,
            cell => cell.x >= 0 && cell.x <= 2 && cell.y >= 0 && cell.y <= 2 && !water.Contains(cell));

        Assert.That(path, Is.Null);
    }
}
