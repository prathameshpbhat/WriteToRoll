# ScriptWriter Pro - A4 Page Layout & Pagination Implementation

## Overview
Implemented professional screenplay formatting and A4 page layout support per industry standards from "How to Write a Movie Script Like Professional Screenwriters."

## Professional Standards Implemented

### Page Format Specifications
- **Page Size**: Letter (8.5" × 11") / A4 (8.27" × 11.69")
- **Font**: Courier New 12pt (ensures 1 page = ~1 minute screen time)
- **Line Spacing**: Standard double-spacing (55 lines per page)
- **Margins**:
  - Left: 1.5" (for hole-punching/binding)
  - Right: 1.0"
  - Top: 1.0"
  - Bottom: 1.0"

### Screenplay Length Standards
- **Typical Range**: 70-120 pages
- **Average**: ~110 pages (≈ 110 minutes ≈ 1 hour 50 minutes)
- **Conversion**: 1 page ≈ 1 minute of screen time (Courier 12pt, proper spacing)

### Element Formatting Rules

#### Scene Heading (Slugline)
- Format: `INT/EXT. LOCATION - TIME OF DAY`
- Case: ALL CAPS
- Margin: Left 1.5", Right 1.0"
- Alignment: Left
- Examples: 
  - `INT. COFFEE SHOP - DAY`
  - `EXT. BEACH - SUNSET`
  - `INT/EXT. CAR - MOVING - NIGHT`

#### Action (Description)
- Purpose: Visual/audible description of what happens
- Case: Normal (Title Case, First letter capitalized)
- Margin: Left 1.5", Right 1.0"
- Alignment: Left
- Tense: Third-person present
- Example: `John walks across the room, carefully avoiding the broken glass.`

#### Character Name
- Position: Centered at 3.7" from left margin
- Case: ALL CAPS
- Alignment: Center
- Note: Extensions (V.O.), (O.S.) go in parenthetical below
- Example: `JOHN` (without extensions)

