using System;
using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Component;
using Godot;


namespace Game.Manager;

public partial class GridManager : Node
{
    private HashSet<Vector2I> validBuildableTiels = new();
    [Export]
    private TileMapLayer highlightTilemapLayer;

    [Export]
    private TileMapLayer baseTerrainTilemapLayer;

    private List<TileMapLayer> allTilemapLayers = new();


    public override void _Ready()
    {
        GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
        allTilemapLayers = GetAllTilemapLayers(baseTerrainTilemapLayer);

        foreach (var layer in allTilemapLayers)
        {
            GD.Print(layer.Name);
        }
    }


    public Boolean IsTilePositionValid(Vector2I tilePosition)
    {

        foreach (var layer in allTilemapLayers)
        {

            var customData = layer.GetCellTileData(tilePosition);
            if (customData == null) continue;
            return (bool)customData.GetCustomData("buildable");
        }


        return false;

    }

    public Boolean IsTilePositionBuildable(Vector2I tilePosition)
    {
        return validBuildableTiels.Contains(tilePosition);
    }

    public void HighlightBuildableTiles()
    {
        foreach (var tilePosition in validBuildableTiels)
        {
            highlightTilemapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
        }

    }

    public void HighlightExpandedBuildableTiles(Vector2I rootCell, int radius)
    {
        ClearHighLightedTiles();
        HighlightBuildableTiles();

        var validTiles = GetValidTilesInRadus(rootCell, radius).ToHashSet();
        var expandedTiles = validTiles.Except(validBuildableTiels).Except(GetOccupiedTiles());
        var atlasCoords = new Vector2I(1, 0);

        foreach (var tilePosition in expandedTiles)
        {
            highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords);
        }


    }


    public void ClearHighLightedTiles()
    {
        highlightTilemapLayer.Clear();
    }

    public Vector2I GetMouseGridCellPosition()
    {
        var mousePosition = highlightTilemapLayer.GetGlobalMousePosition();

        var gridPosition = mousePosition / 64;
        gridPosition = gridPosition.Floor();

        return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
    }

    private List<TileMapLayer> GetAllTilemapLayers(TileMapLayer rootTilemapLayer)
    {
        var result = new List<TileMapLayer>();
        foreach (var child in rootTilemapLayer.GetChildren())
        {
            if (child is TileMapLayer childLayer)
            {
                result.AddRange(GetAllTilemapLayers(childLayer));
            }

        }
        result.Add(rootTilemapLayer);
        return result;
    }


    private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
    {
        var rootCell = buildingComponent.GetGridCellPosition();
        var radius = buildingComponent.BuildableRadius;

        var validTiles = GetValidTilesInRadus(rootCell, buildingComponent.BuildableRadius);
        validBuildableTiels.UnionWith(validTiles);
        validBuildableTiels.ExceptWith(GetOccupiedTiles());

    }

    private List<Vector2I> GetValidTilesInRadus(Vector2I rootCell, int radius)
    {
        var result = new List<Vector2I>();
        for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
        {
            for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
            {

                var tilePosition = new Vector2I(x, y);
                if (!IsTilePositionValid(tilePosition)) continue;
                result.Add(tilePosition);
            }
        }

        return result;

    }

    private IEnumerable<Vector2I> GetOccupiedTiles()
    {
        var buildingComponents = GetTree().GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>();
        var occupedTiles = buildingComponents.Select(x => x.GetGridCellPosition());

        return occupedTiles;
    }

    private void OnBuildingPlaced(BuildingComponent buildingComponent)
    {
        UpdateValidBuildableTiles(buildingComponent);
    }


}
