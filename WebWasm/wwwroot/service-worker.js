importScripts('https://www.gstatic.com/firebasejs/9.15.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/9.15.0/firebase-messaging-compat.js');

const firebaseConfig = {
	apiKey: "AIzaSyCcnA6c8s9ML0VJu35JhwzTGQoG-PcNnN0",
	authDomain: "kliffort-site.firebaseapp.com",
	projectId: "kliffort-site",
	storageBucket: "kliffort-site.firebasestorage.app",
	messagingSenderId: "250756228052",
	appId: "1:250756228052:web:e0623066a48c50c8512c41"
};

firebase.initializeApp(firebaseConfig);
const messaging = firebase.messaging();

messaging.onBackgroundMessage(async (payload) => {
	console.log('[Service Worker] Received background message:', payload);

	await saveNotification(payload);

	// Fallback to data properties if notification properties are missing (Data-only message support)
	const notificationTitle = payload.notification?.title || payload.data?.title || 'New Notification';
	const notificationBody = payload.notification?.body || payload.data?.body || '';

	const notificationOptions = {
		body: notificationBody,
		icon: 'icon-192.png',
		badge: 'icon-192.png',
		data: payload.data, // Ensure data is passed for click handling
		tag: payload.data?.orderId || payload.data?.tag || 'default',
		renotify: true
	};

	return self.registration.showNotification(notificationTitle, notificationOptions);
});

self.addEventListener('notificationclick', (event) => {
	console.log('[Service Worker] Notification click received:', event);
	event.notification.close();

	const data = event.notification.data;

	// Handle different data structures depending on how the notification was created
	const action = data?.clickAction || data?.action;
	const orderId = data?.orderId || data?.id;
	let path = '/';

	if (data?.url) {
		path = data.url;
	} else if ((action === 'OPEN_ORDER_DETAILS' || action === 'OPEN_DELIVERY_DETAILS') && orderId) {
		path = `orders/${orderId}`;
	}

	const urlToOpen = new URL(path, self.registration.scope).href;

	event.waitUntil(
		clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
			for (const client of clientList) {
				if (client.url === urlToOpen && 'focus' in client) {
					return client.focus();
				}
			}

			if (clients.openWindow) {
				return clients.openWindow(urlToOpen);
			}
		})
	);
});

self.addEventListener('fetch', () => { });

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