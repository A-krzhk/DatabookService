# Используем официальный образ .NET SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы проектов
COPY ["DatabookService.Api/DatabookService.Api.csproj", "DatabookService.Api/"]
COPY ["DatabookService.Application/DatabookService.Application.csproj", "DatabookService.Application/"]
COPY ["DatabookService.Domain/DatabookService.Domain.csproj", "DatabookService.Domain/"]
COPY ["DatabookService.Infrastructure/DatabookService.Infrastructure.csproj", "DatabookService.Infrastructure/"]

# Восстанавливаем зависимости
RUN dotnet restore "DatabookService.Api/DatabookService.Api.csproj"

# Копируем весь исходный код
COPY . .

# Собираем приложение
WORKDIR "/src/DatabookService.Api"
RUN dotnet build "DatabookService.Api.csproj" -c Release -o /app/build

# Публикуем приложение
FROM build AS publish
RUN dotnet publish "DatabookService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5000

# Копируем опубликованное приложение
COPY --from=publish /app/publish .

# Устанавливаем переменные окружения
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DatabookService.Api.dll"]

