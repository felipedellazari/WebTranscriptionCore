CREATE TABLE TranscriptionDataWeb(
	TaskId INT NOT NULL,
    TranscribedIn NUMERIC (1,0) NOT NULL,
    "text" TEXT NULL,
    PlainText TEXT NULL,
    "date" TIMESTAMP NULL,
    UserId INT NULL,
	CONSTRAINT PK_TranscriptionDataWeb PRIMARY KEY (TaskId),
	CONSTRAINT FK_TrDataWeb_SessionTask FOREIGN KEY(TaskId) REFERENCES SessionTask (Id),
    CONSTRAINT FK_TrDataWeb_User FOREIGN KEY(UserId) REFERENCES "user" (Id)
);

CREATE TABLE TranscriptionHistoryWeb (
    "id" INT NOT NULL,
	TaskId INT NOT NULL,
	"date" TIMESTAMP  NOT NULL,
	UserId INT NULL,
	"text" TEXT NULL,
	PlainText TEXT NULL,
	CONSTRAINT PK_TranscriptionHistoryWeb PRIMARY KEY (Id),
	CONSTRAINT FK_TrHistWeb_TrDataweb FOREIGN KEY(TaskId) REFERENCES TranscriptionDataWeb (TaskId)
);
CREATE SEQUENCE SQ_TranscriptionHistoryWeb;

ALTER TABLE FlowStep ADD WebTranscription NUMERIC(1,0) NULL;

INSERT INTO SystemConfiguration(name, value) VALUES ('WebTranscription', 'True')
ON CONFLICT ("name") DO UPDATE SET value = 'True' WHERE SystemConfiguration.name = 'WebTranscription';