# PowerDocu Settings

PowerDocu provides a range of configuration settings that control what gets documented and in which format. Settings can be configured through the **GUI** (Settings tab), **command line** flags, or by editing the **configuration file** directly.

## Configuration File

Settings are stored as a JSON file at:

```
%APPDATA%\PowerDocu\powerdocu.config.json
```

For example: `C:\Users\<username>\AppData\Roaming\PowerDocu\powerdocu.config.json`

The configuration file is loaded automatically when the GUI starts. In the GUI, click **"Save current configuration as default"** on the Settings tab to persist your current settings.

> **Note:** The command line interface (CLI) does **not** load the saved configuration file. CLI runs use default values unless overridden by command line flags.

---

## Input

### Items to Document

A semicolon-delimited list of file paths to generate documentation for. Accepted file types are `.zip` (Power Platform solution packages) and `.msapp` (Canvas App packages).

- **CLI flag:** `-q` / `--itemsToDocument <paths>`
- **GUI:** Use the file picker on the main tab to select files.

> This option is only used via the CLI. In the GUI, files are selected interactively.

---

## Output Settings

### Output Format

Determines in which format(s) the documentation is generated.

| Value | Description |
|---|---|
| `All` | Generate documentation in all formats (Word, Markdown, and HTML) |
| `Word` | Generate a Word document (.docx) |
| `Markdown` | Generate Markdown (.md) files |
| `HTML` | Generate HTML files |

- **Config key:** `outputFormat`
- **Default:** `"All"`
- **CLI flags:** `-w` / `--word`, `-m` / `--markDown`, `-h` / `--html` (combine flags for multiple formats; if all three are set, equivalent to `All`; if none are set, defaults to `Word`)

### Word Template

An optional path to a Word template file (`.docx`, `.docm`, or `.dotx`) used when generating Word documents. This allows you to apply custom styles, headers, footers, and branding to the generated documentation.

- **Config key:** `wordTemplate`
- **Default:** `null` (no template)
- **CLI flag:** `-t` / `--wordTemplate <path>`

> The Word template option is only relevant when the output format includes Word.

### Add Table of Contents

Whether to add a Table of Contents to generated Word documents.

- **Config key:** `addTableOfContents`
- **Default:** `false`
- **CLI flag:** `-n` / `--addTableOfContents`

> This setting is only relevant when the output format includes Word.

---

## Documentation Scope

These settings control which parts of a Power Platform solution are documented.

### Document Solution

Whether to generate documentation for the solution overview itself.

- **Config key:** `documentSolution`
- **Default:** `true`
- **CLI flag:** `-l` / `--documentSolution`

### Document Default Columns

Whether to include default Dataverse table columns in the solution documentation. These are the standard columns present on every Dataverse table (e.g., `Created On`, `Modified By`). Disabling this reduces noise when you only care about custom columns.

- **Config key:** `documentDefaultColumns`
- **Default:** `false`
- **CLI flag:** `-j` / `--documentDefaultColumns`

> This setting is only relevant when **Document Solution** is enabled.

### Show All Components in Graph

Whether to show all solution components in the relationship graph, including isolated ones that have no connections to other components.

- **Config key:** `showAllComponentsInGraph`
- **Default:** `true`
- **CLI flag:** `--showAllComponentsInGraph` (no short flag)

> This setting is only relevant when **Document Solution** is enabled.

### Document Flows

Whether to generate documentation for Power Automate flows contained in the solution.

- **Config key:** `documentFlows`
- **Default:** `true`
- **CLI flag:** `-p` / `--documentFlows`

### Flow Action Sort Order

Controls how flow actions are ordered in the generated documentation.

| Value | Description |
|---|---|
| `By name` | Sort actions alphabetically by name |
| `By order of appearance` | Keep actions in the order they appear in the flow definition |

- **Config key:** `flowActionSortOrder`
- **Default:** `"By name"`
- **CLI flag:** `-s` / `--sortFlowsByName` (when `true`, sorts by name; when `false`, sorts by order of appearance)

> This setting is only relevant when **Document Flows** is enabled.

### Document Agents

Whether to generate documentation for Copilot Studio agents (chatbots) contained in the solution.

- **Config key:** `documentAgents`
- **Default:** `true`
- **CLI flag:** Not available via CLI

### Document Model-Driven Apps

Whether to generate documentation for Model-Driven Apps contained in the solution.

- **Config key:** `documentModelDrivenApps`
- **Default:** `true`
- **CLI flag:** `-k` / `--documentModelDrivenApps`

### Document Business Process Flows

Whether to generate documentation for Business Process Flows contained in the solution.

- **Config key:** `documentBusinessProcessFlows`
- **Default:** `true`
- **CLI flag:** `-u` / `--documentBusinessProcessFlows`

### Document Apps

Whether to generate documentation for Canvas Apps and other apps contained in the solution. Disabling this also disables all app-specific sub-settings below.

- **Config key:** `documentApps`
- **Default:** `true`
- **CLI flag:** `-a` / `--documentApps`

---

## App Documentation Options

These settings provide fine-grained control over what is included in the app documentation. They are only relevant when **Document Apps** is enabled.

