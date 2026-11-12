/*
Учебные логины: admin, manager, ivanova, petrov, smirnova. Пароль: Demo2026!
В password_hash хранится PBKDF2-SHA256: iterations.base64(salt).base64(hash).
*/

-- 1. Создание базы данных.
USE [master];
GO
IF DB_ID(N'EstateCRM') IS NULL
    EXEC(N'CREATE DATABASE [EstateCRM] COLLATE Cyrillic_General_CI_AS;');
GO
USE [EstateCRM];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Не допустить создания таблиц в другой базе при ошибке подключения.
IF DB_NAME() <> N'EstateCRM'
BEGIN
    ;THROW 50001, N'Не удалось выбрать EstateCRM. Проверьте подключение и право создания базы.', 1;
END;

-- Защита существующих данных. DROP DATABASE и DELETE в файле нет.
IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
BEGIN
    ;THROW 50002, N'EstateCRM уже содержит таблицы. Скрипт ничего не изменил. Не удаляйте базу без резервной копии.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

-- 2. Структура таблиц и связи.
CREATE TABLE dbo.roles (
    role_id int IDENTITY(1,1) NOT NULL,
    role_name nvarchar(30) NOT NULL,
    CONSTRAINT pk_roles PRIMARY KEY (role_id),
    CONSTRAINT uq_roles_role_name UNIQUE (role_name)
);

CREATE TABLE dbo.property_types (
    property_type_id int IDENTITY(1,1) NOT NULL,
    type_name nvarchar(50) NOT NULL,
    CONSTRAINT pk_property_types PRIMARY KEY (property_type_id),
    CONSTRAINT uq_property_types_type_name UNIQUE (type_name)
);

CREATE TABLE dbo.districts (
    district_id int IDENTITY(1,1) NOT NULL,
    district_name nvarchar(60) NOT NULL,
    CONSTRAINT pk_districts PRIMARY KEY (district_id),
    CONSTRAINT uq_districts_district_name UNIQUE (district_name)
);

CREATE TABLE dbo.property_statuses (
    status_id int IDENTITY(1,1) NOT NULL,
    status_name nvarchar(30) NOT NULL,
    CONSTRAINT pk_property_statuses PRIMARY KEY (status_id),
    CONSTRAINT uq_property_statuses_status_name UNIQUE (status_name)
);

CREATE TABLE dbo.deal_types (
    deal_type_id int IDENTITY(1,1) NOT NULL,
    deal_type_name nvarchar(30) NOT NULL,
    commission_percent decimal(5, 2) NOT NULL,
    CONSTRAINT pk_deal_types PRIMARY KEY (deal_type_id),
    CONSTRAINT uq_deal_types_deal_type_name UNIQUE (deal_type_name),
    CONSTRAINT ck_deal_types_percent CHECK (commission_percent > 0 AND commission_percent <= 100)
);

CREATE TABLE dbo.viewing_statuses (
    viewing_status_id int IDENTITY(1,1) NOT NULL,
    status_name nvarchar(30) NOT NULL,
    CONSTRAINT pk_viewing_statuses PRIMARY KEY (viewing_status_id),
    CONSTRAINT uq_viewing_statuses_status_name UNIQUE (status_name)
);

