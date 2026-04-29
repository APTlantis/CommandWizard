# Aptlantis Command Wizard

**Aptlantis Command Wizard** is a schema-driven tool for constructing terminal commands through an interactive interface.
Instead of memorizing complex command syntax or reading lengthy help manuals, the wizard guides the user through structured questions and generates correct CLI commands automatically.

The project is built on a simple principle:

**Describe the task → generate the command.**

This tool is designed to work particularly well in environments where the command line is the preferred interface for automation, scripting, and system management.

---

# Philosophy

Command-line tools are powerful but often difficult to use because they assume prior knowledge of syntax. Most CLI documentation explains *how commands are structured*, but users typically think in terms of *what they want to accomplish*.

The Aptlantis Command Wizard reverses that relationship.

Instead of starting with syntax, the wizard begins with intent.

```
Intent → Options → Parameters → Command
```

For example:

```
Task: Mirror a directory to a remote server
Tool: rsync
Options: compression, archive mode, delete removed files
Source: /data/crates
Destination: mirror:/srv/crates
```

Generated command:

```
rsync -avz --delete /data/crates mirror:/srv/crates
```

The user never needs to remember the exact command structure.

---

# Core Idea

Every command-line tool has a hidden structure:

```
tool [action] [flags] [options] [parameters]
```

Examples:

```
git clone repository
docker run -d nginx
rsync -av source dest
```

The Command Wizard stores this structure in a schema so that commands can be assembled programmatically.

The interface simply asks questions derived from that schema.

---

# Schema Driven Architecture

Command syntax is described using structured metadata stored as TOML or JSON.

Each CLI tool has its own schema definition.

Example:

```toml
[tool]
name = "rsync"
description = "Fast file synchronization tool"

[[arguments]]
flag = "-a"
long = "--archive"
description = "archive mode"
type = "boolean"

[[arguments]]
flag = "--delete"
description = "delete removed files"
type = "boolean"

[[arguments]]
flag = "--progress"
description = "show transfer progress"
type = "boolean"

[[parameters]]
name = "source"
type = "path"

[[parameters]]
name = "destination"
type = "path"
```

The wizard reads these schemas and generates the interactive interface automatically.

This approach allows the system to support thousands of CLI tools without hardcoding logic for each one.

---

# Command Construction Flow

The wizard walks the user through several stages.

### 1. Tool Selection

The user chooses the command-line tool.

```
Select tool:
  rsync
  git
  docker
  ffmpeg
  winget
```

### 2. Action Selection

Some tools include subcommands.

```
git clone
git pull
git push
```

### 3. Option Selection

Boolean flags and optional arguments are presented.

```
Enable archive mode?        [Y/N]
Enable compression?         [Y/N]
Delete removed files?       [Y/N]
```

### 4. Parameter Input

Required parameters are requested.

```
Source directory: ________
Destination: ________
```

### 5. Command Generation

The wizard assembles the command.

```
rsync -avz --delete /data/crates mirror:/srv/crates
```

---

# Command Shapes

Many CLI tools follow common structural patterns.

Recognizing these patterns simplifies schema creation.

### File Operations

```
tool [options] source destination
```

Examples:

```
cp file1 file2
mv old new
rsync source dest
```

### Action Commands

```
tool action [options]
```

Examples:

```
git clone repo
docker run image
systemctl restart service
```

### Pipeline Commands

```
tool | tool | tool
```

Examples:

```
cat file | grep text | sort
```

These patterns allow the wizard to build interfaces dynamically.

---

# Project Architecture

The project is designed as a modular system separating the command engine from the interface.

```
Aptlantis.CommandWizard.Core
Aptlantis.CommandWizard.UI
```

## Core Engine

Responsible for:

* loading command schemas
* parsing help menus
* constructing commands
* exporting scripts
* validating options

## UI Layer

Responsible for:

* displaying wizard pages
* collecting user input
* previewing generated commands

The UI layer never needs to understand command syntax directly.

---

# Recommended Visual Studio Project

The desktop version of the wizard should be built as a:

```
WPF App (.NET 8)
```

Reasons:

* excellent support for dynamic interfaces
* strong data binding
* mature Windows UI framework
* ideal for wizard-style applications

This allows the UI to automatically adapt based on schema definitions.

---

# Schema Storage

