CREATE TABLE ConnectedUsers(
	SessionId NVARCHAR(200) NOT NULL,
	UserId NVARCHAR(200) NOT NULL
CONSTRAINT PK_ConnectedUsers PRIMARY KEY (ConnectedUsers)
)