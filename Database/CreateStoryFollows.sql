IF OBJECT_ID(N'dbo.StoryFollows', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.StoryFollows
	(
		UserId int NOT NULL,
		StoryId int NOT NULL,
		FollowedAt datetime2(0) NOT NULL
			CONSTRAINT DF_StoryFollows_FollowedAt DEFAULT SYSUTCDATETIME(),
		CONSTRAINT PK_StoryFollows PRIMARY KEY (UserId, StoryId),
		CONSTRAINT FK_StoryFollows_Users FOREIGN KEY (UserId)
			REFERENCES dbo.Users (UserId) ON DELETE CASCADE,
		CONSTRAINT FK_StoryFollows_Stories FOREIGN KEY (StoryId)
			REFERENCES dbo.Stories (StoryId) ON DELETE CASCADE
	);
END;

IF COL_LENGTH(N'dbo.Chapters', N'UpdatedAt') IS NULL
BEGIN
	ALTER TABLE dbo.Chapters
	ADD UpdatedAt datetime2(0) NULL;

	UPDATE dbo.Chapters
	SET UpdatedAt = CONVERT(datetime2(0), '20000101', 112)
	WHERE UpdatedAt IS NULL;

	ALTER TABLE dbo.Chapters
	ALTER COLUMN UpdatedAt datetime2(0) NOT NULL;

	ALTER TABLE dbo.Chapters
	ADD CONSTRAINT DF_Chapters_UpdatedAt DEFAULT (SYSUTCDATETIME()) FOR UpdatedAt;
END;
