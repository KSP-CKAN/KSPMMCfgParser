# Kerbal Space Program Module Manager Config File Parser

## Validator

The validator is a small command line tool that finds cfg files, parses them, and reports syntax errors. It can be used as a GitHub Action/Workflow or as a standalone utility. The validator returns success (`0`) if no syntax errors are found, or failure (`2`) otherwise.

### Configuring on GitHub

To validate all of the cfg files in a GitHub repo every time they change, add a file called `.github/workflows/validate-cfg.yml` to your repo containing:

```yaml
name: Config file validation
on:
  push:
    branches:
      - master
  pull_request:
    types:
      - opened
      - synchronize
      - reopened
jobs:
  Validate .cfg files:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout repo
        uses: actions/checkout@v2
        with:
          fetch-depth: 1
      - name: Validate config files
        uses: KSP-CKAN/KSPMMCfgParser@master
```

The results will appear in the Actions tab, as annotations in the Files changed tab of pull requests, and probably also in your email inbox.

### Standalone

You may pass zero or more parameters to the validator. If you pass no parameters, it will scan the current working directory recursively. If you pass the name of a file, it will scan that file. If you pass the name of a directory, it will scan that directory recursively.

The file, line, and column of each syntax error will be reported, one per line:

```
> KSPMMCfgValidator.exe UnKerballedStart
UnKerballedStart\Mod Support\Breaking_Ground.cfg:48:1: Unexpected '@<0x40>'
UnKerballedStart\Mod Support\CNAR.cfg:164:1: Unexpected '<EndOfStream>'
UnKerballedStart\Mod Support\InterstellarExtended.cfg:3:1: Unexpected '<EndOfStream>'
UnKerballedStart\Mod Support\knes.cfg:1:1: Unexpected '/<0x2F>'
UnKerballedStart\Mod Support\Luciole.cfg:1:1: Unexpected '/<0x2F>'
UnKerballedStart\Mod Support\NovaPunch.cfg:178:1: Unexpected '[<0x5B>'
UnKerballedStart\Mod Support\Orion.cfg:1:1: Unexpected '/<0x2F>'
UnKerballedStart\Mod Support\reDIRECT.cfg:1:1: Unexpected '/<0x2F>'
UnKerballedStart\Mod Support\SOCK.cfg:1:1: Unexpected '/<0x2F>'
UnKerballedStart\Mod Support\TACSelfDestruct.cfg:25:1: Expected '<EndOfStream>' but was '/<0x2F>'
UnKerballedStart\Mod Support\X-20.cfg:1:1: Unexpected '/<0x2F>'
```

### See also

Consider validating your version file as well:

<https://github.com/DasSkelett/AVC-VersionFileValidator>

## Parser

The `KSPMMCfgParser` project provides a monadic parser for Kerbal Space Program / Module Manager config files. It turns a `string` or a `Stream` containing KSP/ModuleManager config file data into a sequence of `KSPConfigNode` objects, and you can also use it to find syntax errors.

### Installing

NuGet:

```
nuget install KSPMMCfgParser
```

The equivalent `dotnet` command might also work, but we don't use those tools for CKAN currently so I haven't tested it.

Or you can add this to your csproj:

```xml
<ItemGroup>
  <PackageReference Include="ParsecSharp" Version="3.4.0" />
  <PackageReference Include="KSPMMCfgParser" Version="1.0.5" />
</ItemGroup>
```

Remember to tell MSBuild to download it:

```
msbuild -r
```

### Using

See [the code for the validator](KSPMMCfgValidator/KSPMMCfgValidator.cs) for a full working example; here's a more concise excerpt:

```csharp
using System;
using System.IO;
using System.Linq;

using ParsecSharp;

using KSPMMCfgParser;
using static KSPMMCfgParser.KSPMMCfgParser;

namespace KSPMMCfgExample
{
    public class KSPMMCfgExample
    {
        public static int Main(string[] args)
        {
            ConfigFile.Parse(File.Open(args[0], FileMode.Open))
                      .CaseOf(failure =>
                              {
                                  Console.WriteLine("{0}:{1}:{2}: {3}",
                                                    args[0],
                                                    failure.State.Position.Line,
                                                    failure.State.Position.Column,
                                                    failure.Message);
                                  return Enumerable.Empty<KSPConfigNode>();
                              },
                              success => success.Value);
        }
    }
}
```

The `success.Value` variable at the end contains the sequence of `KSPConfigNode` objects from the file.
