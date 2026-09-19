-- ============================================================
-- MIGRACIÓN: Tickets.PrioridadId pasa a ser NULLABLE
-- ------------------------------------------------------------
-- Motivo: el rol "Usuario" crea tickets sin prioridad; el
-- equipo de Soporte/Supervisor la asigna durante el triage
-- (pantalla Nuevos / Details → "Asignar prioridad").
-- El mapeo NHibernate (TicketMap) ya la declara Nullable.
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Tickets')
      AND name = 'PrioridadId'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE [dbo].[Tickets] ALTER COLUMN [PrioridadId] INT NULL;
    PRINT 'Tickets.PrioridadId ahora permite NULL.';
END
ELSE
    PRINT 'Tickets.PrioridadId ya permitia NULL. Sin cambios.';
GO
