PRINT N'╔══════════════════════════════════════════════════════════════════════╗';
PRINT N'║        ONE VENUE MULTI HALL BOOKING SYSTEM  —  OneVenueDB           ║';
PRINT N'║        Database Design Specification  v1.0  |  SQL Server            ║';
PRINT N'╚══════════════════════════════════════════════════════════════════════╝';
PRINT N'';

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
DROP SEQUENCE IF EXISTS dbo.BookingReferenceSeq;
GO
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

DROP VIEW  IF EXISTS dbo.vw_BookingSummary;
DROP VIEW  IF EXISTS dbo.vw_HallRevenue;
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
DROP USER  IF EXISTS ov_app;
DROP USER  IF EXISTS ov_report;
DROP USER  IF EXISTS ov_dba;

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 1 : Users
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Users (
    UserId       INT           IDENTITY(1,1) NOT NULL,
    Username     NVARCHAR(80)  NOT NULL,
    Email        NVARCHAR(120) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName     NVARCHAR(150) NOT NULL,
    PhoneNumber  NVARCHAR(15)  NULL,
    Role         NVARCHAR(20)  NOT NULL
        CONSTRAINT CHK_Users_Role CHECK (Role IN ('Admin','Customer')),
    IsActive     BIT           NOT NULL CONSTRAINT DF_Users_IsActive  DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT GETDATE(),
    UpdatedAt    DATETIME2     NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT GETDATE(),

    CONSTRAINT PK_Users          PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email    UNIQUE (Email),
    CONSTRAINT UQ_Users_Phone    UNIQUE (PhoneNumber)
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 2 : HallCategories
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.HallCategories (
    CategoryId   INT           IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    Description  NVARCHAR(MAX) NULL,
    IsActive     BIT           NOT NULL CONSTRAINT DF_HallCat_IsActive DEFAULT 1,

    CONSTRAINT PK_HallCategories      PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_HallCategories_Name UNIQUE (CategoryName)
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 3 : VenueLocations
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.VenueLocations (
    LocationId   INT           IDENTITY(1,1) NOT NULL,
    LocationName NVARCHAR(150) NOT NULL,
    Floor        NVARCHAR(50)  NULL,
    Wing         NVARCHAR(50)  NULL,
    Description  NVARCHAR(MAX) NULL,

    CONSTRAINT PK_VenueLocations      PRIMARY KEY (LocationId),
    CONSTRAINT UQ_VenueLocations_Name UNIQUE (LocationName)
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 4 : Halls
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Halls (
    HallId       INT           IDENTITY(1,1) NOT NULL,
    CategoryId   INT           NOT NULL,
    LocationId   INT           NOT NULL,
    HallName     NVARCHAR(150) NOT NULL,
    Capacity     INT           NOT NULL  CONSTRAINT CHK_Halls_Capacity CHECK (Capacity > 0),
    PricePerHour DECIMAL(10,2) NOT NULL  CONSTRAINT CHK_Halls_Price    CHECK (PricePerHour > 0),
    Description  NVARCHAR(MAX) NULL,
    IsAvailable  BIT           NOT NULL  CONSTRAINT DF_Halls_IsAvailable DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL  CONSTRAINT DF_Halls_CreatedAt   DEFAULT GETDATE(),

    CONSTRAINT PK_Halls         PRIMARY KEY (HallId),
    CONSTRAINT UQ_Halls_Name    UNIQUE (HallName),
    CONSTRAINT FK_Hall_Category FOREIGN KEY (CategoryId) REFERENCES dbo.HallCategories (CategoryId) ON DELETE NO ACTION,
    CONSTRAINT FK_Hall_Location FOREIGN KEY (LocationId) REFERENCES dbo.VenueLocations  (LocationId) ON DELETE NO ACTION
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 5 : HallImages
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.HallImages (
    ImageId   INT           IDENTITY(1,1) NOT NULL,
    HallId    INT           NOT NULL,
    ImageUrl  NVARCHAR(500) NOT NULL,
    Caption   NVARCHAR(200) NULL,
    SortOrder INT           NOT NULL CONSTRAINT DF_HallImg_Sort DEFAULT 0,

    CONSTRAINT PK_HallImages     PRIMARY KEY (ImageId),
    CONSTRAINT FK_HallImage_Hall FOREIGN KEY (HallId) REFERENCES dbo.Halls (HallId) ON DELETE CASCADE
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 6 : Amenities
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Amenities (
    AmenityId   INT           IDENTITY(1,1) NOT NULL,
    AmenityName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsActive    BIT           NOT NULL CONSTRAINT DF_Amenities_IsActive DEFAULT 1,

    CONSTRAINT PK_Amenities      PRIMARY KEY (AmenityId),
    CONSTRAINT UQ_Amenities_Name UNIQUE (AmenityName)
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 7 : HallAmenities  (many-to-many)
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.HallAmenities (
    HallId    INT NOT NULL,
    AmenityId INT NOT NULL,

    CONSTRAINT PK_HallAmenities       PRIMARY KEY (HallId, AmenityId),
    CONSTRAINT FK_HallAmenity_Hall    FOREIGN KEY (HallId)    REFERENCES dbo.Halls     (HallId)    ON DELETE CASCADE,
    CONSTRAINT FK_HallAmenity_Amenity FOREIGN KEY (AmenityId) REFERENCES dbo.Amenities (AmenityId) ON DELETE CASCADE
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 8 : HallAvailability
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.HallAvailability (
    AvailabilityId INT  IDENTITY(1,1) NOT NULL,
    HallId         INT  NOT NULL,
    AvailableDate  DATE NOT NULL,
    IsAvailable    BIT  NOT NULL CONSTRAINT DF_HallAvail_IsAvail DEFAULT 1,
    Remarks        NVARCHAR(500) NULL,

    CONSTRAINT PK_HallAvailability   PRIMARY KEY (AvailabilityId),
    CONSTRAINT UQ_HallAvail_HallDate UNIQUE (HallId, AvailableDate),
    CONSTRAINT FK_HallAvail_Hall     FOREIGN KEY (HallId) REFERENCES dbo.Halls (HallId) ON DELETE CASCADE
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 9 : Bookings
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Bookings (
    BookingId            INT           IDENTITY(1,1) NOT NULL,
    BookingReference     NVARCHAR(20)  NOT NULL,
    UserId               INT           NOT NULL,
    HallId               INT           NOT NULL,
    HallNameSnapshot     NVARCHAR(150) NOT NULL,
    CustomerNameSnapshot NVARCHAR(150) NOT NULL,
    EventDate            DATE          NOT NULL,
    StartTime            TIME          NOT NULL,
    EndTime              TIME          NOT NULL,
    GuestCount           INT           NOT NULL  CONSTRAINT CHK_Bk_Guests CHECK (GuestCount > 0),
    TotalAmount          DECIMAL(10,2) NOT NULL  CONSTRAINT CHK_Bk_Amount CHECK (TotalAmount > 0),
    BookingStatus        NVARCHAR(20)  NOT NULL
        CONSTRAINT CHK_Bk_Status CHECK (BookingStatus IN ('Pending','Confirmed','Cancelled'))
        CONSTRAINT DF_Bk_Status  DEFAULT 'Pending',
    Purpose   NVARCHAR(300) NULL,
    CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_Bk_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2    NOT NULL CONSTRAINT DF_Bk_UpdatedAt DEFAULT GETDATE(),

    CONSTRAINT PK_Bookings     PRIMARY KEY (BookingId),
    CONSTRAINT UQ_Bookings_Ref UNIQUE (BookingReference),
    CONSTRAINT FK_Bk_User      FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId) ON DELETE NO ACTION,
    CONSTRAINT FK_Bk_Hall      FOREIGN KEY (HallId) REFERENCES dbo.Halls (HallId) ON DELETE NO ACTION,
    CONSTRAINT CHK_Bk_Times   CHECK (EndTime > StartTime)
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 10 : Payments
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Payments (
    PaymentId            INT           IDENTITY(1,1) NOT NULL,
    BookingId            INT           NOT NULL,
    Amount               DECIMAL(10,2) NOT NULL  CONSTRAINT CHK_Pay_Amount CHECK (Amount > 0),
    PaymentMethod        NVARCHAR(50)  NOT NULL
        CONSTRAINT CHK_Pay_Method CHECK (PaymentMethod IN ('Cash','Card','UPI','BankTransfer')),
    TransactionReference NVARCHAR(150) NOT NULL,
    PaymentStatus        NVARCHAR(20)  NOT NULL
        CONSTRAINT CHK_Pay_Status CHECK (PaymentStatus IN ('Pending','Success','Failed'))
        CONSTRAINT DF_Pay_Status  DEFAULT 'Pending',
    PaymentDate DATETIME2 NULL,
    CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_Pay_CreatedAt DEFAULT GETDATE(),

    CONSTRAINT PK_Payments           PRIMARY KEY (PaymentId),
    CONSTRAINT UQ_Pay_Booking        UNIQUE (BookingId),
    CONSTRAINT UQ_Pay_TransactionRef UNIQUE (TransactionReference),
    CONSTRAINT FK_Pay_Booking        FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId) ON DELETE NO ACTION
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 11 : Cancellations
   FIX: Added Status and AdminRemarks columns required by SP 7 and SP 8b.
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Cancellations (
    CancellationId   INT           IDENTITY(1,1) NOT NULL,
    BookingId        INT           NOT NULL,
    Reason           NVARCHAR(MAX) NULL,
    RefundAmount     DECIMAL(10,2) NOT NULL
        CONSTRAINT CHK_Can_Refund CHECK (RefundAmount >= 0)
        CONSTRAINT DF_Can_Refund  DEFAULT 0,
    /* ── FIX ── these two columns were missing and caused Msg 207 ── */
    Status           NVARCHAR(20)  NOT NULL
        CONSTRAINT CHK_Can_Status CHECK (Status IN ('Pending','Approved','Rejected'))
        CONSTRAINT DF_Can_Status  DEFAULT 'Pending',
    AdminRemarks     NVARCHAR(500) NULL,
    /* ─────────────────────────────────────────────────────────────── */
    CancellationDate DATETIME2     NOT NULL CONSTRAINT DF_Can_Date DEFAULT GETDATE(),

    CONSTRAINT PK_Cancellations  PRIMARY KEY (CancellationId),
    CONSTRAINT UQ_Can_Booking    UNIQUE (BookingId),
    CONSTRAINT FK_Can_Booking    FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId) ON DELETE NO ACTION
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 12 : Refunds
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Refunds (
    RefundId       INT           IDENTITY(1,1) NOT NULL,
    CancellationId INT           NOT NULL,
    RefundStatus   NVARCHAR(20)  NOT NULL
        CONSTRAINT CHK_Ref_Status CHECK (RefundStatus IN ('Initiated','Processed','Failed'))
        CONSTRAINT DF_Ref_Status  DEFAULT 'Initiated',
    RefundDate  DATETIME2    NULL,
    ProcessedBy NVARCHAR(80) NULL,
    Remarks     NVARCHAR(500) NULL,

    CONSTRAINT PK_Refunds          PRIMARY KEY (RefundId),
    CONSTRAINT FK_Ref_Cancellation FOREIGN KEY (CancellationId) REFERENCES dbo.Cancellations (CancellationId) ON DELETE NO ACTION
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 13 : Feedback
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Feedback (
    FeedbackId INT           IDENTITY(1,1) NOT NULL,
    UserId     INT           NOT NULL,
    HallId     INT           NOT NULL,
    BookingId  INT           NOT NULL,
    Rating     INT           NOT NULL  CONSTRAINT CHK_FB_Rating CHECK (Rating BETWEEN 1 AND 5),
    Comment    NVARCHAR(MAX) NULL,
    IsVisible  BIT           NOT NULL  CONSTRAINT DF_FB_IsVisible DEFAULT 1,
    CreatedAt  DATETIME2     NOT NULL  CONSTRAINT DF_FB_CreatedAt DEFAULT GETDATE(),

    CONSTRAINT PK_Feedback   PRIMARY KEY (FeedbackId),
    CONSTRAINT UQ_FB_Booking UNIQUE (BookingId),
    CONSTRAINT FK_FB_User    FOREIGN KEY (UserId)    REFERENCES dbo.Users    (UserId)    ON DELETE NO ACTION,
    CONSTRAINT FK_FB_Hall    FOREIGN KEY (HallId)    REFERENCES dbo.Halls    (HallId)    ON DELETE NO ACTION,
    CONSTRAINT FK_FB_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId) ON DELETE NO ACTION
);
GO

/* ─────────────────────────────────────────────────────────────────────────
   TABLE 14 : Invoices
   ───────────────────────────────────────────────────────────────────────── */
CREATE TABLE dbo.Invoices (
    InvoiceId     INT           IDENTITY(1,1) NOT NULL,
    BookingId     INT           NOT NULL,
    InvoiceNumber NVARCHAR(50)  NOT NULL,
    TotalAmount   DECIMAL(10,2) NOT NULL,
    GeneratedDate DATETIME2     NOT NULL CONSTRAINT DF_Inv_GenDate DEFAULT GETDATE(),
    IssuedTo      NVARCHAR(150) NULL,
    Notes         NVARCHAR(MAX) NULL,

    CONSTRAINT PK_Invoices    PRIMARY KEY (InvoiceId),
    CONSTRAINT UQ_Inv_Booking UNIQUE (BookingId),
    CONSTRAINT UQ_Inv_Number  UNIQUE (InvoiceNumber),
    CONSTRAINT FK_Inv_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId) ON DELETE NO ACTION
);
GO

PRINT N'  → All tables created.';
PRINT N'';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 3 ─ INDEXES
   ═══════════════════════════════════════════════════════════════════════════ */
PRINT N'[SECTION 3]  Creating indexes ...';

CREATE NONCLUSTERED INDEX IX_Users_Role        ON dbo.Users (Role);
CREATE NONCLUSTERED INDEX IX_Users_Active      ON dbo.Users (IsActive);

CREATE NONCLUSTERED INDEX IX_Halls_Category    ON dbo.Halls (CategoryId);
CREATE NONCLUSTERED INDEX IX_Halls_Available   ON dbo.Halls (IsAvailable);
CREATE NONCLUSTERED INDEX IX_Halls_Location    ON dbo.Halls (LocationId);

CREATE NONCLUSTERED INDEX IX_HallAvail_HallDate ON dbo.HallAvailability (HallId, AvailableDate);

CREATE NONCLUSTERED INDEX IX_Bk_User           ON dbo.Bookings (UserId);
CREATE NONCLUSTERED INDEX IX_Bk_Hall           ON dbo.Bookings (HallId);
CREATE NONCLUSTERED INDEX IX_Bk_Status         ON dbo.Bookings (BookingStatus);
CREATE NONCLUSTERED INDEX IX_Bk_EventDate      ON dbo.Bookings (EventDate);
CREATE NONCLUSTERED INDEX IX_Bk_HallDate       ON dbo.Bookings (HallId, EventDate, StartTime, EndTime);

CREATE NONCLUSTERED INDEX IX_Pay_Status        ON dbo.Payments (PaymentStatus);
CREATE NONCLUSTERED INDEX IX_FB_Hall           ON dbo.Feedback (HallId);
GO

PRINT N'  → All indexes created.';
PRINT N'';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 4 ─ SEED DATA
   ═══════════════════════════════════════════════════════════════════════════ */
PRINT N'[SECTION 4]  Inserting seed data ...';

INSERT INTO dbo.HallCategories (CategoryName, Description) VALUES
('Conference Hall', 'Professional meeting and conference spaces with AV equipment'),
('Wedding Hall',    'Elegant venues for weddings, receptions and grand celebrations'),
('Banquet Hall',    'Versatile banquet spaces for corporate and social events'),
('Seminar Hall',    'Academic and training spaces with tiered or flat-floor layouts'),
('Exhibition Hall', 'Large open-plan spaces for exhibitions and trade fairs'),
('Auditorium',      'Dedicated performance and presentation auditoriums');
GO

INSERT INTO dbo.VenueLocations (LocationName, Floor, Wing) VALUES
('Main Building – Ground Floor', 'Ground', 'Main'),
('Main Building – 1st Floor',    '1st',    'Main'),
('Main Building – 3rd Floor',    '3rd',    'Main'),
('East Wing – Ground Floor',     'Ground', 'East'),
('West Wing – Ground Floor',     'Ground', 'West'),
('West Wing – 1st Floor',        '1st',    'West'),
('North Wing – Ground Floor',    'Ground', 'North'),
('South Wing – 1st Floor',       '1st',    'South'),
('Block A – 2nd Floor',          '2nd',    'Block A'),
('Block A – 3rd Floor',          '3rd',    'Block A'),
('Block A – 4th Floor',          '4th',    'Block A'),
('Block B – 1st Floor',          '1st',    'Block B'),
('Block B – 2nd Floor',          '2nd',    'Block B');
GO

INSERT INTO dbo.Halls (CategoryId, LocationId, HallName, Capacity, PricePerHour, Description) VALUES
(2,  1, 'Grand Ballroom',               500, 15000.00, 'Luxurious wedding hall with crystal chandeliers and premium catering facilities'),
(1,  9, 'Executive Conference Room A',   30,  3500.00, 'Premium conference room with latest AV equipment and video conferencing setup'),
(4, 12, 'Innovation Hub',                60,  2500.00, 'Modern training space with flexible seating and interactive whiteboards'),
(6,  3, 'Sunrise Auditorium',           800, 25000.00, 'Grand auditorium with stadium seating, professional lighting and sound systems'),
(2,  4, 'Diamond Banquet Hall',         300, 12000.00, 'Elegant banquet hall for weddings, receptions and grand celebrations'),
(1, 10, 'Strategy Room B',               20,  2800.00, 'Compact conference room ideal for board meetings and strategy sessions'),
(5,  5, 'Art Gallery Exhibition',        200,  8000.00, 'Spacious open-plan space with movable partitions and gallery-quality lighting'),
(4, 13, 'Tech Training Lab',             40,  3000.00, 'Computer lab with 40 workstations, dual monitors and high-speed internet'),
(6,  8, 'Heritage Auditorium',          500, 18000.00, 'Classic auditorium with wooden interiors and excellent acoustics'),
(3,  7, 'Royal Banquet Suite',          150,  9000.00, 'Intimate banquet suite with private garden access and personal butler service'),
(5,  6, 'Innovation Exhibition Center', 350, 10000.00, 'Large exhibition space with loading dock access and industrial power outlets'),
(1, 11, 'Boardroom C',                   15,  2200.00, 'Intimate boardroom with premium leather seating and 75-inch display');
GO

INSERT INTO dbo.Amenities (AmenityName, Description) VALUES
('Air Conditioning',      'Central AC with individual temperature control'),
('HD Projector',          '4K-capable projector with motorised screen'),
('Video Conferencing',    'Zoom/Teams-compatible setup with PTZ camera'),
('High-Speed WiFi',       '500 Mbps dedicated WiFi with backup connection'),
('PA Sound System',       'Professional PA system with wireless microphones'),
('Stage Lighting',        'Programmable LED stage lights with DMX control'),
('Interactive Whiteboard','Digital whiteboard with screen sharing'),
('Catering Kitchen',      'Attached kitchen with hot and cold holding equipment'),
('Parking – 50 cars',     'Dedicated covered parking for 50 vehicles'),
('Parking – 100 cars',    'Large open-air parking lot for 100+ vehicles'),
('Green Room',            'Backstage prep area with mirrors and seating'),
('Live Streaming',        'Professional live streaming setup with OBS'),
('Podium',                'Height-adjustable podium with gooseneck microphone'),
('Breakout Rooms',        '2–3 smaller rooms adjacent to the main hall'),
('Wheelchair Access',     'Ramps, wide doors and accessible restrooms'),
('Generator Backup',      'Full-load diesel generator with auto-switchover');
GO

INSERT INTO dbo.HallAmenities (HallId, AmenityId) VALUES
(1,1),(1,4),(1,5),(1,8),(1,10),(1,15),(1,16),
(2,1),(2,2),(2,3),(2,4),(2,7),
(3,1),(3,2),(3,4),(3,7),(3,14),
(4,1),(4,2),(4,4),(4,5),(4,6),(4,10),(4,11),(4,12),(4,13),(4,15),(4,16),
(5,1),(5,4),(5,5),(5,8),(5,9),(5,16),
(6,1),(6,2),(6,3),(6,4),(6,7),
(7,1),(7,4),(7,6),
(8,1),(8,2),(8,4),(8,7),(8,14),
(9,1),(9,2),(9,4),(9,5),(9,6),(9,11),(9,13),(9,15),
(10,1),(10,4),(10,8),(10,9),
(11,4),(11,10),(11,16),
(12,1),(12,2),(12,3),(12,4);
GO

INSERT INTO dbo.Users (Username, Email, PasswordHash, FullName, PhoneNumber, Role) VALUES
('admin1',    'admin1@onevenue.com',  '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Rajesh Kumar',   '9876543210', 'Admin'),
('admin2',    'admin2@onevenue.com',  '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Priya Sharma',   '9876543211', 'Admin'),
('lakchay',   'lakchay@gmail.com',    '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Lakchay Sharma', '9876543212', 'Customer'),
('aditya01',  'aditya@gmail.com',     '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Aditya Singh',   '9876543213', 'Customer'),
('neha_m',    'neha@outlook.com',     '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Neha Mehta',     '9876543214', 'Customer'),
('rahul_v',   'rahul@gmail.com',      '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Rahul Verma',    '9876543215', 'Customer'),
('ankita_j',  'ankita@yahoo.com',     '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Ankita Jain',    '9876543216', 'Customer'),
('vikram_r',  'vikram@gmail.com',     '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Vikram Reddy',   '9876543217', 'Customer'),
('sunita_d',  'sunita@outlook.com',   '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Sunita Devi',    '9876543218', 'Customer'),
('amit_p',    'amit@gmail.com',       '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Amit Patel',     '9876543219', 'Customer'),
('deepika_s', 'deepika@gmail.com',    '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Deepika Saxena', '9876543220', 'Customer'),
('rohan_k',   'rohan@yahoo.com',      '$2a$12$ON1YiP.Sq3lhvaN56aF6WeUDZWikmis2DZSbGTR4Tvpri3J8GN4qa', 'Rohan Kapoor',   '9876543221', 'Customer');
GO

INSERT INTO dbo.Bookings
    (BookingReference, UserId, HallId, HallNameSnapshot, CustomerNameSnapshot,
     EventDate, StartTime, EndTime, GuestCount, TotalAmount, BookingStatus, Purpose)
VALUES
('BK-1000', 3,  1,  'Grand Ballroom',              'Lakchay Sharma', '2026-07-10','10:00','14:00', 400,  60000.00,'Confirmed','Annual Company Meet'),
('BK-1001', 4,  2,  'Executive Conference Room A', 'Aditya Singh',   '2026-07-11','09:00','12:00',  25,  10500.00,'Confirmed','Product Launch Meeting'),
('BK-1002', 5,  4,  'Sunrise Auditorium',          'Neha Mehta',     '2026-07-12','18:00','22:00', 700, 100000.00,'Confirmed','College Annual Day Ceremony'),
('BK-1003', 6,  5,  'Diamond Banquet Hall',        'Rahul Verma',    '2026-07-13','11:00','16:00', 250,  60000.00,'Confirmed','Wedding Reception'),
('BK-1004', 7,  3,  'Innovation Hub',              'Ankita Jain',    '2026-07-14','09:00','13:00',  50,  10000.00,'Confirmed','Corporate Training Workshop'),
('BK-1005', 8,  7,  'Art Gallery Exhibition',      'Vikram Reddy',   '2026-07-15','10:00','18:00', 180,  64000.00,'Confirmed','Art Exhibition Opening'),
('BK-1006', 9,  8,  'Tech Training Lab',           'Sunita Devi',    '2026-07-16','09:00','17:00',  35,  24000.00,'Confirmed','Python Programming Bootcamp'),
('BK-1007',10, 10,  'Royal Banquet Suite',         'Amit Patel',     '2026-07-17','19:00','23:00', 120,  36000.00,'Confirmed','Birthday Celebration'),
('BK-1008',11,  1,  'Grand Ballroom',              'Deepika Saxena', '2026-08-01','10:00','15:00', 450,  75000.00,'Pending',  'Charity Gala Dinner'),
('BK-1009',12,  6,  'Strategy Room B',             'Rohan Kapoor',   '2026-08-02','14:00','17:00',  18,   8400.00,'Pending',  'Investor Pitch Session'),
('BK-1010', 3,  9,  'Heritage Auditorium',         'Lakchay Sharma', '2026-06-01','10:00','14:00', 400,  72000.00,'Cancelled','Music Concert Rehearsal');
GO

INSERT INTO dbo.Payments
    (BookingId, Amount, PaymentMethod, TransactionReference, PaymentStatus, PaymentDate) VALUES
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

-- BK-1010 is BookingId = 11
INSERT INTO dbo.Cancellations (BookingId, Reason, RefundAmount, Status) VALUES
(11, 'Customer requested cancellation — venue conflict', 54000.00, 'Approved');

INSERT INTO dbo.Refunds (CancellationId, RefundStatus, RefundDate, ProcessedBy, Remarks) VALUES
(1, 'Processed', '2026-06-03 10:00:00', 'admin1', 'Refund processed via original payment channel');
GO

INSERT INTO dbo.Feedback (UserId, HallId, BookingId, Rating, Comment) VALUES
(3, 1, 1, 5, 'Absolutely stunning ballroom — exceeded all expectations!'),
(4, 2, 2, 4, 'Excellent AV setup. Video conferencing worked flawlessly.'),
(5, 4, 3, 5, 'Sunrise Auditorium is world-class. Lighting and sound were perfect.'),
(6, 5, 4, 4, 'Beautiful banquet hall. Staff were very professional.'),
(7, 3, 5, 5, 'Innovation Hub has great flexible seating. Loved the whiteboards.'),
(8, 7, 6, 4, 'Gallery lighting was fantastic for the exhibition.');
GO

INSERT INTO dbo.Invoices (BookingId, InvoiceNumber, TotalAmount, IssuedTo) VALUES
(1, 'INV-2026-001',  60000.00, 'Lakchay Sharma'),
(2, 'INV-2026-002',  10500.00, 'Aditya Singh'),
(3, 'INV-2026-003', 100000.00, 'Neha Mehta'),
(4, 'INV-2026-004',  60000.00, 'Rahul Verma'),
(5, 'INV-2026-005',  10000.00, 'Ankita Jain'),
(6, 'INV-2026-006',  64000.00, 'Vikram Reddy'),
(7, 'INV-2026-007',  24000.00, 'Sunita Devi'),
(8, 'INV-2026-008',  36000.00, 'Amit Patel');
GO

PRINT N'  → Seed data inserted.';
PRINT N'';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 5 ─ STORED PROCEDURES
   ═══════════════════════════════════════════════════════════════════════════ */
PRINT N'[SECTION 5]  Creating stored procedures ...';

/* ── SP 1 ─ usp_RegisterUser ─────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_RegisterUser;
GO
CREATE PROCEDURE dbo.usp_RegisterUser
    @Username     NVARCHAR(80),
    @Email        NVARCHAR(120),
    @PasswordHash NVARCHAR(255),
    @FullName     NVARCHAR(150),
    @PhoneNumber  NVARCHAR(15) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username)
            THROW 51001, 'Username is already taken. Please choose a different username.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email)
            THROW 51002, 'An account with this email address already exists.', 1;
        IF @PhoneNumber IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Users WHERE PhoneNumber = @PhoneNumber)
            THROW 51003, 'This phone number is already registered with another account.', 1;

        INSERT INTO dbo.Users (Username, Email, PasswordHash, FullName, PhoneNumber, Role)
        VALUES (@Username, @Email, @PasswordHash, @FullName, @PhoneNumber, 'Customer');

        SELECT UserId, Username, Email, FullName, PhoneNumber, Role, IsActive, CreatedAt
        FROM   dbo.Users WHERE UserId = SCOPE_IDENTITY();
    END TRY
    BEGIN CATCH THROW; END CATCH
END
GO

/* ── SP 2 ─ usp_LoginUser ────────────────────────────────────────────────── */
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
    END TRY
    BEGIN CATCH THROW; END CATCH
END
GO

/* ── SP 3 ─ usp_AddHall ──────────────────────────────────────────────────── */
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
        IF NOT EXISTS (SELECT 1 FROM dbo.HallCategories WHERE CategoryId = @CategoryId AND IsActive = 1)
            THROW 52001, 'Invalid or inactive hall category.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.VenueLocations WHERE LocationId = @LocationId)
            THROW 52002, 'Invalid venue location.', 1;

        INSERT INTO dbo.Halls (CategoryId, LocationId, HallName, Capacity, PricePerHour, Description)
        VALUES (@CategoryId, @LocationId, @HallName, @Capacity, @PricePerHour, @Description);

        SELECT HallId, CategoryId, LocationId, HallName, Capacity, PricePerHour, Description, IsAvailable, CreatedAt
        FROM   dbo.Halls WHERE HallId = SCOPE_IDENTITY();
    END TRY
    BEGIN CATCH THROW; END CATCH
END
GO

/* ── SP 4 ─ usp_UpdateHall ───────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_UpdateHall;
GO
CREATE PROCEDURE dbo.usp_UpdateHall
    @HallId       INT,
    @HallName     NVARCHAR(150) = NULL,
    @Capacity     INT           = NULL,
    @PricePerHour DECIMAL(10,2) = NULL,
    @Description  NVARCHAR(MAX) = NULL,
    @IsAvailable  BIT           = NULL,
    @CategoryId   INT           = NULL,
    @LocationId   INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.Halls WHERE HallId = @HallId)
            THROW 52010, 'Hall not found.', 1;

        UPDATE dbo.Halls SET
            HallName     = ISNULL(@HallName,     HallName),
            Capacity     = ISNULL(@Capacity,     Capacity),
            PricePerHour = ISNULL(@PricePerHour, PricePerHour),
            Description  = ISNULL(@Description,  Description),
            IsAvailable  = ISNULL(@IsAvailable,  IsAvailable),
            CategoryId   = ISNULL(@CategoryId,   CategoryId),
            LocationId   = ISNULL(@LocationId,   LocationId)
        WHERE HallId = @HallId;

        SELECT HallId, CategoryId, LocationId, HallName, Capacity, PricePerHour, Description, IsAvailable, CreatedAt
        FROM   dbo.Halls WHERE HallId = @HallId;
    END TRY
    BEGIN CATCH THROW; END CATCH
END
GO

/* ── SP 5 ─ usp_CreateBooking ────────────────────────────────────────────── */
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

        IF @EventDate < CAST(GETDATE() AS DATE)
        BEGIN ROLLBACK TRAN; THROW 53001, 'Booking date cannot be in the past.', 1; END

        IF @StartTime >= @EndTime
        BEGIN ROLLBACK TRAN; THROW 53002, 'Start time must be before end time.', 1; END

        DECLARE @HallName   NVARCHAR(150),
                @HallCap    INT,
                @PricePerHr DECIMAL(10,2);

        SELECT @HallName   = HallName,
               @HallCap    = Capacity,
               @PricePerHr = PricePerHour
        FROM   dbo.Halls
        WHERE  HallId = @HallId AND IsAvailable = 1;

        IF @HallName IS NULL
        BEGIN ROLLBACK TRAN; THROW 53003, 'Hall is not available for booking.', 1; END

        IF @GuestCount > @HallCap
        BEGIN ROLLBACK TRAN; THROW 53004, 'Guest count exceeds hall capacity.', 1; END

        IF EXISTS (
            SELECT 1 FROM dbo.Bookings
            WHERE  HallId = @HallId AND EventDate = @EventDate
              AND  BookingStatus <> 'Cancelled'
              AND  StartTime < @EndTime AND EndTime > @StartTime
        )
        BEGIN ROLLBACK TRAN; THROW 53005, 'The selected hall is already booked for this date and time slot.', 1; END

        IF EXISTS (
            SELECT 1 FROM dbo.HallAvailability
            WHERE  HallId = @HallId AND AvailableDate = @EventDate AND IsAvailable = 0
        )
        BEGIN ROLLBACK TRAN; THROW 53006, 'The hall has been blocked by administration for this date.', 1; END

        DECLARE @TotalHours  DECIMAL(10,2) = DATEDIFF(MINUTE, @StartTime, @EndTime) / 60.0;
        DECLARE @TotalAmount DECIMAL(10,2) = @TotalHours * @PricePerHr;
        DECLARE @CustomerName NVARCHAR(150);
        SELECT  @CustomerName = FullName FROM dbo.Users WHERE UserId = @UserId;

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
        FROM   dbo.Bookings WHERE BookingId = @NewBookingId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END-- sql
SELECT DB_NAME() AS CurrentDatabase;
SELECT OBJECT_ID('dbo.Cancellations') AS ObjId;
SELECT s.name AS SchemaName, o.name AS TableName
FROM sys.objects o
JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE o.type = 'U' AND o.name = 'Cancellations';-- sql
CREATE TABLE dbo.Cancellations
(
    CancellationId    INT IDENTITY(1,1) PRIMARY KEY,
    BookingId         INT        NOT NULL,
    RefundAmount      DECIMAL(18,2) NOT NULL DEFAULT (0),
    CancellationDate  DATETIME   NOT NULL DEFAULT GETDATE(),
    Reason            NVARCHAR(MAX) NULL,
    Status            NVARCHAR(50)  NOT NULL DEFAULT ('Pending'),
    AdminRemarks      NVARCHAR(MAX) NULL
    -- Optionally add FK:
    -- CONSTRAINT FK_Cancellations_Bookings FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(BookingId)
);
GO

/* ── SP 6 ─ usp_ConfirmPayment ───────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_ConfirmPayment;
GO
CREATE PROCEDURE dbo.usp_ConfirmPayment
    @BookingId            INT,
    @Amount               DECIMAL(10,2),
    @PaymentMethod        NVARCHAR(50),
    @TransactionReference NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (SELECT 1 FROM dbo.Bookings WHERE BookingId = @BookingId AND BookingStatus = 'Pending')
        BEGIN ROLLBACK TRAN; THROW 54001, 'Only Pending bookings can be confirmed via payment.', 1; END

        IF EXISTS (SELECT 1 FROM dbo.Payments WHERE BookingId = @BookingId)
        BEGIN ROLLBACK TRAN; THROW 54002, 'A payment record already exists for this booking.', 1; END

        DECLARE @BookingAmount DECIMAL(10,2);
        SELECT  @BookingAmount = TotalAmount FROM dbo.Bookings WHERE BookingId = @BookingId;

        IF @Amount <> @BookingAmount
        BEGIN ROLLBACK TRAN; THROW 54003, 'Payment amount does not match the booking total.', 1; END

        INSERT INTO dbo.Payments
            (BookingId, Amount, PaymentMethod, TransactionReference, PaymentStatus, PaymentDate)
        VALUES
            (@BookingId, @Amount, @PaymentMethod, @TransactionReference, 'Success', GETDATE());

        DECLARE @NewPaymentId INT = SCOPE_IDENTITY();

        UPDATE dbo.Bookings
        SET    BookingStatus = 'Confirmed', UpdatedAt = GETDATE()
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

/* ── SP 7 ─ usp_CancelBooking  (FIXED: Status column now exists) ─────────── */
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

        DECLARE @CurrentStatus NVARCHAR(20),
                @EventDate     DATE,
                @TotalAmount   DECIMAL(10,2);

        SELECT @CurrentStatus = BookingStatus,
               @EventDate     = EventDate,
               @TotalAmount   = TotalAmount
        FROM   dbo.Bookings WHERE BookingId = @BookingId;

        IF @CurrentStatus IS NULL
        BEGIN ROLLBACK TRAN; THROW 55001, 'Booking not found.', 1; END

        IF @CurrentStatus = 'Cancelled'
        BEGIN ROLLBACK TRAN; THROW 55002, 'Booking is already cancelled and cannot be modified.', 1; END

        DECLARE @HoursUntilEvent INT = DATEDIFF(HOUR, GETDATE(), CAST(@EventDate AS DATETIME2));
        DECLARE @RefundAmount DECIMAL(10,2) =
            CASE
                WHEN @HoursUntilEvent >= 48 THEN @TotalAmount
                WHEN @HoursUntilEvent >= 24 THEN @TotalAmount * 0.50
                ELSE 0.00
            END;

        UPDATE dbo.Bookings
        SET    BookingStatus = 'Cancelled', UpdatedAt = GETDATE()
        WHERE  BookingId = @BookingId;

        -- Status column now exists in dbo.Cancellations — no more Msg 207
        INSERT INTO dbo.Cancellations (BookingId, Reason, RefundAmount, Status)
        VALUES (@BookingId, @Reason, @RefundAmount, 'Pending');

        DECLARE @NewCancellationId INT = SCOPE_IDENTITY();
        COMMIT TRAN;

        SELECT c.CancellationId, c.BookingId, c.Reason, c.RefundAmount,
               c.Status, c.CancellationDate,
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

/* ── SP 8 ─ usp_ProcessRefund ────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_ProcessRefund;
GO
CREATE PROCEDURE dbo.usp_ProcessRefund
    @CancellationId INT,
    @RefundAmount   DECIMAL(10,2),
    @ProcessedBy    NVARCHAR(80)  = NULL,
    @Remarks        NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        DECLARE @StoredRefundAmt DECIMAL(10,2);
        SELECT  @StoredRefundAmt = RefundAmount
        FROM    dbo.Cancellations WHERE CancellationId = @CancellationId;

        IF @StoredRefundAmt IS NULL
        BEGIN ROLLBACK TRAN; THROW 56001, 'Cancellation record not found.', 1; END

        IF EXISTS (SELECT 1 FROM dbo.Refunds WHERE CancellationId = @CancellationId AND RefundStatus = 'Processed')
        BEGIN ROLLBACK TRAN; THROW 56002, 'Refund has already been processed for this cancellation.', 1; END

        IF @RefundAmount > @StoredRefundAmt
        BEGIN ROLLBACK TRAN; THROW 56003, 'Refund amount exceeds the approved cancellation refund.', 1; END

        IF EXISTS (SELECT 1 FROM dbo.Refunds WHERE CancellationId = @CancellationId)
            UPDATE dbo.Refunds SET
                RefundStatus = 'Processed',
                RefundDate   = GETDATE(),
                ProcessedBy  = ISNULL(@ProcessedBy, ProcessedBy),
                Remarks      = ISNULL(@Remarks, Remarks)
            WHERE CancellationId = @CancellationId;
        ELSE
            INSERT INTO dbo.Refunds (CancellationId, RefundStatus, RefundDate, ProcessedBy, Remarks)
            VALUES (@CancellationId, 'Processed', GETDATE(), @ProcessedBy, @Remarks);

        UPDATE dbo.Cancellations SET RefundAmount = @RefundAmount
        WHERE  CancellationId = @CancellationId;

        COMMIT TRAN;

        SELECT r.RefundId, r.CancellationId, r.RefundStatus, r.RefundDate,
               r.ProcessedBy, r.Remarks, c.RefundAmount
        FROM   dbo.Refunds r
        INNER JOIN dbo.Cancellations c ON r.CancellationId = c.CancellationId
        WHERE  r.CancellationId = @CancellationId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

/* ── SP 8b ─ sp_UpdateCancellationStatus  (FIXED: Status + AdminRemarks) ─── */
DROP PROCEDURE IF EXISTS dbo.sp_UpdateCancellationStatus;
GO
CREATE PROCEDURE dbo.sp_UpdateCancellationStatus
    @CancellationId INT,
    @Status         NVARCHAR(20),      -- 'Approved' or 'Rejected'
    @AdminRemarks   NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (SELECT 1 FROM dbo.Cancellations WHERE CancellationId = @CancellationId)
        BEGIN ROLLBACK TRAN; THROW 58001, 'Cancellation record not found.', 1; END

        IF @Status NOT IN ('Approved','Rejected')
        BEGIN ROLLBACK TRAN; THROW 58002, 'Status must be Approved or Rejected.', 1; END

        DECLARE @CurrentStatus NVARCHAR(20);
        SELECT  @CurrentStatus = Status FROM dbo.Cancellations WHERE CancellationId = @CancellationId;

        IF @CurrentStatus <> 'Pending'
        BEGIN ROLLBACK TRAN; THROW 58003, 'This cancellation request has already been decided.', 1; END

        -- Status and AdminRemarks columns now exist — no more Msg 207
        UPDATE dbo.Cancellations
        SET    Status       = @Status,
               AdminRemarks = @AdminRemarks
        WHERE  CancellationId = @CancellationId;

        IF @Status = 'Approved'
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.Refunds WHERE CancellationId = @CancellationId)
                INSERT INTO dbo.Refunds (CancellationId, RefundStatus, Remarks)
                VALUES (@CancellationId, 'Initiated', @AdminRemarks);
        END

        COMMIT TRAN;

        SELECT c.CancellationId, c.BookingId, c.Status, c.AdminRemarks,
               c.RefundAmount, c.CancellationDate
        FROM   dbo.Cancellations c WHERE CancellationId = @CancellationId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

/* ── SP 9 ─ usp_SubmitFeedback ───────────────────────────────────────────── */
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
        IF @Rating NOT BETWEEN 1 AND 5
            THROW 57001, 'Rating must be between 1 and 5.', 1;

        DECLARE @UserId INT, @HallId INT, @BkStatus NVARCHAR(20);
        SELECT  @UserId   = UserId,
                @HallId   = HallId,
                @BkStatus = BookingStatus
        FROM    dbo.Bookings WHERE BookingId = @BookingId;

        IF @UserId IS NULL
            THROW 57002, 'Booking not found.', 1;
        IF @BkStatus = 'Cancelled'
            THROW 57003, 'Feedback cannot be submitted for a cancelled booking.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Feedback WHERE BookingId = @BookingId)
            THROW 57004, 'Feedback has already been submitted for this booking.', 1;

        INSERT INTO dbo.Feedback (UserId, HallId, BookingId, Rating, Comment)
        VALUES (@UserId, @HallId, @BookingId, @Rating, @Comment);

        SELECT FeedbackId, UserId, HallId, BookingId, Rating, Comment, IsVisible, CreatedAt
        FROM   dbo.Feedback WHERE FeedbackId = SCOPE_IDENTITY();
    END TRY
    BEGIN CATCH THROW; END CATCH
END
GO

/* ── SP 10 ─ usp_GenerateInvoice ─────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.usp_GenerateInvoice;
GO
CREATE PROCEDURE dbo.usp_GenerateInvoice
    @BookingId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @UserId      INT,
                @HallName    NVARCHAR(150),
                @CustName    NVARCHAR(150),
                @TotalAmount DECIMAL(10,2),
                @BkStatus    NVARCHAR(20),
                @BkRef       NVARCHAR(20);

        SELECT @UserId      = UserId,
               @HallName    = HallNameSnapshot,
               @CustName    = CustomerNameSnapshot,
               @TotalAmount = TotalAmount,
               @BkStatus    = BookingStatus,
               @BkRef       = BookingReference
        FROM   dbo.Bookings WHERE BookingId = @BookingId;

        IF @UserId IS NULL
            THROW 58001, 'Booking not found.', 1;
        IF @BkStatus <> 'Confirmed'
            THROW 58002, 'Invoices can only be generated for Confirmed bookings.', 1;

        IF EXISTS (SELECT 1 FROM dbo.Invoices WHERE BookingId = @BookingId)
        BEGIN
            SELECT i.InvoiceId, i.BookingId, i.InvoiceNumber, i.TotalAmount,
                   i.GeneratedDate, i.IssuedTo, i.Notes
            FROM   dbo.Invoices i WHERE i.BookingId = @BookingId;
            RETURN;
        END

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
    BEGIN CATCH THROW; END CATCH
END
GO

PRINT N'  → All 10 stored procedures created.';
PRINT N'';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 6 ─ TRIGGERS
   ═══════════════════════════════════════════════════════════════════════════ */
PRINT N'[SECTION 6]  Creating triggers ...';

DROP TRIGGER IF EXISTS dbo.trg_Bookings_UpdatedAt;
GO
CREATE TRIGGER dbo.trg_Bookings_UpdatedAt
ON dbo.Bookings AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
        UPDATE b SET b.UpdatedAt = GETDATE()
        FROM   dbo.Bookings b INNER JOIN inserted i ON b.BookingId = i.BookingId;
END
GO

DROP TRIGGER IF EXISTS dbo.trg_Users_UpdatedAt;
GO
CREATE TRIGGER dbo.trg_Users_UpdatedAt
ON dbo.Users AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
        UPDATE u SET u.UpdatedAt = GETDATE()
        FROM   dbo.Users u INNER JOIN inserted i ON u.UserId = i.UserId;
END
GO

DROP TRIGGER IF EXISTS dbo.trg_PreventDoubleBooking;
GO
CREATE TRIGGER dbo.trg_PreventDoubleBooking
ON dbo.Bookings AFTER INSERT
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
   SECTION 7 ─ SECURITY
   ═══════════════════════════════════════════════════════════════════════════ */
PRINT N'[SECTION 7]  Configuring security ...';

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ov_app_login')
    EXECUTE ('CREATE LOGIN ov_app_login WITH PASSWORD = N''Ov@App#2026Secure!''');
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ov_app')
BEGIN
    CREATE USER ov_app FOR LOGIN ov_app_login;
    GRANT SELECT, INSERT, UPDATE, EXECUTE ON SCHEMA::dbo TO ov_app;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ov_report_login')
    EXECUTE ('CREATE LOGIN ov_report_login WITH PASSWORD = N''Ov@Rpt#2026Secure!''');
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ov_report')
BEGIN
    CREATE USER ov_report FOR LOGIN ov_report_login;
    GRANT SELECT ON SCHEMA::dbo TO ov_report;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ov_dba_login')
    EXECUTE ('CREATE LOGIN ov_dba_login WITH PASSWORD = N''Ov@DBA#2026Secure!''');
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ov_dba')
BEGIN
    CREATE USER ov_dba FOR LOGIN ov_dba_login;
    ALTER ROLE db_owner ADD MEMBER ov_dba;
END
GO

PRINT N'';
PRINT N'╔══════════════════════════════════════════════════════════════════════╗';
PRINT N'║   OneVenueDB setup COMPLETE                                          ║';
PRINT N'║   • 14 Tables    • 10 Stored Procedures    • 3 Triggers              ║';
PRINT N'║   • 13 Indexes   •  3 Security Accounts    • Full Seed Data          ║';
PRINT N'╚══════════════════════════════════════════════════════════════════════╝';

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 8 ─ ADDITIONAL INDEXES
   ═══════════════════════════════════════════════════════════════════════════ */
CREATE INDEX IX_Halls_CategoryId       ON dbo.Halls(CategoryId);
CREATE INDEX IX_Halls_LocationId       ON dbo.Halls(LocationId);
CREATE INDEX IX_Bookings_UserId        ON dbo.Bookings(UserId);
CREATE INDEX IX_Bookings_HallId        ON dbo.Bookings(HallId);
CREATE INDEX IX_Payments_BookingId     ON dbo.Payments(BookingId);
CREATE INDEX IX_Refunds_CancellationId ON dbo.Refunds(CancellationId);
CREATE INDEX IX_Feedback_BookingId     ON dbo.Feedback(BookingId);
CREATE INDEX IX_Invoices_BookingId     ON dbo.Invoices(BookingId);
GO

/* ═══════════════════════════════════════════════════════════════════════════
   SECTION 9 ─ VIEWS & HELPER FUNCTION
   ═══════════════════════════════════════════════════════════════════════════ */
CREATE VIEW dbo.vw_BookingSummary AS
SELECT b.BookingId, u.Username, h.HallName,
       b.EventDate, b.StartTime, b.EndTime,
       b.GuestCount, b.TotalAmount, b.BookingStatus,
       b.CreatedAt,  b.UpdatedAt
FROM   dbo.Bookings b
JOIN   dbo.Users    u ON b.UserId = u.UserId
JOIN   dbo.Halls    h ON b.HallId = h.HallId;
GO

CREATE VIEW dbo.vw_HallRevenue AS
SELECT h.HallId, h.HallName,
       ISNULL(SUM(p.Amount), 0) AS TotalRevenue
FROM   dbo.Halls h
LEFT JOIN dbo.Bookings b ON h.HallId    = b.HallId AND b.BookingStatus = 'Confirmed'
LEFT JOIN dbo.Payments p ON b.BookingId = p.BookingId
GROUP BY h.HallId, h.HallName;
GO

CREATE FUNCTION dbo.fn_AvailableHalls(@Date DATE)
RETURNS TABLE AS
RETURN (
    SELECT h.HallId, h.HallName, h.Capacity, h.PricePerHour
    FROM   dbo.Halls h
    WHERE  h.IsAvailable = 1
      AND  NOT EXISTS (
               SELECT 1 FROM dbo.Bookings b
               WHERE  b.HallId = h.HallId
                 AND  b.EventDate = @Date
                 AND  b.BookingStatus NOT IN ('Cancelled')
           )
);
GO

PRINT N'  → Indexes, views, and helper function created.';
GO

/* ─────────────────────────────────────────────────────────────────────────
   SP 11 ─ sp_UpdateBookingStatus
   Purpose: Update status of a booking. Resolves Cancelled status to usp_CancelBooking.
   ───────────────────────────────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.sp_UpdateBookingStatus;
GO
CREATE PROCEDURE dbo.sp_UpdateBookingStatus
    @BookingId INT,
    @Status    INT,
    @Remarks   NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @StatusStr NVARCHAR(20) = 
        CASE @Status
            WHEN 0 THEN 'Pending'
            WHEN 1 THEN 'Confirmed'
            WHEN 2 THEN 'Cancelled'
            ELSE 'Pending'
        END;

    IF @StatusStr = 'Cancelled'
    BEGIN
        EXEC dbo.usp_CancelBooking @BookingId = @BookingId, @Reason = @Remarks;
    END
    ELSE
    BEGIN
        UPDATE dbo.Bookings
        SET    BookingStatus = @StatusStr,
               UpdatedAt     = GETDATE()
        WHERE  BookingId = @BookingId;
    END
END
GO

/* ─────────────────────────────────────────────────────────────────────────
   SP 12 ─ Halls Procedures (Self-healing mapping)
   ───────────────────────────────────────────────────────────────────────── */
DROP PROCEDURE IF EXISTS dbo.sp_GetAllHalls;
GO
CREATE PROCEDURE dbo.sp_GetAllHalls
    @Page INT = 1,
    @PageSize INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        h.HallId,
        h.HallName,
        cat.CategoryName AS HallType,
        h.Capacity,
        h.PricePerHour,
        h.Description,
        loc.LocationName AS Location,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HallAmenities ha INNER JOIN dbo.Amenities a ON ha.AmenityId = a.AmenityId WHERE ha.HallId = h.HallId AND a.AmenityName LIKE '%AC%') THEN 1 ELSE 0 END AS BIT) AS HasAC,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HallAmenities ha INNER JOIN dbo.Amenities a ON ha.AmenityId = a.AmenityId WHERE ha.HallId = h.HallId AND a.AmenityName LIKE '%Projector%') THEN 1 ELSE 0 END AS BIT) AS HasProjector,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HallAmenities ha INNER JOIN dbo.Amenities a ON ha.AmenityId = a.AmenityId WHERE ha.HallId = h.HallId AND a.AmenityName LIKE '%Wifi%') THEN 1 ELSE 0 END AS BIT) AS HasWifi,
        h.IsAvailable AS IsActive,
        h.CreatedAt
    FROM dbo.Halls h
    INNER JOIN dbo.HallCategories cat ON h.CategoryId = cat.CategoryId
    INNER JOIN dbo.VenueLocations loc ON h.LocationId = loc.LocationId;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_GetHallById;
