using System.Diagnostics.Tracing;
using Game.Building;
using Game.Resources.Building;
using Game.UI;
using Godot;

namespace Game.Manager;

public partial class BuildingManager : Node
{

	private readonly StringName ACTION_LEFT_CLICK = "left_click";
	private readonly StringName ACTION_CANCEL = "cancel";

	[Export]
	private GridManager gridManager;
	[Export]
	private GameUI gameUI;
	[Export]
	private Node2D ySortRoot;
	[Export]
	private PackedScene buildingGhostScene;



	private BuildingResource toPlaceBuildingResource;
	private Vector2I? hoveredGridCell;
	private int currentResourceCount;
	private int startingResourceCount = 4;
	private int currentlyUsedResourceCount;
	private BuildingGhost buildingGhost;

	private int AvailableResourceCount => startingResourceCount + currentResourceCount - currentlyUsedResourceCount;

	public override void _Ready()
	{
		gridManager.ResourceTileUpdated += OnResourceTileUpdated;

		gameUI.BuildingResourceSelected += OnPlaceBuildingResourceSelected;
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed(ACTION_CANCEL))
		{
			ClearBuildingGhost();
		}

		if (
			hoveredGridCell.HasValue &&
			toPlaceBuildingResource != null &&
			@event.IsActionPressed(ACTION_LEFT_CLICK) && IsBuildingPlaceableAtTile(hoveredGridCell.Value))
		{
			PlaceBuildingAtHoverCellPosition();
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var gridPosition = gridManager.GetMouseGridCellPosition();
		if (!IsInstanceValid(buildingGhost)) return;

		buildingGhost.GlobalPosition = gridPosition * 64;

		if (
			toPlaceBuildingResource != null &&
			(!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition)
		 )
		{
			hoveredGridCell = gridPosition;
			UpdateGridDisplay();
		}
	}

	private void UpdateGridDisplay()
	{
		if (hoveredGridCell == null) return;

		gridManager.ClearHighLightedTiles();
		gridManager.HighlightBuildableTiles();
		if (IsBuildingPlaceableAtTile(hoveredGridCell.Value))
		{

			gridManager.HighlightExpandedBuildableTiles(hoveredGridCell.Value, toPlaceBuildingResource.BuildableRadius);
			gridManager.HighlightResourceTiles(hoveredGridCell.Value, toPlaceBuildingResource.ResourceRadius);
			buildingGhost.SetValid();
		}
		else
		{
			buildingGhost.SetInvalid();
		}

	}

	private void PlaceBuildingAtHoverCellPosition()
	{
		if (!hoveredGridCell.HasValue) return;

		var building = toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
		ySortRoot.AddChild(building);

		building.GlobalPosition = hoveredGridCell.Value * 64;

		currentlyUsedResourceCount += toPlaceBuildingResource.ResourceCost;

		ClearBuildingGhost();

	}

	private void ClearBuildingGhost()
	{
		hoveredGridCell = null;
		gridManager.ClearHighLightedTiles();

		if (IsInstanceValid(buildingGhost))
		{

			buildingGhost.QueueFree();
		}
		buildingGhost = null;

	}

	private bool IsBuildingPlaceableAtTile(Vector2I tilePosition)
	{
		return gridManager.IsTilePositionBuildable(tilePosition) &&
			AvailableResourceCount >= toPlaceBuildingResource.ResourceCost;

	}

	private void OnResourceTileUpdated(int resourceCount)
	{
		currentResourceCount = resourceCount;
	}


	private void OnPlaceBuildingResourceSelected(BuildingResource buildingResource)
	{
		if (IsInstanceValid(buildingGhost))
		{
			buildingGhost.QueueFree();
		}
		buildingGhost = buildingGhostScene.Instantiate<BuildingGhost>();
		ySortRoot.AddChild(buildingGhost);

		var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
		buildingGhost.AddChild(buildingSprite);

		toPlaceBuildingResource = buildingResource;
		gridManager.HighlightBuildableTiles();
	}
}
