-- ============================================================
-- Sprint 4 — Módulo de Inventario
-- ============================================================

-- ------------------------------------------------------------
-- Sedes
-- ------------------------------------------------------------
CREATE TABLE Sedes (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    Nombre    NVARCHAR(100) NOT NULL,
    Ciudad    NVARCHAR(100) NOT NULL,
    Direccion NVARCHAR(200) NULL,
    Activa    BIT NOT NULL DEFAULT 1
);

  -- Crear tabla ActivoTipos
  CREATE TABLE ActivoTipos (
      Id     INT           IDENTITY(1,1) PRIMARY KEY,
      Nombre NVARCHAR(50)  NOT NULL,
      Activo BIT           NOT NULL DEFAULT 1
  );

  -- Poblar con los tipos que existían en el sistema
  INSERT INTO ActivoTipos (Nombre) VALUES
      ('PC'),
      ('Laptop'),
      ('Servidor'),
      ('Monitor'),
      ('Impresora'),
      ('Telefono'),
      ('Red'),
      ('Otro');

INSERT INTO Sedes (Nombre, Ciudad, Direccion) VALUES
    ('Sede Central',    'Bogotá',     'Cra 7 # 45-20'),
    ('Sede Norte',      'Medellín',   'Cll 80 # 32-15'),
    ('Sede Sur',        'Cali',       'Av 6N # 22-05');

-- ------------------------------------------------------------
-- Areas
-- ------------------------------------------------------------
CREATE TABLE Areas (
    Id      INT IDENTITY(1,1) PRIMARY KEY,
    Nombre  NVARCHAR(100) NOT NULL,
    SedeId  INT NOT NULL REFERENCES Sedes(Id),
    Activa  BIT NOT NULL DEFAULT 1
);

INSERT INTO Areas (Nombre, SedeId) VALUES
    ('Sistemas',        1),
    ('Administración',  1),
    ('Recursos Humanos',1),
    ('Soporte TI',      1),
    ('Sistemas',        2),
    ('Administración',  2),
    ('Sistemas',        3);

-- ------------------------------------------------------------
-- Activos  (activos de TI asignados a usuarios o sedes)
-- ------------------------------------------------------------
CREATE TABLE Activos (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    Nombre           NVARCHAR(150) NOT NULL,
    Descripcion      NVARCHAR(500) NULL,
    Tipo             NVARCHAR(50)  NOT NULL,   -- PC, Laptop, Servidor, Monitor, Impresora, Telefono, Red, Otro
    NumeroSerie      NVARCHAR(100) NULL,
    Marca            NVARCHAR(100) NULL,
    Modelo           NVARCHAR(100) NULL,
    Estado           NVARCHAR(50)  NOT NULL DEFAULT 'Disponible',  -- Disponible, Asignado, En Mantenimiento, Baja
    SedeId           INT NULL REFERENCES Sedes(Id),
    AreaId           INT NULL REFERENCES Areas(Id),
    AsignadoAId      INT NULL REFERENCES Usuarios(Id),
    FechaAdquisicion DATE NULL,
    FechaGarantia    DATE NULL,
    Activo           BIT NOT NULL DEFAULT 1,
    CreadoEn         DATETIME NOT NULL DEFAULT GETUTCDATE()
);

INSERT INTO Activos (Nombre, Tipo, NumeroSerie, Marca, Modelo, Estado, SedeId, AreaId) VALUES
    ('Laptop Dell Admin',    'Laptop',    'SN-DELL-001', 'Dell',  'Latitude 5520', 'Disponible', 1, 2),
    ('PC Soporte #1',        'PC',        'SN-HP-001',   'HP',    'EliteDesk 800', 'Disponible', 1, 4),
    ('Servidor Principal',   'Servidor',  'SN-SRV-001',  'HP',    'ProLiant DL380','Disponible', 1, 1),
    ('Monitor 24" LG',       'Monitor',   'SN-LG-001',   'LG',    '24MK430H',      'Disponible', 1, 4),
    ('Impresora Sistemas',   'Impresora', 'SN-BRO-001',  'Brother','HL-L2350DW',   'Disponible', 1, 1);

-- ------------------------------------------------------------
-- Herramientas  (herramientas del equipo de soporte técnico)
-- ------------------------------------------------------------
CREATE TABLE Herramientas (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Nombre      NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    Codigo      NVARCHAR(50)  NULL,
    Tipo        NVARCHAR(50)  NOT NULL,   -- Hardware, Diagnóstico, Red, Eléctrico, Otro
    Estado      NVARCHAR(50)  NOT NULL DEFAULT 'Disponible',  -- Disponible, Asignada, En Reparación, Baja
    Activa      BIT NOT NULL DEFAULT 1,
    CreadoEn    DATETIME NOT NULL DEFAULT GETUTCDATE()
);

INSERT INTO Herramientas (Nombre, Codigo, Tipo, Estado) VALUES
    ('Probador de cables RJ45', 'HRR-001', 'Red',        'Disponible'),
    ('Destornillador set 32pc', 'HRR-002', 'Hardware',   'Disponible'),
    ('Multímetro digital',      'HRR-003', 'Eléctrico',  'Disponible'),
    ('Laptop diagnóstico',      'HRR-004', 'Diagnóstico','Disponible'),
    ('Switch administrable 8p', 'HRR-005', 'Red',        'Disponible');

-- ------------------------------------------------------------
-- AsignacionesHerramientas
-- ------------------------------------------------------------
CREATE TABLE AsignacionesHerramientas (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    HerramientaId    INT NOT NULL REFERENCES Herramientas(Id),
    TecnicoId        INT NOT NULL REFERENCES Usuarios(Id),
    AsignadoPorId    INT NOT NULL REFERENCES Usuarios(Id),
    FechaAsignacion  DATETIME NOT NULL DEFAULT GETUTCDATE(),
    FechaDevolucion  DATETIME NULL,
    Observaciones    NVARCHAR(500) NULL,
    Activa           BIT NOT NULL DEFAULT 1
);

-- ------------------------------------------------------------
-- InventarioMovimientos  (log de todos los cambios)
-- ------------------------------------------------------------
CREATE TABLE InventarioMovimientos (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    ActivoId       INT NULL REFERENCES Activos(Id),
    HerramientaId  INT NULL REFERENCES Herramientas(Id),
    TipoMovimiento NVARCHAR(50)  NOT NULL,  -- Registro, Asignación, Devolución, Transferencia, Mantenimiento, Baja
    UsuarioId      INT NOT NULL REFERENCES Usuarios(Id),
    FechaMovimiento DATETIME NOT NULL DEFAULT GETUTCDATE(),
    Descripcion    NVARCHAR(500) NULL,
    ValorAnterior  NVARCHAR(200) NULL,
    ValorNuevo     NVARCHAR(200) NULL
);
