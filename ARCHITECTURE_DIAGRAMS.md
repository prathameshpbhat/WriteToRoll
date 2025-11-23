# ScriptWriter Pro - Architecture & Data Flow Diagrams

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      SCRIPTWRITER PRO                           │
│                    (WPF Application)                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
         ┌────────────────────────────────────────┐
         │   MainWindow (App.Host)                │
         │  - Script Editor TextBox               │
         │  - Auto-complete Dropdown              │
         │  - Status Bar Display                  │
         └────────────────────────────────────────┘
                    │              │
         ┌──────────┴──────────────┴──────────┐
         │                                    │
         ▼                                    ▼
    ┌─────────────────┐          ┌──────────────────────┐
    │ PAGINATION      │          │ FORMATTING           │
    │ (App.Core)      │          │ (App.Core)           │
    ├─────────────────┤          ├──────────────────────┤
    │                 │          │                      │
    │ PageFormatting  │◄─────────┤ ScreenplayFmtRules   │
    │ - A4/Letter     │          │ - Scene Heading      │
    │ - Margins       │          │ - Character          │
    │ - Font (Courier)│          │ - Dialogue           │
    │ - 55 lines/page │          │ - Parenthetical      │
    │                 │          │ - Action             │
    │                 │          │ - Transition         │
    │ PaginationEngine│          │                      │
    │ - Page count    │          │ Element Detection    │
    │ - Screen time   │          │ - Type identification│
    │ - Page breaks   │          │ - Case enforcement   │
    └─────────────────┘          └──────────────────────┘
         │                                    │
         └──────────────┬─────────────────────┘
                        │
                        ▼
              ┌──────────────────────┐
              │  STATUS BAR          │
              │  Updates Real-Time   │
              │  "Pages: X (~Y min)" │
              └──────────────────────┘
```

## Data Flow - Text Input to Display

```
┌──────────────────────────────────────────────────────────┐
│  User types in TextBox (Script Editor)                   │
└────────────────────┬─────────────────────────────────────┘
                     │
                     ▼
        ┌────────────────────────┐
        │ TextChanged Event      │
        │ - Get current line     │
        │ - Detect element type  │
        └────────────┬───────────┘
                     │
        ┌────────────┴──────────────────┐
        │                               │
        ▼                               ▼
  ┌──────────────────────┐    ┌────────────────────┐
  │ Apply Formatting     │    │ Check Auto-Complete│
  │ (if not autocomplete)│    │ - Show dropdown    │
  │ - Scene Heading?     │    │ - Suggest element  │
  │ - Character?         │    │   next            │
  │ - Etc.               │    └────────────────────┘
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────────────────┐
  │ UpdateStatistics()               │
  ├──────────────────────────────────┤
  │ PaginationEngine.GetTotalPageCount
  │ → Splits by newlines
  │ → Count = lines / 55
  │ → PageCount = ceil(Count/55)
  │                                  │
  │ PaginationEngine.GetEstimatedScreenMinutes
  │ → Minutes = PageCount * 1.0      │
  └──────────────┬───────────────────┘
                 │
                 ▼
  ┌──────────────────────────────────┐
  │ Update Status Bar                │
  │ "Pages: X (~Y min)"              │
  │ "Words: Z"                       │
  │ "Element: Action"                │
  │ "Next: Dialogue"                 │
  └──────────────────────────────────┘
```

## Element Detection Flow

```
Line of Text: "INT. COFFEE SHOP - DAY"
      │
      ▼
┌─────────────────────────────────────────┐
│ ScreenplayFormattingRules.IsValidElement│
├─────────────────────────────────────────┤
│                                         │
│ 1. Check for Scene Heading:             │
│    Regex: ^(INT|EXT|INT/EXT)            │
│    ✓ Match found                        │
│    → ElementType.SceneHeading           │
│                                         │
│ 2. Check for Transition:                │
│    Regex: ^(CUT|FADE|DISSOLVE...)       │
│    ✗ No match                           │
│                                         │
│ 3. Check for Character:                 │
│    Regex: ^[A-Z\s]+...                  │
│    ✗ No match (contains ".")            │
│                                         │
│ 4. Default: Action                      │
│    → ElementType.Action                 │
│                                         │
└─────────────────────────────────────────┘
      │
      ▼
