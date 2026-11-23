USE [WebDocTruyen];
GO

DECLARE @Genres TABLE (CategoryName nvarchar(100) NOT NULL PRIMARY KEY);

INSERT INTO @Genres (CategoryName)
VALUES
	(N'Hành động'),
	(N'Phiêu lưu'),
	(N'Romance'),
	(N'Hài hước'),
	(N'Fantasy'),
	(N'Manhwa'),
	(N'Siêu nhiên'),
	(N'Kinh dị'),
	(N'Trinh thám'),
	(N'Truyện chữ');

INSERT INTO dbo.Categories (CategoryName)
SELECT genres.CategoryName
FROM @Genres AS genres
WHERE NOT EXISTS
(
	SELECT 1
	FROM dbo.Categories AS categories
	WHERE categories.CategoryName = genres.CategoryName
);

SELECT CategoryId, CategoryName
FROM dbo.Categories
ORDER BY CategoryName;
