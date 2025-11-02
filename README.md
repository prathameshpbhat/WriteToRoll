# ScriptWriter

A professional screenwriting application built with WPF and .NET 8, designed for screenwriters who need a powerful, modern tool for writing and managing screenplays.

## Features

- WYSIWYG screenplay editor with proper formatting
- Fountain and Final Draft (FDX) import/export
- Index cards and corkboard for scene management
- Outline view with drag-and-drop reordering
- Multi-document workspace
- Autosave and versioning
- PDF export with industry-standard formatting
- Modern, themeable UI
- Keyboard-first experience

## Development Setup

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or later (recommended)
- Git

### Building the Project

1. Clone the repository
2. Open the solution in Visual Studio
3. Restore NuGet packages
4. Build the solution

```powershell
dotnet restore
dotnet build
```

### Running Tests

```powershell
dotnet test
```

## Project Structure

- `src/App.Host` - WPF startup project and main window
- `src/App.Core` - Domain models and service interfaces
- `src/App.UI` - Views and XAML resources
- `src/App.ViewModels` - ViewModels and presentation logic
- `src/App.Persistence` - File I/O and format conversion
- `tests/Unit.*` - Unit tests

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to your branch
5. Create a Pull Request

## License

[MIT License](LICENSE)