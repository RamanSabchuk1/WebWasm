# Kliffort Admin (WebWasm)

Админка суперадмина Kliffort: Blazor WebAssembly PWA на .NET 11, хостинг GitHub Pages.

## Dev и prod: один код, разница в переменной окружения

Один и тот же код публикуется дважды:

| | dev | prod |
|---|---|---|
| репозиторий | `RamanSabchuk1/WebWasm` | `RamanSabchuk1/ProdA` (копия этого репозитория) |
| адрес | `https://ramansabchuk1.github.io/WebWasm/` | `https://ramansabchuk1.github.io/ProdA/` |
| переменная `APP_ENVIRONMENT` | `Dev` (или не задана) | `Prod` |
| API | `https://kliffort.com/api/dev/` | `https://kliffort.com/api/` |
| иконки и манифест PWA | `WebWasm/Environments/Dev/` | `WebWasm/Environments/Prod/` |
| метка в интерфейсе | `DEV` | нет |

Как это устроено:

- `APP_ENVIRONMENT` — переменная репозитория в GitHub: Settings → Secrets and variables → Actions → Variables.
  Workflow передаёт её в `dotnet publish -p:WasmApplicationEnvironmentName=...`. Другие значения, кроме `Dev` и
  `Prod`, workflow не пропускает.
- Настройки окружения: `WebWasm/wwwroot/appsettings.json` (dev) и `appsettings.Prod.json` (что меняется в prod).
- Иконки и `manifest.webmanifest` лежат в `WebWasm/Environments/<Dev|Prod>/`. При сборке нужный набор попадает в
  корень сайта под теми же именами.
- Имя репозитория в коде не встречается. Путь сайта (`/WebWasm/`, `/ProdA/`) workflow подставляет в `<base href>`,
  остальное берёт путь из него: область service worker, имя его кэша, база истории уведомлений, ключи localStorage.
  Поэтому две админки на одном домене `ramansabchuk1.github.io` не мешают друг другу: вход в одну не выкидывает из другой.

### Обновить prod (ProdA)

1. Скопировать в ProdA всё содержимое этого репозитория, кроме папки `.git`. Ничего не переименовывать и не править.
2. Один раз: в ProdA создать переменную репозитория `APP_ENVIRONMENT` со значением `Prod`.
3. Push в `main` репозитория ProdA запускает сборку и выкладку.

Проверка после выкладки: в prod нет метки `DEV`, иконка prod-набора, запросы идут на `https://kliffort.com/api/`.

Первое обновление ProdA до версии 2.0: ключи localStorage и база истории уведомлений получили новые имена,
поэтому каждому пользователю prod нужно один раз войти заново, а история уведомлений начнётся с нуля.

## Работа с кодом

```bash
dotnet build WebWasm/WebWasm.slnx                                # сборка (окружение Development, настройки dev)
dotnet test --project WebWasm.Tests/WebWasm.Tests.csproj           # тесты
dotnet format WebWasm/WebWasm.slnx --verify-no-changes           # формат по .editorconfig
dotnet run --project WebWasm/WebWasm.csproj                      # локальный запуск
dotnet publish WebWasm/WebWasm.csproj -c Release -o publish -p:WasmApplicationEnvironmentName=Prod   # сборка prod
```

Нужны .NET SDK из `global.json` и workload `wasm-tools` (`dotnet workload install wasm-tools`).

Настройки сборки: trimming `partial`, без AOT (AOT увеличивает загрузку в 2.5 раза), `InvariantGlobalization`.
GitHub Pages не отдаёт `.br`-файлы со сжатием, поэтому brotli распаковывает в браузере пакет `BlazorWasmBrotliLoader`.

## Push-уведомления

- Токен устройства страница получает через Firebase (модуль `Components/PushSubscription.razor.js`, Firebase
  загружается только при нажатии на колокольчик).
- Push принимает и показывает свой service worker (`wwwroot/js/push-sw.js`), без Firebase. Уведомление
  показывается всегда, даже если админка открыта: iPhone отзывает подписку, если push пришёл без уведомления.
- На iPhone и iPad push работает только в админке, установленной на экран «Домой» (iOS 16.4 и новее).
