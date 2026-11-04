IF DB_ID(N'WebsiteAmThuc') IS NULL
BEGIN
    CREATE DATABASE WebsiteAmThuc;
END;
GO

USE WebsiteAmThuc;
GO

/* 1. Tai khoan. Moi tai khoan co dung 1 vai tro.*/
CREATE TABLE dbo.Accounts
(
    Id INT IDENTITY(1,1) NOT NULL,
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(254) NOT NULL,
    Password NVARCHAR(100) NOT NULL,
    DisplayName NVARCHAR(100) NOT NULL,
    Avatar NVARCHAR(500) NULL,
    Bio NVARCHAR(1000) NULL,
    Role NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Accounts_Role DEFAULT (N'User'),
    IsLocked BIT NOT NULL
        CONSTRAINT DF_Accounts_IsLocked DEFAULT (0),
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Accounts_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt DATETIME2(0) NULL,

    CONSTRAINT PK_Accounts PRIMARY KEY (Id),
    CONSTRAINT UQ_Accounts_Username UNIQUE (Username),
    CONSTRAINT UQ_Accounts_Email UNIQUE (Email),
    CONSTRAINT CK_Accounts_Role
        CHECK (Role IN (N'User', N'Chef', N'Provider', N'Admin')),
    CONSTRAINT CK_Accounts_Username CHECK (LEN(LTRIM(RTRIM(Username))) > 0),
    CONSTRAINT CK_Accounts_Email CHECK (LEN(LTRIM(RTRIM(Email))) > 0),
    CONSTRAINT CK_Accounts_Password CHECK (LEN(Password) > 0),
    CONSTRAINT CK_Accounts_DisplayName CHECK (LEN(LTRIM(RTRIM(DisplayName))) > 0)
);
GO

/* 2. Ho so doanh nghiep cua tai khoan nha cung cap (moi tai khoan toi da 1 ho so) */
CREATE TABLE dbo.ProviderProfiles
(
    Id INT IDENTITY(1,1) NOT NULL,
    AccountId INT NOT NULL,
    BusinessName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(2000) NULL,
    Logo NVARCHAR(500) NULL,
    Address NVARCHAR(300) NULL,
    Phone NVARCHAR(30) NULL,
    ContactEmail NVARCHAR(254) NULL,
    Website NVARCHAR(500) NULL,
    SocialLink NVARCHAR(500) NULL,
    IsPublic BIT NOT NULL
        CONSTRAINT DF_ProviderProfiles_IsPublic DEFAULT (1),
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_ProviderProfiles_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt DATETIME2(0) NULL,

    CONSTRAINT PK_ProviderProfiles PRIMARY KEY (Id),
    CONSTRAINT UQ_ProviderProfiles_AccountId UNIQUE (AccountId),
    CONSTRAINT FK_ProviderProfiles_Accounts
        FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id),
    CONSTRAINT CK_ProviderProfiles_BusinessName
        CHECK (LEN(LTRIM(RTRIM(BusinessName))) > 0)
);
GO

/* 3. Bai dang. Moi bai dang cung mot loai doi tuong, phan loai bang nhan. */
CREATE TABLE dbo.Posts
(
    Id INT IDENTITY(1,1) NOT NULL,
    AccountId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Summary NVARCHAR(500) NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Cover NVARCHAR(500) NULL,
    Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Posts_Status DEFAULT (N'Draft'),
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Posts_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt DATETIME2(0) NULL,
    PublishedAt DATETIME2(0) NULL,

    CONSTRAINT PK_Posts PRIMARY KEY (Id),
    CONSTRAINT FK_Posts_Accounts
        FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id),
    CONSTRAINT CK_Posts_Title CHECK (LEN(LTRIM(RTRIM(Title))) > 0),
    CONSTRAINT CK_Posts_Status
        CHECK (Status IN (N'Draft', N'Public', N'Hidden', N'Deleted')),
    CONSTRAINT CK_Posts_PublishedAt
        CHECK (Status <> N'Public' OR PublishedAt IS NOT NULL)
);
GO

/* 4. Nhan. SystemKey khac NULL la 5 nhan he thong (loai bai dang), NULL la nhan thuong. */
CREATE TABLE dbo.Labels
(
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(80) NOT NULL,
    Description NVARCHAR(300) NULL,
    SystemKey NVARCHAR(20) NULL,
    IsActive BIT NOT NULL
        CONSTRAINT DF_Labels_IsActive DEFAULT (1),
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Labels_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Labels PRIMARY KEY (Id),
    CONSTRAINT UQ_Labels_Name UNIQUE (Name),
    CONSTRAINT CK_Labels_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
    CONSTRAINT CK_Labels_SystemKey
        CHECK (SystemKey IS NULL OR SystemKey IN (N'Recipe', N'Tip', N'Experience', N'Review', N'Promo'))
);
GO

