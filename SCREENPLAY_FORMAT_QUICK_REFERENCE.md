# ScriptWriter Pro - Professional Screenplay Format Quick Reference

## Complete Screenplay Example

```
                         MY STORY
                       
                      Written by
                         John Doe
                   
                    john@example.com
                        555-1234


FADE IN:

INT. COFFEE SHOP - MORNING

A cozy cafe with morning light streaming through large windows. 
JOHN (30s, tired eyes, expensive suit) sits at a corner table, 
nervously checking his watch.

The bell above the door CHIMES.

SARAH (late 20s, confident, warm smile) enters. She spots John 
and approaches.

                              SARAH
                         (sitting down)
                    Sorry I'm late.

JOHN
You're always late.

SARAH
(beat)
That's not fair.

John softens, reaches across the table.

JOHN
I know. I'm sorry.

Sarah takes his hand. A moment passes between them.

                              SARAH
                   Is this goodbye?

JOHN
I don't want it to be.

CUT TO:

INT. AIRPORT TERMINAL - DAY

John walks through the terminal, pulling his suitcase. He looks 
back one last time.

FADE OUT.

                          THE END
```

## Format Reference Chart

| Element | Format | Case | Margin | Alignment | Example |
|---------|--------|------|--------|-----------|---------|
| Scene Heading | `INT/EXT. LOCATION - TIME` | UPPER | 1.5" | Left | `INT. OFFICE - DAY` |
| Action | Description of what happens | Title | 1.5" | Left | `John walks to the window.` |
| Character | Speaker name | UPPER | Centered | Center | `JOHN` |
| Parenthetical | (direction or extension) | lower* | 3.1" | Left | `(beat)` or `(V.O.)` |
| Dialogue | What character says | Normal | 2.5" | Left | `"I love you."` |
| Transition | Scene change method | UPPER | 1.0" | Right | `CUT TO:` |

