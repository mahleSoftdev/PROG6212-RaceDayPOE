CREATE DATABASE racedayDB;

CREATE TABLE Users(
	UserID INT PRIMARY KEY,
	FirstName NVARCHAR(100),
	LastName NVARCHAR(100),
	Email NVARCHAR UNIQUE,
	PasswordHash NVARCHAR(100),
	Role NVARCHAR,
	CreatedAt DATETIME
);

Create TABLE Events(
	EventID INT PRIMARY KEY,
);

ALTER TABLE Events ADD OrganiserID INT NOT NULL;
ALTER TABLE Events ADD VenueID INT NOT NULL;
ALTER TABLE Events ADD Name NVARCHAR(150);
ALTER TABLE Events ADD Description NVARCHAR(500);
ALTER TABLE Events ADD EventDate DATE;
ALTER TABLE Events ADD CreatedAt DATETIME;

ALTER TABLE Events
ADD CONSTRAINT FK_Events_Users
    FOREIGN KEY (OrganiserID) REFERENCES Users(UserID);

ALTER TABLE Events
ADD CONSTRAINT FK_Events_Venues
    FOREIGN KEY (VenueID) REFERENCES Venues(VenueID);

CREATE TABLE Venues (
    VenueID INT PRIMARY KEY,
    Name NVARCHAR(150),
    AddressLine NVARCHAR(200),
    City NVARCHAR(100),
    Province NVARCHAR(100),
    Latitude DECIMAL(9,6),
    Longitude DECIMAL(9,6)
);

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

CREATE TABLE Results (
    ResultID INT PRIMARY KEY,
    EnrolmentID INT NOT NULL,
    FinishTime TIME,
    Position INT,
    RecordedAt DATETIME,
    CONSTRAINT FK_Results_Enrolments
        FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID)
);