GO
CREATE PROCEDURE dbo.sp_GetHallById
    @HallId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        h.HallId,
        h.HallName,
        cat.CategoryName AS HallType,
        h.Capacity,
        h.PricePerHour,
        h.Description,
        loc.LocationName AS Location,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HallAmenities ha INNER JOIN dbo.Amenities a ON ha.AmenityId = a.AmenityId WHERE ha.HallId = h.HallId AND a.AmenityName LIKE '%AC%') THEN 1 ELSE 0 END AS BIT) AS HasAC,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HallAmenities ha INNER JOIN dbo.Amenities a ON ha.AmenityId = a.AmenityId WHERE ha.HallId = h.HallId AND a.AmenityName LIKE '%Projector%') THEN 1 ELSE 0 END AS BIT) AS HasProjector,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HallAmenities ha INNER JOIN dbo.Amenities a ON ha.AmenityId = a.AmenityId WHERE ha.HallId = h.HallId AND a.AmenityName LIKE '%Wifi%') THEN 1 ELSE 0 END AS BIT) AS HasWifi,
        h.IsAvailable AS IsActive,
        h.CreatedAt
    FROM dbo.Halls h
    INNER JOIN dbo.HallCategories cat ON h.CategoryId = cat.CategoryId
    INNER JOIN dbo.VenueLocations loc ON h.LocationId = loc.LocationId
    WHERE h.HallId = @HallId;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_GetAvailableHalls;
