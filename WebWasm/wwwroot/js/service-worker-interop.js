// Page side of the service worker: update notifications, foreground push messages, notification history.
window.serviceWorkerInterop = {
	dotNetReference: null,
	_listeners: [],
	_updateTimer: null,

	_notifyUpdate: function (version) {
		this.dotNetReference?.invokeMethodAsync('OnUpdateAvailable', version);
	},

	_listen: function (target, type, handler) {
		target.addEventListener(type, handler);
		this._listeners.push(() => target.removeEventListener(type, handler));
	},

	initialize: function (dotNetRef) {
		this.dispose();
		this.dotNetReference = dotNetRef;

		if ('serviceWorker' in navigator) {
			const sw = navigator.serviceWorker;
			this._listen(sw, 'message', (event) => {
				if (event.data?.type === 'SERVICE_WORKER_UPDATED') {
					this._notifyUpdate(event.data.version);
				}
			});
			this._listen(sw, 'controllerchange', () => this._notifyUpdate('new'));

			sw.ready.then((registration) => {
				registration.update();
				this._updateTimer = setInterval(() => registration.update(), 5 * 60 * 1000);

				if (registration.waiting) {
					this._notifyUpdate('waiting');
				}

				this._listen(registration, 'updatefound', () => {
					const newWorker = registration.installing;
					newWorker?.addEventListener('statechange', () => {
						if (newWorker.state === 'installed' && sw.controller) {
							this._notifyUpdate('installed');
						}
					});
				});
			});
		}

		window.onForegroundPush = (payload) => this.dotNetReference?.invokeMethodAsync('OnForegroundMessage', payload);
	},

	// Activates a waiting service worker (if any) and reloads the page.
	applyUpdate: async function () {
		if ('serviceWorker' in navigator) {
			const registration = await navigator.serviceWorker.getRegistration();
			if (registration?.waiting) {
				const activated = new Promise(resolve => navigator.serviceWorker.addEventListener('controllerchange', resolve, { once: true }));
				registration.waiting.postMessage('SKIP_WAITING');
				await Promise.race([activated, new Promise(resolve => setTimeout(resolve, 3000))]);
			}
		}
		location.reload();
	},

	getNotifications: function () {
		return notificationStore.getAll();
	},

	dispose: function () {
		this._listeners.forEach(remove => remove());
		this._listeners = [];
		clearInterval(this._updateTimer);
		this._updateTimer = null;
		window.onForegroundPush = null;
		this.dotNetReference = null;
	}
};
