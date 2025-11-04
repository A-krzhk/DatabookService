# Инструкция по запуску приложения в Docker

## Требования
- Docker Desktop или Docker Engine
- Docker Compose

## Запуск приложения

1. **Запустите все сервисы командой:**
   ```bash
   docker-compose up --build
   ```

2. **Или для запуска в фоновом режиме:**
   ```bash
   docker-compose up -d --build
   ```

## Структура сервисов

### PostgreSQL (База данных)
- **Порт**: 5432
- **База данных**: DatabookService
- **Пользователь**: postgres
- **Пароль**: postgres
- **Имя контейнера**: databook-postgres

### Backend API
- **Порт**: 5000
- **URL**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger (в Development режиме)
- **Имя контейнера**: databook-backend
- Автоматически применяет миграции БД при запуске

### Frontend
- **Порт**: 80
- **URL**: http://localhost
- **Имя контейнера**: databook-frontend
- Запросы к `/api/*` автоматически проксируются на backend

## Остановка приложения

```bash
docker-compose down
```

## Остановка с удалением данных БД

```bash
docker-compose down -v
```

## Просмотр логов

```bash
# Все сервисы
docker-compose logs -f

# Конкретный сервис
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f postgres
```

## Пересборка после изменений

```bash
docker-compose up --build
```

## Проверка статуса контейнеров

```bash
docker-compose ps
```

## Доступ к контейнерам

```bash
# Backend
docker exec -it databook-backend bash

# Frontend
docker exec -it databook-frontend sh

# PostgreSQL
docker exec -it databook-postgres psql -U postgres -d DatabookService
```

## Настройка

### Изменение портов
Порты можно изменить в файле `docker-compose.yml`:
- PostgreSQL: `5432:5432` → `ваш_порт:5432`
- Backend: `5000:5000` → `ваш_порт:5000`
- Frontend: `80:80` → `ваш_порт:80`

### Изменение пароля БД
1. Измените `POSTGRES_PASSWORD` в `docker-compose.yml`
2. Измените строку подключения в `docker-compose.yml` (переменная `ConnectionStrings__DefaultConnection`)
3. Пересоберите контейнеры

### Переменные окружения
Переменные окружения для backend можно настроить в секции `environment` сервиса `backend` в `docker-compose.yml`.

