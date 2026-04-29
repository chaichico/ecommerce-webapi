# Code Review Report

Review language: English
Date: 2026-04-29
Scope: Current workspace snapshot, with emphasis on the active controller/service/config changes in this branch

## Findings

No new critical issues found in the reviewed changes. The remaining problems are important correctness and API-contract issues.

**🟡 IMPORTANT - Correctness: `GET /api/orders/{id}` can return 500 for a valid but stale JWT**

The new single-order endpoint does not handle the `UnauthorizedAccessException` that is thrown when the token email no longer maps to a user record.

- [Controllers/OrdersController.cs](c:/Users/Itsc01/Desktop/ecommerce/Controllers/OrdersController.cs)
- [Services/OrderService.cs](c:/Users/Itsc01/Desktop/ecommerce/Services/OrderService.cs)

**Why this matters:**
`GetById()` only maps `KeyNotFoundException` and `SecurityException`, but `GetUserByEmailOrThrowAsync()` throws `UnauthorizedAccessException` for a deleted or otherwise missing user. In that case the request currently escapes the action as an unhandled exception and becomes a 500 instead of the same 401 behavior used by the other order endpoints.

**Suggested fix:**
Add an `UnauthorizedAccessException` catch to `GetById()` and return `Unauthorized(new { message = ex.Message })`, matching the behavior already used in create, update, and confirm.

**Reference:** Consistent authentication failure handling across resource endpoints

---

**🟡 IMPORTANT - REST: `POST /api/users/register` still returns a misleading `Location` header**

The register action now returns `201 Created`, but it still points `CreatedAtAction` at the registration endpoint itself rather than at a retrievable user resource.

- [Controllers/UsersController.cs](c:/Users/Itsc01/Desktop/ecommerce/Controllers/UsersController.cs)

**Why this matters:**
`CreatedAtAction(nameof(Register), result)` generates a `Location` for `POST /api/users/register`, which is an action endpoint, not the canonical URI of the created user. That does not actually solve the REST contract problem that motivated the earlier change and can mislead clients that follow the header.

**Suggested fix:**
Either add a real `GET /api/users/{id}` or `GET /api/users/{email}` resource endpoint and point `CreatedAtAction(...)` at it, or return `StatusCode(201, result)` until such a read endpoint exists.

**Reference:** RFC 9110 `201 Created`; project rule requiring meaningful typed resource responses

---

**🟡 IMPORTANT - Error Handling: unexpected server failures are still surfaced as `400 Bad Request` with raw exception messages**

Several controller actions still treat any unhandled exception as a client error and echo the exception text back to the caller.

- [Controllers/AdminController.cs](c:/Users/Itsc01/Desktop/ecommerce/Controllers/AdminController.cs)
- [Controllers/OrdersController.cs](c:/Users/Itsc01/Desktop/ecommerce/Controllers/OrdersController.cs)
- [Controllers/UsersController.cs](c:/Users/Itsc01/Desktop/ecommerce/Controllers/UsersController.cs)

**Why this matters:**
This misclassifies infrastructure or programming faults as request-validation failures and leaks internal exception detail in API responses. For example, a database outage in admin search or order create would currently look like a 400 from the client side, which makes operational diagnosis and API semantics worse.

**Suggested fix:**
Keep explicit mappings for expected domain exceptions such as `InvalidOperationException`, `KeyNotFoundException`, and `UnauthorizedAccessException`, but replace the final catch-all with `StatusCode(500, new { message = "Internal server error" })` or centralize the behavior in ASP.NET Core exception-handling middleware.

**Reference:** Project error-response guideline and standard 4xx/5xx separation

## Validation Notes

Static diagnostics reported no compile errors in the touched files.

Targeted test execution through the test tool returned no discovered results for the selected files in this session, so this review is based on source inspection plus IDE diagnostics rather than a confirmed passing test run.