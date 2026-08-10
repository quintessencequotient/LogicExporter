# LogicExporter

This is a Blazor WebAssembly applet for composing logical formulas through a GUI, checking whether they are well formed, and exporting them to Unicode or LaTeX. The tooltip explains the currently accepted syntax for purposes of wff checks, which is narrow. However, the tool can build LaTeX in any convention, e.g., without parentheses. So take "not a wff" or "not a sentence" to mean "according to the conventions described in the tooltip."

## File Tree

```text
.
├── Program.cs
├── App.razor
├── Pages/
│   ├── Home.razor
│   └── NotFound.razor
├── Logic/
│   ├── FormulaParser.cs
│   ├── FormulaNode.cs
│   ├── FormulaValidator.cs
│   ├── LatexConverter.cs
│   ├── ParserOptions.cs
│   ├── ParseResult.cs
│   ├── SymbolNormalizer.cs
│   └── Token.cs
├── wwwroot/
│   ├── index.html
│   ├── logicexporter.js
│   └── css/app.css
└── LogicExporter.Tests/
```

`Home.razor` is the visible interface.

`Logic/` is the parser.

## Getting Started

Install the .NET SDK from Microsoft, navigate to the LogicExporter folder in the terminal,
then type:

```bash
dotnet run
```

## Contributing

Contributions are welcome, especially from philosophers. If you want to contribute, please confine yourself to the current framework (Blazor C# with Razor pages, .NET 10), and include comments. We want to expand the parser and GUI to handle more specialized philosophical logics, especially free logic, plural logic, second-order logic, and more exotic modal logics. When implementing support for new logical systems, prefer extending the shared parser wherever possible, and please add validation tests.

## Deployment

This app is configured to run under `/logic/`.

From the repository directory, run:

```bash
dotnet restore
dotnet test LogicExporter.Tests/LogicExporter.Tests.csproj
dotnet publish LogicExporter.csproj -c Release
```

The deployable static files will be in:

```text
bin/Release/net10.0/publish/wwwroot/
```

Upload the contents of that `wwwroot` directory—not the directory itself—to the website's document root for `example.com/logic/`. The deployed structure should include:

```text
/logic/index.html
/logic/_framework/
/logic/css/
/logic/logicexporter.js
```
