-- ============================================================
-- Sprint 5 — Campo y Repuestos
-- ============================================================

-- ------------------------------------------------------------
-- VisitasTecnicas
-- ------------------------------------------------------------
CREATE TABLE VisitasTecnicas (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    TicketId      INT NULL REFERENCES Tickets(Id),
    TecnicoId     INT NOT NULL REFERENCES Usuarios(Id),
    FechaVisita   DATETIME NOT NULL,
    Direccion     NVARCHAR(300) NOT NULL,
    Descripcion   NVARCHAR(1000) NULL,
    Estado        NVARCHAR(50) NOT NULL DEFAULT 'Programada',  -- Programada, En Curso, Completada, Cancelada
    Observaciones NVARCHAR(1000) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT GETUTCDATE()
);

-- ------------------------------------------------------------
-- Repuestos
-- ------------------------------------------------------------
CREATE TABLE Repuestos (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    Nombre         NVARCHAR(150) NOT NULL,
    Descripcion    NVARCHAR(500) NULL,
    Codigo         NVARCHAR(50)  NULL,
    Categoria      NVARCHAR(50)  NOT NULL,   -- Hardware, Cable, Periférico, Consumible, Otro
    UnidadMedida   NVARCHAR(30)  NOT NULL DEFAULT 'Unidad',  -- Unidad, Caja, Metro, Litro
    PrecioUnitario DECIMAL(10,2) NOT NULL DEFAULT 0,
    Activo         BIT NOT NULL DEFAULT 1,
    CreadoEn       DATETIME NOT NULL DEFAULT GETUTCDATE()
);

INSERT INTO Repuestos (Nombre, Codigo, Categoria, UnidadMedida, PrecioUnitario) VALUES
    ('Cable UTP Cat6 1m',       'REP-001', 'Cable',      'Unidad', 3500),
    ('Pasta térmica',           'REP-002', 'Consumible', 'Unidad', 8000),
    ('Memoria RAM DDR4 8GB',    'REP-003', 'Hardware',   'Unidad', 95000),
    ('Disco SSD 240GB',         'REP-004', 'Hardware',   'Unidad', 180000),
    ('Teclado USB genérico',    'REP-005', 'Periférico', 'Unidad', 35000),
    ('Mouse USB óptico',        'REP-006', 'Periférico', 'Unidad', 22000),
    ('Cable HDMI 1.5m',         'REP-007', 'Cable',      'Unidad', 12000),
    ('Brida/amarres x100',      'REP-008', 'Consumible', 'Caja',   5000);

-- ------------------------------------------------------------
-- StockRepuestos  (stock por sede)
-- ------------------------------------------------------------
CREATE TABLE StockRepuestos (
    Id                 INT IDENTITY(1,1) PRIMARY KEY,
    RepuestoId         INT NOT NULL REFERENCES Repuestos(Id),
    SedeId             INT NOT NULL REFERENCES Sedes(Id),
    CantidadActual     INT NOT NULL DEFAULT 0,
    CantidadMinima     INT NOT NULL DEFAULT 5,
    FechaActualizacion DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Stock_Repuesto_Sede UNIQUE (RepuestoId, SedeId)
);

-- Stock inicial en Sede Central (Id=1)
INSERT INTO StockRepuestos (RepuestoId, SedeId, CantidadActual, CantidadMinima) VALUES
    (1, 1, 20, 5),
    (2, 1, 10, 3),
    (3, 1, 8,  2),
    (4, 1, 5,  2),
    (5, 1, 10, 3),
    (6, 1, 10, 3),
    (7, 1, 15, 5),
    (8, 1, 6,  2);

-- ------------------------------------------------------------
-- ConsumoRepuestos
-- ------------------------------------------------------------
CREATE TABLE ConsumoRepuestos (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    RepuestoId      INT NOT NULL REFERENCES Repuestos(Id),
    TicketId        INT NULL REFERENCES Tickets(Id),
    VisitaId        INT NULL REFERENCES VisitasTecnicas(Id),
    TecnicoId       INT NOT NULL REFERENCES Usuarios(Id),
    Cantidad        INT NOT NULL,
    FechaConsumo    DATETIME NOT NULL DEFAULT GETUTCDATE(),
    Observaciones   NVARCHAR(500) NULL
);
