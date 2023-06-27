CREATE TABLE TranscriptionDataWeb (
	TaskId NUMBER(10,0) NOT NULL,
    TranscribedIn NUMBER(1,0) NOT NULL,
    Text CLOB NULL,
    PlainText CLOB NULL,
    "DATE" DATE NULL,
    UserId NUMBER(10,0) NULL,
	CONSTRAINT PK_TranscriptionDataWeb PRIMARY KEY (TaskId),
	CONSTRAINT FK_TrDataWeb_SessionTask FOREIGN KEY(TaskId) REFERENCES SessionTask (Id),
    CONSTRAINT FK_TrDataWeb_User FOREIGN KEY(UserId) REFERENCES "USER" (Id)
);

CREATE TABLE TranscriptionHistoryWeb (
    Id NUMBER(10,0) NOT NULL,
	TaskId NUMBER(10,0) NOT NULL,
	"DATE" DATE NOT NULL,
	UserId NUMBER(10,0) NULL,
	Text CLOB NULL,
	PlainText CLOB NULL,
	CONSTRAINT PK_TranscriptionHistoryWeb PRIMARY KEY (Id),
	CONSTRAINT FK_TrHistWeb_TrDataweb FOREIGN KEY(TaskId) REFERENCES TranscriptionDataWeb (TaskId)
);
CREATE SEQUENCE SQ_TranscriptionHistoryWeb;

ALTER TABLE FlowStep ADD WebTranscription NUMBER(1,0) NULL;

declare
  conf number(1);
begin
  select count(*) into conf from SystemConfiguration where Name = 'WebTranscription';

  if conf = 1 then
    UPDATE SystemConfiguration SET Value = 'True' WHERE Name = 'WebTranscription';
  else
    INSERT INTO SystemConfiguration(Name, Value) VALUES ('WebTranscription', 'True');
  end if;
  commit;
end;