# UIHUD Examples

Open **UIHUD Examples.unity** and press Play. Press **M** (desktop) to open the menu. In VR use the wrist button, on mobile the MENU button.

| Where | Example | Shows |
|---|---|---|
| Menu ▸ Welcome | no code | Buttons with Notification, Open Menu, Respawn. Live Info text. |
| Menu ▸ Health | `ExampleHealth.cs` | Buttons call your methods (*Target* + *Event*), drive the HUD health bar, warn on low health. |
| Menu ▸ Settings | `ExampleSettings.cs` | Toggle / Slider / Selector / Text input pass their value (*Value Variable*) then send an event. |
| Menu ▸ Runtime | `ExampleRuntimeList.cs` | `manager.AddWidget`, `SetLabel`, `SetEvent`, `RemoveWidget` into a scroll list. |
| Menu ▸ Animations | no code | Every entrance effect (VRCTween), played in sequence when the page opens. |
| Menu ▸ Layouts | no code | Row, grid, nested tabs, < > selector pages. |
| Menu ▸ Data | no code | *Value* and *Graph* watch the private `_health` of `ExampleHealth` (use Hurt / Heal on the hotbar), *Table* shows a JSON leaderboard sorted by score. |
| Menu ▸ Charts | no code | *Stat* tile and *Gauge* watch `_health` (Hurt / Heal), a *Pie* chart of team sizes and a *Bar List* of team scores from JSON. |
| Quick menu (left) | no code | A second menu with its own key (**Q**), its own wrist / mobile button ("Quick"), and an *Open Quick menu* button. Its buttons call Manager methods directly. |
| Hotbar (VR + desktop: 45° below you, locked to the body; mobile: bottom of the screen) | no code | Separate from the HUD. Mixed slots: *Heal* / *Hurt* buttons call `ExampleHealth`, a *Music* toggle, a *Volume* slider, and two toast buttons. Desktop keys 1–6. |
| World panel (in front of spawn) | `ExampleWorldCounter.cs` | A kiosk panel with a **synced** counter everyone sees. |
| HUD | `ExampleMenuEvents.cs` | Manager callbacks: menu opened / closed, platform changed. |

Everything is local UI. Only the world counter syncs its number.
