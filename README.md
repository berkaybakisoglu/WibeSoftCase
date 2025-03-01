

## Project Overview
The game features two main scenes since quests are seperated:
1. **Growing Scene**: Dedicated to planting, growing, and harvesting crops
2. **Building Scene**: Focused on placing and managing buildings on the grid



## System Architecture

### Core Systems

#### Manager System
 a base manager class that implements the singleton pattern:
- `BaseManager<T>`: Abstract base class for all managers, implementing the singleton pattern
- `GridManager`: Handles the grid system and cell management
- `InputManager`: Processes player input and interactions
- `BuildingManager`: Manages building placement and functionality
- `CropManager`: Handles crop planting, growth, and harvesting

#### Grid System
- `GridCell`: Represents a single cell in the grid that can contain buildings or crops
- `GridCellVisualizerPool`: Object pool for grid cell visualizations

#### Building System
- `BaseBuilding`: Abstract base class for all buildings
- `Building`: Concrete implementation of buildings with various functionalities
- `BuildingData`: Scriptable object for storing building configurations

#### Crop System
- `Crop`: Base class for all crop types
- Specific crop implementations (e.g., `CornCrop`, `BroccoliCrop`)
- `HarvestedItemPool`: Object pool for harvested crop items

#### UI System
- `BuildingUI`: User interface for building placement and management
- `CropUI`: User interface for crop planting and management

#### Scene Management
- `SceneSwitcher`: Handles transitions between the Building and Growing scenes

## How It Works

### Grid System
The game world is represented as a grid of cells. Each cell can contain either a building or a crop. The `GridManager` is responsible for:
- Creating and initializing the grid
- Converting between world and grid coordinates
- Managing cell selection and interaction
- Checking if areas are valid for placement

### Building Placement
1. Player selects a building type from the UI
2. The building preview follows the touch 
3. When the player clicks on a valid grid location, the building is placed if:
   - The area is free (no other buildings or crops)
   - The building fits within the grid boundaries

### Crop Management
1. Player selects a crop type from the UI
2. Player clicks on empty grid cells to plant crops
3. Crops grow over time through different growth stages
4. Corns can be watered to increase harvest amount.
4. When fully grown, crops can be harvested for resources

### Scene Switching
The game features two main scenes that can be switched between using the scene switcher button:
1. Farm Scene:  Crop planting and harvesting
1. Building Scene: Construction and building management

## Development Notes
- New building or crop types can be added by extending the base classes and related Scriptable Objects
- The grid system can be configured for different sizes and cell dimensions 