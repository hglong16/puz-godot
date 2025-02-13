using Game.Manager;
using Godot;
using Game.Resources.Building;

namespace Game;

public partial class Main : Node
{
    private GridManager gridManager;
    private Node2D ySortRoot;
    private Sprite2D cursor;

    private BuildingResource towerResource;
    private BuildingResource villageResource;

    private Button placeTowerButton;
    private Button placeVillageButton;

    private Vector2I? hoveredGridCell;
    private BuildingResource toPlaceBuildingResource;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        towerResource = GD.Load<BuildingResource>("res://resources/building/tower.tres");
        villageResource = GD.Load<BuildingResource>("res://resources/building/village.tres");

        gridManager = GetNode<GridManager>("GridManager");

        ySortRoot = GetNode<Node2D>("YSortRoot");
        cursor = GetNode<Sprite2D>("Cursor");

        placeTowerButton = GetNode<Button>("PlaceTowerButton");
        placeVillageButton = GetNode<Button>("PlaceVillageButton");



        placeTowerButton.Pressed += OnPlaceTowerButtonPressed;
        placeVillageButton.Pressed += OnPlaceVillageButtonPressed;
        gridManager.ResourceTileUpdated += OnResourceTileUpdated;

        cursor.Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (hoveredGridCell.HasValue && @event.IsActionPressed("left_click") && gridManager.IsTilePositionBuildable(hoveredGridCell.Value) && gridManager.IsTilePositionBuildable(hoveredGridCell.Value))
        {
            PlaceBuildingAtHoverCellPosition();
            cursor.Visible = false;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        var gridPosition = gridManager.GetMouseGridCellPosition();
        cursor.GlobalPosition = gridPosition * 64;
        if (toPlaceBuildingResource != null && cursor.Visible && (!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition))
        {
            hoveredGridCell = gridPosition;
            gridManager.ClearHighLightedTiles();
            gridManager.HighlightExpandedBuildableTiles(hoveredGridCell.Value, toPlaceBuildingResource.BuildableRadius);
            gridManager.HighlightResourceTiles(hoveredGridCell.Value, toPlaceBuildingResource.ResourceRadius);
        }
    }

    private void PlaceBuildingAtHoverCellPosition()
    {
        if (!hoveredGridCell.HasValue) return;

        var building = toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
        ySortRoot.AddChild(building);

        building.GlobalPosition = hoveredGridCell.Value * 64;
        hoveredGridCell = null;
        gridManager.ClearHighLightedTiles();
    }

    private void OnPlaceTowerButtonPressed()
    {
        toPlaceBuildingResource = towerResource;
        cursor.Visible = true;
        gridManager.HighlightBuildableTiles();
    }

    private void OnPlaceVillageButtonPressed()
    {
        toPlaceBuildingResource = villageResource;
        cursor.Visible = true;
        gridManager.HighlightBuildableTiles();
    }

    private void OnResourceTileUpdated(int resourceCount)
    {
        GD.Print("Resource count: ", resourceCount);
    }

}
