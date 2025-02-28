# Crop System Setup Guide

This guide explains how to set up the crop system in your Unity project.

## 1. Create Crop Prefabs

### Broccoli Prefab
1. Create a new empty GameObject and name it "BroccoliCrop"
2. Add the `BroccoliCrop` script to it
3. Create child GameObjects for each growth stage (at least 3 recommended):
   - BroccoliStage1 (seed)
   - BroccoliStage2 (growing)
   - BroccoliStage3 (ready to harvest)
4. Add visual models to each stage (can be simple 3D models or sprites)
5. Assign these stage GameObjects to the `growthStageModels` array in the BroccoliCrop component
6. Set the crop name to "Broccoli" and configure other settings as desired
7. Save as a prefab

### Corn Prefab
1. Create a new empty GameObject and name it "CornCrop"
2. Add the `CornCrop` script to it
3. Create child GameObjects for each growth stage (at least 3 recommended):
   - CornStage1 (seed)
   - CornStage2 (growing)
   - CornStage3 (ready to harvest)
4. Add visual models to each stage (can be simple 3D models or sprites)
5. Assign these stage GameObjects to the `growthStageModels` array in the CornCrop component
6. Set the crop name to "Corn" and configure other settings as desired
7. Save as a prefab

## 2. Set Up Grid Cells

1. Make sure your grid cells have colliders (Box Collider or similar)
2. The system will automatically calculate the height of each cell based on its collider
3. This height is used to position crops on top of the cells

## 3. Set Up CropManager

1. Create a new empty GameObject in your scene and name it "CropManager"
2. Add the `CropManager` script to it
3. Assign the broccoli and corn prefabs to the respective fields
4. The cropYOffset value is only used as a fallback if a cell doesn't have a collider

## 4. Set Up UI

1. Create a Canvas for your UI
2. Add buttons for tool selection (Plant, Harvest, Water)
3. Add buttons for crop type selection (Broccoli, Corn)
4. Add text elements for displaying inventory
5. Create a new empty GameObject as a child of the Canvas and name it "CropUI"
6. Add the `CropUI` script to it
7. Assign all the UI elements to their respective fields in the inspector

## 5. Update GameManager

1. Make sure your GameManager has a reference to the CropManager
2. If using the singleton pattern, it should automatically find the CropManager

## 6. Testing

1. Make sure your grid cells have colliders and are on the correct layer for raycasting
2. Set up the InputManager's gridLayerMask to match your grid cells' layer
3. Run the game and test planting, growing, and harvesting crops

## Notes

- The growth time can be adjusted in the crop prefabs
- Each crop type has unique properties:
  - Broccoli has a growth speed multiplier
  - Corn benefits from watering, which increases growth speed and yield
- You can add more crop types by creating new scripts that inherit from the base Crop class
- Performance and design optimizations:
  - Crop positioning uses cached collider information in the GridCell class
  - Single source of truth: CropManager maintains all crop references in a dictionary
  - Clear separation of responsibilities:
    - GridCell only tracks basic occupancy state
    - CropManager handles all crop-specific operations and type checking 