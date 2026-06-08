
PRINT N'╔══════════════════════════════════════════════════════════════════════╗';
PRINT N'║        ONE VENUE MULTI HALL BOOKING SYSTEM  —  OneVenueDB           ║';
PRINT N'║        Database Design Specification  v1.0  |  SQL Server            ║';
PRINT N'╚══════════════════════════════════════════════════════════════════════╝';
PRINT N'';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 1 ─ CREATE DATABASE
   ═══════════════════════════════════════════════════════════════════════════ */
PRINT N'[SECTION 1]  Creating database OneVenueDB ...';

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'OneVenueDB')
BEGIN
    ALTER DATABASE OneVenueDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE OneVenueDB;
    PRINT N'  → Existing database dropped.';
END
GO

CREATE DATABASE OneVenueDB
    COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

PRINT N'  → Database OneVenueDB created.';

USE OneVenueDB;
GO

/* Booking reference sequence — generates readable booking IDs  */
CREATE SEQUENCE dbo.BookingReferenceSeq
    START WITH 1000
    INCREMENT BY 1
    NO MAXVALUE;
GO

PRINT N'  → Sequence dbo.BookingReferenceSeq created.';
PRINT N'';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 2 ─ TABLES  (ordered so parents precede children)
   ═══════════════════════════════════════════════════════════════════════════ */
PRINT N'[SECTION 2]  Creating tables ...';

