# JWT Secured Todo API

A secure Todo REST API built with ASP.NET Core Minimal API using JWT authentication.

## 🚀 Features
- JWT Authentication & Authorization
- CRUD operations for Todo items
- ASP.NET Core Minimal API
- Swagger / OpenAPI documentation
- SQLite with Entity Framework Core
- CORS enabled

## 🛠 Tech Stack
- ASP.NET Core
- Minimal API
- JWT (JSON Web Token)
- Entity Framework Core
- SQLite
- Swagger

## 🔐 Authentication
Login endpoint returns a JWT token.  
Protected endpoints require:
Authorization: Bearer <token>
## 📌 Endpoints
- POST /auth/login
- GET /todos
- POST /todos
- GET /todos/{id}
- PUT /todos/{id}
- DELETE /todos/{id}

## ▶️ Run Locally
```bash
dotnet restore
dotnet run
Swagger UI:

http://localhost:5146/swagger
