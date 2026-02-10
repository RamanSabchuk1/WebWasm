importScripts('https://www.gstatic.com/firebasejs/9.15.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/9.15.0/firebase-messaging-compat.js');

self.importScripts('./rxjs.umd.min.js'); 
self.importScripts('./service-worker-assets.js');

const firebaseConfig = {
	apiKey: "AIzaSyCcnA6c8s9ML0VJu35JhwzTGQoG-PcNnN0",
	authDomain: "kliffort-site.firebaseapp.com",
	projectId: "kliffort-site",
	storageBucket: "kliffort-site.firebasestorage.app",
	messagingSenderId: "250756228052",
	appId: "1:250756228052:web:e0623066a48c50c8512c41"
};

firebase.initializeApp(firebaseConfig);
const firebaseMessaging = firebase.messaging();

const { fromEvent } = rxjs;

const install$ = fromEvent(self, 'install');
const activate$ = fromEvent(self, 'activate');
const fetch$ = fromEvent(self, 'fetch');
const message$ = fromEvent(self, 'message');
const notificationClick$ = fromEvent(self, 'notificationclick');

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];
const baseUrl = new URL('/', self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

// Update check interval (every 1 hour)
const UPDATE_CHECK_INTERVAL = 60 * 60 * 1000;

install$.subscribe(event => {
	console.log('Service Worker: Install event detected, version:', self.assetsManifest.version);
	event.waitUntil(
		onInstall().then(() => {
			// Skip waiting to activate new service worker immediately
			return self.skipWaiting();
		})
	);
});

activate$.subscribe(event => {
	console.log('Service Worker: Activate event detected');
	event.waitUntil(
		onActivate().then(() => {
			// Take control of all clients immediately
			return self.clients.claim();
		})
	);
});

fetch$.subscribe(event => {
	event.respondWith(onFetch(event));
});

message$.subscribe(event => {
	console.log('Service Worker: Message received', event.data);
	
	// Handle update check message from client
	if (event.data === 'CHECK_FOR_UPDATES') {
		event.waitUntil(checkForUpdates(event));
		return;
	}

	// Handle skip waiting message
	if (event.data === 'SKIP_WAITING') {
		self.skipWaiting();
		return;
	}

	event.waitUntil(onMessage(event.data));
});

notificationClick$.subscribe(event => {
	console.log('Service Worker: Notification click received', event.data);
	event.notification.close();
	event.waitUntil(onNotificationClick(event));
});

firebaseMessaging.onBackgroundMessage(async (payload) => {
	console.log('[Service Worker] Received Firebase background message:', payload);

	await saveNotification(payload);

	// Fallback to data properties if notification properties are missing (Data-only message support)
	const notificationTitle = payload.notification?.title || payload.data?.title || 'New Notification';
	const notificationBody = payload.notification?.body || payload.data?.body || '';

	const notificationOptions = {
		body: notificationBody,
		icon: 'icon-192.png',
		badge: 'icon-192.png',
		data: payload.data, // Ensure data is passed for click handling
		tag: payload.data?.orderId || payload.data?.tag || 'firebase-notification',
		renotify: true
	};

	return self.registration.showNotification(notificationTitle, notificationOptions);
});

// Periodic update check
setInterval(() => {
	console.log('Service Worker: Periodic update check');
	self.registration.update();
}, UPDATE_CHECK_INTERVAL);

async function onInstall() {
	console.info('Service worker: Installing version', self.assetsManifest.version);

	const assetsToCache = self.assetsManifest.assets
		.filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
		.filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)));
	
	const cache = await caches.open(cacheName);

	await Promise.all(assetsToCache.map(async asset => {
		try {
			const response = await fetch(asset.url, { cache: 'no-cache' });
			if (response.ok) {
				await cache.put(asset.url, response);
			}
		} catch (error) {
			console.warn(`Service worker: Failed to cache ${asset.url}`, error);
		}
	}));

	console.info('Service worker: Installed version', self.assetsManifest.version);
}

async function onActivate() {
	console.info('Service worker: Activating version', self.assetsManifest.version);

	// Delete old caches
	const cacheKeys = await caches.keys();
	await Promise.all(cacheKeys
		.filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
		.map(key => {
			console.log('Service Worker: Deleting old cache:', key);
			return caches.delete(key);
		}));

	// Notify all clients about the new version
	const clients = await self.clients.matchAll({ type: 'window' });
	clients.forEach(client => {
		client.postMessage({
			type: 'SERVICE_WORKER_UPDATED',
			version: self.assetsManifest.version,
			cacheName: cacheName
		});
	});

	console.info('Service worker: Activated version', self.assetsManifest.version);
}