### Document App Properties

Whether to include the app-level properties (such as app name, description, and other metadata) in the documentation.

- **Config key:** `documentAppProperties`
- **Default:** `true`
- **CLI flag:** `-b` / `--documentAppProperties`

### Document App Variables

Whether to include app variables and collections in the documentation.

- **Config key:** `documentAppVariables`
- **Default:** `true`
- **CLI flag:** `-v` / `--documentAppVariables`

### Document App Data Sources

Whether to include the list of data sources (connectors) used by the app in the documentation.

- **Config key:** `documentAppDataSources`
- **Default:** `true`
- **CLI flag:** `-x` / `--documentAppDataSources`

### Document App Resources

Whether to include the resources (media files, images, etc.) bundled with the app in the documentation.

- **Config key:** `documentAppResources`
- **Default:** `true`
- **CLI flag:** `-r` / `--documentAppResources`

### Document App Controls

Whether to include the individual screen controls and their properties in the documentation.

- **Config key:** `documentAppControls`
- **Default:** `true`
- **CLI flag:** `-g` / `--documentAppControls`

---

## Canvas App Property Settings

These settings control how control properties in Canvas Apps are documented.

### Document Changes Only

When enabled, only properties that have been modified from their default values are documented. When disabled, all properties of every control are included, which produces more comprehensive but significantly larger documentation.

- **Config key:** `documentChangesOnlyCanvasApps`
- **Default:** `true` (changes only)
- **CLI flags:** `-f` / `--fullDocumentation` (set to `true` to document all properties), `-c` / `--changesOnly`

### Document Default Values

Whether to include the default values of Canvas App control properties in the documentation. This can be helpful for understanding what a default configuration looks like.

- **Config key:** `documentDefaultValuesCanvasApps`
- **Default:** `true`
- **CLI flag:** `-d` / `--defaultValues`

### Document Sample Data Sources

Whether to include sample/demo data sources used by Canvas Apps in the documentation. Sample data sources are typically placeholder data added during app development.

- **Config key:** `documentSampleData`
- **Default:** `false`
- **CLI flag:** `-e` / `--sampledatasources`

---

## Other Settings

### Check for Updates on Launch

Whether PowerDocu should automatically check for newer releases when the GUI application starts.

- **Config key:** `checkForUpdatesOnLaunch`
- **Default:** `true`
- **CLI flag:** Not available via CLI

---

## Example Configuration File

```json
{
  "outputFormat": "All",
  "documentChangesOnlyCanvasApps": true,
  "documentDefaultValuesCanvasApps": true,
  "flowActionSortOrder": "By name",
  "wordTemplate": null,
  "documentSampleData": false,
  "documentSolution": true,
  "documentFlows": true,
  "documentAgents": true,
  "documentApps": true,
  "documentAppProperties": true,
  "documentAppVariables": true,
  "documentAppDataSources": true,
  "documentAppResources": true,
  "documentAppControls": true,
  "documentDefaultColumns": false,
  "addTableOfContents": false,
  "showAllComponentsInGraph": true,
  "documentModelDrivenApps": true,
  "documentBusinessProcessFlows": true,
  "checkForUpdatesOnLaunch": true
}
```

---

## CLI Quick Reference

| Flag | Long Form | Description | Default |
|---|---|---|---|
| `-q` | `--itemsToDocument` | Semicolon-delimited list of file paths to document | — |
| `-w` | `--word` | Output as Word | `false` |
| `-m` | `--markDown` | Output as Markdown | `false` |
| `-h` | `--html` | Output as HTML | `false` |
| `-t` | `--wordTemplate` | Path to Word template | — |
| `-l` | `--documentSolution` | Document the solution | `true` |
| `-j` | `--documentDefaultColumns` | Document default Dataverse columns | `false` |
| `-p` | `--documentFlows` | Document flows | `true` |
| `-s` | `--sortFlowsByName` | Sort flow actions by name | `false` |
| `-a` | `--documentApps` | Document apps | `true` |
| `-b` | `--documentAppProperties` | Document app properties | `true` |
| `-v` | `--documentAppVariables` | Document app variables | `true` |
| `-x` | `--documentAppDataSources` | Document app data sources | `true` |
| `-r` | `--documentAppResources` | Document app resources | `true` |
| `-g` | `--documentAppControls` | Document app controls | `true` |
| `-f` | `--fullDocumentation` | Document all properties (not just changes) | `false` |
| `-c` | `--changesOnly` | Document changes only | `false` |
| `-d` | `--defaultValues` | Document default property values | `false` |
| `-e` | `--sampledatasources` | Document sample data sources | `false` |
| `-n` | `--addTableOfContents` | Add Table of Contents to Word docs | `false` |
| `-k` | `--documentModelDrivenApps` | Document Model-Driven Apps | `true` |
| `-u` | `--documentBusinessProcessFlows` | Document Business Process Flows | `true` |
| | `--showAllComponentsInGraph` | Show all components in relationship graph | `true` |
| `-i` | `--updateIcons` | Update connector icons | `false` |
| `-o` | `--outputPath` | Output directory path | Item path |
