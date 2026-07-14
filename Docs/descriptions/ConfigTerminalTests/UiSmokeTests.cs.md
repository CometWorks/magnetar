# ConfigTerminalTests/UiSmokeTests.cs

**Project:** ConfigTerminalTests · **Namespace:** `Magnetar.ConfigTerminal.Tests` · **Kind:** class · **Lines:** 356

## Summary
Headless UI tests that build the `AppShell` view tree against Terminal.Gui's `FakeDriver` and pump a few main-loop iterations, catching constructor/layout exceptions without a real terminal — plus focused coverage of the log viewer's behaviour. Beyond the smoke test that constructs and navigates every main content view, it opens the log viewer over seeded logs and verifies: the keyword-highlight colours actually reach the driver's cell buffer, the pane shows a full screen of the tail on open (regression for the ctor-time scroll running before layout), text search selects/scrolls to a match with wrap-around, clearing the search drops the selection and forgets the term, `Esc` also clears a highlighted-line-navigation status, `[`/`]` step between highlighted lines with wrap-around, and search honours the case-sensitivity and whole-word options. Runs in the `ui-single-threaded` collection so Terminal.Gui's global state is never touched concurrently. The log-viewer behaviours are key-triggered through a modal prompt, so the tests drive the view's private members via reflection.

## Types
### UiSmokeTests — class, public, implements `IDisposable`, `[Collection("ui-single-threaded")]`
Sets up a temp data dir with a `Saves` folder in the ctor; `Dispose` calls `Application.Shutdown()` and deletes the dir.

- **Fields:** `dir` — temp instance directory; `Private` — `BindingFlags` for the reflection helpers.
- **Methods:**
  - `Shell_builds_and_pumps_without_throwing()` — builds an `AppShell`, `Application.Begin`s it, pumps, then exercises navigation into every main content view (`ShowServerSettings` … `ShowProfiles`).
  - `Log_viewer_renders_highlight_colours_into_the_cell_buffer()` — asserts the "Game ready" (black-on-green) and "Exception" (yellow-on-red) attributes appear in the `FakeDriver` cell buffer.
  - `Log_viewer_shows_a_full_screen_of_the_tail_on_open()` — over a 200-line log, asserts the last line sits in a full viewport (`TopRow <= Lines-2`).
  - `Log_viewer_search_selects_the_matching_line()` / `Log_viewer_clear_search_drops_selection_and_restores_hint()` — search selects/scrolls to a match; clearing drops the selection and forgets the term.
  - `Log_viewer_navigates_between_highlighted_lines()` / `Log_viewer_esc_clears_highlight_navigation_status()` — `[`/`]` step between highlighted lines with wrap-around; Esc restores the default hints.
  - `Log_viewer_search_honours_the_case_sensitivity_option()` / `Log_viewer_search_honours_the_whole_word_option()` — the Find options change what matches.
  - Helpers: `WithLogViewer(Action<AppShell,FakeDriver>)` (boot + open + pump + teardown), `InitHeadless()` (reflectively constructs Terminal.Gui's internal `FakeMainLoop`), `Pump`, `GetContent`/`GetLogPane`/`GetLogField`/`SetLogField`/`CallLog` (reflection into the viewer's private state), and `BufferHasAttribute` (scans the `FakeDriver` cell buffer).

## Cross-references
- **Uses:** `AppShell`, `TurboVisionTheme`, `LogViewerView` (`ConfigTerminal/Ui/`); `InstanceBinding` (`ConfigTerminal/Model/`); Terminal.Gui `FakeDriver`/`Application`/`IMainLoopDriver`/`TextView`/`Label`/`Attribute`/`KeyEvent` and internal `FakeMainLoop` (via reflection); xUnit (`Collection`, `Fact`); `System.Reflection`.
- **Used by:** _none within the repository_