#### Dialogue
- Position: Indented left 2.5", right 1.0"
- Case: Normal (preserve speaker's exact words)
- Alignment: Left
- Position: Below character name
- Example: `"I should have known better."`

#### Parenthetical (Character Direction)
- Format: `(direction text)`
- Case: Lowercase (except special codes)
- Margin: Left 3.1", Right varies
- Special Codes (UPPERCASE): `(V.O.)`, `(O.S.)`, `(CONT'D)`
- Examples:
  - `(beat)` - pause in dialogue
  - `(looking back)` - character action during dialogue
  - `(V.O.)` - Voice Over
  - `(O.S.)` - Off Screen
  - `(CONT'D)` - Character continues from previous page

#### Transition
- Format: Proper transition verb with colon
- Case: ALL CAPS
- Common Transitions:
  - `CUT TO:` (most common)
  - `DISSOLVE TO:`
  - `FADE TO:`
  - `SMASH CUT TO:` (sudden, jarring)
  - `MATCH CUT TO:` (visual/audio match)
  - `WIPE TO:`
  - `IRIS TO:`
  - `MONTAGE:` (series of scenes)
  - `BACK TO:` (returning from flashback)

### Page Layout Features
- **Lines per Page**: 55 lines (industry standard with Courier 12pt)
- **Page Numbers**: Top-right corner, starting from page 2
- **First Page**: Title page optional
- **Blank Lines**: Standard spacing between elements

## Code Architecture

### New Services & Models

#### `PageFormatting.cs` (Model)
```csharp
public class PageFormatting
{
    public PageSize Size { get; set; }          // Letter or A4
    public double MarginLeft { get; set; }      // 1.5" for binding
    public double MarginRight { get; set; }     // 1.0"
    public double MarginTop { get; set; }       // 1.0"
    public double MarginBottom { get; set; }    // 1.0"
    public int LinesPerPage { get; set; }       // 55 lines
    public string FontFamily { get; set; }      // "Courier New"
    public int FontSizePoints { get; set; }     // 12 points
    
    public double GetContentWidth()  // Calculates usable width
}
```

Preconfigured profiles:
- `PageFormatting.StandardLetter()` - 8.5" × 11"
- `PageFormatting.StandardA4()` - 8.27" × 11.69"

#### `PaginationEngine.cs` (Service)
```csharp
public interface IPaginationEngine
{
    int GetCurrentPageNumber(int caretPosition);
    int GetTotalPageCount(string scriptText);
    int GetLinesOnCurrentPage(string scriptText, int caretPosition);
    List<int> GetPageBreakPositions(string scriptText);
    double GetEstimatedScreenMinutes(string scriptText);
}
```

Functions:
- **GetTotalPageCount()**: Counts total pages (divides lines by 55)
- **GetEstimatedScreenMinutes()**: Calculates screen time (1 page = 1 minute)
- **GetPageBreakPositions()**: Identifies automatic page break positions
- **GetLinesOnCurrentPage()**: Shows line count on current page

#### `ScreenplayFormattingRules.cs` (Service)
```csharp
public interface IScreenplayFormattingRules
{
    string FormatSceneHeading(string text);
    string FormatAction(string text);
    string FormatCharacter(string text);
    string FormatDialogue(string text);
    string FormatParenthetical(string text);
    string FormatTransition(string text);
    bool IsValidElement(string line, out ScriptElementType elementType);
}
```

Functions:
- **FormatSceneHeading()**: Normalizes `INT/EXT. LOCATION - TIME` format
- **FormatCharacter()**: Converts to UPPERCASE, removes extensions
- **FormatParenthetical()**: Enforces lowercase with special case exceptions
- **FormatTransition()**: Ensures proper transition formatting (`CUT TO:`, `DISSOLVE TO:`, etc.)
- **FormatAction()**: Applies title case and third-person present tense
- **FormatDialogue()**: Preserves speaker's words, ensures quote marks
- **IsValidElement()**: Detects element type from line content

### Integration with MainWindow

#### Initialization
```csharp
_pageFormat = PageFormatting.StandardLetter();
_paginationEngine = new PaginationEngine(_pageFormat);
_formattingRules = new ScreenplayFormattingRules(_pageFormat);
```

#### UpdateStatistics() Enhancement
- Now shows: `Pages: {count} (~{minutes} min)`
- Uses PaginationEngine for accurate page counting
- Displays estimated screen time per page

#### Status Bar Update
- Title includes: `"(A4 Letter, Courier 12pt)"`
- PageCountText displays pagination info

## Usage Examples

### Scene Writing
```
INT. COFFEE SHOP - MORNING

John sits at a table, nervously tapping his fingers.

JOHN
(looking at watch)
She's going to be late.
```

### Pagination
- After 55 lines, automatic page break occurs
- Status bar shows: `Pages: 2 (~2 min)`
- Each page represents ~1 minute of screen time

### Format Validation
```csharp
var formatting = new ScreenplayFormattingRules(pageFormat);

// Apply formatting to user input
string formatted = formatting.FormatSceneHeading("int. office");
// Result: "INT. OFFICE"

// Detect element type
bool valid = formatting.IsValidElement("INT. COFFEE SHOP - DAY", out var type);
// type = ScriptElementType.SceneHeading
```

## Professional Compliance

✓ **Courier 12pt font** - Ensures industry-standard page-to-minute ratio  
✓ **A4/Letter margins** - 1.5" left binding margin per spec  
✓ **55 lines per page** - Professional screenplay standard  
✓ **1 page = 1 minute** - Accurate screen time estimation  
✓ **Element spacing** - Proper margins and alignment per element type  
✓ **Case enforcement** - Scene headings, character names in CAPS; parentheticals in lowercase  
✓ **Transition formatting** - Standard industry transitions with proper punctuation  
✓ **Page numbering** - Supports top-right positioning starting page 2  

## Implementation Status

### ✓ Completed
- PageFormatting model with A4/Letter support
- PaginationEngine with 55 lines/page standard
- ScreenplayFormattingRules with all element types
- MainWindow integration for pagination display
- Status bar showing page count + estimated screen time
- Comprehensive formatting rule enforcement

### ⚠ Future Enhancements
- Visual page break display in editor
- Print-to-PDF with proper pagination
- Title page generation
- Character/location tracking per page
- Export to industry-standard formats (.fdx, .pdf)
- Automatic parenthetical detection
- Multi-page view mode

## Testing

Build Status: ✓ **Success** (26 warnings, all nullable field deferred initialization)

```powershell
dotnet build ScriptWriter.sln
# Result: Build succeeded. 0 Errors
```

## References
- "How to Write a Movie Script Like Professional Screenwriters" (StudioBinder Guide)
- Industry Standard: Courier 12pt for screenplay formatting
- Professional Screenplay Format (Writers Guild of America)
- A4 vs Letter page dimensions and margin standards
