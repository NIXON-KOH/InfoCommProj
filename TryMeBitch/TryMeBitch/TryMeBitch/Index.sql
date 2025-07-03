
Drop Table Blockchain;
Drop Table Staions; 

CREATE TABLE Blockchain (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CardId UNIQUEIDENTIFIER NOT NULL,
    EventType NVARCHAR(20) NOT NULL, -- 'TopUp', 'TapIn', 'TapOut'
    FareCharged FLOAT NOT NULL DEFAULT 0,
    NewBalance FLOAT NOT NULL,
    Station NVARCHAR(100) NOT NULL,
    Timestamp DATETIME2 NOT NULL,
    Hash NVARCHAR(256) NOT NULL,
    PrevHash NVARCHAR(256) NULL
);

CREATE TABLE Stations (
    StationID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    StationName NVARCHAR(100) NOT NULL,
    [Type] NVARCHAR(20) CHECK ([Type] IN ('Interchange', 'Station')),
    Lat Decimal(8,6),
    Lon Decimal(9,6),
);