Schemas are stored as files and loaded dynamically.

Example structure:

```
schemas/
  git.toml
  docker.toml
  rsync.toml
  ffmpeg.toml
  winget.toml
```

This enables the command catalog to grow indefinitely without modifying application code.

---

# Automatic Command Discovery

Future versions will be able to discover installed CLI tools automatically.

### Step 1 — Discover executables

Example PowerShell command:

```
Get-Command -CommandType Application | Select-Object -ExpandProperty Name
```

### Step 2 — Probe help output

Most CLI tools expose help text:

```
tool --help
tool -h
```

The wizard can capture this output and attempt to extract flags automatically.

### Step 3 — Build schema

Detected arguments are converted into structured metadata which can then be refined by the user.

---

# Current Features (V1 Snapshot)

The current app ships with a practical subset of the long‑term vision.

### Help‑Based Schema Import

You can import a schema by running a tool’s help output directly in the app.

* **File → Import From Help...** opens a dialog.
* Enter a command (for example, `ffmpeg`, `bun`, `bon`, `bonnet`) and a help argument (`--help` by default).
* The app captures help output, parses flags and actions, and creates a schema in memory.
* Review in the **Schema Editor** and click **Save Schema** to write a TOML file.

### Tooltip Descriptions

Parsed flag descriptions appear as hover tooltips in the Wizard.
If a help entry is incorrect or incomplete, update the description in the **Schema Editor** and the tooltip updates automatically.

### File‑Based Schemas

Schemas are stored as `.toml` files in:

```
schemas/
```

They are loaded on app startup and can be edited at any time.

### Command History (JSON)

Generated commands are appended to a local JSON file:

```
history.json
```

This creates a lightweight personal command history without requiring a database.

---

# WinAppSDK Init (CLI)

If you want to bootstrap a similar WinAppSDK desktop app using the official Microsoft CLI flow:

```
dotnet new --install Microsoft.WindowsAppSDK.Templates
dotnet new winappsdk -n CommandWizard
```

This project follows that template style (WPF + WinAppSDK conventions), but does not require any structural migration for V1.

---

# Command Explanation Mode

The wizard can also explain commands.

Example input:

```
ffmpeg -i input.mkv -c:v libx264 -crf 23 -preset slow output.mp4
```

Explanation:

```
ffmpeg          video processing tool
-i input.mkv    input file
-c:v libx264    H.264 codec
-crf 23         quality level
-preset slow    encoding speed preset
```

This turns the wizard into a learning tool as well as a command generator.

---

# Script Export

Generated commands can be exported as scripts.

Supported formats may include:

```
PowerShell
Bash
Batch
Shell pipelines
```

Example export:

```powershell
rsync -avz --delete /data/crates mirror:/srv/crates
sha256sum crates.tar.zst > crates.hash
gpg --sign crates.hash
```

---

# Command History Database

Every generated command can be stored as structured metadata.

Example record:

```json
{
  "tool": "rsync",
  "task": "mirror directory",
  "command": "rsync -avz --delete /data/crates mirror:/srv/crates",
  "tags": ["backup", "mirror", "sync"]
}
```

This effectively creates a searchable personal command library.

---

# Long-Term Vision

The Aptlantis Command Wizard can evolve into a comprehensive structured map of the command-line ecosystem.

Possible future features:

* automatic CLI schema discovery
* natural language command generation
* pipeline construction tools
* command visualization
* script generation workflows
* integration with automation systems
* local AI-assisted command search

The underlying dataset — structured command definitions — becomes valuable in its own right.

---

# Development Roadmap

### Version 0.1

* manual command schemas
* basic wizard interface
* command preview

### Version 0.5

* import `--help` menus
* schema editor

### Version 1.0

* automatic PATH scanning
* command catalog

### Version 2.0

* natural language interface
* AI command suggestions

### Version 3.0

* full CLI knowledge database
* pipeline builder
* automation integrations

---

# Summary

The Aptlantis Command Wizard transforms the command line from a memorization problem into a structured system.

Instead of requiring users to remember syntax, the wizard asks questions and constructs commands automatically.

By representing CLI tools as structured schemas, the system can scale to support thousands of commands while remaining easy to use.

In the long term, this approach enables the creation of a structured knowledge base for command-line tools — turning terminal expertise into data that can be explored, reused, and automated.
