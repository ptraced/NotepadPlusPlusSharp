# Caret

A fast, dark-mode text editor for Windows built from scratch in C# / WPF.

## Why does this exist?

In early 2026, [Kaspersky reported](https://securelist.com/notepad-supply-chain-attack/118708/) that the Notepad++ update infrastructure had been compromised for months — from roughly June through December 2025. Attackers pushed malicious updates to real users, targeting government orgs, financial institutions, and individuals across multiple countries. The infection chains rotated constantly, making the whole thing hard to detect.

After reading that, I didn't feel great about running Notepad++ anymore. So I built my own replacement. Caret does everything I actually used Notepad++ for, without depending on someone else's update pipeline.

## Features

**Editor**
- Syntax highlighting for 25+ languages (C#, C/C++, Python, JavaScript/TypeScript, Java, Rust, Go, Kotlin, Swift, HTML, XML, CSS, PHP, SQL, JSON, YAML, TOML, Dockerfile, Shell/Bash, Markdown, PowerShell, and more)
- Auto-detects the language from file content when there's no file extension to go on
- Code folding for brace-based and XML/HTML languages
- Find & Replace with regex support, match case, whole word, wrap around
- Go to line
- Line operations — duplicate, move up/down, toggle comment
- Multi-tab editing with drag-and-drop file opening
- Ctrl+scroll zoom per tab
- Word wrap, whitespace/EOL visualization, indent guides, line numbers

**Session persistence**
- Remembers everything on exit — open tabs, unsaved content, cursor positions, scroll offsets, zoom levels, window size/position, and all editor settings
- No "do you want to save?" on close. Just reopen and pick up where you left off.

**File handling**
- Auto-detect encoding (UTF-8, UTF-8 BOM, UTF-16 LE/BE, ANSI)
- Switch encoding on the fly
- Line ending detection (CRLF, LF, CR)
- Recent files list
- Print support

**Encrypted backups** *(optional — requires MongoDB)*
- Back up any open document to a local MongoDB instance with AES-256-GCM authenticated encryption
- Encryption key derived from your password using PBKDF2-SHA512 with 600,000 iterations and a random salt
- Each backup gets its own salt and nonce — identical files produce different ciphertext
- Browse, restore, and delete backups from the built-in Backup Manager (`Ctrl+B`)
- Fully local — no cloud, no third-party service, no network calls

**Editing tools**
- Upper/lowercase conversion
- Trim trailing whitespace
- Tab ↔ space conversion
- Select all, cut, copy, paste, undo, redo

**UI**
- Dark theme across the entire app — menus, tabs, dialogs, scrollbars, status bar, everything
- Dark title bar on Windows 10/11
- Status bar showing position, selection info, language, encoding, line endings, zoom, and file stats
- Right-click tab context menu (close, close others, close to left/right, copy path, open folder)
- Always on top option

**Keyboard shortcuts**
- `Ctrl+N/O/S/W` — new, open, save, close
- `Ctrl+Shift+S` — save as
- `Ctrl+F/H/G` — find, replace, go to line
- `Ctrl+D` — duplicate line
- `Alt+Up/Down` — move line
- `Ctrl+/` — toggle comment
- `Ctrl+Shift+U / Ctrl+U` — uppercase / lowercase
- `Ctrl+Tab / Ctrl+Shift+Tab` — switch tabs
- `Ctrl+Mousewheel` — zoom
- `Ctrl+0` — reset zoom
- `Ctrl+B` — backup manager
- `F3 / Shift+F3` — find next / previous

**Installer**
- Custom installer built from scratch in C# / WPF — no third-party installer frameworks (no Inno Setup, no WiX, no NSIS)
- Desktop shortcut
- Start menu entry
- Right-click context menu: "Edit with Caret" on any file or folder
- Add/Remove Programs registration with full uninstaller
- Silent uninstall support (`/uninstall /quiet`)

## Build

Requires .NET 10 SDK.

```
dotnet build
dotnet run
```

To publish a self-contained exe:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

To build the installer:

```
.\build-installer.ps1
```

The installer will be in the `Output/` folder. No external tools needed — the installer is a self-contained C# WPF app that gets compiled alongside Caret. It handles file extraction, shortcuts, context menu registration, and uninstallation using only Windows COM and .NET APIs.

## Encrypted backups (optional)

The backup feature requires a local MongoDB instance. MongoDB is **not** required to use Caret — it's only needed if you want encrypted backups.

### Install MongoDB

**Option 1 — MSI installer (recommended)**

1. Download the MongoDB Community Server installer from [mongodb.com/try/download/community](https://www.mongodb.com/try/download/community)
2. Choose "Complete" during setup
3. Leave "Install MongoDB as a Service" checked — it will start automatically on boot
4. Finish the installer. That's it.

**Option 2 — winget**

```
winget install MongoDB.Server
```

**Option 3 — Docker**

```
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

### Using backups

1. Open Caret and press `Ctrl+B` (or File → Backup Manager)
2. The default connection string `mongodb://localhost:27017` works out of the box if MongoDB is running locally
3. Enter an encryption password (minimum 8 characters) — this is never stored anywhere
4. Click "Backup Current Document" to create an encrypted backup
5. Select any backup from the list to restore or delete it

Your password is used to derive the encryption key. If you lose it, your backups cannot be recovered. This is by design.

## License

[GPL-3.0](LICENSE)
