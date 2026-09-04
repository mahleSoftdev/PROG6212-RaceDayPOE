CREATE DATABASE racedayDB;

CREATE TABLE Users (
    UserID INT PRIMARY KEY,
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    Email NVARCHAR(255) UNIQUE,
    PasswordHash NVARCHAR(100),
    Role NVARCHAR(50),
    CreatedAt DATETIME
);

INSERT INTO Users
    (UserID, FirstName, LastName, Email, PasswordHash, Role, CreatedAt)
VALUES
    (1, 'Naledi', 'Khumalo', 'naledi.khumalo@raceday.co.za', 'hashed_pw_1', 'Organiser', GETDATE()),
    (2, 'Thabo', 'Mokoena', 'thabo.mokoena@raceday.co.za', 'hashed_pw_2', 'Organiser', GETDATE()),
    (3, 'Lindiwe', 'Nkosi', 'lindiwe.nkosi@raceday.co.za', 'hashed_pw_3', 'Organiser', GETDATE()),
    (4, 'Sipho', 'Dlamini', 'sipho.dlamini@example.com', 'hashed_pw_4', 'Participant', GETDATE()),
    (5, 'Amahle', 'Van Wyk', 'amahle.vanwyk@example.com', 'hashed_pw_5', 'Participant', GETDATE());

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

    CONSTRAINT FK_Events_Users
        FOREIGN KEY (OrganiserID) REFERENCES Users(UserID),

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
    UserID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME,
    Status NVARCHAR(50),

    CONSTRAINT FK_Enrolments_Users
        FOREIGN KEY (UserID) REFERENCES Users(UserID),

    CONSTRAINT FK_Enrolments_Categories
        FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);

INSERT INTO Enrolments
    (EnrolmentID, UserID, CategoryID, EnrolmentDate, Status)
VALUES
    (1, 4, 1, GETDATE(), 'Confirmed'),
    (2, 5, 2, GETDATE(), 'Confirmed'),
    (3, 4, 4, GETDATE(), 'Pending'),
    (4, 5, 5, GETDATE(), 'Confirmed');

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