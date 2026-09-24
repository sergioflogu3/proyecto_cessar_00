-- M3: auditoría de cambios de rol y de-activación/activación de usuarios.
-- Ejecutar después de users.sql (depende de la tabla Usuarios ya existente).

CREATE TABLE [dbo].[AuditoriaUsuarios] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UsuarioId] INT NOT NULL,
    [ModificadoPorId] INT NOT NULL,
    [Campo] NVARCHAR(50) NOT NULL,
    [ValorAnterior] NVARCHAR(100) NULL,
    [ValorNuevo] NVARCHAR(100) NULL,
    [FechaCambio] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [FK_AuditoriaUsuarios_Usuario] FOREIGN KEY ([UsuarioId])
        REFERENCES [dbo].[Usuarios]([Id]),
    CONSTRAINT [FK_AuditoriaUsuarios_ModificadoPor] FOREIGN KEY ([ModificadoPorId])
        REFERENCES [dbo].[Usuarios]([Id])
);

CREATE INDEX [IX_AuditoriaUsuarios_UsuarioId] ON [dbo].[AuditoriaUsuarios]([UsuarioId]);
