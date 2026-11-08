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
| World panel ▸ Welcome Board (left) | no code | Tabs on a world panel: Welcome text with live *Info* (time, players, uptime), Rules, *Player List*. |
| World panel ▸ Teleport Board | no code | Buttons with *Teleport To* points (pads on the floor), *Respawn*, and a HUD toast. |
| World panel ▸ Room Controls (right) | no code | *Toggle* with *On Objects* (lamp), *Selector* with *Option Objects* (shape on the pedestal), *Slider* with *Audio Volume* (music). |
| World panel ▸ Poll Board | `ExamplePoll.cs` | A **synced** poll: one vote per player, sent to the owner with `[NetworkCallable]`; *Pie* and *Bar List* filled with `SetEntry`. |
| World panel ▸ Scoreboard (back left) | no code | A *Table* of 30 players inside a *Scroll* (the table grows, the scroll shows a window). Long names are cut with *Max Cell Chars*. |
| World panel ▸ News Board (front right) | no code | A *Bar List* with long names, and a *Scroll* of text items whose height follows their wrapped lines. |
| Test Wall (teleport **West**) | no code | *Button List*: 20 buttons in a *Scroll*. *Button Grid*: 12 cells (the grid grows with its cells). *More*: a row of buttons, a long toggle label, a long selector option and a text input. Every button shows a toast. Long labels shrink to fit. |
| World panel ▸ Shop Sign + Shop (behind spawn) | `ExampleShop.cs` | A sign's button opens a closed panel (*Menu To Toggle*, *Start Open* off, X to close). *Value* watches private `_coins`; buy / earn buttons show toasts. |
| HUD | `ExampleMenuEvents.cs` | Manager callbacks: menu opened / closed, platform changed. |

Everything is local UI. Only the world counter and the poll sync.
