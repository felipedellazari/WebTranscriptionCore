CREATE TABLE TranscriptionDataWeb (
	TaskId INT NOT NULL,
    TranscribedIn tinyint NOT NULL,
    Text NVARCHAR(MAX) NULL,
    PlainText NVARCHAR(MAX) NULL,
    [Date] DATETIME NULL,
    UserId INT NULL,
	CONSTRAINT PK_TranscriptionDataWeb PRIMARY KEY (TaskId),
	CONSTRAINT FK_TrDataWeb_SessionTask FOREIGN KEY(TaskId) REFERENCES SessionTask (Id),
    CONSTRAINT FK_TrDataWeb_User FOREIGN KEY(UserId) REFERENCES "USER" (Id)
);

CREATE TABLE TranscriptionHistoryWeb (
    Id INT NOT NULL,
	TaskId INT NOT NULL,
	[Date] DATETIME NOT NULL,
	UserId INT NULL,
	Text NVARCHAR(MAX) NULL,
	PlainText NVARCHAR(MAX) NULL,
	CONSTRAINT PK_TranscriptionHistoryWeb PRIMARY KEY (Id),
	CONSTRAINT FK_TrHistWeb_TrDataweb FOREIGN KEY(TaskId) REFERENCES TranscriptionDataWeb (TaskId)
);

IF ((SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()) > 100) BEGIN
	EXEC ('CREATE SEQUENCE SQ_TranscriptionHistoryWeb			START WITH 1 INCREMENT BY 1');
END ELSE BEGIN
	CREATE TABLE SQ_TranscriptionHistoryWeb			(ID BIGINT IDENTITY(1,1) );
END

ALTER TABLE FlowStep ADD WebTranscription BIT NULL;
GO

IF EXISTS (SELECT 1 FROM SystemConfiguration WHERE [Name] = 'WebTranscription') BEGIN
	UPDATE SystemConfiguration SET [Value] = 'True' WHERE [Name] = 'WebTranscription';
END ELSE BEGIN
	INSERT INTO SystemConfiguration([Name], [Value]) VALUES ('WebTranscription', 'True');
END
GO