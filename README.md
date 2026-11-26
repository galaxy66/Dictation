# Dictation

## Frontend

`frontend/` is a React app built with Vite. Run `npm install`, then `npm run dev` from that folder.

## Backend

`backend/` is a .NET 10 API backed by PostgreSQL. At startup it creates the initial schema and seeds the current frontend letters in sort order. Words and sentences belong to a letter.

Configure these environment variables before running the API:

- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string.
- `Jwt__Key`: JWT signing key of at least 32 bytes.
- `BootstrapAdmin__Email` and `BootstrapAdmin__Password`: optional initial admin credentials; provide both to create/assign the admin account. Passwords must be at least eight characters and contain a digit.

Public registration creates user-role accounts only. User content endpoints are read-only; admin content-management endpoints require an admin JWT. OpenAPI is available at `/openapi/v1.json` in Development. See [backend/Dictation.Api.http](./backend/Dictation.Api.http) for example requests.
