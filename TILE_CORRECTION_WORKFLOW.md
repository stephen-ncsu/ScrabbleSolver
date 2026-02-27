# Tile Correction Workflow - Implementation Guide

## 🎯 Overview

The **Tile Correction Workflow** provides a guided, step-by-step interface for users to manually correct OCR errors after scanning a Scrabble board. Instead of requiring users to hunt for and click on red-highlighted tiles on the main board, this workflow presents each unrecognized tile one at a time with a clear interface for correction.

---

## ✨ Features

### 1. **Step-by-Step Correction with Visual Preview**
- Each unrecognized tile is presented individually
- **OCR image preview** - See the actual tile image that failed recognition
- Clear indication of position (Board: Row X, Column Y or Rack: Slot X)
- Large input field for easy correction
- Progress indicator showing current tile vs. total

### 2. **Navigation Controls**
- **Next** - Move to next tile (enabled when current tile has a value)
- **Previous** - Go back to previous tile
- **Skip** - Skip current tile (leaves it as wildcard *)
- **Finish** - Complete corrections and return to main screen

### 3. **Wildcard Support**
- Checkbox to mark tile as intentional wildcard/blank (*)
- Automatic conversion of ?, _ to *
- Can skip tiles to leave them as wildcards

### 4. **Visual Feedback**
- Large, centered input box (100x100px)
- Wheat-colored background matching game tiles (#FFFACD)
- Real-time corrections summary panel
- Shows count of board vs rack corrections

### 5. **Keyboard Shortcuts**
- **Enter** - Move to next tile
- **Escape** - Skip current tile
- Input automatically uppercase
- Single character limit

---

## 📋 User Workflow

### Before (Old Workflow):
```
1. Process Image
2. See OCR Results Window
3. Click Continue
4. Main screen shows board with red-highlighted tiles
5. User must click each red tile individually to correct
6. Easy to miss tiles or lose track of what needs correction
```

### After (New Workflow):
```
1. Process Image
2. If errors found:
   → Tile Correction Window appears automatically
   → Each unrecognized tile presented one by one
   → User enters correction for each
   → Progress tracked clearly
   → Summary of all corrections shown
3. Click "Finish Corrections"
4. Return to main screen with corrected board
5. All done - ready to solve!
```

---

## 🎨 UI Design

### Window Layout:

```
┌─────────────────────────────────────────────────────────────────┐
│                    Tile Correction                              │
│                Correcting tile 3 of 7                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│           Board Position: Row 5, Column 8                       │
│         OCR could not recognize this tile                       │
│        Please enter the correct letter below                    │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│    ┌─────────────────┐              Enter Letter:              │
│    │  OCR Image:     │                                         │
│    │  ┌───────────┐  │              ┌──────────┐              │
│    │  │           │  │              │    E     │              │
│    │  │   [Tile]  │  │              └──────────┘              │
│    │  │   Image   │  │                                         │
│    │  │           │  │       Press Enter to continue           │
│    │  └───────────┘  │       ☐ This is a wildcard (*)         │
│    │ What letter do  │                                         │
│    │   you see?      │                                         │
│    └─────────────────┘                                         │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  Corrections Made:                                              │
│  (2 board, 1 rack)                                             │
│  Board (3,4): T                                                │
│  Board (5,8): E                                                │
│  Rack Slot 2: A                                                │
├─────────────────────────────────────────────────────────────────┤
│  ⬅ Previous  ⏭ Skip              Next ➡  │
└─────────────────────────────────────────────────────────────────┘
```

### Final State (All Corrected):
```
┌─────────────────────────────────────────────────┐
│              Tile Correction                     │
│            All tiles corrected!                  │
├─────────────────────────────────────────────────┤
│     Review your corrections below                │
├─────────────────────────────────────────────────┤
│  Corrections Made:                               │
│  (5 board, 2 rack)                              │
│  Board (2,3): Q                                  │
│  Board (3,4): T                                  │
│  ...                                            │
├─────────────────────────────────────────────────┤
│        ✓ Finish Corrections                      │
└─────────────────────────────────────────────────┘
```

---

## 🔧 Technical Implementation

### New Files Created:

#### 1. `TileCorrectionWindow.xaml`
- Window UI definition
- Custom styles for large input box
- **Image control for OCR tile preview** (150x150px)
- Side-by-side layout: Image preview | Input controls
- Navigation buttons
- Progress and summary panels

#### 2. `TileCorrectionWindow.xaml.cs`
- `TileError` class to track each error **including tile image**
- Correction logic for board and rack tiles
- **Image display using Mat to BitmapSource conversion**
- Navigation between errors
- Validation and completion

### Modified Files:

#### `BoardImagePrep.cs` & `RackImagePrep.cs`
**New Features:**
- Dictionary to store tile images: `Dictionary<(int,int), Mat>` for board, `Dictionary<int, Mat>` for rack
- When OCR fails, clones and saves the tile image: `cell.Clone()`
- New public method: `GetUnrecognizedTileImages()` returns the image dictionary
- Images saved before any preprocessing for clearest view

#### `MainWindow.xaml.cs`
**ProcessButton_Click() Updated:**
```csharp
// After OCR processing...
Dictionary<(int row, int col), Mat>? boardTileImages = null;
Dictionary<int, Mat>? rackTileImages = null;

await Task.Run(() =>
{
    var boardImagePrep = new BoardImagePrep();
    boardState = boardImagePrep.Run(_fileName);
    unrecognizedBoardTiles = boardImagePrep.GetUnrecognizedTiles();
    boardTileImages = boardImagePrep.GetUnrecognizedTileImages(); // NEW

    var rackImagePrep = new RackImagePrep();
    rackState = rackImagePrep.Run(_fileName);
    unrecognizedRackTiles = rackImagePrep.GetUnrecognizedTiles();
    rackTileImages = rackImagePrep.GetUnrecognizedTileImages(); // NEW
});

if (totalUnrecognized > 0)
{
    // Pass images to correction window
    var tileCorrectionWindow = new TileCorrectionWindow(
        boardState, rackState,
        unrecognizedBoardTiles, unrecognizedRackTiles,
        boardTileImages, rackTileImages); // NEW PARAMETERS
}
```

**New Helper Method:**
```csharp
private int CountUnrecognizedTiles(char[,] boardState, char[] rackState)
```
- Counts remaining wildcards (*) in board and rack
- Used for status messages

---

## 🎯 Key Classes & Methods

### TileCorrectionWindow

#### Constructor:
```csharp
TileCorrectionWindow(
    char[,] boardState,
    char[] rackState,
    List<(int row, int col)> unrecognizedBoardTiles,
    List<int> unrecognizedRackTiles,
    Dictionary<(int row, int col), Mat>? boardTileImages = null,  // NEW
    Dictionary<int, Mat>? rackTileImages = null)                   // NEW
```

#### Public Methods:
- `char[,] GetCorrectedBoardState()` - Returns corrected board
- `char[] GetCorrectedRackState()` - Returns corrected rack

#### Private Classes:
```csharp
class TileError
{
    bool IsBoard;           // Board tile or rack tile?
    int Row, Col;           // Board position (if IsBoard)
    int RackIndex;          // Rack position (if !IsBoard)
    string? CorrectedValue; // User's correction
    Mat? TileImage;         // OCR image for visual reference - NEW
}
```

#### Private Methods:
- `LoadCurrentError()` - Display current error tile **and its image**
- `ShowFinishState()` - Show final review screen
- `UpdateNextButton()` - Enable/disable Next based on input
- `UpdateCorrectionsList()` - Refresh summary panel
- `ApplyCorrections()` - Write corrections back to arrays
- `MatToBitmapSource(Mat)` - **NEW** - Convert OpenCV Mat to WPF BitmapSource for display

---

## 🔍 Error Handling

### Validation:
1. **Empty Input** - Next button disabled until value entered
2. **Skip Warning** - If tiles skipped, warns user before finishing
3. **Wildcard Detection** - Automatically detects ?, _ and converts to *

### User Feedback:
- Real-time progress counter
- Live corrections summary
- Color-coded buttons (Blue=Next, Orange=Skip, Green=Finish)
- Warning dialog if uncorrected tiles remain

---

## 📊 User Experience Improvements

### Before:
- ❌ Red tiles scattered across large board
- ❌ Easy to miss errors
- ❌ No guidance on what needs fixing
- ❌ Requires clicking each tile individually
- ❌ No progress tracking
- ❌ **Can't see what the OCR actually detected**

### After:
- ✅ Focused one-at-a-time correction
- ✅ **Visual preview of the actual tile image**
- ✅ Clear progress indicator
- ✅ Impossible to miss errors
- ✅ Guided workflow with navigation
- ✅ Summary of all corrections
- ✅ Keyboard shortcuts for speed
- ✅ Can review before finishing
- ✅ **See exactly what character to enter**

---

## 🎮 Usage Examples

### Scenario 1: Full Correction
```
1. User processes image
2. 5 board tiles + 2 rack tiles unrecognized
3. Tile Correction Window appears
4. User enters letter for each tile (7 total)
5. Clicks "Finish Corrections"
6. Returns to main screen with perfect board
7. Ready to solve immediately
```

### Scenario 2: Some Skipped
```
1. User processes image
2. 3 tiles unrecognized
3. User corrects 2, skips 1 (intentional wildcard)
4. Warning: "1 tile will remain marked with *"
5. User confirms
6. Board shows 2 corrected, 1 wildcard
7. Can solve with wildcard
```

### Scenario 3: Correction with Navigation
```
1. 10 tiles to correct
2. User corrects first 5
3. Realizes mistake on tile 3
4. Clicks "Previous" to go back
5. Fixes mistake
6. Continues forward with "Next"
7. Completes all corrections
```

---

## 🚀 Build & Test

### Build Status:
✅ **Build Successful**

### Testing Checklist:
- [x] Window displays correctly
- [x] Progress counter updates
- [x] Navigation buttons work (Previous/Next/Skip)
- [x] Input validation (1 character, uppercase)
- [x] Wildcard checkbox functionality
- [x] Keyboard shortcuts (Enter, Escape)
- [x] Corrections summary updates in real-time
- [x] Final review state shows correctly
- [x] Finish button applies corrections
- [x] Board and rack updated correctly
- [x] Status messages accurate
- [x] Skip warning appears when needed

---

## 🎉 Summary

The Tile Correction Workflow transforms the error correction experience from:
- **Hunting and clicking** scattered red tiles
  
To:
- **Guided step-by-step** correction with clear progress

This makes the app:
- ✅ More user-friendly
- ✅ Faster to use
- ✅ Less error-prone
- ✅ More professional
- ✅ Easier for beginners

**Result:** A polished, production-ready OCR correction experience! 🎯
