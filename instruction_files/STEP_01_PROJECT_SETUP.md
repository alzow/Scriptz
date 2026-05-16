# STEP 1: Create Project and Install Packages

Execute this step first. This creates the base MAUI project structure.

## Commands to Run:

```bash
# Create new .NET MAUI project
dotnet new maui -n ScriptzApp

# Navigate to project
cd ScriptzApp

# Add required packages
dotnet add package CommunityToolkit.Maui --version 12.2.0
dotnet add package CommunityToolkit.Mvvm --version 8.4.0
dotnet add package MPowerKit.Navigation --version 1.5.0
dotnet add package MPowerKit.Popups --version 1.5.0
dotnet add package PropertyChanged.Fody --version 4.1.0
dotnet add package SkiaSharp.Extended.UI.Maui --version 3.1.0-preview.3
dotnet add package Refit --version 7.2.22
dotnet add package Refit.HttpClientFactory --version 7.2.22
```

## Update ScriptzApp.csproj

Replace the `<ItemGroup>` containing PackageReferences with this:

```xml
<ItemGroup>
    <PackageReference Include="CommunityToolkit.Maui" Version="12.2.0" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="9.0.10" />
    <PackageReference Include="MPowerKit.Navigation" Version="1.5.0" />
    <PackageReference Include="MPowerKit.Popups" Version="1.5.0" />
    <PackageReference Include="PropertyChanged.Fody" Version="4.1.0" />
    <PackageReference Include="SkiaSharp.Extended.UI.Maui" Version="3.1.0-preview.3" />
    <PackageReference Include="Refit" Version="7.2.22" />
    <PackageReference Include="Refit.HttpClientFactory" Version="7.2.22" />
</ItemGroup>
```

## Create FodyWeavers.xml

Create a new file `FodyWeavers.xml` in the root of the project:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Weavers xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="FodyWeavers.xsd">
  <PropertyChanged />
</Weavers>
```

After this step, run `dotnet restore` to ensure all packages are installed.

**STOP HERE - Confirm packages are installed before proceeding to Step 2**
