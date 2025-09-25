# ASP.NET 4.8 GB18030 Rendering Test Harness

This repository contains:

- `WebAppUnderTest`: An ASP.NET WebForms site hosting pages with common WebControls populated with GB18030 test strings.
- `TestDriver`: A console app that drives HTTP requests against the site, parses returned HTML, and validates that rendered text matches the expected Unicode strings from `Shared/GB18030-strings.txt`.
- `Shared/GB18030-strings.txt`: Placeholder list of test strings (replace with official dataset).

## High-level Flow

1. Start the ASP.NET WebForms site (IIS Express or full IIS) targeting .NET Framework 4.8.
2. Run the console driver specifying base URL and (optionally) desired response encoding.
3. The driver requests dedicated test pages (or parameterized endpoints) that render each control type with each test string.
4. HTML is parsed and decoded as Unicode; driver compares the inner text/value attributes against expected Unicode string.
5. A summary report is printed with pass/fail counts and details for failures.

## Planned Controls

Label, Literal, TextBox, HyperLink, LinkButton, Button, CheckBox, RadioButton, DropDownList, ListBox, BulletedList, GridView, Repeater, DataList, ListView, DetailsView, FormView.

## Additional Tested Surfaces

Beyond the primary visible text for each control, the harness now also validates:

- title attribute (ToolTip) for: Label, HyperLink, LinkButton, Button, CheckBox, RadioButton, DropDownList, ListBox, BulletedList, TextBox
- placeholder attribute for: TextBox
- Second list item text for: DropDownList, ListBox (uses next string in dataset)
- GridView: first data cell (base), header cell (next string), caption (base string)
- DetailsView: data cell (base), header cell (next string)
- Repeater: header (base), first item (base)

Synthetic IDs are used in the report to distinguish these surfaces, e.g.:

```text
TextBoxTest.placeholder
ButtonTest.title
DropDownListTest.item2
GridViewTest.header
GridViewTest.caption
DetailsViewTest.header
RepeaterTest.header
```

## Command-line (planned)

```powershell
TestDriver.exe --baseUrl http://localhost:12345 --encoding utf-8 --only ControlName(optional)
```

## Next Steps

- Add Visual Studio solution with two projects targeting .NET Framework 4.8.
- Implement shared library for loading test strings.
- Build pages for each control category.
- Implement driver logic (HTTP client, HTML parsing via HtmlAgilityPack, validation & report).