/* Moi SystemKey chi xuat hien tren toi da 1 nhan */
CREATE UNIQUE INDEX UX_Labels_SystemKey
ON dbo.Labels (SystemKey)
WHERE SystemKey IS NOT NULL;
GO

/* N-N: mot bai dang co nhieu nhan, mot nhan gan cho nhieu bai dang */
CREATE TABLE dbo.PostLabels
(
    PostId INT NOT NULL,
    LabelId INT NOT NULL,

    CONSTRAINT PK_PostLabels PRIMARY KEY (PostId, LabelId),
    CONSTRAINT FK_PostLabels_Posts
        FOREIGN KEY (PostId) REFERENCES dbo.Posts(Id),
    CONSTRAINT FK_PostLabels_Labels
        FOREIGN KEY (LabelId) REFERENCES dbo.Labels(Id)
);
GO

/* 5. Du lieu rieng cua bai dang cong thuc (moi bai toi da 1 cong thuc) */
CREATE TABLE dbo.Recipes
(
    Id INT IDENTITY(1,1) NOT NULL,
    PostId INT NOT NULL,
    PrepMinutes INT NULL,
    CookMinutes INT NULL,
    Servings INT NULL,
    Difficulty NVARCHAR(10) NULL,
    Note NVARCHAR(1000) NULL,

    CONSTRAINT PK_Recipes PRIMARY KEY (Id),
    CONSTRAINT UQ_Recipes_PostId UNIQUE (PostId),
    CONSTRAINT FK_Recipes_Posts
        FOREIGN KEY (PostId) REFERENCES dbo.Posts(Id),
    CONSTRAINT CK_Recipes_PrepMinutes CHECK (PrepMinutes IS NULL OR PrepMinutes >= 0),
    CONSTRAINT CK_Recipes_CookMinutes CHECK (CookMinutes IS NULL OR CookMinutes >= 0),
    CONSTRAINT CK_Recipes_Servings CHECK (Servings IS NULL OR Servings > 0),
    CONSTRAINT CK_Recipes_Difficulty
        CHECK (Difficulty IS NULL OR Difficulty IN (N'Easy', N'Medium', N'Hard'))
);
GO

CREATE TABLE dbo.Ingredients
(
    Id INT IDENTITY(1,1) NOT NULL,
    RecipeId INT NOT NULL,
    Position INT NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Amount DECIMAL(12,3) NULL,
    Unit NVARCHAR(50) NULL,
    Note NVARCHAR(250) NULL,

    CONSTRAINT PK_Ingredients PRIMARY KEY (Id),
    CONSTRAINT FK_Ingredients_Recipes
        FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id),
    CONSTRAINT UQ_Ingredients_Position UNIQUE (RecipeId, Position),
    CONSTRAINT CK_Ingredients_Position CHECK (Position > 0),
    CONSTRAINT CK_Ingredients_Amount CHECK (Amount IS NULL OR Amount > 0),
    CONSTRAINT CK_Ingredients_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

CREATE TABLE dbo.RecipeSteps
(
    Id INT IDENTITY(1,1) NOT NULL,
    RecipeId INT NOT NULL,
    Position INT NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Image NVARCHAR(500) NULL,

    CONSTRAINT PK_RecipeSteps PRIMARY KEY (Id),
    CONSTRAINT FK_RecipeSteps_Recipes
        FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id),
    CONSTRAINT UQ_RecipeSteps_Position UNIQUE (RecipeId, Position),
    CONSTRAINT CK_RecipeSteps_Position CHECK (Position > 0),
    CONSTRAINT CK_RecipeSteps_Content CHECK (LEN(LTRIM(RTRIM(Content))) > 0)
);
GO

/*
   6. Binh luan va danh gia.
      - Rating NULL = binh luan thuong, Rating 1..10 = binh luan danh gia.
      - Dich cua binh luan: PostId (bai dang) HOAC TargetAccountId (ho so), dung 1 trong 2.
      - ParentId: tra loi mot binh luan khac (chi 1 cap, phan nay kiem tra trong code).
*/
CREATE TABLE dbo.Comments
(
    Id INT IDENTITY(1,1) NOT NULL,
    AccountId INT NOT NULL,
    PostId INT NULL,
    TargetAccountId INT NULL,
    ParentId INT NULL,
    Content NVARCHAR(2000) NOT NULL,
    Rating INT NULL,
    Status NVARCHAR(15) NOT NULL
        CONSTRAINT DF_Comments_Status DEFAULT (N'Visible'),
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Comments_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt DATETIME2(0) NULL,

    CONSTRAINT PK_Comments PRIMARY KEY (Id),
    CONSTRAINT FK_Comments_Accounts
        FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id),
    CONSTRAINT FK_Comments_Posts
        FOREIGN KEY (PostId) REFERENCES dbo.Posts(Id),
    CONSTRAINT FK_Comments_TargetAccounts
        FOREIGN KEY (TargetAccountId) REFERENCES dbo.Accounts(Id),
    CONSTRAINT FK_Comments_Parent
        FOREIGN KEY (ParentId) REFERENCES dbo.Comments(Id),
    CONSTRAINT CK_Comments_OneTarget
        CHECK (
            (PostId IS NOT NULL AND TargetAccountId IS NULL)
            OR
            (PostId IS NULL AND TargetAccountId IS NOT NULL)
        ),
    CONSTRAINT CK_Comments_Rating CHECK (Rating IS NULL OR Rating BETWEEN 1 AND 10),
    CONSTRAINT CK_Comments_RatingNotReply CHECK (Rating IS NULL OR ParentId IS NULL),
    CONSTRAINT CK_Comments_NoSelfRating
        CHECK (Rating IS NULL OR TargetAccountId IS NULL OR AccountId <> TargetAccountId),
    CONSTRAINT CK_Comments_NotOwnParent CHECK (ParentId IS NULL OR ParentId <> Id),
    CONSTRAINT CK_Comments_Status CHECK (Status IN (N'Visible', N'Hidden', N'Deleted')),
    CONSTRAINT CK_Comments_Content CHECK (LEN(LTRIM(RTRIM(Content))) > 0)
);
GO

