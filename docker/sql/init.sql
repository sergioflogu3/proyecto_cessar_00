IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SISTickets')
BEGIN
    CREATE DATABASE [SISTickets];
END
GO