Result: ScriptElementType.SceneHeading
```

## Formatting Application Flow

```
Raw Text: "john"
      │
      ▼
┌──────────────────────────────────────────────────┐
│ ScreenplayFormattingRules                        │
│   .FormatCharacter("john")                       │
├──────────────────────────────────────────────────┤
│                                                  │
│ 1. Trim whitespace: "john"                       │
│ 2. Convert to UPPERCASE: "JOHN"                  │
│ 3. Remove extensions: (V.O.), (O.S.)             │
│    - "JOHN" has no extensions                    │
│ 4. Return result: "JOHN"                         │
│                                                  │
└──────────────────────────────────────────────────┘
      │
      ▼
Result: "JOHN"
      │
      ▼
Display (centered at 3.7" from left)
```

## Pagination Calculation

```
Script Text: ~250 lines (sample screenplay)
      │
      ▼
┌──────────────────────────────────────────┐
│ PaginationEngine.GetTotalPageCount()     │
├──────────────────────────────────────────┤
│                                          │
│ 1. Split text by newlines:               │
│    lines = text.Split('\n')              │
│    lineCount = 250                       │
│                                          │
│ 2. Divide by LinesPerPage:               │
│    LinesPerPage = 55                     │
│    pageCount = (250 + 55 - 1) / 55       │
│    pageCount = 304 / 55                  │
│    pageCount = 5.52 → 6 (ceil)           │
│                                          │
│ 3. Return: 6 pages                       │
│                                          │
└──────────────────────────────────────────┘
      │
      ▼
PaginationEngine.GetEstimatedScreenMinutes()
      │
      ▼
┌──────────────────────────────────────────┐
│ minutes = pageCount * 1.0                │
│ minutes = 6 * 1.0                        │
│ minutes = 6.0                            │
└──────────────────────────────────────────┘
      │
      ▼
Status Bar: "Pages: 6 (~6 min)"
```

## Service Initialization

```
MainWindow Constructor
      │
      ├─ Initialize ScreenwritingLogic
      │
      ├─ Initialize AutoFormattingEngine
      │
      ├─ Initialize SmartIndentationEngine
      │
      ├─ Initialize ScreenplayStructureAdvisor
      │
      ├─ Initialize FountainParser
      │
      ├─ NEW: Initialize PageFormatting
      │       _pageFormat = PageFormatting.StandardLetter()
      │       or
      │       _pageFormat = PageFormatting.StandardA4()
      │
      ├─ NEW: Initialize PaginationEngine
      │       _paginationEngine = new PaginationEngine(_pageFormat)
      │
      ├─ NEW: Initialize ScreenplayFormattingRules
      │       _formattingRules = new ScreenplayFormattingRules(_pageFormat)
      │
      └─ Services ready for use
         • PaginationEngine used in UpdateStatistics()
         • ScreenplayFormattingRules available for formatting
         • PageFormatting defines all layout specifications
```

## Page Layout Visualization

```
Letter Format (8.5" × 11")
┌─────────────────────────────────────────────────┐
│ 1"                                          1"  │
├─────────────────────────────────────────────────┤
│ ← 1.5"                                    1.0"→│
│                                                 │
│   INT. COFFEE SHOP - DAY                       │
│   (Scene Heading: 1.5" from left)              │
│                                                 │
│   John sits at a table, nervously. Sarah       │
│   enters and sits down. They look at each      │
│   other for a long moment.                     │
│   (Action: 1.5" from left, right margin 1.0") │
│                                                 │
│                      JOHN                      │
│               (Character: centered at 3.7")    │
│                                                 │
│               (looking up)                      │
│          (Parenthetical: 3.1" from left)       │
│                                                 │
│              "You came."                        │
│         (Dialogue: 2.5" from left)             │
│                                                 │
│                      SARAH                     │
│                                                 │
│              "Of course I came."               │
│                                                 │
│                          CUT TO:               │
│                  (Transition: right-aligned)   │
│                                                 │
│ Blank lines separate major elements            │
│ 55 lines total per page = ~1 min screen time   │
│                                                 │
└─────────────────────────────────────────────────┘
     ↑ Bottom: 1"
```

## Professional Standards Reference

```
SCREENPLAY FORMAT CHECKLIST:

Font & Size:
  ✓ Courier New (monospace)
  ✓ 12 points
  ✓ Line spacing: Standard (55 lines/page)

Margins:
  ✓ Left: 1.5" (for binding)
  ✓ Right: 1.0"
  ✓ Top: 1.0"
  ✓ Bottom: 1.0"

Page Size:
  ✓ Letter: 8.5" × 11"
  ✓ A4: 8.27" × 11.69"

Page Count:
  ✓ ~55 lines per page
  ✓ 1 page ≈ 1 minute screen time
  ✓ Screenplay: 70-120 pages (avg ~110)

Elements:
  ✓ Scene Heading: INT/EXT. LOCATION - TIME (UPPERCASE)
  ✓ Action: Natural text (Title case)
  ✓ Character: UPPERCASE (centered, 3.7" from left)
  ✓ Dialogue: Natural text (2.5" from left)
  ✓ Parenthetical: lowercase (3.1" from left)
  ✓ Transition: VERB TO: (right-aligned)

Case Enforcement:
  ✓ UPPERCASE: Scene Heading, Character, Transition, Extensions
  ✓ lowercase: Parentheticals (except special codes)
  ✓ Normal: Action, Dialogue

Extensions:
  ✓ (V.O.) - Voice Over
  ✓ (O.S.) - Off Screen
  ✓ (CONT'D) - Continued

Transitions:
  ✓ CUT TO: (most common)
  ✓ DISSOLVE TO:
  ✓ FADE TO: / FADE IN: / FADE OUT:
  ✓ SMASH CUT TO: (jarring)
  ✓ MATCH CUT TO: (visual match)
  ✓ MONTAGE: (series of scenes)
  ✓ And 4+ more standard transitions
```

## Module Interaction Diagram

```
User Input
    │
    ├─→ MainWindow.xaml.cs
    │   │
    │   ├─→ ScriptEditor_TextChanged()
    │   │   │
    │   │   ├─→ AutoFormattingEngine (existing)
    │   │   │   └─ Formats as-you-type
    │   │   │
    │   │   ├─→ SmartIndentationService (existing)
    │   │   │   └─ Auto-indent on Enter/Tab
    │   │   │
    │   │   └─→ UpdateStatistics() ◄─── NEW
    │   │       │
    │   │       ├─→ PaginationEngine.GetTotalPageCount()
    │   │       │   └─ Returns page count
    │   │       │
    │   │       ├─→ PaginationEngine.GetEstimatedScreenMinutes()
    │   │       │   └─ Returns estimated duration
    │   │       │
    │   │       └─→ Update Status Bar
    │   │           └─ Shows "Pages: X (~Y min)"
    │   │
    │   └─→ CheckAutoCompleteActivation()
    │       │
    │       └─→ ScreenplayFormattingRules.IsValidElement()
    │           └─ Detects element type for dropdown
    │
    └─→ Display Output
        ├─ Formatted text in TextBox
        ├─ Page count in status bar
        ├─ Screen time estimate
        └─ Current/next element hints
```

## Configuration Model

```
PageFormatting Configuration
├─ Page Size
│  ├─ Letter (8.5" × 11")
│  └─ A4 (8.27" × 11.69")
│
├─ Margins (inches)
│  ├─ Left: 1.5 (binding margin)
│  ├─ Right: 1.0
│  ├─ Top: 1.0
│  └─ Bottom: 1.0
│
├─ Typography
│  ├─ Font Family: "Courier New"
│  └─ Font Size: 12 points
│
├─ Screenplay Standard
│  └─ Lines Per Page: 55
│
└─ Calculated Properties
   ├─ Content Width: 5.77" (Letter) or 5.27" (A4)
   └─ Content Height: 9.0" (Letter) or 9.69" (A4)
```

---

*ScriptWriter Pro - Architecture & Data Flow*  
*Version 1.0 - A4 Pagination Implementation*
