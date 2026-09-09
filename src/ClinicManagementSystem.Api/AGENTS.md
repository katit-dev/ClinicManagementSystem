# Clinic Management System

## Project Overview

Clinic Management System is a capstone project built with ASP.NET Core Web API, Blazor, Entity Framework Core, and SQL Server.

The system manages patients, doctors, appointments, examinations, laboratory results, prescriptions, medicines, invoices, and payments.

## Architecture

Follow the existing layered architecture:

Blazor → API → Application → Infrastructure → Database → SQL Server

This is a single backend application. Do not introduce microservices, YARP, Aspire, Kafka, or separate service databases unless explicitly requested.

Keep the existing project names, namespaces, and folder structure. Inspect the repository before creating new files or changing architecture.

## Layer Responsibilities

**API:** Controllers, HTTP requests/responses, authentication, authorization, Swagger, middleware, and dependency injection.

**Application:** DTOs, interfaces, services, validation, business logic, helpers, and constants.

**Infrastructure:** EF Core DbContext, models, repositories, Unit of Work, and external integrations.

**Database:** SQL Server schema, SQL scripts, relationships, constraints, and seed data.

**Blazor:** Razor pages, components, layouts, frontend state services, and API calls.

## Database Workflow

The current project follows a Database First workflow.

Inspect the existing SQL scripts, DbContext, models, and relationships before making database changes.

Do not modify the database schema, run destructive SQL, or re-scaffold existing models without approval. Preserve existing relationships, constraints, and custom code.

Do not connect to or modify the teacher's shared database unless explicitly requested.

## Repository and Unit of Work

Follow the existing Repository Pattern and Unit of Work implementation.

Use IRepositoryBase<T> and RepositoryBase<T> for shared data-access operations.

Keep entity-specific repository interfaces and implementations together when following the existing project convention.

Keep database queries in repositories and business logic in Application services.

Register dependencies in the API project.

## Feature Workflow

Implement one feature end-to-end instead of completing the entire backend before starting frontend.

Use this order:

DTO → Repository (if needed) → Service → Controller → Swagger test → Blazor StateService → Razor Page → End-to-end test.

For example, complete Register on both Backend and Frontend before moving to Login.

Before implementing a feature, inspect existing code and explain which files need to change. Do not modify unrelated files.

## Coding Conventions

Follow existing naming conventions and response DTO patterns.

Use async/await for asynchronous database operations.

Keep Controllers thin and place business rules in Application services.

Use dependency injection instead of manually creating repositories or DbContexts.

Do not add new packages or technologies without a clear need.

## Build and Testing

After code changes, build the affected project and run relevant tests when available.

Do not claim a build or test passed unless it was actually executed.

Report errors clearly and explain which files were changed.

## Git and Secrets

Do not commit passwords, JWT secrets, connection strings, or other credentials.

Do not commit bin/, obj/, or ignored local documents.

Do not run git add ., git commit, or git push unless explicitly requested.

Keep commits small and organized by feature or implementation step.

## Future Skill

After Register is completed from Backend to Frontend and passes end-to-end testing, create an implement-feature skill based on the actual workflow.

Do not create the skill yet.
