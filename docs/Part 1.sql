CREATE DATABASE racedayDB;
USE racedayDB;

-- Organisers are provisioned accounts (they cannot self-register). Separate from Participants.
CREATE TABLE Organisers (
    OrganiserID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME NOT NULL
);

INSERT INTO Organisers
    (FirstName, LastName, Email, PasswordHash, CreatedAt)
VALUES
    ('Naledi', 'Khumalo', 'naledi.khumalo@raceday.co.za', 'hashed_pw_1', GETDATE()),
    ('Thabo', 'Mokoena', 'thabo.mokoena@raceday.co.za', 'hashed_pw_2', GETDATE()),
    ('Lindiwe', 'Nkosi', 'lindiwe.nkosi@raceday.co.za', 'hashed_pw_3', GETDATE());

CREATE TABLE Participants (
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME NOT NULL
);

INSERT INTO Participants
    (FirstName, LastName, Email, PasswordHash, CreatedAt)
VALUES
    ('Sipho', 'Dlamini', 'sipho.dlamini@example.com', 'hashed_pw_4', GETDATE()),
    ('Amahle', 'Van Wyk', 'amahle.vanwyk@example.com', 'hashed_pw_5', GETDATE());

CREATE TABLE Venues (
    VenueID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    AddressLine NVARCHAR(200) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    Province NVARCHAR(100) NOT NULL,
    Latitude DECIMAL(9,6) NOT NULL,
    Longitude DECIMAL(9,6) NOT NULL
);

INSERT INTO Venues
    (Name, AddressLine, City, Province, Latitude, Longitude)
VALUES
    ('Zwartkops Raceway', '1 Kudu Road', 'Pretoria', 'Gauteng', -25.834200, 28.101600),
    ('Kirstenbosch Gardens', 'Rhodes Drive', 'Cape Town', 'Western Cape', -33.988400, 18.432500),
    ('Moses Mabhida Stadium', '44 Isaiah Ntshangase Road', 'Durban', 'KwaZulu-Natal', -29.830800, 31.030100);

CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    VenueID INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    EventDate DATE NOT NULL,
    EventType NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME NOT NULL,

    CONSTRAINT CK_Events_EventType
        CHECK (EventType IN ('Run', 'Walk', 'Cycle')),

    CONSTRAINT FK_Events_Organisers
        FOREIGN KEY (OrganiserID) REFERENCES Organisers(OrganiserID),

    CONSTRAINT FK_Events_Venues
        FOREIGN KEY (VenueID) REFERENCES Venues(VenueID)
);

INSERT INTO Events
    (OrganiserID, VenueID, Name, Description, EventDate, EventType, CreatedAt)
VALUES
    (2, 1, 'Pretoria 10K Challenge',
     'Annual road running event through the city.',
     '2026-10-10', 'Run', GETDATE()),

    (3, 2, 'Cape Town Trail Run',
     'Scenic trail run through Kirstenbosch Gardens.',
     '2026-11-15', 'Run', GETDATE()),

    (2, 3, 'Durban Beachfront Marathon',
     'Full and half marathon along the Durban coastline.',
     '2026-12-05', 'Run', GETDATE());

CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    MaxParticipants INT NOT NULL,
    EntryFee DECIMAL(10,2) NOT NULL,

    CONSTRAINT FK_Categories_Events
        FOREIGN KEY (EventID) REFERENCES Events(EventID)
);

INSERT INTO Categories
    (EventID, Name, DistanceKm, MaxParticipants, EntryFee)
VALUES
    (1, '10km Individual', 10.00, 200, 150.00),
    (1, '5km Fun Run', 5.00, 300, 80.00),
    (2, 'Trail Run 15km', 15.00, 150, 200.00),
    (3, 'Half Marathon', 21.10, 500, 250.00),
    (3, 'Full Marathon', 42.20, 300, 350.00);

CREATE TABLE Enrolments (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME NOT NULL,
    Status NVARCHAR(50) NOT NULL,

    CONSTRAINT CK_Enrolments_Status
        CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),

    CONSTRAINT FK_Enrolments_Participants
        FOREIGN KEY (ParticipantID) REFERENCES Participants(ParticipantID),

    CONSTRAINT FK_Enrolments_Categories
        FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);

INSERT INTO Enrolments
    (ParticipantID, CategoryID, EnrolmentDate, Status)
VALUES
    (1, 1, GETDATE(), 'Confirmed'),
    (2, 2, GETDATE(), 'Confirmed'),
    (1, 4, GETDATE(), 'Pending'),
    (2, 5, GETDATE(), 'Confirmed');

CREATE TABLE Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL,
    FinishTime TIME NOT NULL,
    Position INT NOT NULL,
    RecordedAt DATETIME NOT NULL,

    CONSTRAINT FK_Results_Enrolments
        FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID)
);

INSERT INTO Results
    (EnrolmentID, FinishTime, Position, RecordedAt)
VALUES
    (1, '00:45:32', 3, GETDATE()),
    (2, '00:22:10', 1, GETDATE()),
    (4, '03:15:47', 5, GETDATE());