/* Moi tai khoan chi co 1 danh gia dang hien thi cho 1 bai dang */
CREATE UNIQUE INDEX UX_Comments_PostRating
ON dbo.Comments (AccountId, PostId)
WHERE Rating IS NOT NULL AND PostId IS NOT NULL AND Status = N'Visible';
GO

/* Moi tai khoan chi co 1 danh gia dang hien thi cho 1 ho so */
CREATE UNIQUE INDEX UX_Comments_ProfileRating
ON dbo.Comments (AccountId, TargetAccountId)
WHERE Rating IS NOT NULL AND TargetAccountId IS NOT NULL AND Status = N'Visible';
GO

/* 7. Luot truy cap: moi lan xem la 1 dong (de tinh luot xem bai dang, ho so va luu luong website) */
CREATE TABLE dbo.Visits
(
    Id INT IDENTITY(1,1) NOT NULL,
    AccountId INT NULL,
    Kind NVARCHAR(10) NOT NULL,
    PostId INT NULL,
    ProfileAccountId INT NULL,
    Path NVARCHAR(500) NOT NULL,
    VisitedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Visits_VisitedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Visits PRIMARY KEY (Id),
    CONSTRAINT FK_Visits_Accounts
        FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id),
    CONSTRAINT FK_Visits_Posts
        FOREIGN KEY (PostId) REFERENCES dbo.Posts(Id),
    CONSTRAINT FK_Visits_ProfileAccounts
        FOREIGN KEY (ProfileAccountId) REFERENCES dbo.Accounts(Id),
    CONSTRAINT CK_Visits_Kind
        CHECK (
            (Kind = N'Page' AND PostId IS NULL AND ProfileAccountId IS NULL)
            OR
            (Kind = N'Post' AND PostId IS NOT NULL AND ProfileAccountId IS NULL)
            OR
            (Kind = N'Profile' AND PostId IS NULL AND ProfileAccountId IS NOT NULL)
        ),
    CONSTRAINT CK_Visits_Path CHECK (LEN(LTRIM(RTRIM(Path))) > 0)
);
GO

/* 8. Noi dung cac trang thong tin (gioi thieu, lien he, tro giup). Slug la ma trang. */
CREATE TABLE dbo.InfoPages
(
    Id INT IDENTITY(1,1) NOT NULL,
    Slug NVARCHAR(100) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    IsPublic BIT NOT NULL
        CONSTRAINT DF_InfoPages_IsPublic DEFAULT (1),
    UpdatedByAccountId INT NULL,
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_InfoPages_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt DATETIME2(0) NULL,

    CONSTRAINT PK_InfoPages PRIMARY KEY (Id),
    CONSTRAINT UQ_InfoPages_Slug UNIQUE (Slug),
    CONSTRAINT FK_InfoPages_Accounts
        FOREIGN KEY (UpdatedByAccountId) REFERENCES dbo.Accounts(Id),
    CONSTRAINT CK_InfoPages_Slug CHECK (LEN(LTRIM(RTRIM(Slug))) > 0),
    CONSTRAINT CK_InfoPages_Title CHECK (LEN(LTRIM(RTRIM(Title))) > 0)
);
GO

/* 9. Cong tac bat/tat chuc nang (Name: Register, Post, Comment, Rating, Promo) */
CREATE TABLE dbo.Settings
(
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Enabled BIT NOT NULL
        CONSTRAINT DF_Settings_Enabled DEFAULT (1),
    Description NVARCHAR(300) NULL,
    UpdatedByAccountId INT NULL,
    UpdatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Settings_UpdatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Settings PRIMARY KEY (Id),
    CONSTRAINT UQ_Settings_Name UNIQUE (Name),
    CONSTRAINT FK_Settings_Accounts
        FOREIGN KEY (UpdatedByAccountId) REFERENCES dbo.Accounts(Id),
    CONSTRAINT CK_Settings_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO
