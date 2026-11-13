// Push handling for both service workers (service-worker.js in development, service-worker.published.js after publish).
//
// Every push ends in showNotification inside event.waitUntil, also when the app is open. WebKit (iPhone, iPad) counts
// a push without a visible notification as "silent" and revokes the subscription after a few of them. The Firebase SDK
// skips the notification when a window is visible, so Firebase is not used here: the page uses it only to get a token.
//
// The backend sends web devices data-only messages: { data: { title, body, clickAction, orderId, ... } }.

self.addEventListener('push', (event) => event.waitUntil(onPush(event)));

self.addEventListener('notificationclick', (event) => {
	event.notification.close();
	event.waitUntil(openFromNotification(event.notification.data));
});

async function onPush(event) {
	let message = {};
	try {
		message = event.data?.json() ?? {};
	} catch {
		// Not JSON: a notification is still required.
	}

	const data = message.data ?? {};
	const title = message.notification?.title || data.title || 'Kliffort';
	const body = message.notification?.body || data.body || '';

	await self.registration.showNotification(title, {
		body,
		icon: 'icon-192.png',
		badge: 'badge-96.png',
		data,
		// Unique tag: notifications never replace each other.
		tag: `${data.orderId || 'push'}-${Date.now()}`
	});

	await notificationStore.save({ title, body, data });

	const windows = await clients.matchAll({ type: 'window', includeUncontrolled: true });
	windows.forEach((client) => client.postMessage({ type: 'PUSH_RECEIVED' }));
}

function notificationPath(data) {
	if (data?.url) {
		return data.url;
	}

	const action = data?.clickAction || data?.action;
	const orderId = data?.orderId || data?.id;
	return (action === 'OPEN_ORDER_DETAILS' || action === 'OPEN_DELIVERY_DETAILS') && orderId ? `orders/${orderId}` : './';
}

async function openFromNotification(data) {
	const url = new URL(notificationPath(data), self.registration.scope).href;
	const windows = await clients.matchAll({ type: 'window', includeUncontrolled: true });

	const exact = windows.find((client) => client.url === url);
	if (exact) {
		return exact.focus();
	}

	if (windows.length > 0) {
		const client = await windows[0].focus();
		return client.navigate(url);
	}

	return clients.openWindow(url);
}