GO
CREATE PROCEDURE dbo.sp_GetAvailableHalls
    @StartDateTime DATETIME,
    @EndDateTime DATETIME,
    @Page INT = 1,
    @PageSize INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        h.HallId,
        h.HallName,
        cat.CategoryName AS HallType,
        h.Capacity,
        h.PricePerHour,
        h.Description,
        loc.LocationName AS Location,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HallAmenities ha INNER JOIN dbo.Amenities a ON ha.AmenityId = a.AmenityId WHERE ha.HallId = h.HallId AND a.AmenityName LIKE '%AC%') THEN 1 ELSE 0 END AS BIT) AS HasAC,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HallAmenities ha INNER JOIN dbo.Amenities a ON ha.AmenityId = a.AmenityId WHERE ha.HallId = h.HallId AND a.AmenityName LIKE '%Projector%') THEN 1 ELSE 0 END AS BIT) AS HasProjector,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HallAmenities ha INNER JOIN dbo.Amenities a ON ha.AmenityId = a.AmenityId WHERE ha.HallId = h.HallId AND a.AmenityName LIKE '%Wifi%') THEN 1 ELSE 0 END AS BIT) AS HasWifi,
        h.IsAvailable AS IsActive,
        h.CreatedAt
    FROM dbo.Halls h
    INNER JOIN dbo.HallCategories cat ON h.CategoryId = cat.CategoryId
    INNER JOIN dbo.VenueLocations loc ON h.LocationId = loc.LocationId
    WHERE h.IsAvailable = 1
      AND h.HallId NOT IN (
          SELECT HallId FROM dbo.Bookings 
          WHERE BookingStatus IN ('Pending', 'Confirmed')
            AND EventDate = CAST(@StartDateTime AS DATE)
            AND StartTime < CAST(@EndDateTime AS TIME)
            AND EndTime > CAST(@StartDateTime AS TIME)
      );
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_SearchHalls;
GO
CREATE PROCEDURE dbo.sp_SearchHalls
    @Keyword NVARCHAR(100),
    @Page INT = 1,
    @PageSize INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        h.HallId,
        h.HallName,
        cat.CategoryName AS HallType,
        h.Capacity,
        h.PricePerHour,
        h.Description,
        loc.LocationName AS Location,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HallAmenities ha INNER JOIN dbo.Amenities a ON ha.AmenityId = a.AmenityId WHERE ha.HallId = h.HallId AND a.AmenityName LIKE '%AC%') THEN 1 ELSE 0 END AS BIT) AS HasAC,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HallAmenities ha INNER JOIN dbo.Amenities a ON ha.AmenityId = a.AmenityId WHERE ha.HallId = h.HallId AND a.AmenityName LIKE '%Projector%') THEN 1 ELSE 0 END AS BIT) AS HasProjector,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HallAmenities ha INNER JOIN dbo.Amenities a ON ha.AmenityId = a.AmenityId WHERE ha.HallId = h.HallId AND a.AmenityName LIKE '%Wifi%') THEN 1 ELSE 0 END AS BIT) AS HasWifi,
        h.IsAvailable AS IsActive,
        h.CreatedAt
    FROM dbo.Halls h
    INNER JOIN dbo.HallCategories cat ON h.CategoryId = cat.CategoryId
    INNER JOIN dbo.VenueLocations loc ON h.LocationId = loc.LocationId
    WHERE h.HallName LIKE '%' + @Keyword + '%'
       OR cat.CategoryName LIKE '%' + @Keyword + '%'
       OR loc.LocationName LIKE '%' + @Keyword + '%';
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_GetHallCount;
GO
CREATE PROCEDURE dbo.sp_GetHallCount
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(1) FROM dbo.Halls;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_CreateHall;
GO
CREATE PROCEDURE dbo.sp_CreateHall
    @HallName NVARCHAR(150),
    @HallType NVARCHAR(100),
    @Capacity INT,
    @PricePerHour DECIMAL(10,2),
    @Description NVARCHAR(MAX) = NULL,
    @Location NVARCHAR(150),
    @HasAC BIT = 0,
    @HasProjector BIT = 0,
    @HasWifi BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CategoryId INT;
    SELECT @CategoryId = CategoryId FROM dbo.HallCategories WHERE CategoryName = @HallType;
    IF @CategoryId IS NULL
    BEGIN
        INSERT INTO dbo.HallCategories (CategoryName, Description, IsActive) VALUES (@HallType, @HallType, 1);
        SET @CategoryId = SCOPE_IDENTITY();
    END

    DECLARE @LocationId INT;
    SELECT @LocationId = LocationId FROM dbo.VenueLocations WHERE LocationName = @Location;
    IF @LocationId IS NULL
    BEGIN
        INSERT INTO dbo.VenueLocations (LocationName, Description) VALUES (@Location, @Location);
        SET @LocationId = SCOPE_IDENTITY();
    END

    INSERT INTO dbo.Halls (CategoryId, LocationId, HallName, Capacity, PricePerHour, Description, IsAvailable)
    VALUES (@CategoryId, @LocationId, @HallName, @Capacity, @PricePerHour, @Description, 1);
    
    DECLARE @NewHallId INT = SCOPE_IDENTITY();

    IF NOT EXISTS (SELECT 1 FROM dbo.Amenities WHERE AmenityName = 'AC')
        INSERT INTO dbo.Amenities (AmenityName, Description) VALUES ('AC', 'Air Conditioning');
    IF NOT EXISTS (SELECT 1 FROM dbo.Amenities WHERE AmenityName = 'Projector')
        INSERT INTO dbo.Amenities (AmenityName, Description) VALUES ('Projector', 'Digital Projector');
    IF NOT EXISTS (SELECT 1 FROM dbo.Amenities WHERE AmenityName = 'Wifi')
        INSERT INTO dbo.Amenities (AmenityName, Description) VALUES ('Wifi', 'Wireless Internet');

    IF @HasAC = 1
    BEGIN
        DECLARE @AcId INT = (SELECT AmenityId FROM dbo.Amenities WHERE AmenityName = 'AC');
        IF NOT EXISTS (SELECT 1 FROM dbo.HallAmenities WHERE HallId = @NewHallId AND AmenityId = @AcId)
            INSERT INTO dbo.HallAmenities (HallId, AmenityId) VALUES (@NewHallId, @AcId);
    END

    IF @HasProjector = 1
    BEGIN
        DECLARE @ProjId INT = (SELECT AmenityId FROM dbo.Amenities WHERE AmenityName = 'Projector');
        IF NOT EXISTS (SELECT 1 FROM dbo.HallAmenities WHERE HallId = @NewHallId AND AmenityId = @ProjId)
            INSERT INTO dbo.HallAmenities (HallId, AmenityId) VALUES (@NewHallId, @ProjId);
    END

    IF @HasWifi = 1
    BEGIN
        DECLARE @WifiId INT = (SELECT AmenityId FROM dbo.Amenities WHERE AmenityName = 'Wifi');
        IF NOT EXISTS (SELECT 1 FROM dbo.HallAmenities WHERE HallId = @NewHallId AND AmenityId = @WifiId)
            INSERT INTO dbo.HallAmenities (HallId, AmenityId) VALUES (@NewHallId, @WifiId);
    END
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_AddHall;
GO
CREATE PROCEDURE dbo.sp_AddHall
    @Name NVARCHAR(150),
    @HallType NVARCHAR(100),
    @Capacity INT,
    @PricePerHour DECIMAL(10,2),
    @Description NVARCHAR(MAX) = NULL,
    @Location NVARCHAR(150),
    @HasAC BIT = 0,
    @HasProjector BIT = 0,
    @HasWifi BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_CreateHall @Name, @HallType, @Capacity, @PricePerHour, @Description, @Location, @HasAC, @HasProjector, @HasWifi;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_UpdateHall;
