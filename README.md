# WebWasm - Optimized Blazor WebAssembly PWA

A highly optimized Blazor WebAssembly standalone application with Progressive Web App (PWA) support, configured for minimal download size and GitHub Pages deployment.

### 1. IL Trimming & Linking
- **PublishTrimmed**: Removes unused code at publish time
- **TrimMode**: Set to `full` for maximum size reduction

### 2. AOT (Ahead-of-Time) Compilation
- **RunAOTCompilation**: Compiles .NET assemblies to native WebAssembly
- **WasmStripILAfterAOT**: Removes IL code after AOT compilation
- Results in a single optimized `dotnet.native.wasm` file

### 3. Compression
- Brotli compression (`.br` files) - best compression ratio
- Gzip compression (`.gz` files) - fallback for older browsers

## 🚀 Getting Started
First, ensure you have .NET 10.0 SDK installed, then install the wasm-tools workload:

```bash
# Check .NET version
dotnet --version
# Should be 10.0.x or later

# Install wasm-tools workload (required for AOT compilation)
dotnet workload install wasm-tools
```

**Why wasm-tools?** This workload includes:
- Emscripten compiler for native WebAssembly
- AOT compiler for .NET to WASM
- Optimization tools (wasm-opt)

### Step 2: Update .csproj File

Open `WebWasm/WebWasm.csproj` and add the following optimization properties to the `<PropertyGroup>` section:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>
    <ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>
    
    <!-- Optimization settings for size reduction -->
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>full</TrimMode>
    <RunAOTCompilation>true</RunAOTCompilation>
    <WasmStripILAfterAOT>true</WasmStripILAfterAOT>
    <InvariantGlobalization>false</InvariantGlobalization>
  </PropertyGroup>

  <!-- Rest of the file remains the same -->
</Project>
```

**What each setting does**:
- `PublishTrimmed="true"`: Enables IL trimming to remove unused code
- `TrimMode="full"`: Most aggressive trimming (removes all unused members)
- `RunAOTCompilation="true"`: Compiles .NET IL to native WebAssembly
- `WasmStripILAfterAOT="true"`: Removes IL bytecode after AOT (saves space)
- `InvariantGlobalization="false"`: Keeps globalization support (set to true to save more space if you don't need it)

3. **Restore dependencies**
   ```bash
   cd WebWasm
   dotnet restore
   ```
   Create a `wwwroot/web.config` file to ensure proper Brotli and Gzip compression support:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>
    <staticContent>
      <!-- MIME types for WebAssembly -->
      <mimeMap fileExtension=".wasm" mimeType="application/wasm" />
      <mimeMap fileExtension=".br" mimeType="application/octet-stream" />
      <mimeMap fileExtension=".gz" mimeType="application/gzip" />
      <!-- other MIME types... -->
    </staticContent>
    
    <!-- URL Rewrite for pre-compressed files -->
    <rewrite>
      <rules>
        <rule name="Serve Brotli" stopProcessing="true">
          <match url="^(.*)$" />
          <conditions>
            <add input="{HTTP_ACCEPT_ENCODING}" pattern="br" />
            <add input="{REQUEST_FILENAME}.br" matchType="IsFile" />
          </conditions>
          <action type="Rewrite" url="{R:1}.br" />
        </rule>
        <!-- similar rule for Gzip... -->
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

**Important**: This file ensures that:
- Browsers that support Brotli get `.br` files automatically
- Proper Content-Encoding headers are sent
- Falls back to Gzip for older browsers

## 🧪 Local Testing

### Test the Optimized Build

You can serve the optimized build locally:

```bash
# Option 1: Using Python
cd publish/wwwroot
python3 -m http.server 8080

# Option 2: Using dotnet serve (if installed)
dotnet tool install -g dotnet-serve
cd publish/wwwroot
dotnet serve -p 8080

# Option 3: Using Node.js http-server (if installed)
npx http-server publish/wwwroot -p 8080
```
Navigate to `http://localhost:8080` in your browser.