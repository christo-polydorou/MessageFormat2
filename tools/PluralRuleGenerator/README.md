# PluralRuleGenerator

Generates `src/MessageFormat2/Plurals/PluralRules.g.cs` from the CLDR plural rules in [cldr/](cldr/).
The generated file is checked in, so the library reads no data files at run time.

Run from the repository root:

```sh
dotnet run --project tools/PluralRuleGenerator
```

## Data

| | |
|---|---|
| Source | [cldr-json](https://github.com/unicode-org/cldr-json), `cldr-core/supplemental/plurals.json` and `ordinals.json` |
| Version | CLDR 48 (tag `48.0.0`) |
| License | Unicode License v3, see [cldr/LICENSE](cldr/LICENSE) |

To update, download both files from the new tag into `cldr/`, update the table above, run the
generator, and run the tests: `Unit/Plurals/PluralRuleSampleTests.cs` checks every sample number
listed in the CLDR files.
