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

messaging.onBackgroundMessage((payload) => {
	console.log('[Service Worker] Received background message:', payload);

	const notificationTitle = payload.notification?.title || 'New Notification';
	const notificationOptions = {
		body: payload.notification?.body || '',
		icon: 'icon-192.png',
		badge: 'icon-192.png',
		data: payload.data,
		tag: payload.data?.tag || 'default'
	};

	return self.registration.showNotification(notificationTitle, notificationOptions);
});

self.addEventListener('notificationclick', (event) => {
	console.log('[Service Worker] Notification click received:', event);
	event.notification.close();

	event.waitUntil(
		clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
			for (const client of clientList) {
				if (client.url && 'focus' in client) {
					return client.focus();
				}
			}

			if (clients.openWindow) {
				return clients.openWindow('/');
			}
		})
	);
});

self.addEventListener('push', (event) => {
	console.log('[Service Worker] Push event received:', event);
	
	if (event.data) {
		try {
			const payload = event.data.json();
			const notificationTitle = payload.notification?.title || 'New Notification';
			const notificationOptions = {
				body: payload.notification?.body || '',
				icon: 'icon-192.png',
				badge: 'icon-192.png',
				data: payload.data
			};

			event.waitUntil(
				self.registration.showNotification(notificationTitle, notificationOptions)
			);
		} catch (e) {
			console.log('[Service Worker] Push data:', event.data.text());
		}
	}
});

self.addEventListener('fetch', () => { });