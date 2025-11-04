# Многоступенчатая сборка для React приложения

# Этап 1: Сборка приложения
FROM node:20-alpine AS build
WORKDIR /app

# Копируем package.json и package-lock.json
COPY package*.json ./

# Устанавливаем зависимости
RUN npm ci

# Копируем исходный код
COPY . .

# Собираем приложение для production 
RUN npm run build

# Этап 2: Production сервер
FROM nginx:alpine

# Копируем собранное приложение 
COPY --from=build /app/dist /usr/share/nginx/html

# Копируем конфигурацию nginx (настроить проксирование API)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Открываем порт
EXPOSE 80

# Запускаем nginx
CMD ["nginx", "-g", "daemon off;"]