GO
CREATE PROCEDURE dbo.sp_UpdateHall
    @HallId INT,
    @Name NVARCHAR(150),
    @HallType NVARCHAR(100),
    @Capacity INT,
    @PricePerHour DECIMAL(10,2),
    @Description NVARCHAR(MAX) = NULL,
    @Location NVARCHAR(150),
    @HasAC BIT = 0,
    @HasProjector BIT = 0,
    @HasWifi BIT = 0,
    @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CategoryId INT;
    SELECT @CategoryId = CategoryId FROM dbo.HallCategories WHERE CategoryName = @HallType;
    IF @CategoryId IS NULL
    BEGIN
        INSERT INTO dbo.HallCategories (CategoryName, Description, IsActive) VALUES (@HallType, @HallType, 1);
        SET @CategoryId = SCOPE_IDENTITY();
    END

    DECLARE @LocationId INT;
    SELECT @LocationId = LocationId FROM dbo.VenueLocations WHERE LocationName = @Location;
    IF @LocationId IS NULL
    BEGIN
        INSERT INTO dbo.VenueLocations (LocationName, Description) VALUES (@Location, @Location);
        SET @LocationId = SCOPE_IDENTITY();
    END

    UPDATE dbo.Halls SET
        HallName = @Name,
        CategoryId = @CategoryId,
        LocationId = @LocationId,
        Capacity = @Capacity,
        PricePerHour = @PricePerHour,
        Description = @Description,
        IsAvailable = @IsActive
    WHERE HallId = @HallId;

    DELETE FROM dbo.HallAmenities WHERE HallId = @HallId;

    IF @HasAC = 1
    BEGIN
        DECLARE @AcId INT = (SELECT AmenityId FROM dbo.Amenities WHERE AmenityName = 'AC');
        IF @AcId IS NULL
        BEGIN
            INSERT INTO dbo.Amenities (AmenityName, Description) VALUES ('AC', 'Air Conditioning');
            SET @AcId = SCOPE_IDENTITY();
        END
        INSERT INTO dbo.HallAmenities (HallId, AmenityId) VALUES (@HallId, @AcId);
    END

    IF @HasProjector = 1
    BEGIN
        DECLARE @ProjId INT = (SELECT AmenityId FROM dbo.Amenities WHERE AmenityName = 'Projector');
        IF @ProjId IS NULL
        BEGIN
            INSERT INTO dbo.Amenities (AmenityName, Description) VALUES ('Projector', 'Digital Projector');
            SET @ProjId = SCOPE_IDENTITY();
        END
        INSERT INTO dbo.HallAmenities (HallId, AmenityId) VALUES (@HallId, @ProjId);
    END

    IF @HasWifi = 1
    BEGIN
        DECLARE @WifiId INT = (SELECT AmenityId FROM dbo.Amenities WHERE AmenityName = 'Wifi');
        IF @WifiId IS NULL
        BEGIN
            INSERT INTO dbo.Amenities (AmenityName, Description) VALUES ('Wifi', 'Wireless Internet');
            SET @WifiId = SCOPE_IDENTITY();
        END
        INSERT INTO dbo.HallAmenities (HallId, AmenityId) VALUES (@HallId, @WifiId);
    END
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_DeleteHall;
GO
CREATE PROCEDURE dbo.sp_DeleteHall
    @HallId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.HallAmenities WHERE HallId = @HallId;
    DELETE FROM dbo.Halls WHERE HallId = @HallId;
