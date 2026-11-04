USE WebsiteAmThuc;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @keep TABLE (Id INT PRIMARY KEY);
INSERT INTO @keep (Id)
SELECT Id FROM dbo.Accounts WHERE Username IN (N'admin', N'nhacungcap', N'nguoidung');

IF (SELECT COUNT(*) FROM @keep) <> 3
BEGIN
    RAISERROR(N'Khong tim thay du 3 tai khoan admin, nhacungcap, nguoidung. Da huy, chua xoa gi ca.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

/* Xoa du lieu theo thu tu tu bang con den bang cha */
DELETE FROM dbo.Visits;
DELETE FROM dbo.Comments;
DELETE FROM dbo.PostLabels;
DELETE FROM dbo.Ingredients;
DELETE FROM dbo.RecipeSteps;
DELETE FROM dbo.Recipes;
DELETE FROM dbo.Posts;

DELETE FROM dbo.ProviderProfiles WHERE AccountId NOT IN (SELECT Id FROM @keep);

/* Bang con tro toi tai khoan sap xoa thi go lien ket truoc */
UPDATE dbo.InfoPages SET UpdatedByAccountId = NULL WHERE UpdatedByAccountId NOT IN (SELECT Id FROM @keep);
UPDATE dbo.Settings SET UpdatedByAccountId = NULL WHERE UpdatedByAccountId NOT IN (SELECT Id FROM @keep);

DELETE FROM dbo.Accounts WHERE Id NOT IN (SELECT Id FROM @keep);
UPDATE dbo.Accounts SET IsLocked = 0;

/* Nhan: bo nhan nguoi dung tu them, bat lai tat ca */
DELETE FROM dbo.Labels
WHERE SystemKey IS NULL
  AND Name NOT IN (N'Món Việt', N'Món chay', N'Nhanh gọn', N'Tráng miệng', N'Đồ uống', N'Quán ăn');
UPDATE dbo.Labels SET IsActive = 1;

/* Cong tac chuc nang: bat het */
UPDATE dbo.Settings SET Enabled = 1, UpdatedAt = SYSDATETIME();

COMMIT TRANSACTION;
GO

/* Dat lai so thu tu ID (cac bang nay da rong) */
DBCC CHECKIDENT (N'dbo.Visits', RESEED, 0);
DBCC CHECKIDENT (N'dbo.Comments', RESEED, 0);
DBCC CHECKIDENT (N'dbo.Ingredients', RESEED, 0);
DBCC CHECKIDENT (N'dbo.RecipeSteps', RESEED, 0);
DBCC CHECKIDENT (N'dbo.Recipes', RESEED, 0);
DBCC CHECKIDENT (N'dbo.Posts', RESEED, 0);
GO

PRINT N'Da lam sach du lieu. Chay tiep WebsiteAmThucSample.sql neu muon co bai dang mau.';
GO