*Special codes stay UPPERCASE: (V.O.), (O.S.), (CONT'D)

## Element Type Detection (What ScriptWriter Recognizes)

### Scene Heading
```
INT. COFFEE SHOP - DAY
EXT. BEACH - SUNSET
INT/EXT. CAR - MOVING - NIGHT
```
→ Detected by: `INT/EXT` at start

### Action
```
John walks across the room.
He picks up a pen from the desk.
```
→ Detected by: Normal text (default type)

### Character
```
JOHN
SARAH (V.O.)
JOHN (CONT'D)
```
→ Detected by: ALL CAPS line (no lowercase mixed)

### Dialogue
```
"I should have known better."
"Yes, I understand."
```
→ Detected by: Appears after CHARACTER line

### Parenthetical
```
(beat)
(looking back)
(V.O.)
(O.S.)
(CONT'D)
```
→ Detected by: Starts with `(` and ends with `)`

### Transition
```
CUT TO:
DISSOLVE TO:
FADE IN:
SMASH CUT TO:
```
→ Detected by: Transition verb + `:`

## Page Calculations

### Screenplay Length Guide
| Pages | Minutes | Duration |
|-------|---------|----------|
| 10 | ~10 min | Short scene |
| 30 | ~30 min | TV episode |
| 60 | ~60 min | 1-hour drama |
| 90 | ~90 min | 1.5-hour feature |
| 110 | ~110 min | Standard feature (2 hr) |
| 120 | ~120 min | Long feature |

**Formula**: Pages × 55 lines/page = Total lines  
**Screen Time**: Pages × 1 minute/page = Estimated duration

## Professional Margins Breakdown

### Left Margin (1.5")
- Accounts for hole-punching (binding on left side)
- Critical for production copies
- All elements except transitions respect this margin

### Right Margin (1.0")
- Prevents overflow on right side
- Ensures text doesn't break awkwardly
- Helps with professional appearance

### Top/Bottom (1.0" each)
- Standard for document margins
- Leaves space for page numbers, headers
- Professional appearance

### Character Name Position (3.7" from left)
- Centered between left and right margins
- Standard across all professional scripts
- Positions dialogue naturally on page

### Dialogue Position (2.5" from left)
- Indented from character name
- Narrower width focuses reader on words
- Creates visual hierarchy: Character > Dialogue

### Parenthetical Position (3.1" from left)
- Between character name and dialogue edges
- Smaller indent shows it's sub-element of dialogue
- Clear visual subordination

## Common Transitions

### Scene Transitions
- `CUT TO:` - Immediate scene change (most common)
- `DISSOLVE TO:` - Smooth fade between scenes
- `FADE TO:` - Fade to black, then new scene
- `FADE OUT:` - Scene ends, fade to black

### Special Effects
- `SMASH CUT TO:` - Sudden, jarring cut (dramatic moment)
- `MATCH CUT TO:` - Visual or audio match between scenes
- `MONTAGE:` - Series of quick scenes (music/action sequence)

### Less Common
- `WIPE TO:` - Wipe effect (rarely used in modern scripts)
- `IRIS TO:` - Iris effect (rarely used)
- `IRIS OUT:` - Opposite of above

### Opening/Closing
- `FADE IN:` - Script begins (opens from black)
- `FADE OUT:` - Script ends (fades to black)
- `CUT TO BLACK` - Immediate fade to black

## Character Extensions

Used in dialogue for additional speaker information:

### Standard Extensions
- `(V.O.)` - Voice Over (character speaks but not on screen)
- `(O.S.)` - Off Screen (character speaks from outside camera view)
- `(CONT'D)` - Continued (character continues speaking from previous page)

### Example Usage
```
INT. OFFICE - DAY

John stands alone.

JOHN (V.O.)
I never thought it would come to this.

(Phone RINGS off screen)

JOHN (O.S.)
(into phone)
Hello?

VOICE (O.S.)
Is this John?

(Later...)

JOHN (CONT'D)
So what do we do now?
```

## Professional Formatting Rules Enforced by ScriptWriter

### Scene Headings
- ✓ Automatically converts to UPPERCASE
- ✓ Ensures `INT. / EXT.` format
- ✓ Adds location and time-of-day
- ✓ Proper spacing between elements

### Character Names
- ✓ Forces UPPERCASE
- ✓ Centers on page (3.7" from left)
- ✓ Removes extensions (go in parenthetical)
- ✓ Clear visual hierarchy

### Dialogue
- ✓ Preserves speaker's exact words
- ✓ Maintains proper indentation
- ✓ Supports multi-line dialogue
- ✓ Proper spacing and alignment

### Parentheticals
- ✓ Enforces lowercase (except V.O., O.S., CONT'D)
- ✓ Proper indentation (3.1" from left)
- ✓ Clear visual distinction from dialogue
- ✓ Recognizes direction cues and extensions

### Actions
- ✓ Maintains paragraph format
- ✓ Left alignment at 1.5" margin
- ✓ Title case first letter capitalization
- ✓ Natural text flow

### Transitions
- ✓ All caps formatting
- ✓ Proper transition names recognized
- ✓ Standard punctuation (`:`)
- ✓ Right or top-left alignment options

## Status Bar Information

ScriptWriter displays real-time statistics:

```
Elements: 42 | Words: 1,247 | Pages: 1 (~1 min) | Caret: 156 | 📝 Action | Next: Dialogue
```

- **Elements**: Line count in script
- **Words**: Total word count
- **Pages**: Calculated pages (~55 lines each) + estimated screen time
- **Caret**: Current character position
- **Current Element**: Current line type
- **Next Element**: Suggested next element per screenplay structure

## Keyboard Shortcuts for Formatting

| Action | Shortcut | Effect |
|--------|----------|--------|
| New Line | Enter | Auto-indents next element |
| Tab | Tab | Suggests next element |
| Auto-Complete | Ctrl+Space | Shows dropdown suggestions |
| Character Hint | Alt+C | Shows known character names |
| Transition | Alt+T | Shows common transitions |

## Export & Print

**Current Support**:
- ✓ Text export (.txt)
- ✓ Copy formatted text
- ✓ Print with proper margins
- ✓ Screen time estimation

**Future Support**:
- Final Draft (.fdx)
- PDF with pagination
- Fountain format (.fountain)
- HTML with proper formatting

---

*ScriptWriter Pro - Professional Screenwriting Software*  
*Version: A4 Pagination & Professional Format 1.0*  
*Based on Industry Standards & "How to Write a Movie Script"*
