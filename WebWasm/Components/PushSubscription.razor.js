// Web push subscription for PushSubscription.razor. Firebase is loaded only when the user taps the bell:
// the page needs it only to turn the browser push subscription into an FCM token for the backend.
const firebaseVersion = '13.3.0';
const firebaseConfig = {
	apiKey: 'AIzaSyCcnA6c8s9ML0VJu35JhwzTGQoG-PcNnN0',
	authDomain: 'kliffort-site.firebaseapp.com',
	projectId: 'kliffort-site',
	storageBucket: 'kliffort-site.firebasestorage.app',
	messagingSenderId: '250756228052',
	appId: '1:250756228052:web:e0623066a48c50c8512c41'
};
const vapidKey = 'BAOL6t3T-11IkD0zsIhvJd8cclIhh8VIvoKGtdgm2riQRiW9xoBFHvo4yH__os24AFiPItQDBKKn0_AS8-UK1vE';

// iPadOS Safari reports itself as Macintosh; touch points tell it apart from a Mac.
const isIos = () => /iPhone|iPad|iPod/.test(navigator.userAgent) || (/Macintosh/.test(navigator.userAgent) && navigator.maxTouchPoints > 1);
const isStandalone = () => navigator.standalone === true || matchMedia('(display-mode: standalone)').matches;

/**
 * Starts listening to taps on the bell and returns the current state:
 * install-required (iPhone/iPad Safari tab: push works only in the app added to the Home Screen),
 * unsupported, default, denied, granted (permission without a subscription), subscribed.
 */
export async function attach(button, dotNet) {
	button.pushClick = () => subscribe(dotNet);
	button.addEventListener('click', button.pushClick);
	return await getStatus();
}

export function detach(button) {
	button.removeEventListener('click', button.pushClick);
}

async function getStatus() {
	if (!('serviceWorker' in navigator) || !('PushManager' in window) || !('Notification' in window)) {
		return isIos() && !isStandalone() ? 'install-required' : 'unsupported';
	}

	if (Notification.permission !== 'granted') {
		return Notification.permission;
	}

	const registration = await navigator.serviceWorker.getRegistration();
	return (await registration?.pushManager.getSubscription()) ? 'subscribed' : 'granted';
}

// Runs in the click handler itself, not through .NET: WebKit shows the permission prompt only while the tap is
// being handled, so requestPermission is the first call here.
async function subscribe(dotNet) {
	const permission = Notification.requestPermission();
	dotNet.invokeMethodAsync('OnSubscribing');

	let result;
	try {
		result = (await permission) === 'granted'
			? { status: 'granted', token: await getFcmToken() }
			: { status: Notification.permission };
	} catch (error) {
		result = { status: 'error', error: error?.message ?? String(error) };
	}

	await dotNet.invokeMethodAsync('OnSubscribeResult', result.status, result.token ?? null, result.error ?? null, navigator.userAgent);
}

async function getFcmToken() {
	const base = `https://www.gstatic.com/firebasejs/${firebaseVersion}/`;
	const [{ initializeApp, getApps }, { getMessaging, getToken }] = await Promise.all([
		import(base + 'firebase-app.js'),
		import(base + 'firebase-messaging.js')
	]);

	const messaging = getMessaging(getApps()[0] ?? initializeApp(firebaseConfig));
	const options = { vapidKey, serviceWorkerRegistration: await navigator.serviceWorker.ready };

	try {
		return await getToken(messaging, options);
	} catch (error) {
		// iPhone: the first getToken right after the permission prompt often fails with AbortError, the second works
		// (firebase-js-sdk issues 8269, 8793).
		console.warn('getToken failed, retrying once', error);
		await new Promise((resolve) => setTimeout(resolve, 1000));
		return await getToken(messaging, options);
	}
}
