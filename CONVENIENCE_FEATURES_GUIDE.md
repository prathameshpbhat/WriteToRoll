# Writing Convenience Features - Complete Guide

## Overview
These four convenience-focused services make screenplay writing faster, easier, and more intuitive. They complement the previously implemented Fade-In enterprise features by focusing on user experience and writing efficiency.

---

## 1. QuickFormatTemplateEngine

**Purpose:** Provide pre-built scene templates and formatting presets for instant scene creation.

### Key Features

#### Built-in Templates (8 types)
- **Phone Call Scene** - Two characters having a phone conversation with V.O.
- **Action Sequence** - Fast-paced choreographed action with stunts
- **Dialogue Heavy Scene** - Character-focused conversation
- **Montage Sequence** - Series of short scenes showing passage of time
- **Flashback Scene** - Jump to the past with transition markers
- **Entrance/Exit** - Quick character introduction or exit
- **Reaction/Emotion** - Show character's emotional response
- **Narration/Voiceover** - Narrator provides background or insight

#### Format Presets (4 types)
- **Standard Screenplay** - Industry-standard US format
- **European (A4)** - Adjusted for European paper sizes
- **TV Movie** - Tighter spacing for TV production
- **Stage Play** - Theater-specific formatting

### Usage Examples

```csharp
var engine = new QuickFormatTemplateEngine();

// Get all templates
var templates = engine.GetAllTemplates();

// Apply a template with replacements
var replacements = new Dictionary<string, string>
{
    { "CHARACTER", "JOHN" },
    { "CHARACTER2", "MARY" },
    { "Type dialogue here", "Where have you been?" }
};
var elements = engine.ApplyTemplate("phone_call", replacements);

// Get most used templates (learning pattern)
var popular = engine.GetMostUsedTemplates(count: 5);

// Get format preset
var preset = engine.GetFormatPreset("european");
```

### Key Methods

| Method | Purpose |
|--------|---------|
| `GetAllTemplates()` | List all available templates |
| `GetBuiltInTemplates()` | Get only pre-built templates |
| `GetCustomTemplates()` | Get user-created templates |
| `CreateTemplateFromScene()` | Save current scene as template |
| `ApplyTemplate()` | Create elements from template |
| `GetFormatPreset()` | Get formatting preset |
| `DeleteTemplate()` | Remove custom template |
| `GenerateTemplateStats()` | Get usage statistics |

---

## 2. FormatFixerService

**Purpose:** Automatically detect and fix common screenplay formatting mistakes.

### Detectable Issues

#### Character Name Issues
- Empty character names (CRITICAL)
- Incorrect capitalization
- Parentheticals in character name instead of separate element

#### Scene Heading Issues
- Missing or invalid INT/EXT format
- Incorrect capitalization
- Invalid time-of-day format

#### Dialogue Issues
- Empty dialogue (MAJOR)
- Missing punctuation
- Excessive punctuation/spacing

#### Transition Issues
- Non-standard transitions
- Incorrect capitalization

#### Parenthetical Issues
- Empty parentheticals
- Incorrect case for standard parentheticals

#### Action Issues
- Empty action lines (WARNING)
- Excessive spacing
- Too many capitalized words

### Issue Severity Levels

1. **WARNING (Severity 1)** - Style/consistency issues
2. **MAJOR (Severity 2)** - Formatting problems affecting readability
3. **CRITICAL (Severity 3)** - Blocking issues preventing proper parsing

### Usage Examples

```csharp
var fixer = new FormatFixerService();

// Detect all issues
var allIssues = fixer.DetectAllIssues(script);

// Detect issues in specific element
var elementIssues = fixer.DetectElementIssues(scriptElement, 0);

// Fix all auto-fixable issues
var report = fixer.FixAllIssues(script);
Console.WriteLine($"Fixed {report.IssuesFixed} of {report.TotalIssuesFound} issues");

// Apply specific fix
fixer.ApplyFix(script, formatIssue);

// Get issues by type
var caseIssues = fixer.GetIssuesByType(script, "Character Name Case");

// Generate audit report
var audit = fixer.GenerateFormatAudit(script);

// Quick cleanup (removes empty elements, excessive spacing)
fixer.QuickCleanup(script);
```

### Format Audit Report Example

```
=== FORMAT AUDIT REPORT ===
Total Issues Found: 47
  Critical: 2
  Major: 8
  Warnings: 37

Issues by Type:
  Character Name Case: 12
  Excessive Spacing: 18
  Missing Dialogue Punctuation: 10
  Scene Heading Case: 5
  Parenthetical Case: 2
```

---

## 3. WritingMetricsService

**Purpose:** Provide real-time statistics and insights about screenplay health and progress.

### Metrics Categories

#### Script Metrics
- Total elements count
- Action/Dialogue/Character/Transition counts
- Scene heading count

#### Page Metrics
- **Word Count** - Total words in script
- **Estimated Pages** - Based on 250 words/page
- **Estimated Runtime** - Based on 1 minute per page
- **Line Count** - Total lines

