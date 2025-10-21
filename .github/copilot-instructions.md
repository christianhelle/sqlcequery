# Copilot Instructions for SQL Compact Query Analyzer

## Project Overview
SQL Compact Query Analyzer is a Windows desktop application for querying and managing SQL Server Compact Edition (SQLCE) databases. It provides a GUI for creating databases, executing queries, viewing data, and managing database schema.

## Technology Stack
- **Language**: C#
- **Framework**: .NET Framework 4.7.2
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Build System**: Cake Build (build.cake), MSBuild
- **CI/CD**: GitHub Actions (`.github/workflows/build.yml`)
- **Packaging**: Inno Setup for Windows installers, Chocolatey for package distribution
- **Target Platforms**: Windows (x86 and x64)

## Project Structure
- **Source/Editor**: Main WPF application with MVVM pattern
  - `Controls/`: Custom WPF controls
  - `View/`: XAML views
  - `ViewModel/`: View models for MVVM pattern
  - `Misc/`: Utility and helper classes
- **Source/SqlCeDatabase**: Core database abstraction library (base interfaces)
- **Source/SqlCeDatabase31**: SQLCE 3.1 specific implementation
- **Source/SqlCeDatabase35**: SQLCE 3.5 specific implementation
- **Source/SqlCeDatabase40**: SQLCE 4.0 specific implementation
- **Source/TSqlParser**: T-SQL parsing utilities
- **Dependencies**: External dependencies and tools
- **Screenshots**: Application screenshots for documentation

## Build & Development

### Building the Project
```powershell
# Navigate to Source directory
cd Source

# Run Cake build script (restores NuGet packages, builds solution, creates installers)
./build.ps1
```

The build process:
1. Cleans previous build artifacts
2. Restores NuGet packages
3. Builds solution for x86 and x64 platforms
4. Creates Windows installers using Inno Setup
5. Generates Chocolatey package

### Build Outputs
- **Binaries**: `Source/Binaries/Release/{x86|x64}/`
- **Installers**: `Source/Artifacts/SQLCEQueryAnalyzer-Setup-{x86|x64}.exe`
- **Artifacts**: `Source/Artifacts/`

### Running the Application
The main executable is built as `QueryAnalyzer.exe` in the binaries directory. It requires .NET Framework 4.0 or higher.

## Coding Standards

### Code Style
- Use standard C# naming conventions (PascalCase for classes/methods, camelCase for parameters/locals)
- Use meaningful variable and method names
- Keep methods focused and concise
- Use interfaces for abstraction (e.g., `ISqlCeDatabase`)

### Namespaces
- Root namespace: `ChristianHelle.DatabaseTools.SqlCe`
- Editor/UI namespace: `ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer`

### MVVM Pattern
The application follows the MVVM (Model-View-ViewModel) pattern:
- **Models**: Database entities and business logic in SqlCeDatabase projects
- **Views**: XAML files in `Source/Editor/View/`
- **ViewModels**: View model classes in `Source/Editor/ViewModel/`

### Database Version Support
The project supports multiple SQLCE versions (3.0, 3.1, 3.5, 4.0) through separate assemblies. Each version has its own project that implements the same interfaces.

## Important Notes

### No Unit Tests
This repository does not currently have unit tests. When making changes:
- Manually test functionality where possible
- Consider the impact on all supported SQLCE versions
- Test both x86 and x64 builds if changes affect platform-specific code

### Version Management
- Assembly versions are updated during CI/CD via PowerShell scripts
- Version format: `1.3.4.<build_number>`
- Versions are set in `AssemblyInfo.cs` files and Inno Setup scripts

### Dependencies
- SQL Server Compact Edition runtime libraries are required dependencies
- NuGet packages are restored automatically during build
- External tools (like Inno Setup) are in the `Dependencies` directory

## Common Tasks

### Adding a New Feature
1. Identify which project(s) need changes (Editor for UI, SqlCeDatabase* for database logic)
2. Follow MVVM pattern for UI features (create View, ViewModel, wire up in App.xaml.cs)
3. Update all SQLCE version projects if changing database interfaces
4. Update documentation (README.md) if user-visible changes
5. Build and test manually with Cake build script

### Modifying Database Operations
1. Update the `ISqlCeDatabase` interface if adding new operations
2. Implement in all version-specific projects (SqlCeDatabase31, 35, 40)
3. Update the Editor project to expose new functionality in the UI
4. Consider backward compatibility with older SQLCE versions

### UI Changes
1. WPF XAML files are in `Source/Editor/View/`
2. Follow existing XAML structure and naming conventions
3. Use data binding to ViewModels
4. Custom controls are in `Source/Editor/Controls/`

### Build Configuration Changes
- Modify `Source/build.cake` for build process changes
- Update `.github/workflows/build.yml` for CI/CD changes
- Platform configurations: x86, x64 (no AnyCPU due to SQLCE dependencies)

## File Associations
- `.sdf` files: SQL Server Compact Edition database files
- `.iss` files: Inno Setup installer scripts (`Setup-x86.iss`, `Setup-x64.iss`)
- `.cake` files: Cake build scripts
- `.nuspec` files: Chocolatey package specifications

## External Resources
- Project repository: https://github.com/christianhelle/sqlcequery
- Author's blog: https://christianhelle.com
- Releases: https://github.com/christianhelle/sqlcequery/releases

## Making Changes
When contributing or making changes:
1. Keep changes minimal and focused
2. Follow existing code patterns and conventions
3. Ensure changes work on both x86 and x64 platforms
4. Test manually since there are no automated tests
5. Update README.md if adding user-visible features
6. Consider impact on all supported SQLCE versions (3.0, 3.1, 3.5, 4.0)
