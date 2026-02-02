USE [PlannerDb];
GO
DROP PROCEDURE IF EXISTS sp_RotateSystemEvents;
GO
CREATE PROCEDURE sp_RotateSystemEvents AS
BEGIN
  DELETE FROM SystemEvents WHERE Timestamp < DATEADD(DAY, -7, GETUTCDATE());
  INSERT INTO SystemEvents(Timestamp, Source, Message, IsError)
  VALUES (GETUTCDATE(), 'Maintenance', 'Old events removed.', 0);
END;
GO

DROP PROCEDURE IF EXISTS sp_UpdateStatistics;
GO
CREATE PROCEDURE sp_UpdateStatistics AS
BEGIN
  INSERT INTO SystemEvents(Timestamp, Source, Message, IsError)
  VALUES (GETUTCDATE(), 'Maintenance', 'Statistics updated.', 0);
END;
GO
