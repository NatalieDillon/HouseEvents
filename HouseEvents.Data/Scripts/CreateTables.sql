--Create tables
USE [HouseEvent]
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EventParticipant]') AND type in (N'U'))
DROP TABLE [dbo].[EventParticipant]
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HouseEvent]') AND type in (N'U'))
DROP TABLE [dbo].[HouseEvent]
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EventDetail]') AND type in (N'U'))
DROP TABLE [dbo].[EventDetail]
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[House]') AND type in (N'U'))
DROP TABLE [dbo].[House]
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Event]') AND type in (N'U'))
DROP TABLE [dbo].[Event]
GO

CREATE TABLE [dbo].[House](
	[HouseID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[HouseName] [varchar](50) NOT NULL,
	[UndermasterFirstName] [varchar](50) NOT NULL,
	[EventsCoordinator] [varchar](50) NULL )
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_House] ON [dbo].[House]
(
	[HouseName] ASC
)
GO

CREATE TABLE [dbo].[Event](
	[EventID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[EventName] [varchar](100) NOT NULL,
	[SchoolYear] [int] NOT NULL)
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Event] ON [dbo].[Event]
(
	[EventName] ASC
)

CREATE TABLE [dbo].[EventDetail](
	[EventDetailID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[EventID] [int] NOT NULL FOREIGN KEY REFERENCES [Event](EventID),
	[EventDate] [Date] NOT NULL,
	[EventStartTime] [Time] NOT NULL,
	[EventEndTime] [Time] NOT NULL,
	[EventVenue] [varchar](50) NOT NULL,
	[Notes] [varchar] (400) NULL)
GO

CREATE TABLE [dbo].[HouseEvent](
	[HouseEventID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[EventDetailID] [int] NOT NULL FOREIGN KEY REFERENCES EventDetail(EventDetailID),
	[HouseID] [int] NOT NULL FOREIGN KEY REFERENCES House(HouseID),
	[Points] [int] NOT NULL)
GO

CREATE TABLE [dbo].[EventParticipant](
	[EventParticipantID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[HouseEventID] [int] NOT NULL FOREIGN KEY REFERENCES HouseEvent(HouseEventID),
	[YearGroup] [varchar](50) NOT NULL,
	[Reserve] [bit] NOT NULL,
	[StudentName] [varchar](100) NULL,
	[NoShow] [bit] NULL,)
GO

DROP VIEW [dbo].[vwEventParticipantsNoFixture]
GO

CREATE VIEW [dbo].[vwEventParticipantsNoFixture] AS
(
SELECT a.EventId, b.EventName, c.EventDetailId, c.EventDate, c.EventStartTime, c.EventEndTime,
c.EventVenue, c.Notes, d.HouseEventId, d.HouseId, e.HouseName, d.Points, 
f.EventParticipantId, f.YearGroup, f.Reserve, f.StudentName, f.NoShow FROM
(SELECT EventId FROM dbo.EventDetail
GROUP BY EventId
HAVING count(*) = 1) a 
INNER JOIN dbo.[Event] b on a.EventId = b.EventId
INNER JOIN dbo.EventDetail c on a.EventId = c.EventId
INNER JOIN dbo.HouseEvent d on c.EventDetailID = d.EventDetailID
INNER JOIN dbo.House e on d.HouseId = e.HouseID
INNER JOIN dbo.EventParticipant f on d.HouseEventId = f.HouseEventId
)
GO