END
GO

/* ─────────────────────────────────────────────────────────────────────────
   Booking Stored Procedures (Self-healing mapping)
   ───────────────────────────────────────────────────────────────────────── */

DROP PROCEDURE IF EXISTS dbo.sp_CreateBooking;
GO
CREATE PROCEDURE dbo.sp_CreateBooking
    @CustomerId    INT,
    @HallId        INT,
    @StartDateTime DATETIME,
    @EndDateTime   DATETIME,
    @TotalHours    DECIMAL(18,2),
    @TotalAmount   DECIMAL(18,2),
    @Status        INT = 1,
    @Purpose       NVARCHAR(200),
    @GuestCount    INT
AS BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
        BEGIN TRAN;

        DECLARE @EventDate DATE = CAST(@StartDateTime AS DATE);
        DECLARE @StartTime TIME = CAST(@StartDateTime AS TIME);
        DECLARE @EndTime TIME = CAST(@EndDateTime AS TIME);

        DECLARE @StatusStr NVARCHAR(20) = 
            CASE @Status
                WHEN 1 THEN 'Pending'
                WHEN 2 THEN 'Confirmed'
                WHEN 3 THEN 'Cancelled'
                WHEN 4 THEN 'Completed'
                WHEN 5 THEN 'Rejected'
                ELSE 'Pending'
            END;

        IF EXISTS (
            SELECT 1
            FROM dbo.Bookings
            WHERE HallId = @HallId
              AND BookingStatus NOT IN ('Cancelled', 'Rejected')
              AND EventDate = @EventDate
              AND StartTime < @EndTime
              AND EndTime > @StartTime
        )
        BEGIN
            ROLLBACK TRAN;
            THROW 51000, 'Double booking detected. The slot is no longer available.', 1;
        END

        DECLARE @HallName NVARCHAR(150), @CustomerName NVARCHAR(150);
        SELECT @HallName = HallName FROM dbo.Halls WHERE HallId = @HallId;
        SELECT @CustomerName = FullName FROM dbo.Users WHERE UserId = @CustomerId;

        DECLARE @SeqVal BIGINT = NEXT VALUE FOR dbo.BookingReferenceSeq;
        DECLARE @BookingRef NVARCHAR(20) = 'BK-' + CAST(@SeqVal AS NVARCHAR(10));

        INSERT INTO dbo.Bookings (BookingReference, UserId, HallId, HallNameSnapshot, CustomerNameSnapshot, EventDate, StartTime, EndTime, GuestCount, TotalAmount, BookingStatus, Purpose, CreatedAt, UpdatedAt)
        VALUES (@BookingRef, @CustomerId, @HallId, ISNULL(@HallName, ''), ISNULL(@CustomerName, ''), @EventDate, @StartTime, @EndTime, @GuestCount, @TotalAmount, @StatusStr, @Purpose, GETDATE(), GETDATE());

        DECLARE @NewBookingId INT = SCOPE_IDENTITY();
        COMMIT TRAN;
        SELECT @NewBookingId AS BookingId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_GetBookingById;