#### Character Metrics
- **Total Appearances** - Number of scenes character appears in
- **Line Count** - Number of dialogue lines
- **Words Spoken** - Total word count in dialogue
- **Percentage of Dialogue** - Share of total dialogue
- **Is Protagonist** - True if >25% of dialogue

#### Scene Metrics
- Scene number and heading
- Element count
- Character count
- Dialogue line count
- Action word count
- Estimated duration
- Time of day (INT/EXT analysis)
- Location

#### Health Metrics
- **Dialogue/Action Ratio** - Balance between dialogue and action
- **Average Scene Length** - Average elements per scene
- **Longest/Shortest Scene** - Scene length extremes
- **Character Distribution** - Main character focus percentage
- **Health Score** - Overall screenplay quality (EXCELLENT/GOOD/FAIR/NEEDS WORK/POOR)
- **Recommendations** - Specific suggestions for improvement

### Usage Examples

```csharp
var metrics = new WritingMetricsService();

// Analyze all metrics
var scriptMetrics = metrics.AnalyzeScriptMetrics(script);
var pageMetrics = metrics.CalculatePageMetrics(script);
var characters = metrics.AnalyzeCharacterMetrics(script);
var scenes = metrics.AnalyzeSceneMetrics(script);
var health = metrics.CalculateHealthMetrics(script);

// Quick statistics (one-liner)
var quickStats = metrics.GenerateQuickStats(script);
// Output: "📄 120 pages | ⏱️ 120min | 👥 28 chars | 🎬 45 scenes"

// Generate full report
var fullReport = metrics.GenerateMetricsReport(script);

// Get character appearance timeline
var timeline = metrics.GetCharacterAppearanceTimeline(script);
// Output: { "JOHN": [1, 3, 5, 7, 10], "MARY": [2, 4, 6, 8] }

// Get top 5 characters
var characters = metrics.AnalyzeCharacterMetrics(script).Take(5);
```

### Metrics Report Example

```
=== SCREENPLAY METRICS REPORT ===

📊 SCRIPT OVERVIEW
  Total Elements: 1,247
  Total Scenes: 45

📄 PAGE METRICS
  Word Count: 30,000
  Estimated Pages: 120
  Estimated Runtime: 120 minutes

🎬 CONTENT BREAKDOWN
  Action Lines: 450
  Dialogue Lines: 520
  Character Appearances: 750
  Transitions: 45

👥 TOP CHARACTERS
  JOHN: 185 lines (24.5%)
  MARY: 142 lines (18.8%)
  DETECTIVE: 95 lines (12.6%)
  OFFICER: 78 lines (10.3%)
  WITNESS: 65 lines (8.6%)

🎞️ SCENE ANALYSIS
  Average Scene Length: 27.7 elements
  Longest Scene: 89 elements
  Shortest Scene: 5 elements

💪 SCREENPLAY HEALTH
  Health Score: GOOD
  Dialogue/Action Ratio: 1.16

💡 RECOMMENDATIONS
  • Add more dialogue for character development
  • Develop supporting characters more
```

---

## 4. QuickActionsService

**Purpose:** Provide common operations and shortcuts for rapid screenplay editing.

### Undo/Redo System
- Automatic snapshot creation before modifications
- Stack-based undo/redo with configurable levels (default: 50)
- Clear redo stack on new action
- Full script state preservation

### Quick Operations

#### Scene Operations
- **Duplicate Scene** - Copy scene with all elements
- **Add Scene** - Insert new scene with heading
- **Remove Empty Scenes** - Clean up empty scenes

#### Character Operations
- **Rename Character** - Replace all instances of character name (returns count)
- **Merge Consecutive Dialogue** - Merge multiple lines from same character

