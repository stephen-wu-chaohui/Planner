-- Script: 003_Update_DemoUsers_EntraId.sql
-- Purpose: Update demo user emails to @plannerdemo.com and clear passwords for Microsoft Entra ID
-- Safety: Idempotent, updates existing demo users only

/*
  Update demo users for Microsoft Entra ID integration:
   - Change email domain from @demo.local to @plannerdemo.com
   - Clear PasswordHash (authentication now handled by Entra ID)

  For each tenant:
   - Admin: {tenant.name}.admin@plannerdemo.com (was {tenant.name}.admin@demo.local)
   - User : {tenant.name}.user@plannerdemo.com (was {tenant.name}.user@demo.local)

  Idempotent:
   - Will only update users with @demo.local emails
   - Will not affect users already migrated
*/

SET NOCOUNT ON;

DECLARE @UpdatedCount INT = 0;

-- Update Admin users
UPDATE u
SET 
    Email = REPLACE(u.Email, '@demo.local', '@plannerdemo.com'),
    PasswordHash = ''
FROM Users u
WHERE u.Email LIKE '%@demo.local'
  AND u.Role = 'Admin';

SET @UpdatedCount = @@ROWCOUNT;

-- Update Normal users
UPDATE u
SET 
    Email = REPLACE(u.Email, '@demo.local', '@plannerdemo.com'),
    PasswordHash = ''
FROM Users u
WHERE u.Email LIKE '%@demo.local'
  AND u.Role = 'User';

SET @UpdatedCount = @UpdatedCount + @@ROWCOUNT;

PRINT 'Demo users updated for Microsoft Entra ID: ' + CAST(@UpdatedCount AS NVARCHAR(10)) + ' users updated.';
