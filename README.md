# WebWasm - Optimized Blazor WebAssembly PWA

A highly optimized Blazor WebAssembly standalone application with Progressive Web App (PWA) support, configured for minimal download size and GitHub Pages deployment.

## 🎯 Optimizations Applied

This project includes several optimizations that reduce the compressed download size from **~6MB to 4.5MB** (with Brotli compression), achieving a **25% reduction** in what users actually download:

### 1. IL Trimming & Linking
- **PublishTrimmed**: Removes unused code at publish time
- **TrimMode**: Set to `full` for maximum size reduction

### 2. AOT (Ahead-of-Time) Compilation
- **RunAOTCompilation**: Compiles .NET assemblies to native WebAssembly
- **WasmStripILAfterAOT**: Removes IL code after AOT compilation
- Results in a single optimized `dotnet.native.wasm` file

### 3. Service Worker Caching
- Properly configured service worker that caches all DLLs and WASMs
- Prevents re-downloading on subsequent visits
- Uses Subresource Integrity (SRI) for security

### 4. Compression
- Brotli compression (`.br` files) - best compression ratio
- Gzip compression (`.gz` files) - fallback for older browsers

## 🚀 Getting Started

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- wasm-tools workload

### Installation Steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/RamanSabchuk1/WebWasm.git
   cd WebWasm
   ```

2. **Install the wasm-tools workload** (required for AOT compilation)
   ```bash
   dotnet workload install wasm-tools
   ```

3. **Restore dependencies**
   ```bash
   cd WebWasm
   dotnet restore
   ```

## 🔨 Building and Running

### Development (Debug Mode)
```bash
dotnet run
```
- Runs without optimizations for faster build times
- Service worker is disabled in development mode
- Navigate to `https://localhost:5001` or `http://localhost:5000`

### Production Build (Release Mode)
```bash
dotnet publish -c Release -o publish
```
- Applies all optimizations (trimming, linking, AOT)
- Generates compressed `.br` and `.gz` files
- Service worker is active with full caching
- Build time: ~2-3 minutes (due to AOT compilation)

### Serve Production Build Locally
```bash
cd publish/wwwroot
python3 -m http.server 8080
```
Then navigate to `http://localhost:8080`

## 📦 Project Configuration

The key optimization settings are in `WebWasm.csproj`:

```xml
<PropertyGroup>
  <!-- Enable trimming to remove unused code -->
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>full</TrimMode>
  
  <!-- Enable AOT compilation -->
  <RunAOTCompilation>true</RunAOTCompilation>
  
  <!-- Strip IL after AOT to reduce size -->
  <WasmStripILAfterAOT>true</WasmStripILAfterAOT>
  
  <!-- Service worker configuration -->
  <ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>
</PropertyGroup>
```

## 🌐 GitHub Pages Deployment

This project is automatically deployed to GitHub Pages using GitHub Actions.

### Setup Instructions

1. **Enable GitHub Pages in your repository**
   - Go to Settings → Pages
   - Source: Select "GitHub Actions"

2. **Workflow Configuration**
   - The workflow file is at `.github/workflows/deploy-gh-pages.yml`
   - Automatically triggers on push to `main` branch
   - Can also be manually triggered from Actions tab

3. **Access Your Deployed App**
   - URL: `https://[username].github.io/WebWasm/`
   - Example: `https://RamanSabchuk1.github.io/WebWasm/`

### Manual Deployment Steps

If you need to deploy manually:

1. **Build the release version**
   ```bash
   dotnet publish -c Release -o publish
   ```

2. **Update base path for GitHub Pages**
   ```bash
   sed -i 's|<base href="/" />|<base href="/WebWasm/" />|g' publish/wwwroot/index.html
   sed -i 's|const base = "/";|const base = "/WebWasm/";|g' publish/wwwroot/service-worker.published.js
   ```

3. **Add .nojekyll file** (prevents Jekyll processing)
   ```bash
   touch publish/wwwroot/.nojekyll
   ```

4. **Deploy the `publish/wwwroot` folder** to GitHub Pages

## 📊 Size Comparison

| Configuration | Uncompressed | Brotli | Gzip |
|--------------|--------------|--------|------|
| Without Optimization | 16 MB | ~6 MB | ~8 MB |
| With Optimization | 30 MB | **4.5 MB** | **6.4 MB** |

*Note: Although uncompressed size increases due to native code, the compressed download size is significantly reduced.*

## 🔧 Troubleshooting

### Service Worker Not Caching
- Ensure you're testing the **Release** build, not Debug
- Check browser DevTools → Application → Service Workers
- Clear cache and reload (Ctrl+Shift+R)

### Build Takes Too Long
- AOT compilation is intentionally slow for optimization
- First build after installing wasm-tools takes longer
- Typical build time: 2-3 minutes

### Application Doesn't Load on GitHub Pages
- Verify the base path in `index.html` matches your repository name
- Check that `.nojekyll` file exists
- Ensure GitHub Pages is enabled in repository settings

### Workload Installation Issues
```bash
# If workload install fails, try:
dotnet workload update
dotnet workload install wasm-tools --skip-sign-check
```

## 📝 Service Worker Details

The service worker (`service-worker.published.js`) handles:
- **Installation**: Downloads and caches all assets on first visit
- **Activation**: Cleans up old caches from previous versions
- **Fetch**: Serves cached content when available, with fallback to network

Cached file patterns:
- `.dll`, `.wasm`, `.js`, `.json`, `.css`, `.html`
- `.woff`, `.png`, `.jpg`, `.jpeg`, `.gif`, `.ico`
- `.blat`, `.dat`, `.webmanifest`

## 🎓 Learn More

- [Blazor WebAssembly AOT Compilation](https://learn.microsoft.com/aspnet/core/blazor/host-and-deploy/webassembly#ahead-of-time-aot-compilation)
- [Trim self-contained deployments](https://learn.microsoft.com/dotnet/core/deploying/trimming/trimming-options)
- [Progressive Web Apps with Blazor](https://learn.microsoft.com/aspnet/core/blazor/progressive-web-app)
- [Service Workers API](https://developer.mozilla.org/docs/Web/API/Service_Worker_API)

## 📄 License

This project is provided as-is for educational and development purposes.

## 🤝 Contributing

Feel free to open issues or submit pull requests for improvements!