#### Element Operations
- **Add Action** - Insert action line at specific position
- **Add Dialogue** - Insert character + parenthetical + dialogue
- **Add Dialogue to Character** - Insert after specific character's dialogue
- **Normalize Parentheticals** - Standardize (V.O., O.S., CONT'D)

#### Text Operations
- **Replace All Text** - Find/replace across entire script (case-sensitive option)
- **Cleanup Empty Elements** - Remove all empty elements

### Usage Examples

```csharp
var actions = new QuickActionsService();

// Create snapshot for undo
actions.CreateSnapshot(script, "Before major edit");

// Quick dialogue addition
actions.AddDialogue(script, "JOHN", "Where have you been?", "angry");

// Quick action addition
actions.AddAction(script, "JOHN enters the room, soaking wet.");

// Quick scene addition
actions.AddScene(script, "JOHN'S APARTMENT", "NIGHT");

// Duplicate a scene
actions.DuplicateScene(script, sceneIndex, insertAfter: true);

// Rename character
int renamed = actions.RenameCharacter(script, "JOHN", "JACK");

// Replace all instances
int replaced = actions.ReplaceAllText(script, "hospital", "clinic", caseSensitive: false);

// Merge consecutive dialogue
int merged = actions.MergeConsecutiveDialogue(script);

// Normalize all parentheticals
int normalized = actions.NormalizeParentheticals(script);

// Cleanup
int cleaned = actions.CleanupEmptyElements(script);

// Undo/Redo
if (actions.GetUndoCount() > 0)
{
    actions.Undo(script);
}

if (actions.GetRedoCount() > 0)
{
    actions.Redo(script);
}

// Get all available quick commands
var commands = actions.GetAllCommands();
var menu = actions.GenerateQuickActionsMenu();
```

### Quick Actions Menu Output

```
=== QUICK ACTIONS MENU ===

[Ctrl+D] Duplicate Scene
  Create a copy of the current scene

[Ctrl+H] Rename Character
  Replace all instances of a character name

[Shift+A] Quick Add Action
  Insert action line at cursor

[Shift+D] Quick Add Dialogue
  Insert dialogue line at cursor
```

### Undo/Redo Example

```csharp
// Work sequence
actions.CreateSnapshot(script, "Initial state");
actions.AddAction(script, "Scene 1 action");

actions.CreateSnapshot(script, "After action 1");
actions.AddDialogue(script, "JOHN", "First line");

actions.CreateSnapshot(script, "After dialogue");
actions.AddDialogue(script, "MARY", "Response");

// Now we have 3 undo levels
Console.WriteLine($"Undo levels: {actions.GetUndoCount()}"); // 3

// Undo changes
actions.Undo(script); // Back to "After dialogue"
actions.Undo(script); // Back to "After action 1"
actions.Undo(script); // Back to "Initial state"

Console.WriteLine($"Redo levels: {actions.GetRedoCount()}"); // 3

// Redo forward
actions.Redo(script); // Forward to "After action 1"
```

---

## Integration with Existing Services

### With SmartAutocompleteEngine
- Use QuickFormatTemplateEngine templates as autocomplete suggestions
- Track template usage for smart suggestions

### With AutoFormattingService
- Use FormatFixerService to cleanup after auto-formatting
- Integrate format presets from QuickFormatTemplateEngine

### With WritingMetricsService & PaginationEngine
- Metrics track pagination impact
- Pagination adjustments affect metrics calculations

### With FormattingService
- FormatFixerService validates formatting service output
- Templates use FormattingMeta from QuickFormatTemplateEngine

---

## Performance Characteristics

| Service | Time Complexity | Space Complexity |
|---------|-----------------|------------------|
| QuickFormatTemplateEngine | O(n) for template application | O(n) for template storage |
| FormatFixerService | O(n) for full audit | O(n) for issue list |
| WritingMetricsService | O(n) for metrics calculation | O(m) where m = character count |
| QuickActionsService | O(1) for snapshot | O(n) for snapshot storage |

---

## Best Practices

### 1. Template Management
- Create templates for frequently used scene types
- Use clear, descriptive placeholder names
- Review template statistics to identify patterns

### 2. Format Fixing
- Run format audit before final export
- Use auto-fix for non-critical issues only
- Review critical issues manually

### 3. Metrics Tracking
- Use quick stats for progress monitoring
- Review health score recommendations regularly
- Track character distribution for balance

### 4. Quick Actions
- Create snapshots before major edits
- Use templates + quick actions for rapid scene creation
- Batch similar operations together

---

## Example Workflow

### Complete Scene Addition with Validation

```csharp
var template = new QuickFormatTemplateEngine();
var fixer = new FormatFixerService();
var metrics = new WritingMetricsService();
var actions = new QuickActionsService();

// 1. Create snapshot
actions.CreateSnapshot(script, "Before adding dialogue scene");

// 2. Apply template
var replacements = new Dictionary<string, string>
{
    { "CHARACTER1", "JOHN" },
    { "CHARACTER2", "MARY" }
};
var elements = template.ApplyTemplate("dialogue_scene", replacements);

// 3. Add elements
foreach (var element in elements)
{
    script.Elements.Add(element);
}

// 4. Fix formatting
var issues = fixer.DetectAllIssues(script);
var fixReport = fixer.FixAllIssues(script);

// 5. Check metrics
var metrics = metrics.GenerateQuickStats(script);
Console.WriteLine($"Updated metrics: {metrics}");

// 6. Undo if needed
if (fixReport.IssuesFixed > 5)
{
    actions.Undo(script); // Revert if too many issues
}
```

---

## Summary

These four convenience services work together to make screenplay writing:
- **Faster** - Templates and quick actions eliminate repetitive typing
- **Easier** - Automatic fixes and metrics provide guidance
- **Better** - Health metrics and recommendations improve script quality
- **More Intuitive** - Pre-built templates and common shortcuts match user expectations

Combined with the 10 enterprise Fade-In features, the WriteToRoll application now provides a comprehensive professional screenwriting solution.

---

**Total New Code:** 2,019 lines across 4 services
**Services Added:** QuickFormatTemplateEngine, FormatFixerService, WritingMetricsService, QuickActionsService
**Built-in Templates:** 8 pre-configured scene types
**Format Presets:** 4 industry-standard formats
**Quick Commands:** 4 primary shortcuts + 8+ operations
