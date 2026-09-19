-- ============================================================
-- SPRINT 2 - MÓDULO DE TICKETS
-- Ejecutar en orden. Requiere que la tabla Usuarios ya exista.
-- ============================================================

-- ------------------------------------------------------------
-- 1. ESTADOS DE TICKET
-- ------------------------------------------------------------
CREATE TABLE [dbo].[TicketEstados] (
    [Id]          INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Nombre]      NVARCHAR(50)  NOT NULL,
    [Descripcion] NVARCHAR(200) NULL,
    [Color]       NVARCHAR(20)  NOT NULL DEFAULT '#6c757d',
    [Orden]       INT           NOT NULL DEFAULT 0,
    [Activo]      BIT           NOT NULL DEFAULT 1
);

INSERT INTO [dbo].[TicketEstados] (Nombre, Color, Orden) VALUES
('Abierto',     '#0d6efd', 1),
('En Progreso', '#fd7e14', 2),
('Pendiente',   '#ffc107', 3),
('Resuelto',    '#198754', 4),
('Cerrado',     '#6c757d', 5),
('Cancelado',   '#dc3545', 6);

-- ------------------------------------------------------------
-- 2. PRIORIDADES DE TICKET
-- ------------------------------------------------------------
CREATE TABLE [dbo].[TicketPrioridades] (
    [Id]          INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Nombre]      NVARCHAR(50)  NOT NULL,
    [Descripcion] NVARCHAR(200) NULL,
    [Color]       NVARCHAR(20)  NOT NULL DEFAULT '#6c757d',
    [Orden]       INT           NOT NULL DEFAULT 0,
    [Activo]      BIT           NOT NULL DEFAULT 1
);

INSERT INTO [dbo].[TicketPrioridades] (Nombre, Color, Orden) VALUES
('Baja',    '#198754', 1),
('Media',   '#ffc107', 2),
('Alta',    '#fd7e14', 3),
('Crítica', '#dc3545', 4);

-- ------------------------------------------------------------
-- 3. CATEGORÍAS DE TICKET
-- ------------------------------------------------------------
CREATE TABLE [dbo].[TicketCategorias] (
    [Id]          INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Nombre]      NVARCHAR(100) NOT NULL,
    [Descripcion] NVARCHAR(200) NULL,
    [Activo]      BIT           NOT NULL DEFAULT 1
);

INSERT INTO [dbo].[TicketCategorias] (Nombre) VALUES
('Hardware'),
('Software'),
('Red / Conectividad'),
('Impresoras'),
('Correo Electrónico'),
('Accesos y Permisos'),
('Otro');

-- ------------------------------------------------------------
-- 4. TICKETS (tabla principal)
-- ------------------------------------------------------------
CREATE TABLE [dbo].[Tickets] (
    [Id]                  INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Titulo]              NVARCHAR(200)  NOT NULL,
    [Descripcion]         NVARCHAR(MAX)  NOT NULL,
    [FechaCreacion]       DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    [FechaActualizacion]  DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    [FechaCierre]         DATETIME2      NULL,
    [FechaVencimiento]    DATETIME2      NULL,
    [EstadoId]            INT            NOT NULL,
    [PrioridadId]         INT            NULL,          -- el Usuario crea sin prioridad; Soporte la asigna en el triage
    [CategoriaId]         INT            NULL,
    [CreadoPorId]         INT            NOT NULL,
    [AsignadoAId]         INT            NULL,
    CONSTRAINT FK_Tickets_Estado    FOREIGN KEY ([EstadoId])    REFERENCES [dbo].[TicketEstados]([Id]),
    CONSTRAINT FK_Tickets_Prioridad FOREIGN KEY ([PrioridadId]) REFERENCES [dbo].[TicketPrioridades]([Id]),
    CONSTRAINT FK_Tickets_Categoria FOREIGN KEY ([CategoriaId]) REFERENCES [dbo].[TicketCategorias]([Id]),
    CONSTRAINT FK_Tickets_CreadoPor FOREIGN KEY ([CreadoPorId]) REFERENCES [dbo].[Usuarios]([Id]),
    CONSTRAINT FK_Tickets_AsignadoA FOREIGN KEY ([AsignadoAId]) REFERENCES [dbo].[Usuarios]([Id])
);

-- ------------------------------------------------------------
-- 5. COMENTARIOS
-- ------------------------------------------------------------
CREATE TABLE [dbo].[TicketComentarios] (
    [Id]            INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [TicketId]      INT           NOT NULL,
    [UsuarioId]     INT           NOT NULL,
    [Contenido]     NVARCHAR(MAX) NOT NULL,
    [EsInterno]     BIT           NOT NULL DEFAULT 0,
    [FechaCreacion] DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Comentarios_Ticket  FOREIGN KEY ([TicketId])  REFERENCES [dbo].[Tickets]([Id]),
    CONSTRAINT FK_Comentarios_Usuario FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuarios]([Id])
);

-- ------------------------------------------------------------
-- 6. ADJUNTOS
-- ------------------------------------------------------------
CREATE TABLE [dbo].[TicketAdjuntos] (
    [Id]             INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [TicketId]       INT            NOT NULL,
    [SubidoPorId]    INT            NOT NULL,
    [NombreArchivo]  NVARCHAR(255)  NOT NULL,
    [RutaArchivo]    NVARCHAR(500)  NOT NULL,
    [TipoContenido]  NVARCHAR(100)  NULL,
    [TamanioBytes]   BIGINT         NOT NULL DEFAULT 0,
    [FechaSubida]    DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Adjuntos_Ticket  FOREIGN KEY ([TicketId])    REFERENCES [dbo].[Tickets]([Id]),
    CONSTRAINT FK_Adjuntos_Usuario FOREIGN KEY ([SubidoPorId]) REFERENCES [dbo].[Usuarios]([Id])
);

-- ------------------------------------------------------------
-- 7. HISTORIAL
-- ------------------------------------------------------------
CREATE TABLE [dbo].[TicketHistorial] (
    [Id]            INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [TicketId]      INT            NOT NULL,
    [UsuarioId]     INT            NOT NULL,
    [Accion]        NVARCHAR(100)  NOT NULL,
    [ValorAnterior] NVARCHAR(200)  NULL,
    [ValorNuevo]    NVARCHAR(200)  NULL,
    [FechaAccion]   DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Historial_Ticket  FOREIGN KEY ([TicketId])  REFERENCES [dbo].[Tickets]([Id]),
    CONSTRAINT FK_Historial_Usuario FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuarios]([Id])
);
