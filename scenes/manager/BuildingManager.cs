using Game.Resources.Building;
using Game.UI;
using Godot;

namespace Game.Manager;

public partial class BuildingManager : Node
{
	[Export]
	private GridManager gridManager;
	[Export]
	private GameUI gameUI;
	[Export]
	private Node2D ySortRoot;
	[Export]
	private Node2D cursor;


	private BuildingResource toPlaceBuildingResource;
	private Vector2I? hoveredGridCell;
	private int currentResourceCount;
	private int startingResourceCount = 4;

	private int AvailableResourceCount => startingResourceCount + currentResourceCount - currentResourceCount;

	public override void _Ready()
	{
		gameUI = GetNode<GameUI>("GameUI");
		gridManager.ResourceTileUpdated += OnResourceTileUpdated;

		gameUI.BuildingResourceSelected += OnPlaceBuildingResourceSelected;
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

		currentResourceCount += toPlaceBuildingResource.ResourceCost;
	}
	private void OnResourceTileUpdated(int resourceCount)
	{
		currentResourceCount = resourceCount;
	}


	private void OnPlaceBuildingResourceSelected(BuildingResource buildingResource)
	{
		toPlaceBuildingResource = buildingResource;
		cursor.Visible = true;
		gridManager.HighlightBuildableTiles();
	}
}