CREATE TABLE dbo.employees (
    employee_id int IDENTITY(1,1) NOT NULL,
    last_name nvarchar(50) NOT NULL,
    first_name nvarchar(50) NOT NULL,
    middle_name nvarchar(50) NULL,
    phone varchar(12) NOT NULL,
    login varchar(30) NOT NULL,
    password_hash varchar(200) NOT NULL,
    role_id int NOT NULL,
    is_active bit NOT NULL CONSTRAINT df_employees_is_active DEFAULT (1),
    CONSTRAINT pk_employees PRIMARY KEY (employee_id),
    CONSTRAINT uq_employees_login UNIQUE (login),
    CONSTRAINT fk_employees_role_id FOREIGN KEY (role_id) REFERENCES dbo.roles (role_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT ck_employees_phone CHECK (DATALENGTH(phone) = 12 AND phone COLLATE Latin1_General_100_BIN2 LIKE '+7[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT ck_employees_login CHECK (LEN(LTRIM(RTRIM(login))) > 0),
    CONSTRAINT ck_employees_hash CHECK (LEN(password_hash) >= 60)
);

CREATE TABLE dbo.clients (
    client_id int IDENTITY(1,1) NOT NULL,
    last_name nvarchar(50) NOT NULL,
    first_name nvarchar(50) NOT NULL,
    middle_name nvarchar(50) NULL,
    phone varchar(12) NOT NULL,
    email varchar(100) NULL,
    passport_series char(4) NOT NULL,
    passport_number char(6) NOT NULL,
    registration_date date NOT NULL CONSTRAINT df_clients_registration_date DEFAULT (CONVERT(date, SYSDATETIME())),
    CONSTRAINT pk_clients PRIMARY KEY (client_id),
    CONSTRAINT uq_clients_passport UNIQUE (passport_series, passport_number),
    CONSTRAINT ck_clients_phone CHECK (DATALENGTH(phone) = 12 AND phone COLLATE Latin1_General_100_BIN2 LIKE '+7[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT ck_clients_passport_series CHECK (passport_series COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'),
    CONSTRAINT ck_clients_passport_number CHECK (passport_number COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'),
    CONSTRAINT ck_clients_email CHECK (email IS NULL OR (email LIKE '%_@_%._%' AND email NOT LIKE '% %' AND email NOT LIKE '%@%@%' AND email NOT LIKE '%'+CHAR(9)+'%' AND email NOT LIKE '%'+CHAR(10)+'%' AND email NOT LIKE '%'+CHAR(13)+'%'))
);

CREATE TABLE dbo.properties (
    property_id int IDENTITY(1,1) NOT NULL,
    owner_client_id int NOT NULL,
    property_type_id int NOT NULL,
    district_id int NOT NULL,
    deal_type_id int NOT NULL,
    employee_id int NOT NULL,
    status_id int NOT NULL CONSTRAINT df_properties_status_id DEFAULT (1),
    address nvarchar(200) NOT NULL,
    area decimal(7, 2) NOT NULL,
    rooms tinyint NOT NULL CONSTRAINT df_properties_rooms DEFAULT (0),
    floor smallint NULL,
    price decimal(14, 2) NOT NULL,
    description nvarchar(1000) NULL,
    photo_path nvarchar(260) NULL,
    created_at datetime2(0) NOT NULL CONSTRAINT df_properties_created_at DEFAULT (SYSDATETIME()),
    CONSTRAINT pk_properties PRIMARY KEY (property_id),
    CONSTRAINT fk_properties_employee_id FOREIGN KEY (employee_id) REFERENCES dbo.employees (employee_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_properties_status_id FOREIGN KEY (status_id) REFERENCES dbo.property_statuses (status_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_properties_owner_client_id FOREIGN KEY (owner_client_id) REFERENCES dbo.clients (client_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_properties_property_type_id FOREIGN KEY (property_type_id) REFERENCES dbo.property_types (property_type_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_properties_district_id FOREIGN KEY (district_id) REFERENCES dbo.districts (district_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_properties_deal_type_id FOREIGN KEY (deal_type_id) REFERENCES dbo.deal_types (deal_type_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT ck_properties_area CHECK (area > 0),
    CONSTRAINT ck_properties_price CHECK (price > 0),
    CONSTRAINT ck_properties_address CHECK (LEN(LTRIM(RTRIM(address))) > 0)
);

CREATE TABLE dbo.client_requests (
    request_id int IDENTITY(1,1) NOT NULL,
    client_id int NOT NULL,
    property_type_id int NOT NULL,
    district_id int NULL,
    deal_type_id int NOT NULL,
    price_min decimal(14, 2) NOT NULL CONSTRAINT df_client_requests_price_min DEFAULT (0),
    price_max decimal(14, 2) NOT NULL,
    area_min decimal(7, 2) NULL,
    rooms tinyint NULL,
    is_active bit NOT NULL CONSTRAINT df_client_requests_is_active DEFAULT (1),
    created_at datetime2(0) NOT NULL CONSTRAINT df_client_requests_created_at DEFAULT (SYSDATETIME()),
    CONSTRAINT pk_client_requests PRIMARY KEY (request_id),
    CONSTRAINT fk_client_requests_client_id FOREIGN KEY (client_id) REFERENCES dbo.clients (client_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_client_requests_property_type_id FOREIGN KEY (property_type_id) REFERENCES dbo.property_types (property_type_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_client_requests_district_id FOREIGN KEY (district_id) REFERENCES dbo.districts (district_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_client_requests_deal_type_id FOREIGN KEY (deal_type_id) REFERENCES dbo.deal_types (deal_type_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT ck_client_requests_price CHECK (price_min >= 0 AND price_max > 0 AND price_min <= price_max),
    CONSTRAINT ck_client_requests_area CHECK (area_min IS NULL OR area_min > 0)
);

CREATE TABLE dbo.viewings (
    viewing_id int IDENTITY(1,1) NOT NULL,
    viewing_status_id int NOT NULL CONSTRAINT df_viewings_viewing_status_id DEFAULT (1),
    property_id int NOT NULL,
    request_id int NOT NULL,
    viewing_datetime datetime2(0) NOT NULL,
    result_comment nvarchar(500) NULL,
    CONSTRAINT pk_viewings PRIMARY KEY (viewing_id),
    CONSTRAINT fk_viewings_property_id FOREIGN KEY (property_id) REFERENCES dbo.properties (property_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_viewings_request_id FOREIGN KEY (request_id) REFERENCES dbo.client_requests (request_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_viewings_viewing_status_id FOREIGN KEY (viewing_status_id) REFERENCES dbo.viewing_statuses (viewing_status_id) ON DELETE NO ACTION ON UPDATE NO ACTION
);

CREATE TABLE dbo.deals (
    deal_id int IDENTITY(1,1) NOT NULL,
    viewing_id int NOT NULL,
    employee_id int NOT NULL,
    deal_date date NOT NULL CONSTRAINT df_deals_deal_date DEFAULT (CONVERT(date, SYSDATETIME())),
    contract_number nvarchar(30) NOT NULL,
    contract_file nvarchar(260) NOT NULL,
    amount decimal(14, 2) NOT NULL,
    commission_percent decimal(5, 2) NOT NULL,
    commission_amount decimal(14, 2) NOT NULL,
    CONSTRAINT pk_deals PRIMARY KEY (deal_id),
    CONSTRAINT uq_deals_contract_number UNIQUE (contract_number),
    CONSTRAINT uq_deals_viewing_id UNIQUE (viewing_id),
    CONSTRAINT fk_deals_employee_id FOREIGN KEY (employee_id) REFERENCES dbo.employees (employee_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_deals_viewing_id FOREIGN KEY (viewing_id) REFERENCES dbo.viewings (viewing_id) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT ck_deals_amount CHECK (amount > 0),
    CONSTRAINT ck_deals_percent CHECK (commission_percent > 0 AND commission_percent <= 100),
    CONSTRAINT ck_deals_commission CHECK (commission_amount = CONVERT(decimal(14,2), ROUND(amount * commission_percent / 100.0, 2))),
    CONSTRAINT ck_deals_file CHECK (LEN(LTRIM(RTRIM(contract_file))) > 0),
    CONSTRAINT ck_deals_number CHECK (LEN(LTRIM(RTRIM(contract_number))) > 0)
);

-- Дополнительные индексы для поиска и переходов по связям.
CREATE INDEX ix_properties_search ON dbo.properties (status_id, deal_type_id, property_type_id, price) INCLUDE (district_id, area, rooms, created_at);
CREATE INDEX ix_viewings_property_time ON dbo.viewings (property_id, viewing_datetime);
CREATE INDEX ix_viewings_request ON dbo.viewings (request_id);
CREATE INDEX ix_requests_client ON dbo.client_requests (client_id);
CREATE INDEX ix_deals_period ON dbo.deals (deal_date, employee_id);
-- 3. Заполнение таблиц учебными данными.
-- Сначала справочники, затем сотрудники, клиенты, объекты, заявки, просмотры и сделки.

INSERT INTO dbo.roles (role_name) VALUES
    (N'Администратор'),
    (N'Руководитель'),
    (N'Риелтор');

INSERT INTO dbo.property_types (type_name) VALUES
    (N'Квартира'),
    (N'Комната'),
    (N'Дом'),
    (N'Коммерческое помещение');

INSERT INTO dbo.districts (district_name) VALUES
    (N'Адмиралтейский'),
    (N'Василеостровский'),
    (N'Выборгский'),
    (N'Калининский'),
    (N'Московский'),
    (N'Невский'),
    (N'Петроградский'),
    (N'Приморский'),
    (N'Фрунзенский'),
    (N'Центральный');

INSERT INTO dbo.property_statuses (status_name) VALUES
    (N'Активный'),
    (N'Зарезервирован'),
    (N'Продан'),
    (N'Сдан в аренду');

INSERT INTO dbo.deal_types (deal_type_name, commission_percent) VALUES
    (N'Продажа', 3.00),
    (N'Аренда', 50.00);

INSERT INTO dbo.viewing_statuses (status_name) VALUES
    (N'Запланирован'),
    (N'Проведён'),
    (N'Отменён');

INSERT INTO dbo.employees (last_name, first_name, middle_name, phone, login, password_hash, role_id, is_active) VALUES
    (N'Королёв', N'Андрей', N'Сергеевич', N'+79215550101', N'admin', N'600000.CCqzXPYD++tezcc0aXi3dQ==.GlkZEFa3zH1FvGU0rNgrYnttcXjFxDDu5nqK7eOfomk=', 1, 1),
    (N'Белова', N'Марина', N'Игоревна', N'+79215550102', N'manager', N'600000.2+BAuxGvoV2PNm5ocf/+7A==.bzXV40mpdX221ZLMIfHy2H6wQbD4rHi/hZJXFdqDl88=', 2, 1),
    (N'Иванова', N'Ольга', N'Петровна', N'+79215550103', N'ivanova', N'600000.9MyNhAtxUEDGVIoV8fmGvw==.XwDfTevYa8yaD41a7Lcuo6b84kStD5/NlqN14Ts/m04=', 3, 1),
    (N'Петров', N'Дмитрий', N'Алексеевич', N'+79215550104', N'petrov', N'600000.6dTTlV8mBo4PfMacMj77fQ==.PJdiLjBbi8TC/Bn9Sq3xt6huQotHOYnLbp50NZ8Tuqo=', 3, 1),
    (N'Смирнова', N'Екатерина', N'Владимировна', N'+79215550105', N'smirnova', N'600000.aK6xTizuK7DOii/18WtJKw==.Yaf7m5E47aj6sRf2ZGujYl4pXwr0tltUEIUCr1f3DEA=', 3, 1);

INSERT INTO dbo.clients (last_name, first_name, middle_name, phone, email, passport_series, passport_number, registration_date) VALUES
    (N'Александров', N'Роман', N'Викторович', N'+79112000000', NULL, N'4010', N'514002', N'2026-05-01'),
    (N'Борисова', N'Дарья', N'Андреевна', N'+79112013579', N'borisova11@mail.ru', N'4011', N'483452', N'2026-05-01'),
    (N'Васильев', N'Владимир', N'Викторович', N'+79112027158', N'vasilev12@mail.ru', N'4012', N'139317', N'2026-05-01'),
    (N'Григорьева', N'Светлана', N'Андреевна', N'+79112040737', NULL, N'4013', N'352353', N'2026-05-01'),
    (N'Денисов', N'Николай', N'Андреевич', N'+79112054316', N'denisov14@mail.ru', N'4014', N'967017', N'2026-05-01'),
    (N'Егорова', N'Наталья', N'Михайловна', N'+79112067895', N'egorova15@mail.ru', N'4015', N'757911', N'2026-05-01'),
    (N'Жуков', N'Евгений', N'Юрьевич', N'+79112081474', NULL, N'4016', N'515949', N'2026-05-01'),
    (N'Зайцева', N'Анна', N'Юрьевна', N'+79112095053', N'zayceva17@mail.ru', N'4017', N'239643', N'2026-05-01'),
    (N'Ильин', N'Игорь', N'Юрьевич', N'+79112108632', N'ilin18@mail.ru', N'4018', N'223514', N'2026-05-01'),
    (N'Козлова', N'Дарья', N'Михайловна', N'+79112122211', NULL, N'4019', N'289505', N'2026-05-01'),
    (N'Лебедев', N'Евгений', N'Михайлович', N'+79112135790', N'lebedev20@mail.ru', N'4020', N'296997', N'2026-05-01'),
    (N'Морозова', N'Дарья', N'Михайловна', N'+79112149369', N'morozova21@mail.ru', N'4021', N'165839', N'2026-05-01'),
    (N'Никитин', N'Евгений', N'Викторович', N'+79112162948', NULL, N'4022', N'620528', N'2026-05-01'),
    (N'Орлова', N'Юлия', N'Павловна', N'+79112176527', N'orlova23@mail.ru', N'4023', N'714006', N'2026-05-01'),
    (N'Павлов', N'Сергей', N'Викторович', N'+79112190106', N'pavlov24@mail.ru', N'4024', N'932967', N'2026-05-01'),
    (N'Романова', N'Елена', N'Юрьевна', N'+79112203685', NULL, N'4025', N'414834', N'2026-05-01'),
    (N'Соколов', N'Роман', N'Михайлович', N'+79112217264', N'sokolov26@mail.ru', N'4026', N'570636', N'2026-05-01'),
    (N'Тихонова', N'Елена', N'Юрьевна', N'+79112230843', N'tihonova27@mail.ru', N'4027', N'538433', N'2026-05-01'),
    (N'Фёдоров', N'Игорь', N'Павлович', N'+79112244422', NULL, N'4028', N'542182', N'2026-05-01'),
    (N'Яковлева', N'Дарья', N'Юрьевна', N'+79112258001', N'yakovleva29@mail.ru', N'4029', N'927425', N'2026-05-01'),
    (N'Кузнецов', N'Кирилл', N'Олегович', N'+79112271580', N'kuznecov30@mail.ru', N'4030', N'723241', N'2026-05-01'),
    (N'Новикова', N'Ирина', N'Андреевна', N'+79112285159', NULL, N'4031', N'980770', N'2026-05-01'),
    (N'Волков', N'Артём', N'Михайлович', N'+79112298738', N'volkov32@mail.ru', N'4032', N'796414', N'2026-05-01'),
    (N'Соловьёва', N'Вера', N'Михайловна', N'+79112312317', N'soloveva33@mail.ru', N'4033', N'424646', N'2026-05-01');

INSERT INTO dbo.properties (owner_client_id, property_type_id, district_id, deal_type_id, employee_id, status_id, address, area, rooms, floor, price, description, photo_path, created_at) VALUES
    (1, 1, 1, 1, 3, 3, N'Санкт-Петербург, ул. Садовая, д. 18, кв. 112', 49.9, 2, 10, 9530000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_01.jpg', N'2026-07-24T13:00:00'),
    (8, 1, 10, 1, 4, 2, N'Санкт-Петербург, Невский пр., д. 74, кв. 213', 72.2, 3, 12, 14800000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_02.jpg', N'2026-09-08T12:00:00'),
    (15, 1, 5, 1, 5, 1, N'Санкт-Петербург, ул. Типанова, д. 40, кв. 215', 30.5, 1, 12, 5890000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_03.jpg', N'2026-08-05T18:00:00'),
    (22, 1, 5, 2, 3, 4, N'Санкт-Петербург, Московский пр., д. 104, кв. 205', 38.0, 1, 13, 38000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_04.jpg', N'2026-06-16T16:00:00'),
    (5, 1, 8, 1, 4, 3, N'Санкт-Петербург, ул. Савушкина, д. 41, кв. 275', 29.7, 1, 4, 5230000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_05.jpg', N'2026-08-20T10:00:00'),
    (12, 1, 3, 1, 5, 1, N'Санкт-Петербург, пр. Просвещения, д. 127, кв. 239', 35.1, 1, 16, 7580000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_06.jpg', N'2026-09-10T11:00:00'),
    (19, 1, 4, 1, 3, 2, N'Санкт-Петербург, Гражданский пр., д. 95, кв. 76', 53.5, 2, 1, 12630000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_07.jpg', N'2026-08-21T11:00:00'),
    (2, 1, 2, 2, 4, 4, N'Санкт-Петербург, ул. Кораблестроителей, д. 131, кв. 169', 49.7, 2, 8, 60000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_08.jpg', N'2026-07-26T13:00:00'),
    (9, 1, 7, 1, 5, 1, N'Санкт-Петербург, Каменноостровский пр., д. 69, кв. 100', 71.0, 3, 12, 12280000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_09.jpg', N'2026-09-26T15:00:00'),
    (16, 1, 5, 1, 3, 3, N'Санкт-Петербург, ул. Ленсовета, д. 91, кв. 44', 65.2, 3, 4, 16230000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_10.jpg', N'2026-08-21T13:00:00'),
    (23, 1, 3, 1, 4, 1, N'Санкт-Петербург, пр. Энгельса, д. 43, кв. 88', 84.3, 4, 5, 18550000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_11.jpg', N'2026-06-05T17:00:00'),
    (6, 1, 10, 2, 5, 2, N'Санкт-Петербург, Лиговский пр., д. 36, кв. 11', 54.3, 2, 1, 59000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_12.jpg', N'2026-06-17T12:00:00'),
    (13, 1, 6, 1, 3, 3, N'Санкт-Петербург, ул. Дыбенко, д. 69, кв. 279', 71.3, 3, 14, 14260000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_13.jpg', N'2026-07-02T15:00:00'),
    (20, 1, 9, 1, 4, 1, N'Санкт-Петербург, пр. Ветеранов, д. 137, кв. 262', 85.8, 4, 1, 20080000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_14.jpg', N'2026-09-25T12:00:00'),
    (3, 1, 2, 1, 5, 1, N'Санкт-Петербург, Большой пр. В.О., д. 126, кв. 55', 30.7, 1, 2, 6480000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_15.jpg', N'2026-07-07T14:00:00'),
    (10, 1, 1, 2, 3, 4, N'Санкт-Петербург, ул. Садовая, д. 86, кв. 259', 34.1, 1, 7, 35000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_16.jpg', N'2026-08-15T18:00:00'),
    (17, 1, 10, 1, 4, 1, N'Санкт-Петербург, Невский пр., д. 38, кв. 214', 89.4, 4, 4, 18150000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_17.jpg', N'2026-09-15T15:00:00'),
    (24, 1, 5, 1, 5, 1, N'Санкт-Петербург, ул. Типанова, д. 39, кв. 130', 31.4, 1, 5, 5930000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_18.jpg', N'2026-09-08T11:00:00'),
    (7, 1, 5, 1, 3, 1, N'Санкт-Петербург, Московский пр., д. 53, кв. 183', 64.6, 3, 11, 15180000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_19.jpg', N'2026-06-24T15:00:00'),
    (14, 1, 8, 2, 4, 1, N'Санкт-Петербург, ул. Савушкина, д. 78, кв. 263', 34.5, 1, 3, 39000.0, N'Квартира в хорошем состоянии, окна во двор, рядом метро.', N'photos/property_20.jpg', N'2026-06-26T13:00:00'),
    (21, 2, 3, 1, 5, 3, N'Санкт-Петербург, пр. Просвещения, д. 106, кв. 77', 12.3, 1, 16, 2180000.0, N'Комната в коммунальной квартире, косметический ремонт.', N'photos/property_21.jpg', N'2026-08-03T14:00:00'),
    (4, 2, 4, 1, 3, 1, N'Санкт-Петербург, Гражданский пр., д. 59, кв. 35', 19.2, 1, 9, 3190000.0, N'Комната в коммунальной квартире, косметический ремонт.', N'photos/property_22.jpg', N'2026-06-15T10:00:00'),
    (11, 2, 2, 1, 4, 1, N'Санкт-Петербург, ул. Кораблестроителей, д. 70, кв. 26', 19.3, 1, 6, 3180000.0, N'Комната в коммунальной квартире, косметический ремонт.', N'photos/property_23.jpg', N'2026-07-10T14:00:00'),
    (18, 3, 3, 2, 5, 1, N'пос. Песочный, ул. Ленинградская, д. 33', 114.3, 4, NULL, 106000.0, N'Загородный дом с участком, все коммуникации.', N'photos/property_24.jpg', N'2026-06-01T10:00:00'),
    (1, 3, 3, 1, 3, 1, N'пос. Песочный, ул. Ленинградская, д. 34', 99.6, 5, NULL, 9060000.0, N'Загородный дом с участком, все коммуникации.', N'photos/property_25.jpg', N'2026-09-17T14:00:00'),
    (8, 3, 3, 1, 4, 1, N'пос. Песочный, ул. Ленинградская, д. 35', 153.6, 3, NULL, 9680000.0, N'Загородный дом с участком, все коммуникации.', N'photos/property_26.jpg', N'2026-06-03T14:00:00'),
    (15, 4, 10, 1, 5, 1, N'Санкт-Петербург, Лиговский пр., д. 14, пом. 15-Н', 146.2, 0, 6, 25880000.0, N'Помещение свободного назначения, отдельный вход.', N'photos/property_27.jpg', N'2026-07-09T17:00:00'),
    (22, 4, 6, 2, 3, 1, N'Санкт-Петербург, ул. Дыбенко, д. 82, пом. 7-Н', 107.9, 0, 12, 268000.0, N'Помещение свободного назначения, отдельный вход.', N'photos/property_28.jpg', N'2026-07-01T15:00:00'),
    (5, 4, 9, 1, 4, 1, N'Санкт-Петербург, пр. Ветеранов, д. 39, пом. 13-Н', 146.0, 0, 2, 22050000.0, N'Помещение свободного назначения, отдельный вход.', N'photos/property_29.jpg', N'2026-09-01T14:00:00'),
    (12, 4, 2, 1, 5, 1, N'Санкт-Петербург, Большой пр. В.О., д. 102, пом. 11-Н', 197.6, 0, 16, 42680000.0, N'Помещение свободного назначения, отдельный вход.', N'photos/property_30.jpg', N'2026-07-10T12:00:00');

INSERT INTO dbo.client_requests (client_id, property_type_id, district_id, deal_type_id, price_min, price_max, area_min, rooms, is_active, created_at) VALUES
    (11, 1, 1, 1, 7624000.00, 11436000.00, 39.92, 2, 0, N'2026-07-29T12:00:00'),
    (12, 1, 8, 1, 4184000.00, 6276000.00, 23.76, 1, 0, N'2026-08-13T15:00:00'),
    (13, 1, NULL, 1, 12984000.00, 19476000.00, 52.16, 3, 0, N'2026-08-21T11:00:00'),
    (14, 1, 6, 1, 11408000.00, 17112000.00, 57.04, 3, 0, N'2026-09-03T13:00:00'),
    (15, 1, 5, 2, 30400.00, 45600.00, 30.40, 1, 0, N'2026-09-05T17:00:00'),
    (16, 1, NULL, 2, 48000.00, 72000.00, 39.76, 2, 0, N'2026-09-15T12:00:00'),
    (17, 2, 3, 1, 1744000.00, 2616000.00, 9.84, 1, 0, N'2026-09-19T14:00:00'),
    (18, 1, 1, 2, 28000.00, 42000.00, 27.28, 1, 0, N'2026-09-24T16:00:00'),
    (19, 1, NULL, 1, 11840000.00, 17760000.00, 57.76, 3, 1, N'2026-09-26T12:00:00'),
    (20, 1, 4, 1, 10104000.00, 15156000.00, 42.80, 2, 1, N'2026-09-27T18:00:00'),
    (21, 1, 10, 2, 47200.00, 70800.00, 43.44, 2, 1, N'2026-09-28T11:00:00'),
    (19, 1, NULL, 1, 4712000.00, 7068000.00, 24.40, 1, 1, N'2026-09-29T12:00:00'),
    (22, 1, 3, 1, 6064000.00, 9096000.00, 28.08, 1, 1, N'2026-09-30T10:00:00'),
    (23, 1, 7, 1, 9824000.00, 14736000.00, 56.80, 3, 1, N'2026-10-05T12:00:00'),
    (24, 1, NULL, 1, 16064000.00, 24096000.00, 68.64, 4, 1, N'2026-10-06T15:00:00'),
    (20, 1, 10, 1, 14520000.00, 21780000.00, 71.52, 4, 1, N'2026-10-07T11:00:00');

INSERT INTO dbo.viewings (viewing_status_id, property_id, request_id, viewing_datetime, result_comment) VALUES
    (2, 1, 1, N'2026-08-05T12:00:00', N'Клиент согласен'),
    (2, 5, 2, N'2026-08-20T15:00:00', N'Клиент согласен'),
    (2, 10, 3, N'2026-08-28T11:00:00', N'Клиент согласен'),
    (2, 13, 4, N'2026-09-10T13:00:00', N'Клиент согласен'),
    (2, 4, 5, N'2026-09-12T17:00:00', N'Клиент согласен'),
    (2, 8, 6, N'2026-09-22T12:00:00', N'Клиент согласен'),
    (2, 21, 7, N'2026-09-26T14:00:00', N'Клиент согласен'),
    (2, 16, 8, N'2026-10-01T16:00:00', N'Клиент согласен'),
    (2, 2, 9, N'2026-10-03T12:00:00', N'Клиент согласен'),
    (2, 7, 10, N'2026-10-04T18:00:00', N'Клиент согласен'),
    (2, 12, 11, N'2026-10-05T11:00:00', N'Клиент согласен'),
    (2, 3, 12, N'2026-10-06T12:00:00', N'Клиент отказался: высокая цена'),
    (3, 6, 13, N'2026-10-07T10:00:00', N'Отменён клиентом'),
    (1, 9, 14, N'2026-10-12T12:00:00', NULL),
    (1, 14, 15, N'2026-10-13T15:00:00', NULL),
    (1, 17, 16, N'2026-10-14T11:00:00', NULL);

INSERT INTO dbo.deals (viewing_id, employee_id, deal_date, contract_number, contract_file, amount, commission_percent, commission_amount) VALUES
    (1, 3, N'2026-08-12', N'ДКП-2026-001', N'contracts/ДКП-2026-001.pdf', 9530000.0, 3.0, 285900.0),
    (2, 4, N'2026-08-27', N'ДКП-2026-002', N'contracts/ДКП-2026-002.pdf', 5073000.0, 3.0, 152190.0),
    (3, 3, N'2026-09-03', N'ДКП-2026-003', N'contracts/ДКП-2026-003.pdf', 16230000.0, 3.0, 486900.0),
    (4, 3, N'2026-09-15', N'ДКП-2026-004', N'contracts/ДКП-2026-004.pdf', 13832000.0, 3.0, 414960.0),
    (5, 3, N'2026-09-18', N'ДА-2026-005', N'contracts/ДА-2026-005.pdf', 38000.0, 50.0, 19000.0),
    (6, 4, N'2026-09-29', N'ДА-2026-006', N'contracts/ДА-2026-006.pdf', 58000.0, 50.0, 29000.0),
    (7, 5, N'2026-10-02', N'ДКП-2026-007', N'contracts/ДКП-2026-007.pdf', 2180000.0, 3.0, 65400.0),
    (8, 3, N'2026-10-06', N'ДА-2026-008', N'contracts/ДА-2026-008.pdf', 34000.0, 50.0, 17000.0);


    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- 4. Проверка количества записей по таблицам и общего итога.
;WITH row_counts AS (
    SELECT N'roles' AS table_name, 3 AS expected_rows, COUNT(*) AS actual_rows FROM dbo.roles
    UNION ALL
    SELECT N'property_types' AS table_name, 4 AS expected_rows, COUNT(*) AS actual_rows FROM dbo.property_types
    UNION ALL
    SELECT N'districts' AS table_name, 10 AS expected_rows, COUNT(*) AS actual_rows FROM dbo.districts
    UNION ALL
    SELECT N'property_statuses' AS table_name, 4 AS expected_rows, COUNT(*) AS actual_rows FROM dbo.property_statuses
    UNION ALL
    SELECT N'deal_types' AS table_name, 2 AS expected_rows, COUNT(*) AS actual_rows FROM dbo.deal_types
    UNION ALL
    SELECT N'viewing_statuses' AS table_name, 3 AS expected_rows, COUNT(*) AS actual_rows FROM dbo.viewing_statuses
    UNION ALL
    SELECT N'employees' AS table_name, 5 AS expected_rows, COUNT(*) AS actual_rows FROM dbo.employees
    UNION ALL
    SELECT N'clients' AS table_name, 24 AS expected_rows, COUNT(*) AS actual_rows FROM dbo.clients
    UNION ALL
    SELECT N'properties' AS table_name, 30 AS expected_rows, COUNT(*) AS actual_rows FROM dbo.properties
    UNION ALL
    SELECT N'client_requests' AS table_name, 16 AS expected_rows, COUNT(*) AS actual_rows FROM dbo.client_requests
    UNION ALL
    SELECT N'viewings' AS table_name, 16 AS expected_rows, COUNT(*) AS actual_rows FROM dbo.viewings
    UNION ALL
    SELECT N'deals' AS table_name, 8 AS expected_rows, COUNT(*) AS actual_rows FROM dbo.deals
)
SELECT table_name AS [Таблица], expected_rows AS [Должно быть],
       actual_rows AS [Фактически],
       CASE WHEN actual_rows = expected_rows THEN N'ОК' ELSE N'НЕ СОВПАДАЕТ' END AS [Проверка]
FROM row_counts
UNION ALL
SELECT N'ИТОГО', SUM(expected_rows), SUM(actual_rows),
       CASE WHEN SUM(CASE WHEN actual_rows = expected_rows THEN 0 ELSE 1 END) = 0
            THEN N'ОК' ELSE N'НЕ СОВПАДАЕТ' END
FROM row_counts;

-- Сверка структуры с последней ER-диаграммой: ожидается 12 / 72 / 16.
SELECT
    (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) AS [Таблиц — 12],
    (SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
     WHERE t.is_ms_shipped = 0) AS [Столбцов — 72],
    (SELECT COUNT(*) FROM sys.foreign_keys WHERE is_ms_shipped = 0) AS [Внешних ключей — 16];
