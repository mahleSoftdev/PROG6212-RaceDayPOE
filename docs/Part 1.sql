CREATE DATABASE racedayDB;

CREATE TABLE Organisers (
    OrganiserID INT PRIMARY KEY,
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    Email NVARCHAR(255) UNIQUE,
    PasswordHash NVARCHAR(100),
    CreatedAt DATETIME
);

INSERT INTO Organisers
    (OrganiserID, FirstName, LastName, Email, PasswordHash, CreatedAt)
VALUES
    (1, 'Naledi', 'Khumalo', 'naledi.khumalo@raceday.co.za', 'hashed_pw_1', GETDATE()),
    (2, 'Thabo', 'Mokoena', 'thabo.mokoena@raceday.co.za', 'hashed_pw_2', GETDATE()),
    (3, 'Lindiwe', 'Nkosi', 'lindiwe.nkosi@raceday.co.za', 'hashed_pw_3', GETDATE());

CREATE TABLE Participants (
    ParticipantID INT PRIMARY KEY,
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    Email NVARCHAR(255) UNIQUE,
    PasswordHash NVARCHAR(100),
    CreatedAt DATETIME
);

INSERT INTO Participants
    (ParticipantID, FirstName, LastName, Email, PasswordHash, CreatedAt)
VALUES
    (1, 'Sipho', 'Dlamini', 'sipho.dlamini@example.com', 'hashed_pw_4', GETDATE()),
    (2, 'Amahle', 'Van Wyk', 'amahle.vanwyk@example.com', 'hashed_pw_5', GETDATE());

CREATE TABLE Venues (
    VenueID INT PRIMARY KEY,
    Name NVARCHAR(150),
    AddressLine NVARCHAR(200),
    City NVARCHAR(100),
    Province NVARCHAR(100),
    Latitude DECIMAL(9,6),
    Longitude DECIMAL(9,6)
);

INSERT INTO Venues
    (VenueID, Name, AddressLine, City, Province, Latitude, Longitude)
VALUES
    (1, 'Zwartkops Raceway', '1 Kudu Road', 'Pretoria', 'Gauteng', -25.834200, 28.101600),
    (2, 'Kirstenbosch Gardens', 'Rhodes Drive', 'Cape Town', 'Western Cape', -33.988400, 18.432500),
    (3, 'Moses Mabhida Stadium', '44 Isaiah Ntshangase Road', 'Durban', 'KwaZulu-Natal', -29.830800, 31.030100);

CREATE TABLE Events (
    EventID INT PRIMARY KEY,
    OrganiserID INT NOT NULL,
    VenueID INT NOT NULL,
    Name NVARCHAR(150),
    Description NVARCHAR(500),
    EventDate DATE,
    CreatedAt DATETIME,

    CONSTRAINT FK_Events_Organisers
        FOREIGN KEY (OrganiserID) REFERENCES Organisers(OrganiserID),

    CONSTRAINT FK_Events_Venues
        FOREIGN KEY (VenueID) REFERENCES Venues(VenueID)
);

INSERT INTO Events
    (EventID, OrganiserID, VenueID, Name, Description, EventDate, CreatedAt)
VALUES
    (1, 2, 1, 'Pretoria 10K Challenge',
     'Annual road running event through the city.',
     '2026-10-10', GETDATE()),

    (2, 3, 2, 'Cape Town Trail Run',
     'Scenic trail run through Kirstenbosch Gardens.',
     '2026-11-15', GETDATE()),

    (3, 2, 3, 'Durban Beachfront Marathon',
     'Full and half marathon along the Durban coastline.',
     '2026-12-05', GETDATE());

CREATE TABLE Categories (
    CategoryID INT PRIMARY KEY,
    EventID INT NOT NULL,
    Name NVARCHAR(100),
    DistanceKm DECIMAL(6,2),
    MaxParticipants INT,
    EntryFee DECIMAL(10,2),

    CONSTRAINT FK_Categories_Events
        FOREIGN KEY (EventID) REFERENCES Events(EventID)
);

INSERT INTO Categories
    (CategoryID, EventID, Name, DistanceKm, MaxParticipants, EntryFee)
VALUES
    (1, 1, '10km Individual', 10.00, 200, 150.00),
    (2, 1, '5km Fun Run', 5.00, 300, 80.00),
    (3, 2, 'Trail Run 15km', 15.00, 150, 200.00),
    (4, 3, 'Half Marathon', 21.10, 500, 250.00),
    (5, 3, 'Full Marathon', 42.20, 300, 350.00);

CREATE TABLE Enrolments (
    EnrolmentID INT PRIMARY KEY,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME,
    Status NVARCHAR(50),

    CONSTRAINT FK_Enrolments_Participants
        FOREIGN KEY (ParticipantID) REFERENCES Participants(ParticipantID),

    CONSTRAINT FK_Enrolments_Categories
        FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);

INSERT INTO Enrolments
    (EnrolmentID, ParticipantID, CategoryID, EnrolmentDate, Status)
VALUES
    (1, 1, 1, GETDATE(), 'Confirmed'),
    (2, 2, 2, GETDATE(), 'Confirmed'),
    (3, 1, 4, GETDATE(), 'Pending'),
    (4, 2, 5, GETDATE(), 'Confirmed');

CREATE TABLE Results (
    ResultID INT PRIMARY KEY,
    EnrolmentID INT NOT NULL,
    FinishTime TIME,
    Position INT,
    RecordedAt DATETIME,

    CONSTRAINT FK_Results_Enrolments
        FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID)
);


INSERT INTO Results
    (ResultID, EnrolmentID, FinishTime, Position, RecordedAt)
VALUES
    (1, 1, '00:45:32', 3, GETDATE()),
    (2, 2, '00:22:10', 1, GETDATE()),
    (3, 4, '03:15:47', 5, GETDATE());