async function onFetch(event) {
	if (event.request.method !== 'GET') {
		return fetch(event.request);
	}

	const url = new URL(event.request.url);

	// For navigation requests, always try network first, then fall back to cache
	if (event.request.mode === 'navigate') {
		try {
			const networkResponse = await fetch(event.request);
			if (networkResponse.ok) {
				const cache = await caches.open(cacheName);
				await cache.put(event.request, networkResponse.clone());
				return networkResponse;
			}
		} catch (error) {
			console.log('Service Worker: Network failed for navigation, falling back to cache');
		}
		
		const cache = await caches.open(cacheName);
		const cachedResponse = await cache.match(event.request);
		if (cachedResponse) {
			return cachedResponse;
		}
		
		// Fallback to index.html
		const cachedIndex = await cache.match('index.html');
		if (cachedIndex) {
			return cachedIndex;
		}

		return fetch('index.html');
	}

	// For same-origin requests, use cache-first with background update
	if (url.origin === self.origin) {
		const cache = await caches.open(cacheName);
		const cachedResponse = await cache.match(event.request);

		if (cachedResponse) {
			// Update cache in background, return cached response immediately
			fetch(event.request).then(response => {
				if (response.ok) {
					cache.put(event.request, response.clone());
				}
			}).catch(() => {
				// Ignore network errors for background update
			});
			return cachedResponse;
		}

		// No cache hit — must wait for network
		try {
			const response = await fetch(event.request);
			if (response.ok) {
				cache.put(event.request, response.clone());
			}
			return response;
		} catch (error) {
			// Network failed and nothing in cache
			return new Response('Network error', { status: 408, headers: { 'Content-Type': 'text/plain' } });
		}
	}

	// For cross-origin requests, network only
	return fetch(event.request);
}

function onMessage(payload) {
	console.log('[Service Worker] Message received:', payload);

	const title = payload.title || payload.notification?.title || 'New Notification';
	const body = payload.body || payload.notification?.body || '';
	const url = payload.url || payload.data?.url || '/';

	self.registration.showNotification(title, {
		body: body,
		icon: 'icon-192.png',
		badge: 'icon-192.png',
		vibrate: [100, 50, 100],
		data: { url: url }
	});
}

async function onNotificationClick(event) {
	const data = event.notification.data;
	const action = data?.clickAction || data?.action;
	const orderId = data?.orderId;
	let path = '/';

	if (data?.url) {
		path = data.url;
	} else if ((action === 'OPEN_ORDER_DETAILS' || action === 'OPEN_DELIVERY_DETAILS') && orderId) {
		path = `orders/${orderId}`;
	}

	const urlToOpen = new URL(path, self.registration.scope).href;

	const windowClients = await clients.matchAll({
		type: 'window',
		includeUncontrolled: true
	});

	for (const client of windowClients) {
		if (client.url === urlToOpen && 'focus' in client) {
			return client.focus();
		}
	}

	if (windowClients.length > 0) {
		const client = windowClients[0];
		await client.focus();
		return client.navigate(urlToOpen);
	}

	if (clients.openWindow) {
		return clients.openWindow(urlToOpen);
	}
}

async function checkForUpdates(event) {
	try {
		console.log('Service Worker: Checking for updates...');
		await self.registration.update();

		// Notify the client that update check is complete
		if (event.source) {
			event.source.postMessage({
				type: 'UPDATE_CHECK_COMPLETE'
			});
		}
	} catch (error) {
		console.error('Service Worker: Update check failed', error);
	}
}

async function saveNotification(payload) {
	try {
		const db = await new Promise((resolve, reject) => {
			const req = indexedDB.open('webwasm-db', 1);
			req.onupgradeneeded = (e) => {
				const db = e.target.result;
				if (!db.objectStoreNames.contains('notifications')) {
					db.createObjectStore('notifications', { keyPath: 'id', autoIncrement: true });
				}
			};
			req.onsuccess = () => resolve(req.result);
			req.onerror = () => reject(req.error);
		});

		const tx = db.transaction('notifications', 'readwrite');
		const store = tx.objectStore('notifications');

		const item = {
			title: payload.notification?.title || payload.data?.title || 'Notification',
			body: payload.notification?.body || payload.data?.body || '',
			data: payload.data,
			timestamp: new Date().toISOString(),
			isRead: false
		};
		store.add(item);

		const countReq = store.count();
		countReq.onsuccess = () => {
			if (countReq.result > 50) {
				const keysReq = store.getAllKeys();
				keysReq.onsuccess = () => {
					const keys = keysReq.result;
					const toRemoveCount = keys.length - 40;
					if (toRemoveCount > 0) {
						for (let i = 0; i < toRemoveCount; i++) {
							store.delete(keys[i]);
						}
					}
				};
			}
		};
	} catch (e) {
		console.error('Save notification failed', e);
	}
}