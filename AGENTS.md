# Repository Guidelines

## Project Structure & Module Organization

This is a .NET 8 Windows/WPF solution centered on `VPet.sln`. `VPet-Simulator.Windows/` is the main desktop pet application, with UI windows in `WinDesign/`, runtime functions in `Function/`, resources in `Res/`, tutorials in `GameAssets/`, and bundled mods/assets in `mod/`. `VPet-Simulator.Core/` contains reusable pet logic, animation loading, graph rendering, and display controls under `Handle/`, `Graph/`, and `Display/`. `VPet-Simulator.Windows.Interface/` defines plugin-facing interfaces and save/mod models. `VPet-Simulator.Tool/` contains helper tooling for mod creation. `VPet.Solution/` is a separate WPF utility/editor project.

## Build, Test, and Development Commands

- `dotnet restore .\VPet.sln` restores NuGet packages.
- `dotnet build .\VPet.sln -c Debug -p:Platform=x64` builds the normal desktop configuration.
- `dotnet build .\VPet.sln -c Release -p:Platform=x64` creates a release build.
- `dotnet run --project .\VPet-Simulator.Windows\VPet-Simulator.Windows.csproj -c Debug -p:Platform=x64` runs the main app.

For first local runs, open `VPet.sln` in Visual Studio, select `VPet-Simulator.Windows` and `x64`, then run once. If the app reports missing core mods, run `VPet-Simulator.Windows\mklink.bat` as administrator to link the `mod` folder into the build output.

## Coding Style & Naming Conventions

Follow `.editorconfig`: CRLF line endings, 4-space indentation, `using` directives outside namespaces, PascalCase for types and non-field members, and `I` prefixes for interfaces. Prefer explicit built-in types over `var` unless existing nearby code differs. Keep WPF XAML code-behind patterns consistent with the surrounding window or control. Do not manually edit generated `*.Designer.cs` files unless updating the corresponding `.resx` or `.settings`.

## Testing Guidelines

There is currently no dedicated test project. Validate changes with `dotnet build` and targeted manual checks in the WPF app. For UI or mod-loading changes, test the affected window/control and confirm the app starts with linked core mods. If adding tests, create a clearly named test project such as `VPet-Simulator.Core.Tests` and use `*Tests.cs` file names.

## Commit & Pull Request Guidelines

Recent history uses short, descriptive Chinese subjects such as `修复部分代码MOD加载报错的问题` and `PNG动画性能优化`. Keep commit subjects concise, imperative, and focused on one change. For PRs, include a summary, linked issue when applicable, manual test notes, and screenshots/GIFs for visible UI changes. Discuss major gameplay or feature additions with maintainers first; straightforward bug fixes can go directly to PR.

## Asset & Configuration Notes

Treat `VPet-Simulator.Windows/mod/0000_core/pet/vup` and other bundled art assets carefully; the repository documents separate animation copyright and distribution terms. Avoid large asset churn, preserve existing directory names, and do not commit local `bin/`, `obj/`, `.vs/`, or user-specific launch files.