GO
CREATE PROCEDURE dbo.sp_GetBookingById
    @BookingId INT
AS BEGIN
    SET NOCOUNT ON;
    SELECT b.BookingId, 
           b.UserId AS CustomerId, 
           u.FullName AS CustomerName,
           b.HallId, 
           h.HallName,
           CAST(b.EventDate AS DATETIME) + CAST(b.StartTime AS DATETIME) AS StartDateTime, 
           CAST(b.EventDate AS DATETIME) + CAST(b.EndTime AS DATETIME) AS EndDateTime, 
           CAST(DATEDIFF(MINUTE, b.StartTime, b.EndTime) / 60.0 AS DECIMAL(10,2)) AS TotalHours, 
           b.TotalAmount,
           CASE b.BookingStatus
               WHEN 'Pending' THEN 1
               WHEN 'Confirmed' THEN 2
               WHEN 'Cancelled' THEN 3
               WHEN 'Completed' THEN 4
               WHEN 'Rejected' THEN 5
               ELSE 1
           END AS [Status], 
           b.Purpose, 
           b.GuestCount, 
           b.CreatedAt, 
           b.UpdatedAt
    FROM dbo.Bookings b
    INNER JOIN dbo.Users u ON b.UserId = u.UserId
    INNER JOIN dbo.Halls h ON b.HallId = h.HallId
    WHERE b.BookingId = @BookingId;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_GetAllBookings;