-- Defensive drops to handle cases where database drop/recreate is not possible
DROP VIEW IF EXISTS dbo.vw_BookingSummary;
DROP VIEW IF EXISTS dbo.vw_HallRevenue;
DROP FUNCTION IF EXISTS dbo.fn_AvailableHalls;
DROP TABLE IF EXISTS dbo.Invoices;
DROP TABLE IF EXISTS dbo.Feedback;
DROP TABLE IF EXISTS dbo.Refunds;
DROP TABLE IF EXISTS dbo.Cancellations;
DROP TABLE IF EXISTS dbo.Payments;
DROP TABLE IF EXISTS dbo.Bookings;
DROP TABLE IF EXISTS dbo.HallAvailability;
DROP TABLE IF EXISTS dbo.HallAmenities;
DROP TABLE IF EXISTS dbo.Amenities;
DROP TABLE IF EXISTS dbo.HallImages;
DROP TABLE IF EXISTS dbo.Halls;
DROP TABLE IF EXISTS dbo.VenueLocations;
DROP TABLE IF EXISTS dbo.HallCategories;
DROP TABLE IF EXISTS dbo.Users;
DROP SEQUENCE IF EXISTS dbo.BookingReferenceSeq;
DROP USER IF EXISTS ov_app;
DROP USER IF EXISTS ov_report;
DROP USER IF EXISTS ov_dba;

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 1 : Users
   Purpose  : Core authentication & authorisation for Admins and Customers.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Users (
    UserId      INT           IDENTITY(1,1) NOT NULL,
    Username    NVARCHAR(80)  NOT NULL,
    Email       NVARCHAR(120) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName    NVARCHAR(150) NOT NULL,
    PhoneNumber NVARCHAR(15)  NULL,
    Role        NVARCHAR(20)  NOT NULL
        CONSTRAINT CHK_Users_Role CHECK (Role IN ('Admin', 'Customer')),
    IsActive    BIT           NOT NULL CONSTRAINT DF_Users_IsActive    DEFAULT 1,
    CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Users_CreatedAt   DEFAULT GETDATE(),
    UpdatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Users_UpdatedAt   DEFAULT GETDATE(),

    CONSTRAINT PK_Users            PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Username   UNIQUE (Username),
    CONSTRAINT UQ_Users_Email      UNIQUE (Email),
    CONSTRAINT UQ_Users_Phone      UNIQUE (PhoneNumber)
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 2 : HallCategories
   Purpose  : Classifies halls — Wedding, Conference, Banquet, Seminar, etc.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.HallCategories (
    CategoryId   INT           IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    Description  NVARCHAR(MAX) NULL,
    IsActive     BIT           NOT NULL CONSTRAINT DF_HallCat_IsActive DEFAULT 1,

    CONSTRAINT PK_HallCategories            PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_HallCategories_Name       UNIQUE (CategoryName)
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 3 : VenueLocations
   Purpose  : Physical location/floor/wing for halls within the venue.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.VenueLocations (
    LocationId   INT           IDENTITY(1,1) NOT NULL,
    LocationName NVARCHAR(150) NOT NULL,
    Floor        NVARCHAR(50)  NULL,
    Wing         NVARCHAR(50)  NULL,
    Description  NVARCHAR(MAX) NULL,

    CONSTRAINT PK_VenueLocations PRIMARY KEY (LocationId),
    CONSTRAINT UQ_VenueLocations_Name UNIQUE (LocationName)
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 4 : Halls
   Purpose  : Every bookable hall within the venue.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Halls (
    HallId       INT            IDENTITY(1,1) NOT NULL,
    CategoryId   INT            NOT NULL,
    LocationId   INT            NOT NULL,
    HallName     NVARCHAR(150)  NOT NULL,
    Capacity     INT            NOT NULL
        CONSTRAINT CHK_Halls_Capacity    CHECK (Capacity > 0),
    PricePerHour DECIMAL(10,2)  NOT NULL
        CONSTRAINT CHK_Halls_Price       CHECK (PricePerHour > 0),
    Description  NVARCHAR(MAX)  NULL,
    IsAvailable  BIT            NOT NULL CONSTRAINT DF_Halls_IsAvailable DEFAULT 1,
    CreatedAt    DATETIME2      NOT NULL CONSTRAINT DF_Halls_CreatedAt   DEFAULT GETDATE(),

    CONSTRAINT PK_Halls             PRIMARY KEY (HallId),
    CONSTRAINT UQ_Halls_Name        UNIQUE (HallName),
    CONSTRAINT FK_Hall_Category     FOREIGN KEY (CategoryId)  REFERENCES dbo.HallCategories (CategoryId) ON DELETE NO ACTION,
    CONSTRAINT FK_Hall_Location     FOREIGN KEY (LocationId)  REFERENCES dbo.VenueLocations  (LocationId) ON DELETE NO ACTION
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 5 : HallImages
   Purpose  : Gallery images for hall browsing.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.HallImages (
    ImageId    INT            IDENTITY(1,1) NOT NULL,
    HallId     INT            NOT NULL,
    ImageUrl   NVARCHAR(500)  NOT NULL,
    Caption    NVARCHAR(200)  NULL,
    SortOrder  INT            NOT NULL CONSTRAINT DF_HallImg_Sort DEFAULT 0,

    CONSTRAINT PK_HallImages        PRIMARY KEY (ImageId),
    CONSTRAINT FK_HallImage_Hall    FOREIGN KEY (HallId) REFERENCES dbo.Halls (HallId) ON DELETE CASCADE
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 6 : Amenities
   Purpose  : Master list — AC, WiFi, Projector, Parking, Catering, etc.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Amenities (
    AmenityId   INT           IDENTITY(1,1) NOT NULL,
    AmenityName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsActive    BIT           NOT NULL CONSTRAINT DF_Amenities_IsActive DEFAULT 1,

    CONSTRAINT PK_Amenities           PRIMARY KEY (AmenityId),
    CONSTRAINT UQ_Amenities_Name      UNIQUE (AmenityName)
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 7 : HallAmenities
   Purpose  : Many-to-many bridge — Halls ↔ Amenities.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.HallAmenities (
    HallId    INT NOT NULL,
    AmenityId INT NOT NULL,

    CONSTRAINT PK_HallAmenities         PRIMARY KEY (HallId, AmenityId),
    CONSTRAINT FK_HallAmenity_Hall      FOREIGN KEY (HallId)    REFERENCES dbo.Halls     (HallId)    ON DELETE CASCADE,
    CONSTRAINT FK_HallAmenity_Amenity   FOREIGN KEY (AmenityId) REFERENCES dbo.Amenities (AmenityId) ON DELETE CASCADE
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 8 : HallAvailability
   Purpose  : Blocked / available dates per hall (admin-controlled overrides).
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.HallAvailability (
    AvailabilityId INT  IDENTITY(1,1) NOT NULL,
    HallId         INT  NOT NULL,
    AvailableDate  DATE NOT NULL,
    IsAvailable    BIT  NOT NULL CONSTRAINT DF_HallAvail_IsAvail DEFAULT 1,
    Remarks        NVARCHAR(500) NULL,

    CONSTRAINT PK_HallAvailability      PRIMARY KEY (AvailabilityId),
    CONSTRAINT UQ_HallAvail_HallDate    UNIQUE (HallId, AvailableDate),
    CONSTRAINT FK_HallAvail_Hall        FOREIGN KEY (HallId) REFERENCES dbo.Halls (HallId) ON DELETE CASCADE
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 9 : Bookings
   Purpose  : Central hall reservation transaction table.
   Denorm   : HallNameSnapshot, CustomerNameSnapshot preserved for history.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Bookings (
    BookingId            INT           IDENTITY(1,1) NOT NULL,
    BookingReference     NVARCHAR(20)  NOT NULL,   -- e.g. BK-1000
    UserId               INT           NOT NULL,
    HallId               INT           NOT NULL,
    HallNameSnapshot     NVARCHAR(150) NOT NULL,   -- denorm: name at booking time
    CustomerNameSnapshot NVARCHAR(150) NOT NULL,   -- denorm: name at booking time
    EventDate            DATE          NOT NULL,
    StartTime            TIME          NOT NULL,
    EndTime              TIME          NOT NULL,
    GuestCount           INT           NOT NULL
        CONSTRAINT CHK_Bk_Guests      CHECK (GuestCount > 0),
    TotalAmount          DECIMAL(10,2) NOT NULL
        CONSTRAINT CHK_Bk_Amount      CHECK (TotalAmount > 0),
    BookingStatus        NVARCHAR(20)  NOT NULL
        CONSTRAINT CHK_Bk_Status      CHECK (BookingStatus IN ('Pending','Confirmed','Cancelled'))
        CONSTRAINT DF_Bk_Status       DEFAULT 'Pending',
    Purpose              NVARCHAR(300) NULL,
    CreatedAt            DATETIME2     NOT NULL CONSTRAINT DF_Bk_CreatedAt DEFAULT GETDATE(),
    UpdatedAt            DATETIME2     NOT NULL CONSTRAINT DF_Bk_UpdatedAt DEFAULT GETDATE(),

    CONSTRAINT PK_Bookings          PRIMARY KEY (BookingId),
    CONSTRAINT UQ_Bookings_Ref      UNIQUE (BookingReference),
    CONSTRAINT FK_Bk_User           FOREIGN KEY (UserId)  REFERENCES dbo.Users (UserId)  ON DELETE NO ACTION,
    CONSTRAINT FK_Bk_Hall           FOREIGN KEY (HallId)  REFERENCES dbo.Halls (HallId)  ON DELETE NO ACTION,
    CONSTRAINT CHK_Bk_Times        CHECK (EndTime > StartTime)
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 10 : Payments
   Purpose  : One payment record per booking.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Payments (
    PaymentId            INT            IDENTITY(1,1) NOT NULL,
    BookingId            INT            NOT NULL,
    Amount               DECIMAL(10,2)  NOT NULL
        CONSTRAINT CHK_Pay_Amount       CHECK (Amount > 0),
    PaymentMethod        NVARCHAR(50)   NOT NULL
        CONSTRAINT CHK_Pay_Method       CHECK (PaymentMethod IN ('Cash','Card','UPI','BankTransfer')),
    TransactionReference NVARCHAR(150)  NOT NULL,
    PaymentStatus        NVARCHAR(20)   NOT NULL
        CONSTRAINT CHK_Pay_Status       CHECK (PaymentStatus IN ('Pending','Success','Failed'))
        CONSTRAINT DF_Pay_Status        DEFAULT 'Pending',
    PaymentDate          DATETIME2      NULL,
    CreatedAt            DATETIME2      NOT NULL CONSTRAINT DF_Pay_CreatedAt DEFAULT GETDATE(),

    CONSTRAINT PK_Payments               PRIMARY KEY (PaymentId),
    CONSTRAINT UQ_Pay_Booking            UNIQUE (BookingId),             -- BR-010: one payment per booking
    CONSTRAINT UQ_Pay_TransactionRef     UNIQUE (TransactionReference),
    CONSTRAINT FK_Pay_Booking            FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId) ON DELETE NO ACTION
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 11 : Cancellations
   Purpose  : Cancellation requests linked to bookings.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Cancellations (
    CancellationId   INT            IDENTITY(1,1) NOT NULL,
    BookingId        INT            NOT NULL,
    Reason           NVARCHAR(MAX)  NULL,
    RefundAmount     DECIMAL(10,2)  NOT NULL
        CONSTRAINT CHK_Can_Refund       CHECK (RefundAmount >= 0)        -- BR-020
        CONSTRAINT DF_Can_Refund        DEFAULT 0,
    CancellationDate DATETIME2      NOT NULL CONSTRAINT DF_Can_Date DEFAULT GETDATE(),

    CONSTRAINT PK_Cancellations     PRIMARY KEY (CancellationId),
    CONSTRAINT UQ_Can_Booking       UNIQUE (BookingId),                  -- BR-011: one cancellation per booking
    CONSTRAINT FK_Can_Booking       FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId) ON DELETE NO ACTION
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 12 : Refunds
   Purpose  : Tracks refund processing for cancelled bookings.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Refunds (
    RefundId         INT           IDENTITY(1,1) NOT NULL,
    CancellationId   INT           NOT NULL,
    RefundStatus     NVARCHAR(20)  NOT NULL
        CONSTRAINT CHK_Ref_Status       CHECK (RefundStatus IN ('Initiated','Processed','Failed'))
        CONSTRAINT DF_Ref_Status        DEFAULT 'Initiated',
    RefundDate       DATETIME2     NULL,
    ProcessedBy      NVARCHAR(80)  NULL,
    Remarks          NVARCHAR(500) NULL,

    CONSTRAINT PK_Refunds           PRIMARY KEY (RefundId),
    CONSTRAINT FK_Ref_Cancellation  FOREIGN KEY (CancellationId) REFERENCES dbo.Cancellations (CancellationId) ON DELETE NO ACTION
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 13 : Feedback
   Purpose  : Customer ratings and reviews — one per booking.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Feedback (
    FeedbackId INT           IDENTITY(1,1) NOT NULL,
    UserId     INT           NOT NULL,
    HallId     INT           NOT NULL,
    BookingId  INT           NOT NULL,
    Rating     INT           NOT NULL
        CONSTRAINT CHK_FB_Rating        CHECK (Rating BETWEEN 1 AND 5),  -- BR-015
    Comment    NVARCHAR(MAX) NULL,
    IsVisible  BIT           NOT NULL CONSTRAINT DF_FB_IsVisible DEFAULT 1,
    CreatedAt  DATETIME2     NOT NULL CONSTRAINT DF_FB_CreatedAt DEFAULT GETDATE(),

    CONSTRAINT PK_Feedback          PRIMARY KEY (FeedbackId),
    CONSTRAINT UQ_FB_Booking        UNIQUE (BookingId),                   -- BR-014: one feedback per booking
    CONSTRAINT FK_FB_User           FOREIGN KEY (UserId)    REFERENCES dbo.Users     (UserId)    ON DELETE NO ACTION,
    CONSTRAINT FK_FB_Hall           FOREIGN KEY (HallId)    REFERENCES dbo.Halls     (HallId)    ON DELETE NO ACTION,
    CONSTRAINT FK_FB_Booking        FOREIGN KEY (BookingId) REFERENCES dbo.Bookings  (BookingId) ON DELETE NO ACTION
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 14 : Invoices
   Purpose  : Formal invoice per confirmed booking.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Invoices (
    InvoiceId     INT            IDENTITY(1,1) NOT NULL,
    BookingId     INT            NOT NULL,
    InvoiceNumber NVARCHAR(50)   NOT NULL,
    TotalAmount   DECIMAL(10,2)  NOT NULL,
    GeneratedDate DATETIME2      NOT NULL CONSTRAINT DF_Inv_GenDate DEFAULT GETDATE(),
    IssuedTo      NVARCHAR(150)  NULL,
    Notes         NVARCHAR(MAX)  NULL,

    CONSTRAINT PK_Invoices          PRIMARY KEY (InvoiceId),
    CONSTRAINT UQ_Inv_Booking       UNIQUE (BookingId),                   -- BR-012: one invoice per booking
    CONSTRAINT UQ_Inv_Number        UNIQUE (InvoiceNumber),               -- BR-013
    CONSTRAINT FK_Inv_Booking       FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId) ON DELETE NO ACTION
);
GO

SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='Cancellations';

-- create (example - adapt columns!)
CREATE TABLE dbo.Cancellations (
  CancellationId INT IDENTITY PRIMARY KEY,
  BookingId INT NOT NULL,
  RefundAmount DECIMAL(18,2),
  CancellationDate DATETIME,
  Reason NVARCHAR(500)
);

PRINT N'  → All 14 tables created.';
PRINT N'';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 3 ─ INDEXES
   ═══════════════════════════════════════════════════════════════════════════ */
PRINT N'[SECTION 3]  Creating indexes ...';

-- Users
CREATE NONCLUSTERED INDEX IX_Users_Role   ON dbo.Users (Role);
CREATE NONCLUSTERED INDEX IX_Users_Active ON dbo.Users (IsActive);

-- Halls
CREATE NONCLUSTERED INDEX IX_Halls_Category  ON dbo.Halls (CategoryId);
CREATE NONCLUSTERED INDEX IX_Halls_Available ON dbo.Halls (IsAvailable);
CREATE NONCLUSTERED INDEX IX_Halls_Location  ON dbo.Halls (LocationId);

-- HallAvailability
CREATE NONCLUSTERED INDEX IX_HallAvail_HallDate ON dbo.HallAvailability (HallId, AvailableDate);

-- Bookings (highest-frequency queries)
CREATE NONCLUSTERED INDEX IX_Bk_User       ON dbo.Bookings (UserId);
CREATE NONCLUSTERED INDEX IX_Bk_Hall       ON dbo.Bookings (HallId);
CREATE NONCLUSTERED INDEX IX_Bk_Status     ON dbo.Bookings (BookingStatus);
CREATE NONCLUSTERED INDEX IX_Bk_EventDate  ON dbo.Bookings (EventDate);
CREATE NONCLUSTERED INDEX IX_Bk_HallDate   ON dbo.Bookings (HallId, EventDate, StartTime, EndTime); -- double-booking guard

-- Payments
CREATE NONCLUSTERED INDEX IX_Pay_Status  ON dbo.Payments (PaymentStatus);

-- Feedback
CREATE NONCLUSTERED INDEX IX_FB_Hall ON dbo.Feedback (HallId);
GO

PRINT N'  → All indexes created.';
PRINT N'';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 4 ─ SEED DATA
   ═══════════════════════════════════════════════════════════════════════════ */
PRINT N'[SECTION 4]  Inserting seed data ...';

-- ─── Hall Categories ───────────────────────────────────────────────────────
INSERT INTO dbo.HallCategories (CategoryName, Description) VALUES
('Conference Hall', 'Professional meeting and conference spaces with AV equipment'),
('Wedding Hall',    'Elegant venues for weddings, receptions and grand celebrations'),
('Banquet Hall',    'Versatile banquet spaces for corporate and social events'),
('Seminar Hall',    'Academic and training spaces with tiered or flat-floor layouts'),
('Exhibition Hall', 'Large open-plan spaces for exhibitions and trade fairs'),
('Auditorium',      'Dedicated performance and presentation auditoriums');
GO

-- ─── Venue Locations ──────────────────────────────────────────────────────
INSERT INTO dbo.VenueLocations (LocationName, Floor, Wing) VALUES
('Main Building – Ground Floor',  'Ground', 'Main'),
('Main Building – 1st Floor',     '1st',    'Main'),
('Main Building – 3rd Floor',     '3rd',    'Main'),
('East Wing – Ground Floor',      'Ground', 'East'),
('West Wing – Ground Floor',      'Ground', 'West'),
('West Wing – 1st Floor',         '1st',    'West'),
('North Wing – Ground Floor',     'Ground', 'North'),
('South Wing – 1st Floor',        '1st',    'South'),
('Block A – 2nd Floor',           '2nd',    'Block A'),
('Block A – 3rd Floor',           '3rd',    'Block A'),
('Block A – 4th Floor',           '4th',    'Block A'),
('Block B – 1st Floor',           '1st',    'Block B'),
('Block B – 2nd Floor',           '2nd',    'Block B');
GO

-- ─── Halls ────────────────────────────────────────────────────────────────
--  CategoryId: 1=Conference, 2=Wedding, 3=Banquet, 4=Seminar, 5=Exhibition, 6=Auditorium
INSERT INTO dbo.Halls (CategoryId, LocationId, HallName, Capacity, PricePerHour, Description) VALUES
(2, 1,  'Grand Ballroom',               500, 15000.00, 'Luxurious wedding hall with crystal chandeliers and premium catering facilities'),
(1, 9,  'Executive Conference Room A',   30,  3500.00, 'Premium conference room with latest AV equipment and video conferencing setup'),
(4, 12, 'Innovation Hub',                60,  2500.00, 'Modern training space with flexible seating and interactive whiteboards'),
(6, 3,  'Sunrise Auditorium',           800, 25000.00, 'Grand auditorium with stadium seating, professional lighting and sound systems'),
(2, 4,  'Diamond Banquet Hall',         300, 12000.00, 'Elegant banquet hall for weddings, receptions and grand celebrations'),
(1, 10, 'Strategy Room B',               20,  2800.00, 'Compact conference room ideal for board meetings and strategy sessions'),
(5, 5,  'Art Gallery Exhibition',        200,  8000.00, 'Spacious open-plan space with movable partitions and gallery-quality lighting'),
(4, 13, 'Tech Training Lab',             40,  3000.00, 'Computer lab with 40 workstations, dual monitors and high-speed internet'),
(6, 8,  'Heritage Auditorium',          500, 18000.00, 'Classic auditorium with wooden interiors and excellent acoustics'),
(3, 7,  'Royal Banquet Suite',          150,  9000.00, 'Intimate banquet suite with private garden access and personal butler service'),
(5, 6,  'Innovation Exhibition Center', 350, 10000.00, 'Large exhibition space with loading dock access and industrial power outlets'),
(1, 11, 'Boardroom C',                   15,  2200.00, 'Intimate boardroom with premium leather seating and 75-inch display');
GO

-- ─── Amenities ─────────────────────────────────────────────────────────────
INSERT INTO dbo.Amenities (AmenityName, Description) VALUES
('Air Conditioning',   'Central AC with individual temperature control'),
('HD Projector',       '4K-capable projector with motorised screen'),
('Video Conferencing', 'Zoom/Teams-compatible setup with PTZ camera'),
('High-Speed WiFi',    '500 Mbps dedicated WiFi with backup connection'),
('PA Sound System',    'Professional PA system with wireless microphones'),
('Stage Lighting',     'Programmable LED stage lights with DMX control'),
('Interactive Whiteboard', 'Digital whiteboard with screen sharing'),
('Catering Kitchen',   'Attached kitchen with hot and cold holding equipment'),
('Parking – 50 cars',  'Dedicated covered parking for 50 vehicles'),
('Parking – 100 cars', 'Large open-air parking lot for 100+ vehicles'),
('Green Room',         'Backstage prep area with mirrors and seating'),
('Live Streaming',     'Professional live streaming setup with OBS'),
('Podium',             'Height-adjustable podium with gooseneck microphone'),
('Breakout Rooms',     '2–3 smaller rooms adjacent to the main hall'),
('Wheelchair Access',  'Ramps, wide doors and accessible restrooms'),
('Generator Backup',   'Full-load diesel generator with auto-switchover');
GO

-- ─── Hall ↔ Amenity mapping ────────────────────────────────────────────────
INSERT INTO dbo.HallAmenities (HallId, AmenityId) VALUES
-- Grand Ballroom (1)
(1,1),(1,4),(1,5),(1,8),(1,10),(1,15),(1,16),
-- Executive Conference Room A (2)
(2,1),(2,2),(2,3),(2,4),(2,7),
-- Innovation Hub (3)
(3,1),(3,2),(3,4),(3,7),(3,14),
-- Sunrise Auditorium (4)
(4,1),(4,2),(4,4),(4,5),(4,6),(4,11),(4,12),(4,13),(4,10),(4,15),(4,16),
-- Diamond Banquet Hall (5)
(5,1),(5,4),(5,5),(5,8),(5,9),(5,16),
-- Strategy Room B (6)
(6,1),(6,2),(6,3),(6,4),(6,7),
-- Art Gallery Exhibition (7)
(7,1),(7,4),(7,6),
-- Tech Training Lab (8)
(8,1),(8,2),(8,4),(8,7),(8,14),
-- Heritage Auditorium (9)
(9,1),(9,2),(9,4),(9,5),(9,6),(9,11),(9,13),(9,15),
-- Royal Banquet Suite (10)
(10,1),(10,4),(10,8),(10,9),
-- Innovation Exhibition Center (11)
(11,4),(11,10),(11,16),
-- Boardroom C (12)
(12,1),(12,2),(12,3),(12,4);
GO

-- ─── Users (2 Admins + 10 Customers)  Password: Password@123 (BCrypt) ─────
INSERT INTO dbo.Users (Username, Email, PasswordHash, FullName, PhoneNumber, Role) VALUES
('admin1',    'admin1@onevenue.com',    '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Rajesh Kumar',    '9876543210', 'Admin'),
('admin2',    'admin2@onevenue.com',    '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Priya Sharma',    '9876543211', 'Admin'),
('lakchay',   'lakchay@gmail.com',      '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Lakchay Sharma',  '9876543212', 'Customer'),
('aditya01',  'aditya@gmail.com',       '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Aditya Singh',    '9876543213', 'Customer'),
('neha_m',    'neha@outlook.com',       '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Neha Mehta',      '9876543214', 'Customer'),
('rahul_v',   'rahul@gmail.com',        '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Rahul Verma',     '9876543215', 'Customer'),
('ankita_j',  'ankita@yahoo.com',       '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Ankita Jain',     '9876543216', 'Customer'),
('vikram_r',  'vikram@gmail.com',       '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Vikram Reddy',    '9876543217', 'Customer'),
('sunita_d',  'sunita@outlook.com',     '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Sunita Devi',     '9876543218', 'Customer'),
('amit_p',    'amit@gmail.com',         '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Amit Patel',      '9876543219', 'Customer'),
('deepika_s', 'deepika@gmail.com',      '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Deepika Saxena',  '9876543220', 'Customer'),
('rohan_k',   'rohan@yahoo.com',        '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Rohan Kapoor',    '9876543221', 'Customer');
GO

-- ─── Bookings (sample — mixed statuses) ───────────────────────────────────
INSERT INTO dbo.Bookings
    (BookingReference, UserId, HallId, HallNameSnapshot,        CustomerNameSnapshot, EventDate,    StartTime, EndTime,  GuestCount, TotalAmount,  BookingStatus, Purpose)
VALUES
('BK-1000', 3,  1,  'Grand Ballroom',               'Lakchay Sharma',  '2026-07-10', '10:00', '14:00',  400,  60000.00, 'Confirmed', 'Annual Company Meet'),
('BK-1001', 4,  2,  'Executive Conference Room A',  'Aditya Singh',    '2026-07-11', '09:00', '12:00',   25,  10500.00, 'Confirmed', 'Product Launch Meeting'),
('BK-1002', 5,  4,  'Sunrise Auditorium',           'Neha Mehta',      '2026-07-12', '18:00', '22:00',  700, 100000.00, 'Confirmed', 'College Annual Day Ceremony'),
('BK-1003', 6,  5,  'Diamond Banquet Hall',         'Rahul Verma',     '2026-07-13', '11:00', '16:00',  250,  60000.00, 'Confirmed', 'Wedding Reception'),
('BK-1004', 7,  3,  'Innovation Hub',               'Ankita Jain',     '2026-07-14', '09:00', '13:00',   50,  10000.00, 'Confirmed', 'Corporate Training Workshop'),
('BK-1005', 8,  7,  'Art Gallery Exhibition',       'Vikram Reddy',    '2026-07-15', '10:00', '18:00',  180,  64000.00, 'Confirmed', 'Art Exhibition Opening'),
('BK-1006', 9,  8,  'Tech Training Lab',            'Sunita Devi',     '2026-07-16', '09:00', '17:00',   35,  24000.00, 'Confirmed', 'Python Programming Bootcamp'),
('BK-1007', 10, 10, 'Royal Banquet Suite',          'Amit Patel',      '2026-07-17', '19:00', '23:00',  120,  36000.00, 'Confirmed', 'Birthday Celebration'),
('BK-1008', 11, 1,  'Grand Ballroom',               'Deepika Saxena',  '2026-08-01', '10:00', '15:00',  450,  75000.00, 'Pending',   'Charity Gala Dinner'),
('BK-1009', 12, 6,  'Strategy Room B',              'Rohan Kapoor',    '2026-08-02', '14:00', '17:00',   18,   8400.00, 'Pending',   'Investor Pitch Session'),
('BK-1010', 3,  9,  'Heritage Auditorium',          'Lakchay Sharma',  '2026-06-01', '10:00', '14:00',  400,  72000.00, 'Cancelled', 'Music Concert Rehearsal');
GO

-- ─── Payments ──────────────────────────────────────────────────────────────
INSERT INTO dbo.Payments (BookingId, Amount, PaymentMethod, TransactionReference, PaymentStatus, PaymentDate) VALUES
(1,  60000.00, 'Cash',        'TXN20260709153001', 'Success', '2026-07-09 15:30:00'),
(2,  10500.00, 'UPI',         'TXN20260710091502', 'Success', '2026-07-10 09:15:00'),
(3, 100000.00, 'BankTransfer','TXN20260711120003', 'Success', '2026-07-11 12:00:00'),
(4,  60000.00, 'Card',        'TXN20260712100004', 'Success', '2026-07-12 10:00:00'),
(5,  10000.00, 'Cash',        'TXN20260713083005', 'Success', '2026-07-13 08:30:00'),
(6,  64000.00, 'UPI',         'TXN20260714093006', 'Success', '2026-07-14 09:30:00'),
(7,  24000.00, 'Card',        'TXN20260715080007', 'Success', '2026-07-15 08:00:00'),
(8,  36000.00, 'Cash',        'TXN20260716180008', 'Success', '2026-07-16 18:00:00'),
(9,  75000.00, 'Cash',        'PENDING-BK1008',    'Pending', NULL),
(10,  8400.00, 'Cash',        'PENDING-BK1009',    'Pending', NULL);
GO

-- ─── Cancellation + Refund (for BK-1010) ──────────────────────────────────
INSERT INTO dbo.Cancellations (BookingId, Reason, RefundAmount) VALUES
(11, 'Customer requested cancellation — venue conflict', 54000.00);   -- 75% refund

INSERT INTO dbo.Refunds (CancellationId, RefundStatus, RefundDate, ProcessedBy, Remarks) VALUES
(1, 'Processed', '2026-06-03 10:00:00', 'admin1', 'Refund processed via original payment channel');
GO

-- ─── Sample Feedback ───────────────────────────────────────────────────────
INSERT INTO dbo.Feedback (UserId, HallId, BookingId, Rating, Comment) VALUES
(3,  1,  1, 5, 'Absolutely stunning ballroom — exceeded all expectations!'),
(4,  2,  2, 4, 'Excellent AV setup. Video conferencing worked flawlessly.'),
(5,  4,  3, 5, 'Sunrise Auditorium is world-class. Lighting and sound were perfect.'),
(6,  5,  4, 4, 'Beautiful banquet hall. Staff were very professional.'),
(7,  3,  5, 5, 'Innovation Hub has great flexible seating. Loved the whiteboards.'),
(8,  7,  6, 4, 'Gallery lighting was fantastic for the exhibition.');
GO

-- ─── Sample Invoices ───────────────────────────────────────────────────────
INSERT INTO dbo.Invoices (BookingId, InvoiceNumber, TotalAmount, IssuedTo) VALUES
(1,  'INV-2026-001',  60000.00, 'Lakchay Sharma'),
(2,  'INV-2026-002',  10500.00, 'Aditya Singh'),
(3,  'INV-2026-003', 100000.00, 'Neha Mehta'),
(4,  'INV-2026-004',  60000.00, 'Rahul Verma'),
(5,  'INV-2026-005',  10000.00, 'Ankita Jain'),
(6,  'INV-2026-006',  64000.00, 'Vikram Reddy'),
(7,  'INV-2026-007',  24000.00, 'Sunita Devi'),
(8,  'INV-2026-008',  36000.00, 'Amit Patel');
GO

PRINT N'  → Seed data inserted.';
PRINT N'';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 5 ─ STORED PROCEDURES  (10 total)
   All use TRY/CATCH, explicit transactions, and ROLLBACK support.
   ═══════════════════════════════════════════════════════════════════════════ */
PRINT N'[SECTION 5]  Creating stored procedures ...';

/* ─────────────────────────────────────────────────────────────────────────
   SP 1 ─ usp_RegisterUser                          FR-1.1, FR-1.2
   Purpose: Register a new Customer account with uniqueness validation.
   ───────────────────────────────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_RegisterUser;
GO
CREATE PROCEDURE dbo.usp_RegisterUser
    @Username     NVARCHAR(80),
    @Email        NVARCHAR(120),
    @PasswordHash NVARCHAR(255),
    @FullName     NVARCHAR(150),
    @PhoneNumber  NVARCHAR(15)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- BR-001 duplicate username guard
        IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username)
            THROW 51001, 'Username is already taken. Please choose a different username.', 1;

        -- BR-002 duplicate email guard
        IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email)
            THROW 51002, 'An account with this email address already exists.', 1;

        -- BR-003 duplicate phone guard
        IF @PhoneNumber IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Users WHERE PhoneNumber = @PhoneNumber)
            THROW 51003, 'This phone number is already registered with another account.', 1;

        INSERT INTO dbo.Users (Username, Email, PasswordHash, FullName, PhoneNumber, Role)
        VALUES (@Username, @Email, @PasswordHash, @FullName, @PhoneNumber, 'Customer');

        -- Return the newly created user
        SELECT UserId, Username, Email, FullName, PhoneNumber, Role, IsActive, CreatedAt
        FROM   dbo.Users
        WHERE  UserId = SCOPE_IDENTITY();

    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

/* ─────────────────────────────────────────────────────────────────────────
   SP 2 ─ usp_LoginUser                             FR-1.3
   Purpose: Authenticate credentials and return session profile.
            Password comparison (BCrypt) must be done in application layer.
            SP returns the stored hash + profile for the app to verify.
   ───────────────────────────────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_LoginUser;
GO
CREATE PROCEDURE dbo.usp_LoginUser
    @Username NVARCHAR(80)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username)
            THROW 51010, 'No account found with this username.', 1;

        SELECT UserId, Username, Email, PasswordHash, FullName, PhoneNumber, Role, IsActive, CreatedAt
        FROM   dbo.Users
        WHERE  Username = @Username AND IsActive = 1;

        -- Return 0 rows (via IsActive = 0) signals account inactive to the app
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

/* ─────────────────────────────────────────────────────────────────────────
   SP 3 ─ usp_AddHall                               FR-2.1
   Purpose: Create a new hall with category and location assignment.
            Admin only — enforced at application layer.
   ───────────────────────────────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_AddHall;
GO
CREATE PROCEDURE dbo.usp_AddHall
    @CategoryId   INT,
    @LocationId   INT,
    @HallName     NVARCHAR(150),
    @Capacity     INT,
    @PricePerHour DECIMAL(10,2),
    @Description  NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Validate category exists and is active
        IF NOT EXISTS (SELECT 1 FROM dbo.HallCategories WHERE CategoryId = @CategoryId AND IsActive = 1)
            THROW 52001, 'Invalid or inactive hall category.', 1;

        -- Validate location exists
        IF NOT EXISTS (SELECT 1 FROM dbo.VenueLocations WHERE LocationId = @LocationId)
            THROW 52002, 'Invalid venue location.', 1;

        -- BR-006, BR-007 enforced by CHECK constraints; duplicate name by UNIQUE constraint
        INSERT INTO dbo.Halls (CategoryId, LocationId, HallName, Capacity, PricePerHour, Description)
        VALUES (@CategoryId, @LocationId, @HallName, @Capacity, @PricePerHour, @Description);

        SELECT HallId, CategoryId, LocationId, HallName, Capacity, PricePerHour, Description, IsAvailable, CreatedAt
        FROM   dbo.Halls
        WHERE  HallId = SCOPE_IDENTITY();
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

/* ─────────────────────────────────────────────────────────────────────────
   SP 4 ─ usp_UpdateHall                            FR-2.2
   Purpose: Update hall details. Keeps hall integrity constraints intact.
   ───────────────────────────────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_UpdateHall;
GO
CREATE PROCEDURE dbo.usp_UpdateHall
    @HallId       INT,
    @HallName     NVARCHAR(150)  = NULL,
    @Capacity     INT            = NULL,
    @PricePerHour DECIMAL(10,2)  = NULL,
    @Description  NVARCHAR(MAX)  = NULL,
    @IsAvailable  BIT            = NULL,
    @CategoryId   INT            = NULL,
    @LocationId   INT            = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.Halls WHERE HallId = @HallId)
            THROW 52010, 'Hall not found.', 1;

        UPDATE dbo.Halls
        SET
            HallName     = ISNULL(@HallName,     HallName),
            Capacity     = ISNULL(@Capacity,     Capacity),
            PricePerHour = ISNULL(@PricePerHour, PricePerHour),
            Description  = ISNULL(@Description,  Description),
            IsAvailable  = ISNULL(@IsAvailable,  IsAvailable),
            CategoryId   = ISNULL(@CategoryId,   CategoryId),
            LocationId   = ISNULL(@LocationId,   LocationId)
        WHERE HallId = @HallId;

        SELECT HallId, CategoryId, LocationId, HallName, Capacity, PricePerHour, Description, IsAvailable, CreatedAt
        FROM   dbo.Halls
        WHERE  HallId = @HallId;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

/* ─────────────────────────────────────────────────────────────────────────
   SP 5 ─ usp_CreateBooking                         FR-4.1, FR-4.2
   Purpose: Create a hall booking.
            Enforces: active hall, future date, no double-booking,
            guest count ≤ capacity.
   ───────────────────────────────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_CreateBooking;
GO
CREATE PROCEDURE dbo.usp_CreateBooking
    @UserId     INT,
    @HallId     INT,
    @EventDate  DATE,
    @StartTime  TIME,
    @EndTime    TIME,
    @GuestCount INT,
    @Purpose    NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
        BEGIN TRAN;

        -- BR-022: booking date must not be in the past
        IF @EventDate < CAST(GETDATE() AS DATE)
        BEGIN
            ROLLBACK TRAN;
            THROW 53001, 'Booking date cannot be in the past.', 1;
        END

        -- BR-016: start time must be before end time (also a CHECK constraint)
        IF @StartTime >= @EndTime
        BEGIN
            ROLLBACK TRAN;
            THROW 53002, 'Start time must be before end time.', 1;
        END

        -- BR-021: only active (IsAvailable=1) halls can be booked
        DECLARE @HallName    NVARCHAR(150);
        DECLARE @HallCap     INT;
        DECLARE @PricePerHr  DECIMAL(10,2);

        SELECT @HallName   = HallName,
               @HallCap    = Capacity,
               @PricePerHr = PricePerHour
        FROM   dbo.Halls
        WHERE  HallId = @HallId AND IsAvailable = 1;

        IF @HallName IS NULL
        BEGIN
            ROLLBACK TRAN;
            THROW 53003, 'Hall is not available for booking.', 1;
        END

        -- Guest count vs capacity check
        IF @GuestCount > @HallCap
        BEGIN
            ROLLBACK TRAN;
            THROW 53004, 'Guest count exceeds hall capacity.', 1;
        END

        -- BR-017: double-booking guard
        IF EXISTS (
            SELECT 1
            FROM   dbo.Bookings
            WHERE  HallId    = @HallId
              AND  EventDate = @EventDate
              AND  BookingStatus NOT IN ('Cancelled')
              AND  StartTime < @EndTime
              AND  EndTime   > @StartTime
        )
        BEGIN
            ROLLBACK TRAN;
            THROW 53005, 'The selected hall is already booked for this date and time slot.', 1;
        END

        -- Admin-level block via HallAvailability table
        IF EXISTS (
            SELECT 1
            FROM   dbo.HallAvailability
            WHERE  HallId        = @HallId
              AND  AvailableDate = @EventDate
              AND  IsAvailable   = 0
        )
        BEGIN
            ROLLBACK TRAN;
            THROW 53006, 'The hall has been blocked by administration for this date.', 1;
        END

        -- Calculate total hours and amount
        DECLARE @TotalHours  DECIMAL(10,2) = DATEDIFF(MINUTE, @StartTime, @EndTime) / 60.0;
        DECLARE @TotalAmount DECIMAL(10,2) = @TotalHours * @PricePerHr;

        -- Capture customer name snapshot
        DECLARE @CustomerName NVARCHAR(150);
        SELECT  @CustomerName = FullName FROM dbo.Users WHERE UserId = @UserId;

        -- Generate booking reference from sequence
        DECLARE @SeqVal     BIGINT = NEXT VALUE FOR dbo.BookingReferenceSeq;
        DECLARE @BookingRef NVARCHAR(20) = 'BK-' + CAST(@SeqVal AS NVARCHAR(10));

        INSERT INTO dbo.Bookings
            (BookingReference, UserId, HallId, HallNameSnapshot, CustomerNameSnapshot,
             EventDate, StartTime, EndTime, GuestCount, TotalAmount, Purpose)
        VALUES
            (@BookingRef, @UserId, @HallId, @HallName, @CustomerName,
             @EventDate, @StartTime, @EndTime, @GuestCount, @TotalAmount, @Purpose);

        DECLARE @NewBookingId INT = SCOPE_IDENTITY();
        COMMIT TRAN;

        SELECT BookingId, BookingReference, UserId, HallId, HallNameSnapshot,
               CustomerNameSnapshot, EventDate, StartTime, EndTime,
               GuestCount, TotalAmount, BookingStatus, Purpose, CreatedAt
        FROM   dbo.Bookings
        WHERE  BookingId = @NewBookingId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

/* ─────────────────────────────────────────────────────────────────────────
   SP 6 ─ usp_ConfirmPayment                        FR-5.1
   Purpose: Record payment and confirm booking status atomically.
   ───────────────────────────────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_ConfirmPayment;
GO
CREATE PROCEDURE dbo.usp_ConfirmPayment
    @BookingId           INT,
    @Amount              DECIMAL(10,2),
    @PaymentMethod       NVARCHAR(50),
    @TransactionReference NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        -- Booking must be in Pending state
        IF NOT EXISTS (SELECT 1 FROM dbo.Bookings WHERE BookingId = @BookingId AND BookingStatus = 'Pending')
        BEGIN
            ROLLBACK TRAN;
            THROW 54001, 'Only Pending bookings can be confirmed via payment.', 1;
        END

        -- BR-010: prevent duplicate payment
        IF EXISTS (SELECT 1 FROM dbo.Payments WHERE BookingId = @BookingId)
        BEGIN
            ROLLBACK TRAN;
            THROW 54002, 'A payment record already exists for this booking.', 1;
        END

        -- Validate amount matches booking total
        DECLARE @BookingAmount DECIMAL(10,2);
        SELECT  @BookingAmount = TotalAmount FROM dbo.Bookings WHERE BookingId = @BookingId;

        IF @Amount <> @BookingAmount
        BEGIN
            ROLLBACK TRAN;
            THROW 54003, 'Payment amount does not match the booking total.', 1;
        END

        -- Insert payment record
        INSERT INTO dbo.Payments
            (BookingId, Amount, PaymentMethod, TransactionReference, PaymentStatus, PaymentDate)
        VALUES
            (@BookingId, @Amount, @PaymentMethod, @TransactionReference, 'Success', GETDATE());

        DECLARE @NewPaymentId INT = SCOPE_IDENTITY();

        -- Confirm the booking
        UPDATE dbo.Bookings
        SET    BookingStatus = 'Confirmed',
               UpdatedAt     = GETDATE()
        WHERE  BookingId = @BookingId;

        COMMIT TRAN;

        SELECT p.PaymentId, p.BookingId, p.Amount, p.PaymentMethod,
               p.TransactionReference, p.PaymentStatus, p.PaymentDate,
               b.BookingStatus, b.BookingReference
        FROM   dbo.Payments p
        INNER JOIN dbo.Bookings b ON p.BookingId = b.BookingId
        WHERE  p.PaymentId = @NewPaymentId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

/* ─────────────────────────────────────────────────────────────────────────
   SP 7 ─ usp_CancelBooking                         FR-6.1
   Purpose: Cancel a Pending or Confirmed booking and compute refund.
            Refund policy: 100% if >48h before event, 50% if 24–48h, 0% otherwise.
            BR-019: already Cancelled bookings cannot be modified.
   ───────────────────────────────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_CancelBooking;
GO
CREATE PROCEDURE dbo.usp_CancelBooking
    @BookingId INT,
    @Reason    NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        -- BR-019 guard
        DECLARE @CurrentStatus NVARCHAR(20);
        DECLARE @EventDate     DATE;
        DECLARE @TotalAmount   DECIMAL(10,2);

        SELECT @CurrentStatus = BookingStatus,
               @EventDate     = EventDate,
               @TotalAmount   = TotalAmount
        FROM   dbo.Bookings
        WHERE  BookingId = @BookingId;

        IF @CurrentStatus IS NULL
        BEGIN
            ROLLBACK TRAN;
            THROW 55001, 'Booking not found.', 1;
        END

        IF @CurrentStatus = 'Cancelled'
        BEGIN
            ROLLBACK TRAN;
            THROW 55002, 'Booking is already cancelled and cannot be modified.', 1;
        END

        -- Refund policy
        DECLARE @HoursUntilEvent  INT   = DATEDIFF(HOUR, GETDATE(), CAST(@EventDate AS DATETIME2));
        DECLARE @RefundAmount     DECIMAL(10,2) =
            CASE
                WHEN @HoursUntilEvent >= 48 THEN @TotalAmount            -- 100%
                WHEN @HoursUntilEvent >= 24 THEN @TotalAmount * 0.50     -- 50%
                ELSE 0.00                                                 -- 0%
            END;

        -- Cancel the booking
        UPDATE dbo.Bookings
        SET    BookingStatus = 'Cancelled',
               UpdatedAt     = GETDATE()
        WHERE  BookingId = @BookingId;

        -- Create cancellation record
        INSERT INTO dbo.Cancellations (BookingId, Reason, RefundAmount)
        VALUES (@BookingId, @Reason, @RefundAmount);

        DECLARE @NewCancellationId INT = SCOPE_IDENTITY();

        COMMIT TRAN;

        SELECT c.CancellationId, c.BookingId, c.Reason,
               c.RefundAmount, c.CancellationDate,
               b.BookingReference, b.BookingStatus,
               @HoursUntilEvent AS HoursUntilEvent
        FROM   dbo.Cancellations c
        INNER JOIN dbo.Bookings  b ON c.BookingId = b.BookingId
        WHERE  c.CancellationId = @NewCancellationId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

/* ─────────────────────────────────────────────────────────────────────────
   SP 8 ─ usp_ProcessRefund                         FR-6.2
   Purpose: Mark a cancellation's refund as Processed.
   ───────────────────────────────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_ProcessRefund;
GO
CREATE PROCEDURE dbo.usp_ProcessRefund
    @CancellationId INT,
    @RefundAmount   DECIMAL(10,2),
    @ProcessedBy    NVARCHAR(80) = NULL,
    @Remarks        NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        -- Validate cancellation exists
        DECLARE @StoredRefundAmt DECIMAL(10,2);
        SELECT  @StoredRefundAmt = RefundAmount
        FROM    dbo.Cancellations
        WHERE   CancellationId = @CancellationId;

        IF @StoredRefundAmt IS NULL
        BEGIN
            ROLLBACK TRAN;
            THROW 56001, 'Cancellation record not found.', 1;
        END

        -- Prevent double-processing
        IF EXISTS (SELECT 1 FROM dbo.Refunds WHERE CancellationId = @CancellationId AND RefundStatus = 'Processed')
        BEGIN
            ROLLBACK TRAN;
            THROW 56002, 'Refund has already been processed for this cancellation.', 1;
        END

        -- BR-020 — refund amount must match cancellation calculation
        IF @RefundAmount > @StoredRefundAmt
        BEGIN
            ROLLBACK TRAN;
            THROW 56003, 'Refund amount exceeds the approved cancellation refund.', 1;
        END

        -- Upsert refund record
        IF EXISTS (SELECT 1 FROM dbo.Refunds WHERE CancellationId = @CancellationId)
            UPDATE dbo.Refunds
            SET    RefundStatus = 'Processed',
                   RefundDate   = GETDATE(),
                   ProcessedBy  = ISNULL(@ProcessedBy, ProcessedBy),
                   Remarks      = ISNULL(@Remarks, Remarks)
            WHERE  CancellationId = @CancellationId;
        ELSE
            INSERT INTO dbo.Refunds (CancellationId, RefundStatus, RefundDate, ProcessedBy, Remarks)
            VALUES (@CancellationId, 'Processed', GETDATE(), @ProcessedBy, @Remarks);

        -- Update approved refund amount on cancellation
        UPDATE dbo.Cancellations
        SET    RefundAmount = @RefundAmount
        WHERE  CancellationId = @CancellationId;

        COMMIT TRAN;

        SELECT r.RefundId, r.CancellationId, r.RefundStatus, r.RefundDate,
               r.ProcessedBy, r.Remarks, c.RefundAmount
        FROM   dbo.Refunds       r
        INNER JOIN dbo.Cancellations c ON r.CancellationId = c.CancellationId
        WHERE  r.CancellationId = @CancellationId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

/* ─────────────────────────────────────────────────────────────────────────
   SP 9 ─ usp_SubmitFeedback                        FR-7.1
   Purpose: Store customer rating and review for a completed booking.
            BR-014: one feedback per booking.
            BR-015: rating must be 1–5.
   ───────────────────────────────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_SubmitFeedback;
GO
CREATE PROCEDURE dbo.usp_SubmitFeedback
    @BookingId INT,
    @Rating    INT,
    @Comment   NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- BR-015
        IF @Rating NOT BETWEEN 1 AND 5
            THROW 57001, 'Rating must be between 1 and 5.', 1;

        -- Booking must be Confirmed (completed event)
        DECLARE @UserId   INT;
        DECLARE @HallId   INT;
        DECLARE @BkStatus NVARCHAR(20);

        SELECT @UserId   = UserId,
               @HallId   = HallId,
               @BkStatus = BookingStatus
        FROM   dbo.Bookings
        WHERE  BookingId = @BookingId;

        IF @UserId IS NULL
            THROW 57002, 'Booking not found.', 1;

        IF @BkStatus = 'Cancelled'
            THROW 57003, 'Feedback cannot be submitted for a cancelled booking.', 1;

        -- BR-014: one feedback per booking
        IF EXISTS (SELECT 1 FROM dbo.Feedback WHERE BookingId = @BookingId)
            THROW 57004, 'Feedback has already been submitted for this booking.', 1;

        INSERT INTO dbo.Feedback (UserId, HallId, BookingId, Rating, Comment)
        VALUES (@UserId, @HallId, @BookingId, @Rating, @Comment);

        SELECT FeedbackId, UserId, HallId, BookingId, Rating, Comment, IsVisible, CreatedAt
        FROM   dbo.Feedback
        WHERE  FeedbackId = SCOPE_IDENTITY();
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

/* ─────────────────────────────────────────────────────────────────────────
   SP 10 ─ usp_GenerateInvoice                      FR-8.1
   Purpose: Generate a formatted invoice for a Confirmed booking.
            BR-012: one invoice per booking.
            BR-013: invoice number must be globally unique.
   ───────────────────────────────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_GenerateInvoice;
GO
CREATE PROCEDURE dbo.usp_GenerateInvoice
    @BookingId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Booking must be Confirmed
        DECLARE @UserId       INT;
        DECLARE @HallName     NVARCHAR(150);
        DECLARE @CustName     NVARCHAR(150);
        DECLARE @TotalAmount  DECIMAL(10,2);
        DECLARE @BkStatus     NVARCHAR(20);
        DECLARE @BkRef        NVARCHAR(20);

        SELECT @UserId      = UserId,
               @HallName    = HallNameSnapshot,
               @CustName    = CustomerNameSnapshot,
               @TotalAmount = TotalAmount,
               @BkStatus    = BookingStatus,
               @BkRef       = BookingReference
        FROM   dbo.Bookings
        WHERE  BookingId = @BookingId;

        IF @UserId IS NULL
            THROW 58001, 'Booking not found.', 1;

        IF @BkStatus <> 'Confirmed'
            THROW 58002, 'Invoices can only be generated for Confirmed bookings.', 1;

        -- BR-012
        IF EXISTS (SELECT 1 FROM dbo.Invoices WHERE BookingId = @BookingId)
        BEGIN
            -- Return existing invoice instead of throwing
            SELECT i.InvoiceId, i.BookingId, i.InvoiceNumber, i.TotalAmount,
                   i.GeneratedDate, i.IssuedTo, i.Notes
            FROM   dbo.Invoices i
            WHERE  i.BookingId = @BookingId;
            RETURN;
        END

        -- Generate unique invoice number: INV-YYYY-<BookingRef>
        DECLARE @InvoiceNumber NVARCHAR(50) =
            'INV-' + CAST(YEAR(GETDATE()) AS NVARCHAR(4)) + '-' + @BkRef;

        INSERT INTO dbo.Invoices (BookingId, InvoiceNumber, TotalAmount, IssuedTo)
        VALUES (@BookingId, @InvoiceNumber, @TotalAmount, @CustName);

        SELECT i.InvoiceId, i.BookingId, i.InvoiceNumber, i.TotalAmount,
               i.GeneratedDate, i.IssuedTo, i.Notes,
               b.BookingReference, b.HallNameSnapshot, b.EventDate,
               b.StartTime, b.EndTime, b.GuestCount, b.Purpose
        FROM   dbo.Invoices  i
        INNER JOIN dbo.Bookings b ON i.BookingId = b.BookingId
        WHERE  i.BookingId = @BookingId;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

PRINT N'  → All 10 stored procedures created.';
PRINT N'';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 6 ─ TRIGGERS
   ═══════════════════════════════════════════════════════════════════════════ */
PRINT N'[SECTION 6]  Creating triggers ...';

/* Trigger 1: UpdatedAt auto-stamp on Bookings */
DROP TRIGGER IF EXISTS dbo.trg_Bookings_UpdatedAt;
GO
CREATE TRIGGER dbo.trg_Bookings_UpdatedAt
ON dbo.Bookings
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
        UPDATE b
        SET    b.UpdatedAt = GETDATE()
        FROM   dbo.Bookings b
        INNER JOIN inserted i ON b.BookingId = i.BookingId;
END
GO

/* Trigger 2: UpdatedAt auto-stamp on Users */
DROP TRIGGER IF EXISTS dbo.trg_Users_UpdatedAt;
GO
CREATE TRIGGER dbo.trg_Users_UpdatedAt
ON dbo.Users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
        UPDATE u
        SET    u.UpdatedAt = GETDATE()
        FROM   dbo.Users u
        INNER JOIN inserted i ON u.UserId = i.UserId;
END
GO

/* Trigger 3: Database-level double-booking guard (last line of defence) */
DROP TRIGGER IF EXISTS dbo.trg_PreventDoubleBooking;
GO
CREATE TRIGGER dbo.trg_PreventDoubleBooking
ON dbo.Bookings
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM   dbo.Bookings b
        INNER JOIN inserted i ON b.HallId = i.HallId AND b.EventDate = i.EventDate
        WHERE  b.BookingId     <> i.BookingId
          AND  b.BookingStatus <> 'Cancelled'
          AND  i.BookingStatus <> 'Cancelled'
          AND  b.StartTime      < i.EndTime
          AND  b.EndTime        > i.StartTime
    )
    BEGIN
        RAISERROR('Hall is already booked for this date and time slot.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END
GO

PRINT N'  → All triggers created.';
PRINT N'';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 7 ─ SECURITY & ACCESS CONTROL
   ═══════════════════════════════════════════════════════════════════════════ */
PRINT N'[SECTION 7]  Configuring security ...';

/* ─── Application Runtime Account ─────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ov_app_login')
BEGIN
    /* Replace <strong_password_here> with a real password before execution */
    EXECUTE ('CREATE LOGIN ov_app_login WITH PASSWORD = N''Ov@App#2026Secure!''');
    PRINT N'  → Login ov_app_login created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ov_app')
BEGIN
    CREATE USER ov_app FOR LOGIN ov_app_login;
    GRANT SELECT, INSERT, UPDATE, EXECUTE ON SCHEMA::dbo TO ov_app;
    PRINT N'  → User ov_app created with SELECT, INSERT, UPDATE, EXECUTE.';
END
GO

/* ─── Reporting Account ────────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ov_report_login')
BEGIN
    EXECUTE ('CREATE LOGIN ov_report_login WITH PASSWORD = N''Ov@Rpt#2026Secure!''');
    PRINT N'  → Login ov_report_login created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ov_report')
BEGIN
    CREATE USER ov_report FOR LOGIN ov_report_login;
    GRANT SELECT ON SCHEMA::dbo TO ov_report;
    PRINT N'  → User ov_report created with SELECT only.';
END
GO

/* ─── DBA Account ──────────────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ov_dba_login')
BEGIN
    EXECUTE ('CREATE LOGIN ov_dba_login WITH PASSWORD = N''Ov@DBA#2026Secure!''');
    PRINT N'  → Login ov_dba_login created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ov_dba')
BEGIN
    CREATE USER ov_dba FOR LOGIN ov_dba_login;
    ALTER ROLE db_owner ADD MEMBER ov_dba;
    PRINT N'  → User ov_dba created with db_owner role.';
END
GO

PRINT N'';
PRINT N'╔══════════════════════════════════════════════════════════════════════╗';
PRINT N'║   OneVenueDB setup COMPLETE                                          ║';
PRINT N'║   • 14 Tables    • 10 Stored Procedures    • 3 Triggers              ║';
PRINT N'║   • 13 Indexes   •  3 Security Accounts    • Full Seed Data          ║';
PRINT N'╚══════════════════════════════════════════════════════════════════════╝';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 8 ─ INDEXES & PERFORMANCE OPTIMIZATIONS
   ═══════════════════════════════════════════════════════════════════════════ */
-- Indexes for foreign key columns and frequently queried fields
CREATE INDEX IX_Halls_CategoryId   ON dbo.Halls(CategoryId);
CREATE INDEX IX_Halls_LocationId   ON dbo.Halls(LocationId);
CREATE INDEX IX_Bookings_UserId    ON dbo.Bookings(UserId);
CREATE INDEX IX_Bookings_HallId    ON dbo.Bookings(HallId);
CREATE INDEX IX_Payments_BookingId ON dbo.Payments(BookingId);
CREATE INDEX IX_Refunds_CancellationId ON dbo.Refunds(CancellationId);
CREATE INDEX IX_Feedback_BookingId ON dbo.Feedback(BookingId);
CREATE INDEX IX_Invoices_BookingId ON dbo.Invoices(BookingId);
GO
/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 9 ─ VIEWS & HELPER FUNCTIONS
   ═══════════════════════════════════════════════════════════════════════════ */
-- View: comprehensive booking summary for reporting dashboards
CREATE VIEW dbo.vw_BookingSummary AS
SELECT b.BookingId,
       u.Username,
       h.HallName,
       b.EventDate,
       b.StartTime,
       b.EndTime,
       b.GuestCount,
       b.TotalAmount,
       b.BookingStatus,
       b.CreatedAt,
       b.UpdatedAt
FROM   dbo.Bookings   b
JOIN   dbo.Users      u ON b.UserId = u.UserId
JOIN   dbo.Halls      h ON b.HallId = h.HallId;
GO
-- View: aggregated revenue per hall (confirmed bookings only)
CREATE VIEW dbo.vw_HallRevenue AS
SELECT h.HallId,
       h.HallName,
       ISNULL(SUM(p.Amount), 0) AS TotalRevenue
FROM   dbo.Halls h
LEFT JOIN dbo.Bookings b   ON h.HallId = b.HallId AND b.BookingStatus = 'Confirmed'
LEFT JOIN dbo.Payments p   ON b.BookingId = p.BookingId
GROUP BY h.HallId, h.HallName;
GO
-- Table‑valued function: list of available halls for a specific date
CREATE FUNCTION dbo.fn_AvailableHalls(@Date DATE)
RETURNS TABLE
AS
RETURN (
    SELECT h.HallId,
           h.HallName,
           h.Capacity,
           h.PricePerHour
    FROM   dbo.Halls h
    WHERE  h.IsAvailable = 1
      AND NOT EXISTS (
          SELECT 1
          FROM   dbo.Bookings b
          WHERE  b.HallId = h.HallId
            AND b.EventDate = @Date
            AND b.BookingStatus NOT IN ('Cancelled')
      )
);
GO
PRINT N'  → Indexes, views, and helper function created.';
