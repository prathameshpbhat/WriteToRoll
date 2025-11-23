# ScreenplayServiceFactory Integration Guide

## Overview

The `ScreenplayServiceFactory` provides a clean factory pattern for initializing and managing all professional screenplay services (Tier 1 & 2 enhancements).

## Architecture

```
ScreenplayServiceFactory
├── Creates: ScreenplayServices container
├── Services:
│   ├── PageFormatting (A4 or Letter)
│   ├── PaginationEngine (55 lines/page, 1 page = 1 min)
│   ├── ScreenplayFormattingRules (all 6 element types)
│   ├── TitlePageGenerator (professional title pages)
│   ├── PageBreakVisualizer (visual page break detection)
│   └── ScreenplayTracker (character/location analysis)
└── Statistics: ScreenplayStatistics (analysis results)
```

## Usage Examples

### Basic Initialization

```csharp
// Using default Letter format
var factory = new ScreenplayServiceFactory();
var services = factory.CreateServices();

// Using A4 format
var factoryA4 = new ScreenplayServiceFactory(PageSize.A4);
var servicesA4 = factoryA4.CreateServices();
```

### Complete Screenplay Generation

```csharp
var services = factory.CreateServices();

string titlePage = services.TitlePageGenerator.GenerateTitlePage(
    title: "MY STORY",
    author: "John Doe",
    email: "john@example.com",
    phone: "555-1234"
);

string scriptWithPageBreaks = services.PageBreakVisualizer.InsertPageBreakMarkers(
    scriptBody,
    markerFormat: "--- PAGE {0} ---"
);

string completeScreenplay = titlePage + Environment.NewLine + scriptWithPageBreaks;
```

### Analyze Screenplay Statistics

```csharp
var services = factory.CreateServices();

// Analyze the script
var stats = services.AnalyzeScreenplay(scriptText);

// Display results
Console.WriteLine(stats.ToString());
// Output: "Screenplay: 42 pages (~42 min), 8 characters, 12 locations"

// Access individual statistics
foreach (var character in stats.Characters)
{
    var screenTime = stats.CharacterScreenTime[character];
    Console.WriteLine($"{character}: {screenTime} minutes");
}

foreach (var location in stats.Locations)
{
    Console.WriteLine($"Scene: {location}");
}
```

### Title Page Generation

```csharp
var titleGen = services.TitlePageGenerator;

// Basic title page
string titlePage = titleGen.GenerateTitlePage(
    "THE SCREENPLAY TITLE",
    "Author Name",
    "author@email.com",
    "555-0123"
);

// Extended contact info
string titlePageFull = titleGen.GenerateTitlePageWithContact(
    "THE SCREENPLAY TITLE",
    "Author Name",
    "author@email.com",
    "555-0123",
    website: "www.example.com"
);

// Validate inputs
if (titleGen.ValidateTitlePageInput("", "Author"))
{
    // Title is required
}
```

### Page Break Visualization

```csharp
var visualizer = services.PageBreakVisualizer;

// Get page ranges for UI highlighting
var pageRanges = visualizer.GetPageRanges(scriptText);
foreach (var (startLine, endLine, pageNumber) in pageRanges)
{
    Console.WriteLine($"Page {pageNumber}: Lines {startLine}-{endLine}");
}

// Insert soft markers (not saved, for display only)
string scriptWithMarkers = visualizer.InsertPageBreakMarkers(
    scriptText,
    markerFormat: "========== PAGE {0} =========="
);

// Check if specific line is at page boundary
bool isPageBreak = visualizer.IsLineAtPageBreak(scriptText, lineNumber: 55);
```

### Screenplay Analysis (Character Tracking)

```csharp
var tracker = services.ScreenplayTracker;

// Scan script
tracker.ScanScript(scriptText);

// Get all characters
var characters = tracker.GetAllCharacters();
foreach (var character in characters)
{
    var lineCount = tracker.GetCharacterLineCount(character);
    var screenTime = tracker.GetCharacterScreenTime(character);
    Console.WriteLine($"{character}: {lineCount} lines (~{screenTime} min)");
    
    // Get sample dialogue
    var dialogue = tracker.GetCharacterDialogue(character, maxLines: 3);
    foreach (var line in dialogue)
    {
        Console.WriteLine($"  {line}");
    }
}

// Get all locations
var locations = tracker.GetAllLocations();
foreach (var location in locations)
{
    if (tracker.IsKnownLocation(location))
    {
        Console.WriteLine($"Location: {location}");
    }
}
```

### Format Switching

```csharp
var factory = new ScreenplayServiceFactory(PageSize.Letter);
var services = factory.CreateServices();

// Work with Letter format
var pageCountLetter = services.PaginationEngine.GetTotalPageCount(scriptText);

// Switch to A4
factory.SwitchPageFormat(PageSize.A4);
services = factory.CreateServices(); // Get new services for A4

var pageCountA4 = services.PaginationEngine.GetTotalPageCount(scriptText);

// A4 typically has more content area
Console.WriteLine($"Letter: {pageCountLetter} pages, A4: {pageCountA4} pages");
```

