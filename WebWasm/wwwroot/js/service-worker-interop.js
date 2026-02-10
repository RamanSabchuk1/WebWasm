window.serviceWorkerInterop = {
	dotNetReference: null,

	initialize: function (dotNetRef) {
		this.dotNetReference = dotNetRef;

		if ('serviceWorker' in navigator) {
			// Listen for service worker updates
			navigator.serviceWorker.addEventListener('message', (event) => {
				if (event.data && event.data.type === 'SERVICE_WORKER_UPDATED') {
					console.log('Service Worker updated to version:', event.data.version);
					if (this.dotNetReference) {
						this.dotNetReference.invokeMethodAsync('OnUpdateAvailable', event.data.version);
					}
				}
			});

			// Listen for controller change (new service worker activated)
			navigator.serviceWorker.addEventListener('controllerchange', () => {
				console.log('Service Worker controller changed');
				if (this.dotNetReference) {
					this.dotNetReference.invokeMethodAsync('OnUpdateAvailable', 'new');
				}
			});

			// Check for updates on page load
			navigator.serviceWorker.ready.then((registration) => {
				// Check for updates immediately
				registration.update();

				// Set up periodic update checks every 5 minutes
				setInterval(() => {
					console.log('Checking for service worker updates...');
					registration.update();
				}, 5 * 60 * 1000);
			});

			// Listen for waiting service worker
			navigator.serviceWorker.ready.then((registration) => {
				if (registration.waiting) {
					console.log('Service Worker waiting to activate');
					if (this.dotNetReference) {
						this.dotNetReference.invokeMethodAsync('OnUpdateAvailable', 'waiting');
					}
				}

				// Listen for new service worker installing
				registration.addEventListener('updatefound', () => {
					const newWorker = registration.installing;
					console.log('Service Worker update found');

					newWorker.addEventListener('statechange', () => {
						if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
							console.log('New Service Worker installed, waiting to activate');
							if (this.dotNetReference) {
								this.dotNetReference.invokeMethodAsync('OnUpdateAvailable', 'installed');
							}
						}
					});
				});
			});
		}

		// Initialize Firebase foreground messaging
		if (typeof firebase !== 'undefined') {
			try {
				const messaging = firebase.messaging();
				messaging.onMessage((payload) => {
					console.log('[Interop] Foreground message:', payload);
					if (this.dotNetReference) {
						this.dotNetReference.invokeMethodAsync('OnForegroundMessage', payload);
					}
				});
			} catch (e) {
				console.warn('Firebase messaging initialization failed in interop:', e);
			}
		}
	},

	checkForUpdates: function () {
		if ('serviceWorker' in navigator) {
			navigator.serviceWorker.ready.then((registration) => {
				registration.update();
			});
		}
	},

	skipWaiting: function () {
		if ('serviceWorker' in navigator) {
			navigator.serviceWorker.ready.then((registration) => {
				if (registration.waiting) {
					registration.waiting.postMessage('SKIP_WAITING');
				}
			});
		}
	},

	getNotifications: function () {
		return new Promise((resolve, reject) => {
			const request = indexedDB.open('webwasm-db', 1);

			request.onupgradeneeded = function (event) {
				const db = event.target.result;
				if (!db.objectStoreNames.contains('notifications')) {
					db.createObjectStore('notifications', { keyPath: 'id', autoIncrement: true });
				}
			};

			request.onsuccess = function (event) {
				const db = event.target.result;
				const transaction = db.transaction(['notifications'], 'readonly');
				const store = transaction.objectStore('notifications');
				const getAllRequest = store.getAll();

				getAllRequest.onsuccess = function () {
					const items = getAllRequest.result.sort((a, b) => {
						return new Date(b.timestamp) - new Date(a.timestamp);
					});
					resolve(items);
				};
				getAllRequest.onerror = () => resolve([]);
			};

			request.onerror = () => resolve([]);
		});
	},

	dispose: function () {
		this.dotNetReference = null;
	}
};
