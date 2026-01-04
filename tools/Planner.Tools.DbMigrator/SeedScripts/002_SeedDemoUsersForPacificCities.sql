/*
  Seed demo Admin and User accounts for each tenant.

  For each tenant:
   - Admin: {tenant.name}@demo.local / admin123
   - User : {tenant.name}.user@demo.local / user123

  Idempotent:
   - Will NOT insert if the email already exists.
*/

SET NOCOUNT ON;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- Insert Admin users
INSERT INTO Users (
    TenantId,
    Email,
    PasswordHash,
    Role,
    CreatedAt
)
SELECT
    t.Id AS TenantId,
    LOWER(t.Name) + '.admin@demo.local' AS Email,
    'admin123' AS PasswordHash,   -- demo only
    'Admin' AS Role,
    GETUTCDATE()
FROM Tenants t
WHERE NOT EXISTS (
    SELECT 1
    FROM Users u
    WHERE u.TenantId = t.Id
      AND u.Email = LOWER(t.Name) + '@demo.local'
);

-- Insert Normal users
INSERT INTO Users (
    TenantId,
    Email,
    PasswordHash,
    Role,
    CreatedAt
)
SELECT
    t.Id AS TenantId,
    LOWER(t.Name) + '.user@demo.local' AS Email,
    'user123' AS PasswordHash,     -- demo only
    'User' AS Role,
    GETUTCDATE()
FROM Tenants t
WHERE NOT EXISTS (
    SELECT 1
    FROM Users u
    WHERE u.TenantId = t.Id
      AND u.Email = LOWER(t.Name) + '.user@demo.local'
);

PRINT 'Demo users seeded successfully.';