## Integration Points in MainWindow

### Add Service Factory as Field

```csharp
private ScreenplayServiceFactory _screenplayFactory;
private ScreenplayServices _screenplayServices;
```

### Initialize in Constructor

```csharp
public MainWindow()
{
    InitializeComponent();
    
    // ... existing initialization ...
    
    // Initialize screenplay services
    _screenplayFactory = new ScreenplayServiceFactory(PageSize.Letter);
    _screenplayServices = _screenplayFactory.CreateServices();
}
```

### Use in Text Changed Event

```csharp
private void ScriptEditor_TextChanged(object sender, TextChangedEventArgs e)
{
    var scriptText = ScriptEditor.Text;
    
    // Update pagination
    var pageCount = _screenplayServices.PaginationEngine.GetTotalPageCount(scriptText);
    var screenTime = _screenplayServices.PaginationEngine.GetEstimatedScreenMinutes(scriptText);
    
    StatusText.Text = $"Pages: {pageCount} (~{screenTime:F0} min)";
    
    // Update character tracking
    _screenplayServices.ScreenplayTracker.ScanScript(scriptText);
    var characterCount = _screenplayServices.ScreenplayTracker.GetAllCharacters().Count;
    CurrentElementText.Text = $"Characters: {characterCount}";
}
```

### Add Menu Command for Title Page

```csharp
private void GenerateTitlePage_Click(object sender, RoutedEventArgs e)
{
    var titleDialog = new TitlePageDialog();
    if (titleDialog.ShowDialog() == true)
    {
        string titlePage = _screenplayServices.TitlePageGenerator.GenerateTitlePage(
            titleDialog.Title,
            titleDialog.Author,
            titleDialog.Email,
            titleDialog.Phone
        );
        
        ScriptEditor.Text = titlePage + Environment.NewLine + ScriptEditor.Text;
    }
}
```

### Add Page Break Visualization

```csharp
private void VisualizePage Breaks_Click(object sender, RoutedEventArgs e)
{
    string scriptWithMarkers = _screenplayServices.PageBreakVisualizer.InsertPageBreakMarkers(
        ScriptEditor.Text,
        markerFormat: "--- PAGE {0} ---"
    );
    
    // Display in preview or apply directly
    ScriptPreview.Text = scriptWithMarkers;
}
```

## Service Lifecycle

1. **Factory Creation**: Choose page format (Letter/A4)
2. **Services Creation**: `CreateServices()` instantiates all services
3. **Script Analysis**: Call `ScanScript()` or analyze directly
4. **Results**: Use returned data for UI updates, export, or further processing
5. **Format Switch**: Call `SwitchPageFormat()` to recalculate with new format

## Performance Notes

- **Pagination Engine**: O(n) where n = script length
- **Character Tracking**: O(n) first scan, then O(1) lookups
- **Title Page Generation**: O(1) - fixed format
- **Page Break Visualization**: O(n) to insert markers, O(1) to query

## Professional Standards

All services follow industry-standard screenplay formatting:
- **Font**: Courier New 12pt
- **Pages**: 55 lines = 1 page ≈ 1 minute screen time
- **Margins**: 
  - Left: 1.5" (binding)
  - Right/Top/Bottom: 1.0"
- **Elements**: All 6 types (Scene, Character, Dialogue, Parenthetical, Action, Transition)
- **Format Support**: A4 (210×297mm) and Letter (8.5"×11")

## Error Handling

All validation methods return `bool`:

```csharp
// Validate title page inputs
if (!_screenplayServices.TitlePageGenerator.ValidateTitlePageInput(title, author))
{
    MessageBox.Show("Title and Author are required");
    return;
}

// Check character existence
if (_screenplayServices.ScreenplayTracker.IsKnownCharacter(name))
{
    var stats = _screenplayServices.ScreenplayTracker.GetCharacterLineCount(name);
    // Use stats
}
```

## Next Steps

1. **Basic Integration**: Add factory field + initialization to MainWindow
2. **Pagination Display**: Use PaginationEngine in status bar (already done)
3. **Character Panel**: Display GetAllCharacters() in UI panel
4. **Title Page UI**: Add dialog for title page generation
5. **Export Dialog**: Use services to generate complete screenplay with title page
6. **Advanced Statistics**: Create statistics panel using ScreenplayTracker

## See Also

- `ARCHITECTURE_DIAGRAMS.md` - Service dependency graph
- `A4_PAGINATION_IMPLEMENTATION.md` - Pagination details
- `FINAL_SUMMARY.md` - Complete feature overview
- `DELIVERY_CHECKLIST.md` - Tier 1-3 feature roadmap