GO
CREATE PROCEDURE dbo.sp_GetAllBookings
    @Page     INT = 1,
    @PageSize INT = 1000
AS BEGIN
    SET NOCOUNT ON;
    SELECT b.BookingId, 
           b.UserId AS CustomerId, 
           u.FullName AS CustomerName,
           b.HallId, 
           h.HallName,
           CAST(b.EventDate AS DATETIME) + CAST(b.StartTime AS DATETIME) AS StartDateTime, 
           CAST(b.EventDate AS DATETIME) + CAST(b.EndTime AS DATETIME) AS EndDateTime, 
           CAST(DATEDIFF(MINUTE, b.StartTime, b.EndTime) / 60.0 AS DECIMAL(10,2)) AS TotalHours, 
           b.TotalAmount,
           CASE b.BookingStatus
               WHEN 'Pending' THEN 1
               WHEN 'Confirmed' THEN 2
               WHEN 'Cancelled' THEN 3
               WHEN 'Completed' THEN 4
               WHEN 'Rejected' THEN 5
               ELSE 1
           END AS [Status], 
           b.Purpose, 
           b.GuestCount, 
           b.CreatedAt, 
           b.UpdatedAt
    FROM dbo.Bookings b
    INNER JOIN dbo.Users u ON b.UserId = u.UserId
    INNER JOIN dbo.Halls h ON b.HallId = h.HallId
    ORDER BY b.BookingId DESC
    OFFSET (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_GetBookingsByCustomer;
GO
CREATE PROCEDURE dbo.sp_GetBookingsByCustomer
    @CustomerId INT,
    @Page       INT = 1,
    @PageSize   INT = 1000
AS BEGIN
    SET NOCOUNT ON;
    SELECT b.BookingId, 
           b.UserId AS CustomerId, 
           u.FullName AS CustomerName,
           b.HallId, 
           h.HallName,
           CAST(b.EventDate AS DATETIME) + CAST(b.StartTime AS DATETIME) AS StartDateTime, 
           CAST(b.EventDate AS DATETIME) + CAST(b.EndTime AS DATETIME) AS EndDateTime, 
           CAST(DATEDIFF(MINUTE, b.StartTime, b.EndTime) / 60.0 AS DECIMAL(10,2)) AS TotalHours, 
           b.TotalAmount,
           CASE b.BookingStatus
               WHEN 'Pending' THEN 1
               WHEN 'Confirmed' THEN 2
               WHEN 'Cancelled' THEN 3
               WHEN 'Completed' THEN 4
               WHEN 'Rejected' THEN 5
               ELSE 1
           END AS [Status], 
           b.Purpose, 
           b.GuestCount, 
           b.CreatedAt, 
           b.UpdatedAt
    FROM dbo.Bookings b
    INNER JOIN dbo.Users u ON b.UserId = u.UserId
    INNER JOIN dbo.Halls h ON b.HallId = h.HallId
    WHERE b.UserId = @CustomerId
    ORDER BY b.BookingId DESC
    OFFSET (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_GetPendingBookings;
GO
CREATE PROCEDURE dbo.sp_GetPendingBookings
AS BEGIN
    SET NOCOUNT ON;
    SELECT b.BookingId, 
           b.UserId AS CustomerId, 
           u.FullName AS CustomerName,
           b.HallId, 
           h.HallName,
           CAST(b.EventDate AS DATETIME) + CAST(b.StartTime AS DATETIME) AS StartDateTime, 
           CAST(b.EventDate AS DATETIME) + CAST(b.EndTime AS DATETIME) AS EndDateTime, 
           CAST(DATEDIFF(MINUTE, b.StartTime, b.EndTime) / 60.0 AS DECIMAL(10,2)) AS TotalHours, 
           b.TotalAmount,
           CASE b.BookingStatus
               WHEN 'Pending' THEN 1
               WHEN 'Confirmed' THEN 2
               WHEN 'Cancelled' THEN 3
               WHEN 'Completed' THEN 4
               WHEN 'Rejected' THEN 5
               ELSE 1
           END AS [Status], 
           b.Purpose, 
           b.GuestCount, 
           b.CreatedAt, 
           b.UpdatedAt
    FROM dbo.Bookings b
    INNER JOIN dbo.Users u ON b.UserId = u.UserId
    INNER JOIN dbo.Halls h ON b.HallId = h.HallId
    WHERE b.BookingStatus = 'Pending'
    ORDER BY b.CreatedAt ASC;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_GetBookingsByDateRange;
GO
CREATE PROCEDURE dbo.sp_GetBookingsByDateRange
    @StartDate DATETIME,
    @EndDate   DATETIME
AS BEGIN
    SET NOCOUNT ON;
    SELECT b.BookingId, 
           b.UserId AS CustomerId, 
           u.FullName AS CustomerName,
           b.HallId, 
           h.HallName,
           CAST(b.EventDate AS DATETIME) + CAST(b.StartTime AS DATETIME) AS StartDateTime, 
           CAST(b.EventDate AS DATETIME) + CAST(b.EndTime AS DATETIME) AS EndDateTime, 
           CAST(DATEDIFF(MINUTE, b.StartTime, b.EndTime) / 60.0 AS DECIMAL(10,2)) AS TotalHours, 
           b.TotalAmount,
           CASE b.BookingStatus
               WHEN 'Pending' THEN 1
               WHEN 'Confirmed' THEN 2
               WHEN 'Cancelled' THEN 3
               WHEN 'Completed' THEN 4
               WHEN 'Rejected' THEN 5
               ELSE 1
           END AS [Status], 
           b.Purpose, 
           b.GuestCount, 
           b.CreatedAt, 
           b.UpdatedAt
    FROM dbo.Bookings b
    INNER JOIN dbo.Users u ON b.UserId = u.UserId
    INNER JOIN dbo.Halls h ON b.HallId = h.HallId
    WHERE (CAST(b.EventDate AS DATETIME) + CAST(b.StartTime AS DATETIME)) >= @StartDate 
      AND (CAST(b.EventDate AS DATETIME) + CAST(b.StartTime AS DATETIME)) <= @EndDate
    ORDER BY b.EventDate ASC, b.StartTime ASC;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_GetCustomerBookingCount;
GO
CREATE PROCEDURE dbo.sp_GetCustomerBookingCount
    @CustomerId INT
AS BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS BookingCount
    FROM dbo.Bookings
    WHERE UserId = @CustomerId;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_GetTotalBookingCount;
GO
CREATE PROCEDURE dbo.sp_GetTotalBookingCount
AS BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS TotalBookings FROM dbo.Bookings;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_CheckHallAvailability;
GO
CREATE PROCEDURE dbo.sp_CheckHallAvailability
    @HallId        INT,
    @StartDateTime DATETIME,
    @EndDateTime   DATETIME
AS BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM dbo.Bookings
        WHERE HallId = @HallId
          AND BookingStatus IN ('Pending', 'Confirmed')
          AND EventDate = CAST(@StartDateTime AS DATE)
          AND StartTime < CAST(@EndDateTime AS TIME)
          AND EndTime > CAST(@StartDateTime AS TIME)
    )
        SELECT 0 AS IsAvailable;
    ELSE
        SELECT 1 AS IsAvailable;
END
GO