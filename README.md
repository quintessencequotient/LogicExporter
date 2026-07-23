# LogicExporter

This is a small Blazor WebAssembly applet for composing logical formulas through a GUI, checking whether they are well formed, and exporting them to Unicode or LaTeX. The tooltip explains the currently accepted syntax for purposes of wff checks, which is narrow. However, the tool can build LaTeX in any convention, e.g., without parentheses. So take "not a wff" to mean "according to the conventions described in the tooltip."

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

Contributions are welcome, especially from philosophers. If you want to contribute, please confine yourself to the current framework (Blazor C# with Razor pages, .NET 10), and include comments. We want to expand the parser and GUI to handle more specialized philosophical logics, especially free logic, plural logic, second-order logic, and more exotic modal logics. When implementing support for new logical systems, prefer extending the shared parser wherever possible, and please add validation tests. Note that this is a small, spare-time project; review may be slow.
