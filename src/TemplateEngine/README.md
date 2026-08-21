# TemplateEngine

A small educational .NET template engine that follows the pipeline described in
[`PLAN.md`](PLAN.md): template text → tokenizer → parser → AST → evaluator → rendered HTML.

## Supported syntax

```html
<h1>Hello {{ Name }}</h1>

{{ if IsPremium }}
  <p>Thanks for being a premium user.</p>
{{ end }}

<ul>
{{ foreach product in Products }}
  <li>{{ product.Name }} - {{ product.Price }}</li>
{{ end }}
</ul>
```

Expressions support public property paths (for example, `Model.Address.City`), array
or list indexes (for example, `Orders[0].Lines[1].Name`), and string-keyed dictionaries.
Expression output is HTML-encoded by default. Missing properties, null values, and
out-of-range indexes render as empty text. Conditions treat null, false, empty strings,
and numeric zero as false.

## Usage

```csharp
using TemplateEngine;

var engine = new TemplateEngine();
var html = engine.Render("Hello {{ Name }}", new { Name = "Phong" });
```

## Build and test

```shell
dotnet build TemplateEngine.slnx
dotnet test TemplateEngine.slnx
```
