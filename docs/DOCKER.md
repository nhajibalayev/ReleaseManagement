# Запуск в Docker Desktop (Linux-контейнер)

Собирает приложение в образ `mcr.microsoft.com/dotnet/aspnet:10.0` и запускает его
так же, как оно будет работать в Kubernetes. База данных — удалённый PostgreSQL.

## Шаги

```powershell
git pull
copy .env.example .env      # и заполнить реальные значения
docker compose up --build   # первый раз ~5 минут (качает SDK/runtime образы)
```

Открыть <http://localhost:8080>. Health-check: <http://localhost:8080/health>.

Остановить: `docker compose down`. Логи: `docker compose logs -f web`.

## Что заполнить в .env

| Переменная | Что это |
|---|---|
| `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` | удалённый PostgreSQL. **Не `localhost`** — внутри контейнера это сам контейнер. |
| `AD_DOMAIN` | `NH-NK` |
| `AD_LDAP_SERVER` | FQDN контроллера домена, напр. `dc01.nh-nk.az`. Узнать: `nltest /dsgetdc:NH-NK` или `echo %LOGONSERVER%` на доменной машине. |
| `AD_LDAP_USE_SSL` | `true` + порт 636, если IT требует LDAPS. Если сертификат корпоративного CA не доверен в контейнере — `AD_LDAP_SKIP_CERT_VALIDATION=true` (только для теста). |
| `ADO_*` | как в user-secrets: URL коллекции, проект `Release Management Board`, ApiVersion `6.0`. |

Без корпоративной сети: `AD_MODE=Mock` (любой пользователь, пароль `Mock!123`) и `ADO_MODE=Mock`.

## Миграции БД

Контейнер **не** применяет миграции сам. Один раз с машины, где есть .NET SDK:

```powershell
dotnet ef database update --project src/ReleaseManagement.Infrastructure --startup-project src/ReleaseManagement.Web --connection "Host=...;Port=5432;Database=...;Username=...;Password=..."
```

## Как это работает на Linux

* AD-логин: на Windows используется `System.DirectoryServices.AccountManagement`,
  в Linux-контейнере он недоступен, поэтому автоматически включается
  `LdapActiveDirectoryAuthenticator` (простой bind к контроллеру домена по LDAP,
  затем поиск пользователя; SID сохраняется в том же формате `ad:S-1-5-...`,
  учётки пользователей не дублируются). Требует `libldap` в образе — уже в Dockerfile.
* Azure DevOps Server: NTLM с учёткой вошедшего пользователя — работает на Linux без изменений.
* Порт внутри контейнера 8080 (HTTP). TLS терминирует ingress/Docker-хост.
* Тома: вложения `/app/App_Data/attachments`, логи `/app/logs`,
  ключи Data Protection `/root/.aspnet/DataProtection-Keys` (иначе после рестарта слетают сессии).

## Типичные ошибки

* **`PlatformNotSupportedException` при логине** — не должно быть; значит выбран Windows-аутентификатор. Проверь, что `WindowsAuth__Mode` не `Mock` и образ Linux.
* **LDAP `The LDAP server is unavailable`** — контейнер не видит `AD_LDAP_SERVER`: DNS/фаервол. Проверить с хоста: `Test-NetConnection dc01.nh-nk.az -Port 389`.
* **`invalidCredentials` (49) при верном пароле** — контроллер требует LDAPS/подпись: включить `AD_LDAP_USE_SSL=true`.
* **401 от DevOps** — NTLM не прошёл; проверить, что `ADO_ORG_URL` доступен из контейнера и учётка имеет доступ к проекту.
* **Не качаются образы `mcr.microsoft.com`** — корпоративный прокси: Docker Desktop → Settings → Resources → Proxies, либо внутренний registry.

## Для DevOps-команды (Kubernetes)

Образ собирается из `Dockerfile` в корне. Вся конфигурация — через переменные
окружения (ключи `Section__Key`, см. `docker-compose.yml` как список). Нужны:
Secret с `ConnectionStrings__DefaultConnection`, ConfigMap с `WindowsAuth__*` и
`AzureDevOps__*`, три PersistentVolume (см. выше), liveness `/health/live`,
readiness `/health/ready`, порт 8080. Сетевой доступ из namespace: PostgreSQL,
контроллер домена (389/636), `devops.nh-nk.az` (443).
