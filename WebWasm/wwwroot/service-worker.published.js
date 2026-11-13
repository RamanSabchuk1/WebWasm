// Published service worker: offline cache of the app files + push (js/push-sw.js).
importScripts('./js/notification-store.js', './js/push-sw.js', './service-worker-assets.js');

// The dev and prod admins share one origin on GitHub Pages (/WebWasm/, /ProdA/): the cache name carries the app path,
// otherwise each admin would delete the other's cache on activate.
const appPath = new URL(self.registration.scope).pathname;
const cacheNamePrefix = `offline-cache-${appPath}`;
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
// Before 2.0 the cache was named offline-cache-<version>, without the app path.
const isLegacyCache = (key) => key.startsWith('offline-cache-') && !key.startsWith('offline-cache-/');

const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff2?$/, /\.png$/, /\.svg$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/];
// splash/: iPhone launch screens, iOS loads them only when the app is added to the Home Screen.
const offlineAssetsExclude = [/^service-worker\.js$/, /^splash\//];

self.addEventListener('install', (event) => event.waitUntil(onInstall().then(() => self.skipWaiting())));
self.addEventListener('activate', (event) => event.waitUntil(onActivate().then(() => self.clients.claim())));
self.addEventListener('fetch', (event) => event.respondWith(onFetch(event)));
self.addEventListener('message', (event) => {
	if (event.data === 'SKIP_WAITING') {
		self.skipWaiting();
	}
});

async function onInstall() {
	const assetsToCache = self.assetsManifest.assets
		.filter((asset) => offlineAssetsInclude.some((pattern) => pattern.test(asset.url)))
		.filter((asset) => !offlineAssetsExclude.some((pattern) => pattern.test(asset.url)));

	const cache = await caches.open(cacheName);
	await Promise.all(assetsToCache.map(async (asset) => {
		try {
			const response = await fetch(asset.url, { cache: 'no-cache' });
			if (response.ok) {
				await cache.put(asset.url, response);
			}
		} catch (error) {
			console.warn(`Service worker: failed to cache ${asset.url}`, error);
		}
	}));
}

async function onActivate() {
	const cacheKeys = await caches.keys();
	await Promise.all(cacheKeys
		.filter((key) => (key.startsWith(cacheNamePrefix) && key !== cacheName) || isLegacyCache(key))
		.map((key) => caches.delete(key)));

	const windows = await self.clients.matchAll({ type: 'window' });
	windows.forEach((client) => client.postMessage({ type: 'SERVICE_WORKER_UPDATED', version: self.assetsManifest.version }));
}

async function onFetch(event) {
	if (event.request.method !== 'GET') {
		return fetch(event.request);
	}

	const cache = await caches.open(cacheName);

	// Navigation: network first, then the cached page, then the cached index.html (client-side routes).
	if (event.request.mode === 'navigate') {
		try {
			const networkResponse = await fetch(event.request);
			if (networkResponse.ok) {
				await cache.put(event.request, networkResponse.clone());
				return networkResponse;
			}
		} catch {
			// Offline: fall back to the cache below.
		}

		return (await cache.match(event.request)) ?? (await cache.match('index.html')) ?? fetch('index.html');
	}

	// Cross-origin (API, Firebase, map tiles): network only.
	if (new URL(event.request.url).origin !== self.origin) {
		return fetch(event.request);
	}

	// App files: cache first, refreshed in the background.
	const cachedResponse = await cache.match(event.request);
	if (cachedResponse) {
		fetch(event.request)
			.then((response) => response.ok && cache.put(event.request, response.clone()))
			.catch(() => { });
		return cachedResponse;
	}

	try {
		const response = await fetch(event.request);
		if (response.ok) {
			cache.put(event.request, response.clone());
		}
		return response;
	} catch {
		return new Response('Network error', { status: 408, headers: { 'Content-Type': 'text/plain' } });
	}
}
