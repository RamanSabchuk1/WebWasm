importScripts('https://www.gstatic.com/firebasejs/9.15.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/9.15.0/firebase-messaging-compat.js');
importScripts('./js/firebase-config.js', './js/notification-store.js');

firebase.initializeApp(firebaseConfig);
const messaging = firebase.messaging();

messaging.onBackgroundMessage(async (payload) => {
	console.log('[Service Worker] Received background message:', payload);

	await notificationStore.save(payload);

	// Fallback to data properties if notification properties are missing (Data-only message support)
	const notificationTitle = payload.notification?.title || payload.data?.title || 'New Notification';
	const notificationBody = payload.notification?.body || payload.data?.body || '';

	const notificationOptions = {
		body: notificationBody,
		icon: 'icon-192.png',
		badge: 'icon-192.png',
		data: payload.data, // Ensure data is passed for click handling
		// Unique tag per push so notifications never collapse/replace each other.
		tag: (payload.data?.tag || payload.data?.orderId || 'fcm') + '-' + Date.now() + '-' + Math.random().toString(36).slice(2, 8),
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
