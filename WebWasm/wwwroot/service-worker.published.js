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

install$.subscribe(event => {
    console.log('Service Worker: Install event detected');
    event.waitUntil(onInstall());
});

activate$.subscribe(event => {
    console.log('Service Worker: Activate event detected');
    event.waitUntil(onActivate());
});

fetch$.subscribe(event => {
    event.respondWith(onFetch(event));
});

message$.subscribe(event => {
    console.log('Service Worker: Message received', event.data);
    const payload = event.data.json();
    event.waitUntil(onMessage(payload));
});

notificationClick$.subscribe(event => {
    console.log('Service Worker: Notification click received', event.data);
    event.notification.close();
    event.waitUntil(onNotificationClick(event));
});

firebaseMessaging.onBackgroundMessage((payload) => {
    console.log('[Service Worker] Received Firebase background message:', payload);

    const notificationTitle = payload.notification?.title || 'New Notification';
    const notificationOptions = {
        body: payload.notification?.body || '',
        icon: 'icon-192.png',
        badge: 'icon-192.png',
        data: payload.data,
        tag: payload.data?.tag || 'firebase-notification'
    };

    return self.registration.showNotification(notificationTitle, notificationOptions);
});

async function onInstall() {
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

    console.info('Service worker: Installed');
}

async function onActivate() {
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));

    console.info('Service worker: Activated');
}

async function onFetch(event) {
    if (event.request.method !== 'GET') {
        return fetch(event.request);
    }

    const cache = await caches.open(cacheName);
    if (event.request.mode === 'navigate' && !manifestUrlList.some(url => url === event.request.url)) {
        const cachedIndex = await cache.match('index.html');
        if (cachedIndex) {
            return cachedIndex;
        }

        console.log('Service Worker: index.html not in cache, fetching from network.');
        return await fetch('index.html');
    }

    const cachedResponse = await cache.match(event.request);
    return cachedResponse || fetch(event.request);
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
    const urlToOpen = event.notification.data?.url || '/';

    const windowClients = await clients.matchAll({
        type: 'window',
        includeUncontrolled: true
    });

    for (const client of windowClients) {
        if ('focus' in client) {
            return client.focus();
        }
    }

    if (clients.openWindow) {
        return clients.openWindow(urlToOpen);
    }
}