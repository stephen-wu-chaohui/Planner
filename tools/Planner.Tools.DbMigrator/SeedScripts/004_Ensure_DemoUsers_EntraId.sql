-- Script: 004_Ensure_DemoUsers_EntraId.sql
-- Purpose: Ensure per-tenant demo users use Entra-compatible @plannerdemo.com emails
-- Safety: Idempotent, no deletes

/*
  Why:
  - Older demo datasets may contain @demo.local users.
  - Older seed variants may contain admin emails without ".admin".
  - Demo login modal and Entra setup expect:
      {tenant}.admin@plannerdemo.com
      {tenant}.user@plannerdemo.com

  This script normalizes existing rows and inserts any missing rows.
*/

SET NOCOUNT ON;

-- 1) Normalize legacy admin addresses to the canonical Entra address.
UPDATE u
SET
    u.Email = LOWER(t.Name) + '.admin@plannerdemo.com',
    u.PasswordHash = ''
FROM Users u
INNER JOIN Tenants t ON t.Id = u.TenantId
WHERE u.Role = 'Admin'
  AND (
      u.Email = LOWER(t.Name) + '@demo.local'
      OR u.Email = LOWER(t.Name) + '@plannerdemo.com'
      OR u.Email = LOWER(t.Name) + '.admin@demo.local'
      OR u.Email = LOWER(t.Name) + '.admin@plannerdemo.com'
  );

-- 2) Normalize legacy user addresses to the canonical Entra address.
UPDATE u
SET
    u.Email = LOWER(t.Name) + '.user@plannerdemo.com',
    u.PasswordHash = ''
FROM Users u
INNER JOIN Tenants t ON t.Id = u.TenantId
WHERE u.Role = 'User'
  AND (
      u.Email = LOWER(t.Name) + '.user@demo.local'
      OR u.Email = LOWER(t.Name) + '.user@plannerdemo.com'
      OR u.Email = LOWER(t.Name) + '@demo.local'
      OR u.Email = LOWER(t.Name) + '@plannerdemo.com'
  );

-- 3) Ensure each tenant has an admin account at the canonical Entra address.
INSERT INTO Users (TenantId, Email, PasswordHash, Role, CreatedAt)
SELECT
    t.Id,
    LOWER(t.Name) + '.admin@plannerdemo.com',
    '',
    'Admin',
    SYSUTCDATETIME()
FROM Tenants t
WHERE NOT EXISTS (
    SELECT 1
    FROM Users u
    WHERE u.TenantId = t.Id
      AND u.Email = LOWER(t.Name) + '.admin@plannerdemo.com'
);

-- 4) Ensure each tenant has a user account at the canonical Entra address.
INSERT INTO Users (TenantId, Email, PasswordHash, Role, CreatedAt)
SELECT
    t.Id,
    LOWER(t.Name) + '.user@plannerdemo.com',
    '',
    'User',
    SYSUTCDATETIME()
FROM Tenants t
WHERE NOT EXISTS (
    SELECT 1
    FROM Users u
    WHERE u.TenantId = t.Id
      AND u.Email = LOWER(t.Name) + '.user@plannerdemo.com'
);

PRINT 'Demo users normalized for Entra ID (@plannerdemo.com).';
