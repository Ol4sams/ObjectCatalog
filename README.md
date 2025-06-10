# ObjectCatalog

CRUD ASP.NET Core 6-8 SPA-приложение с хранением данных на Sql Express Структура данных: 1. объект - любая сущность с количеством атрибутов больше трех. 2. категория - сущность, представленная как минимум именем. Объект может входить в несколько категорий, а может не входить ни в одну. Количество объектов в БД - от 1-го миллиона. Количество категорий в БД - от 10 до 100. Как минимум 90% объектов должны входить в одну из категорий. Обеспечить через api приложения возможность получения всех объектов по заданной категории в формате json.

# Запуск

- Сконфигурировать строку подключения к SQL Express серверу в appsettings.json
```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ObjectCatalog;Trusted_Connection=True;TrustServerCertificate=True;"
  },
```

- Запустить приложение
```powershell
cd .\ObjectCatalog\
dotnet run --launch-profile "http"
```
- Дождаться готовности БД

- Открыть ссылку в браузере
http://localhost:5144
http://localhost:5144/swagger
