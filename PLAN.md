# Aptlantis Command Wizard V1 Plan (Delivered)

**Summary**
V1 has been implemented: the app loads TOML schemas, guides users through tool/action/options/parameters, and outputs a single‑line command preview. It also supports help‑based schema import with tooltip descriptions and persists command history to JSON.

**Key Changes**
1. **Schema Loading (TOML)**
   - Add a schema loader that reads `.toml` files from a `schemas/` folder in the app directory.
   - Define a minimal schema model matching the example: `tool` metadata, `arguments` (flags/long, description, type), `parameters` (name, type, required by default).
   - Parse TOML into in‑memory models on startup; surface parse errors as user‑friendly messages.

2. **Wizard UI**
   - Build a single‑window, multi‑section UI:
     - Tool selector (list of schema names + description).
     - Action selector (optional; show only if present in schema).
     - Options list (checkboxes for boolean flags; text boxes for options requiring values).
     - Parameters inputs (text boxes for required params).
     - Live command preview (single line).
   - Use a ViewModel with change notification; updates should recompute preview on any change.

3. **Command Builder**
   - Assemble command in canonical order: `tool` → `action` (if any) → selected flags/options → parameter values in schema order.
   - Omit empty/unchecked options.
   - Do not implement pipeline or script export.

4. **History Persistence**
   - On “Generate” or “Copy,” append a history record to a local JSON file (e.g., `history.json`) containing tool, task label (optional), and command string.
   - Keep history append‑only for v1; no UI to browse history yet (unless trivial to add).

5. **Help‑Based Schema Import**
   - Import tool schemas from `--help` output.
   - Parse flags and commands into editable schema entries.
   - Show flag descriptions as hover tooltips in the Wizard UI.

**Tests**
1. Schema parsing: valid TOML parses into model.
2. Schema parsing: invalid TOML yields a user‑visible error without crashing.
3. Command assembly: given a schema + selected inputs, output matches expected order and formatting.
4. UI binding: toggling options/parameters updates preview.

**Assumptions**
- The app remains WPF (`net10.0-windows`, `UseWPF=true`) in `A:\AptWeb\zypper-operations\CommandWizard\CommandWizard.csproj`.
- Schemas are stored locally in `A:\AptWeb\zypper-operations\CommandWizard\schemas\`.
- Command preview is single‑line only for v1.
- History persistence is JSON in app directory; no migration or versioning yet.

**Known Limitations**
- No positional parameter parsing from `Usage:` lines yet.
- No recursive subcommand probing (e.g., `tool subcommand --help`).
- Help text editing is done via the Schema Editor, not inline in the Wizard.
