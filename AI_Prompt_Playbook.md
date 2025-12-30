# AI Development Prompt Playbook
## Step-by-step prompts for controlled software generation

---

## PROMPT 0 – AI Role & Rules (Run once)
```
You are a senior ASP.NET Core developer working under a strict technical specification.

Rules:
- You MUST strictly follow the uploaded markdown specification files.
- Do NOT change architecture, technologies, or decisions.
- Do NOT introduce SPA frameworks.
- Do NOT generate unnecessary code.
- Ask before making assumptions.
- Work incrementally and wait for approval after each step.

Acknowledge these rules and wait.
```
---

## PROMPT 1 – Solution & Project Structure
```
Based on the uploaded specification files:

Design the full ASP.NET Core solution structure.

Output ONLY:
1. Solution name
2. List of projects
3. Folder structure for each project
4. Responsibility of each project

Rules:
- No code
- No implementation details
- Architecture must follow the defined layered model

Wait for approval.
```
---

## PROMPT 2 – Module Mapping
```
Using the defined modules in the specification:

For EACH module, list:
- Domain entities
- Application services
- API endpoints (names only)
- MVC controllers (names only)

Rules:
- MVP modules only
- No code
- No assumptions beyond the files

Wait for approval.
```
---

## PROMPT 3 – Database Design
```
Design the database schema for MVP modules.

Output:
- Tables
- Columns per table
- Relationships
- Soft delete fields
- Audit fields

Rules:
- No EF Core
- No code
- Relational database mindset (SQL Server)

Wait for approval.
```
---

## PROMPT 4 – Domain Layer
```
Generate Domain layer code for MVP modules.

Rules:
- Plain C# classes
- NO EF Core attributes
- NO data annotations
- NO infrastructure concerns
- Include basic business rules as methods
- Entities must be persistence-ignorant

Output:
- One entity per file
- Clean, minimal code

Stop after Domain layer.
Wait for approval.
```
---

## PROMPT 5 – Application Layer
```
Create Application layer services for MVP features.

For EACH service:
- Interface
- Implementation
- Public methods only
- Clear responsibility

Rules:
- No controllers
- No EF Core
- Use DTOs if needed
- Call Domain entities properly

Wait for approval.
```
---

## PROMPT 6 – Infrastructure Layer
```
Implement Infrastructure layer.

Include:
- DbContext
- Entity configurations
- Repositories
- Soft delete handling
- Audit fields handling

Rules:
- EF Core only here
- Domain must remain clean
- Repository pattern required

Wait for approval.
```
---

## PROMPT 7 – Web API Controllers
```
Create ASP.NET Core Web API controllers for MVP modules.

Rules:
- Use DTOs only
- No business logic
- Proper HTTP status codes
- RESTful naming
- Authorization ready

Output:
- Controller code only

Wait for approval.
```
---

## PROMPT 8 – MVC Controllers & Views
```
Create ASP.NET Core MVC controllers and Razor Views
for MVP features.

Focus on:
- Dashboard
- Content Calendar
- Idea Bank

Rules:
- MVC Controllers consume Application services
- Views must be simple and functional
- No heavy JavaScript frameworks

Wait for approval.
```
---

## PROMPT 9 – Authentication & Authorization
```
Add authentication and authorization.

Requirements:
- ASP.NET Core Identity
- User-Page relationship
- Role-based access
- Secure API and MVC

Rules:
- No UI polish yet
- Focus on correctness

Wait for approval.
```
---

## PROMPT 10 – Validation, Errors, Logging
```
Improve the solution by adding:
- Input validation
- Global error handling
- Logging strategy

Rules:
- No over-engineering
- Production-ready defaults

List changes clearly.
Wait for approval.
```
---

## PROMPT 11 – Final Review
```
Review the entire solution.

Output:
- Architectural risks
- Technical debt
- Refactoring suggestions
- Missing edge cases

Do NOT change code.
Do NOT add features.
```
